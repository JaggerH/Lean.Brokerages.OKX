/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - WebSocket Message Handling (OKX v5 API)
    /// Implements message routing and processing for OKX v5 WebSocket API
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        private DateTime _lastMessageTime = DateTime.UtcNow;

        // ========================================
        // MESSAGE ENTRY POINT
        // ========================================

        /// <summary>
        /// Handles incoming WebSocket messages (called by base class).
        /// Routes messages through BrokerageConcurrentMessageHandler to synchronize with REST API calls.
        /// This prevents race conditions where WebSocket receives order updates before REST response sets BrokerId.
        /// Following Binance pattern.
        /// </summary>
        protected override void OnMessage(object sender, WebSocketMessage e)
        {
            _messageHandler.HandleNewMessage(e);
        }

        /// <summary>
        /// Processes private WebSocket messages after passing through the message handler.
        /// Called by BrokerageConcurrentMessageHandler - may be immediate or deferred if REST operation is in progress.
        /// </summary>
        private void ProcessPrivateMessage(WebSocketMessage e)
        {
            try
            {
                if (e.Data is not WebSocketClientWrapper.TextMessage textMessage)
                {
                    return;
                }

                var rawMessage = textMessage.Message;

                // Skip empty or whitespace messages
                if (string.IsNullOrWhiteSpace(rawMessage) || rawMessage.Length < 2)
                {
                    return;
                }

                // Handle plain text messages (ping/pong)
                if (rawMessage == "pong" || rawMessage == "ping")
                {
                    _lastMessageTime = DateTime.UtcNow;
                    return;
                }

                // Process JSON message
                ProcessMessage(rawMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.ProcessPrivateMessage(): Error: {ex}");
            }
        }

        /// <summary>
        /// Processes a WebSocket message (OKX v5 API format)
        /// </summary>
        /// <param name="rawMessage">Raw JSON message</param>
        protected virtual void ProcessMessage(string rawMessage)
        {
            try
            {
                // Update last message time
                _lastMessageTime = DateTime.UtcNow;

                var jObject = JObject.Parse(rawMessage);

                // OKX v5 API uses two message formats:
                // 1. Event messages (subscribe, login, error): { "event": "...", "code": "...", "msg": "..." }
                // 2. Data push messages: { "arg": { "channel": "..." }, "data": [...] }

                // Check for event messages
                if (jObject["event"] != null)
                {
                    HandleEventMessage(jObject);
                    return;
                }

                // Check for data push messages
                if (jObject["arg"] != null && jObject["data"] != null)
                {
                    HandleDataMessage(jObject);
                    return;
                }

                // Unknown message format
                Log.Trace($"{GetType().Name}.ProcessMessage(): Unknown message format: {rawMessage.Substring(0, Math.Min(200, rawMessage.Length))}");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.ProcessMessage(): Error parsing message: {ex.Message}");
            }
        }

        // ========================================
        // EVENT MESSAGE HANDLERS
        // ========================================

        /// <summary>
        /// Handles event messages (subscribe, login, error)
        /// </summary>
        protected virtual void HandleEventMessage(JObject jObject)
        {
            var response = jObject.ToObject<OKXWebSocketResponse>();

            // Log all event messages for debugging
            Log.Trace($"{GetType().Name}.HandleEventMessage(): Received event '{response.Event}', Code: '{response.Code ?? "null"}', Message: '{response.Message ?? "null"}'");

            switch (response.Event)
            {
                case "login":
                    HandleLoginEvent(response);
                    break;

                case "subscribe":
                    HandleSubscribeEvent(response);
                    break;

                case "unsubscribe":
                    HandleUnsubscribeEvent(response);
                    break;

                case "error":
                    HandleErrorEvent(response);
                    break;

                default:
                    Log.Trace($"{GetType().Name}: Unknown event type: {response.Event}");
                    break;
            }
        }

        /// <summary>
        /// Handles login event
        /// </summary>
        protected virtual void HandleLoginEvent(OKXWebSocketResponse response)
        {
            // OKX login success: code = "0" or no code field
            // OKX login failure: code != "0"
            if (string.IsNullOrEmpty(response.Code) || response.Code == "0")
            {
                Log.Trace($"{GetType().Name}: Login successful (connId: {response.ConnectionId})");

                // Subscribe to private channels after successful login
                try
                {
                    SubscribePrivateChannels();
                }
                catch (Exception ex)
                {
                    Log.Error($"{GetType().Name}: Error subscribing to private channels after login: {ex}");
                }
            }
            else
            {
                Log.Error($"{GetType().Name}: Login failed - Code: {response.Code}, Message: {response.Message}");
                OnMessage(new BrokerageMessageEvent(
                    BrokerageMessageType.Error,
                    response.Code,
                    $"WebSocket login failed: {response.Message}"));
            }
        }

        /// <summary>
        /// Handles subscribe event
        /// OKX subscribe success: no code field or code = "0"
        /// OKX subscribe failure: code field present and != "0"
        /// </summary>
        protected virtual void HandleSubscribeEvent(OKXWebSocketResponse response)
        {
            var channel = response.Arg?.Channel;
            var instType = response.Arg?.InstrumentType;
            var instId = response.Arg?.InstrumentId;

            // Build subscription key for logging
            var key = channel;
            if (!string.IsNullOrEmpty(instType))
            {
                key += $" (instType: {instType})";
            }
            if (!string.IsNullOrEmpty(instId))
            {
                key += $" (instId: {instId})";
            }

            // Check if subscription was successful
            // Success: no code field or code = "0"
            // Failure: code field present and != "0"
            if (string.IsNullOrEmpty(response.Code) || response.Code == "0")
            {
                Log.Trace($"{GetType().Name}: Subscription confirmed - {key}, connId: {response.ConnectionId}");
            }
            else
            {
                Log.Error($"{GetType().Name}: Subscription failed - {key}, Code: {response.Code}, Message: {response.Message}");
                OnMessage(new BrokerageMessageEvent(
                    BrokerageMessageType.Error,
                    response.Code,
                    $"WebSocket subscription failed for {key}: {response.Message}"));
            }
        }

        /// <summary>
        /// Handles unsubscribe event
        /// </summary>
        protected virtual void HandleUnsubscribeEvent(OKXWebSocketResponse response)
        {
            var channel = response.Arg?.Channel;
            var instId = response.Arg?.InstrumentId;
            var key = string.IsNullOrEmpty(instId) ? channel : $"{channel}:{instId}";
            Log.Trace($"{GetType().Name}: Unsubscription confirmed - {key}");
        }

        /// <summary>
        /// Handles error event
        /// </summary>
        protected virtual void HandleErrorEvent(OKXWebSocketResponse response)
        {
            Log.Error($"{GetType().Name}: WebSocket error - Code: {response.Code}, Message: {response.Message}");
            OnMessage(new BrokerageMessageEvent(
                BrokerageMessageType.Warning,
                response.Code,
                $"WebSocket error: {response.Message}"));
        }

        // ========================================
        // DATA MESSAGE HANDLERS
        // ========================================

        /// <summary>
        /// Handles data push messages
        /// </summary>
        protected virtual void HandleDataMessage(JObject jObject)
        {
            var arg = jObject["arg"].ToObject<OKXWebSocketChannel>();
            var channel = arg.Channel;

            // Route to appropriate handler based on channel
            switch (channel)
            {
                case "orders":
                    HandleOrdersChannel(jObject);
                    break;

                case "account":
                    HandleAccountChannel(jObject);
                    break;

                case "positions":
                    HandlePositionsChannel(jObject);
                    break;

                case "tickers":
                    HandleTickersChannel(jObject);
                    break;

                case "trades":
                    HandleTradesChannel(jObject);
                    break;

                case "books":
                    HandleOrderBookChannel(jObject);
                    break;

                default:
                    Log.Trace($"{GetType().Name}: Unknown channel: {channel}");
                    break;
            }
        }

        // ========================================
        // PRIVATE CHANNEL HANDLERS (Orders, Account, Positions)
        // ========================================

        /// <summary>
        /// Handles orders channel data push
        /// Channel: orders
        /// Data: Array of order updates
        /// </summary>
        protected virtual void HandleOrdersChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<OKXWebSocketDataMessage<OKXWebSocketOrder>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                foreach (var order in message.Data)
                {
                    HandleOrderUpdate(order);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleOrdersChannel(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles individual order update from WebSocket orders channel.
        /// Per OKX docs:
        /// - When tradeId has value: this is a fill event, deduplicate by tradeId
        /// - When tradeId is empty and state=filled: this is a market order close event
        /// - Duplicate messages may be pushed (with different uTime), only process first
        /// </summary>
        protected virtual void HandleOrderUpdate(OKXWebSocketOrder order)
        {
            try
            {
                // Log raw OKX order data for debugging
                Log.Trace($"{GetType().Name}.HandleOrderUpdate(): Raw data - {JsonConvert.SerializeObject(order)}");

                // Deduplicate by tradeId - per OKX docs, for the same tradeId, only process first message
                if (!string.IsNullOrEmpty(order.TradeId))
                {
                    if (_processedTradeIds.TryGetValue(order.TradeId, out bool _))
                    {
                        Log.Trace($"{GetType().Name}.HandleOrderUpdate(): Duplicate tradeId ignored: {order.TradeId}");
                        return;
                    }
                    _processedTradeIds.Set(order.TradeId, true, TimeSpan.FromMinutes(5));
                }

                // Parse client order ID to get LEAN order ID
                if (!int.TryParse(order.ClientOrderId ?? "0", out var orderId))
                {
                    Log.Trace($"{GetType().Name}.HandleOrderUpdate(): Cannot parse client order ID: {order.ClientOrderId}");
                    return;
                }

                // Find LEAN order
                if (!CachedOrderIDs.TryGetValue(orderId, out var leanOrder))
                {
                    Log.Trace($"{GetType().Name}.HandleOrderUpdate(): Order not found in cache: {orderId}");
                    return;
                }

                // Register brokerage ID mapping if first time seeing this order
                if (!string.IsNullOrEmpty(order.OrderId) && !_ordersByBrokerId.ContainsKey(order.OrderId))
                {
                    _ordersByBrokerId.TryAdd(order.OrderId, leanOrder);

                    // Add to BrokerId list if not already present
                    if (!leanOrder.BrokerId.Contains(order.OrderId))
                    {
                        leanOrder.BrokerId.Add(order.OrderId);
                    }
                }

                // Convert to OrderEvent using converter
                var orderEvent = order.ToOrderEvent(leanOrder);
                if (orderEvent != null)
                {
                    OnOrderEvent(orderEvent);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleOrderUpdate(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles account channel data push
        /// Channel: account
        /// Data: Array of account balance updates
        /// </summary>
        protected virtual void HandleAccountChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<OKXWebSocketDataMessage<OKXWebSocketAccount>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                foreach (var account in message.Data)
                {
                    HandleAccountUpdate(account);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleAccountChannel(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles individual account update
        /// </summary>
        protected virtual void HandleAccountUpdate(OKXWebSocketAccount account)
        {
            try
            {
                Log.Trace($"{GetType().Name}.HandleAccountUpdate(): Total Equity: {account.TotalEquity}");

                // Trigger account changed event (totalEq is in USD)
                OnAccountChanged(new AccountEvent(
                    "USD",
                    decimal.TryParse(account.TotalEquity ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var totalEq) ? totalEq : 0m));
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleAccountUpdate(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles positions channel data push
        /// Channel: positions
        /// Data: Array of position updates
        /// </summary>
        protected virtual void HandlePositionsChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<OKXWebSocketDataMessage<OKXWebSocketPosition>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                foreach (var position in message.Data)
                {
                    HandlePositionUpdate(position);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandlePositionsChannel(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles individual position update
        /// </summary>
        protected virtual void HandlePositionUpdate(OKXWebSocketPosition position)
        {
            try
            {
                Log.Trace($"{GetType().Name}.HandlePositionUpdate(): {position.InstrumentId} Position: {position.Position}, UPL: {position.UnrealizedPnL}");

                // Position updates can trigger AccountChanged event if needed
                // Implementation depends on specific requirements
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandlePositionUpdate(): Error: {ex.Message}");
            }
        }

        // ========================================
        // PUBLIC CHANNEL HANDLERS (Market Data)
        // ========================================

        /// <summary>
        /// Handles tickers channel data push
        /// Channel: tickers
        /// Data: Array of ticker updates (usually 1 element)
        /// </summary>
        protected virtual void HandleTickersChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<OKXWebSocketDataMessage<OKXWebSocketTicker>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                // Get instId from arg (not from data elements)
                var instId = message.Arg?.InstrumentId;

                foreach (var ticker in message.Data)
                {
                    // Fill in the InstrumentId from arg
                    ticker.InstrumentId = instId;
                    HandleTickerUpdate(ticker);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleTickersChannel(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles individual ticker update
        /// </summary>
        protected virtual void HandleTickerUpdate(OKXWebSocketTicker ticker)
        {
            try
            {
                // Get LEAN symbol
                var securityType = GetSecurityType(ticker.InstrumentId);
                var symbol = _symbolMapper.GetLeanSymbol(ticker.InstrumentId, securityType, Market.OKX);

                // Parse prices and sizes
                if (!decimal.TryParse(ticker.Last, NumberStyles.Any, CultureInfo.InvariantCulture, out var lastPrice))
                    return;
                if (!decimal.TryParse(ticker.BidPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var bidPrice))
                    return;
                if (!decimal.TryParse(ticker.AskPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var askPrice))
                    return;
                if (!decimal.TryParse(ticker.BidSize, NumberStyles.Any, CultureInfo.InvariantCulture, out var bidSize))
                    return;
                if (!decimal.TryParse(ticker.AskSize, NumberStyles.Any, CultureInfo.InvariantCulture, out var askSize))
                    return;

                var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(ticker.Timestamp)).UtcDateTime;

                // Create quote tick
                var quote = new Tick
                {
                    Symbol = symbol,
                    Time = time,
                    TickType = TickType.Quote,
                    BidPrice = bidPrice,
                    AskPrice = askPrice,
                    BidSize = bidSize,
                    AskSize = askSize,
                    Value = lastPrice
                };

                // Send to aggregator
                _aggregator.Update(quote);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleTickerUpdate(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles trades channel data push
        /// Channel: trades
        /// Data: Array of trade updates
        /// </summary>
        protected virtual void HandleTradesChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<OKXWebSocketDataMessage<OKXWebSocketTrade>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                // Get instId from arg (not from data elements)
                var instId = message.Arg?.InstrumentId;

                foreach (var trade in message.Data)
                {
                    // Fill in the InstrumentId from arg
                    trade.InstrumentId = instId;
                    HandleTradeUpdate(trade);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleTradesChannel(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles individual trade update
        /// </summary>
        protected virtual void HandleTradeUpdate(OKXWebSocketTrade trade)
        {
            try
            {
                // Get LEAN symbol
                var securityType = GetSecurityType(trade.InstrumentId);
                var symbol = _symbolMapper.GetLeanSymbol(trade.InstrumentId, securityType, Market.OKX);

                // Parse price and size
                if (!decimal.TryParse(trade.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    return;
                if (!decimal.TryParse(trade.Size, NumberStyles.Any, CultureInfo.InvariantCulture, out var size))
                    return;

                var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(trade.Timestamp)).UtcDateTime;

                // Create trade tick
                var tick = new Tick
                {
                    Symbol = symbol,
                    Time = time,
                    TickType = TickType.Trade,
                    Value = price,
                    Quantity = size
                };

                // Send to aggregator
                _aggregator.Update(tick);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleTradeUpdate(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles order book channel data push
        /// Channel: books (400-level depth with incremental updates)
        /// Data: Array of order book snapshots/updates
        /// </summary>
        protected virtual void HandleOrderBookChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<OKXWebSocketDataMessage<OKXWebSocketOrderBook>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                // Get instId from arg (not from data elements)
                var instId = message.Arg?.InstrumentId;

                // Process each orderbook update in the message
                foreach (var orderBook in message.Data)
                {
                    // Fill in the InstrumentId from arg
                    orderBook.InstrumentId = instId;
                    HandleOrderBookUpdate(orderBook, message.Action);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleOrderBookChannel(): Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles individual order book update with action-based processing
        /// Action: "snapshot" = full 400-level snapshot, "update" = incremental changes
        /// Implements sequence validation using seqId/prevSeqId from OKX docs
        /// </summary>
        protected virtual void HandleOrderBookUpdate(OKXWebSocketOrderBook orderBook, string action)
        {
            try
            {
                // Get LEAN symbol
                var securityType = GetSecurityType(orderBook.InstrumentId);
                var symbol = _symbolMapper.GetLeanSymbol(orderBook.InstrumentId, securityType, Market.OKX);

                var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(orderBook.Timestamp)).UtcDateTime;

                // Try to get existing orderbook context
                if (!_orderBookContexts.TryGetValue(symbol, out var context))
                {
                    // No context found - this shouldn't happen as context is created during subscription
                    // But handle gracefully by just emitting quote tick from top of book
                    EmitQuoteTickFromOrderBook(symbol, orderBook, time);
                    return;
                }

                // Process based on action type
                if (action == "snapshot")
                {
                    // Snapshot: full 400-level orderbook (initial message or after reconnection)
                    Log.Trace($"{GetType().Name}.HandleOrderBookUpdate(): Snapshot received for {symbol}, seqId={orderBook.SequenceId}, prevSeqId={orderBook.PreviousSequenceId}");

                    lock (context.Lock)
                    {
                        // Unsubscribe from events during rebuild
                        context.OrderBook.BestBidAskUpdated -= OnBestBidAskUpdated;

                        // Apply full snapshot
                        context.OrderBook.ApplyFullSnapshot(orderBook.Bids, orderBook.Asks);

                        // Validate checksum if provided
                        if (orderBook.Checksum.HasValue)
                        {
                            if (!OKXChecksumValidator.ValidateChecksum(context.OrderBook, orderBook.Checksum.Value, out var calculatedChecksum))
                            {
                                Log.Error($"{GetType().Name}.HandleOrderBookUpdate(): Checksum validation FAILED for {symbol} snapshot! Expected: {orderBook.Checksum.Value}, Calculated: {calculatedChecksum}");
                                // Continue processing - checksum mismatch is logged but doesn't stop data flow
                            }
                            else
                            {
                                Log.Trace($"{GetType().Name}.HandleOrderBookUpdate(): Checksum validation passed for {symbol} snapshot: {orderBook.Checksum.Value}");
                            }
                        }

                        // Update sequence tracking
                        context.LastUpdateId = orderBook.SequenceId ?? 0;
                        context.LastUpdateTime = time;

                        // Re-subscribe to events
                        context.OrderBook.BestBidAskUpdated += OnBestBidAskUpdated;
                    }
                }
                else if (action == "update")
                {
                    // Incremental update: only changed levels, size=0 means delete
                    // Validate sequence continuity
                    if (orderBook.SequenceId.HasValue && orderBook.PreviousSequenceId.HasValue)
                    {
                        var expectedPrevSeqId = context.LastUpdateId;
                        var actualPrevSeqId = orderBook.PreviousSequenceId.Value;
                        var currentSeqId = orderBook.SequenceId.Value;

                        // Special case: prevSeqId == seqId means no update (keepalive)
                        if (actualPrevSeqId == currentSeqId)
                        {
                            // Keepalive message, no actual update
                            return;
                        }

                        // Special case: sequence reset during maintenance (prevSeqId > seqId)
                        if (actualPrevSeqId > currentSeqId)
                        {
                            Log.Trace($"{GetType().Name}.HandleOrderBookUpdate(): Sequence reset detected for {symbol}, prevSeqId={actualPrevSeqId} > seqId={currentSeqId}. Continuing with new sequence.");
                            // Continue processing with new sequence
                        }
                        // Normal case: validate sequence continuity
                        else if (actualPrevSeqId != expectedPrevSeqId)
                        {
                            Log.Error($"{GetType().Name}.HandleOrderBookUpdate(): Sequence gap detected for {symbol}! Expected prevSeqId={expectedPrevSeqId}, got prevSeqId={actualPrevSeqId}, seqId={currentSeqId}. Requesting resync...");
                            // TODO: In production, should request snapshot resync here
                            // For now, continue processing
                        }

                        // Apply incremental update
                        lock (context.Lock)
                        {
                            context.OrderBook.ApplyIncrementalUpdate(orderBook.Bids, orderBook.Asks);

                            // Validate checksum if provided
                            if (orderBook.Checksum.HasValue)
                            {
                                if (!OKXChecksumValidator.ValidateChecksum(context.OrderBook, orderBook.Checksum.Value, out var calculatedChecksum))
                                {
                                    Log.Error($"{GetType().Name}.HandleOrderBookUpdate(): Checksum validation FAILED for {symbol} update! Expected: {orderBook.Checksum.Value}, Calculated: {calculatedChecksum}, seqId: {currentSeqId}");
                                    // Continue processing - checksum mismatch is logged but doesn't stop data flow
                                }
                            }

                            context.LastUpdateId = currentSeqId;
                            context.LastUpdateTime = time;
                        }
                    }
                    else
                    {
                        // No sequence IDs present (shouldn't happen for books channel, but handle gracefully)
                        lock (context.Lock)
                        {
                            context.OrderBook.ApplyIncrementalUpdate(orderBook.Bids, orderBook.Asks);

                            // Validate checksum if provided
                            if (orderBook.Checksum.HasValue)
                            {
                                if (!OKXChecksumValidator.ValidateChecksum(context.OrderBook, orderBook.Checksum.Value, out var calculatedChecksum))
                                {
                                    Log.Error($"{GetType().Name}.HandleOrderBookUpdate(): Checksum validation FAILED for {symbol} update! Expected: {orderBook.Checksum.Value}, Calculated: {calculatedChecksum}");
                                }
                            }

                            context.LastUpdateTime = time;
                        }
                    }
                }
                else
                {
                    Log.Trace($"{GetType().Name}.HandleOrderBookUpdate(): Unknown action '{action}' for {symbol}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleOrderBookUpdate(): Error processing orderbook for instId='{orderBook?.InstrumentId ?? "NULL"}', action='{action}': {ex.Message}");
                Log.Error($"{GetType().Name}.HandleOrderBookUpdate(): Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Helper method to emit a quote tick from order book data
        /// Used when orderbook context is not available
        /// </summary>
        private void EmitQuoteTickFromOrderBook(Symbol symbol, OKXWebSocketOrderBook orderBook, DateTime time)
        {
            try
            {
                if (orderBook.Bids != null && orderBook.Bids.Count > 0 &&
                    orderBook.Asks != null && orderBook.Asks.Count > 0)
                {
                    var topBid = orderBook.Bids[0];
                    var topAsk = orderBook.Asks[0];

                    if (topBid.Count >= 2 && topAsk.Count >= 2 &&
                        decimal.TryParse(topBid[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var bidPrice) &&
                        decimal.TryParse(topBid[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var bidSize) &&
                        decimal.TryParse(topAsk[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var askPrice) &&
                        decimal.TryParse(topAsk[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var askSize))
                    {
                        var quote = new Tick
                        {
                            Symbol = symbol,
                            Time = time,
                            TickType = TickType.Quote,
                            BidPrice = bidPrice,
                            AskPrice = askPrice,
                            BidSize = bidSize,
                            AskSize = askSize,
                            Value = (bidPrice + askPrice) / 2
                        };

                        lock (_aggregator)
                        {
                            _aggregator.Update(quote);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.EmitQuoteTickFromOrderBook(): Error: {ex.Message}");
            }
        }
    }
}
