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
            var response = jObject.ToObject<WebSocketResponse>();

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
        protected virtual void HandleLoginEvent(WebSocketResponse response)
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
        protected virtual void HandleSubscribeEvent(WebSocketResponse response)
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
        protected virtual void HandleUnsubscribeEvent(WebSocketResponse response)
        {
            var channel = response.Arg?.Channel;
            var instId = response.Arg?.InstrumentId;
            var key = string.IsNullOrEmpty(instId) ? channel : $"{channel}:{instId}";
        }

        /// <summary>
        /// Handles error event
        /// </summary>
        protected virtual void HandleErrorEvent(WebSocketResponse response)
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
            var arg = jObject["arg"].ToObject<WebSocketChannel>();
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
                var message = jObject.ToObject<WebSocketDataMessage<WebSocketOrder>>();

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
        protected virtual void HandleOrderUpdate(WebSocketOrder order)
        {
            try
            {
                // Log raw OKX order data for debugging
                Log.Trace($"{GetType().Name}.HandleOrderUpdate(): Raw data - {JsonConvert.SerializeObject(order)}");

                // Deduplicate by tradeId - per OKX docs, for the same tradeId, only process first message
                if (!string.IsNullOrEmpty(order.TradeId))
                {
                    var now = DateTime.UtcNow;
                    if (_processedTradeIds.TryGetValue(order.TradeId, out var expiry) && now < expiry)
                    {
                        Log.Trace($"{GetType().Name}.HandleOrderUpdate(): Duplicate tradeId ignored: {order.TradeId}");
                        return;
                    }
                    _processedTradeIds[order.TradeId] = now.AddMinutes(5);
                    if (_processedTradeIds.Count > 500)
                    {
                        foreach (var kv in _processedTradeIds)
                        {
                            if (now >= kv.Value) _processedTradeIds.TryRemove(kv.Key, out _);
                        }
                    }
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

                // Add broker order ID to LEAN order if not already present
                if (!string.IsNullOrEmpty(order.OrderId) && !leanOrder.BrokerId.Contains(order.OrderId))
                {
                    leanOrder.BrokerId.Add(order.OrderId);
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
                var message = jObject.ToObject<WebSocketDataMessage<WebSocketAccount>>();

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
        protected virtual void HandleAccountUpdate(WebSocketAccount account)
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
                var message = jObject.ToObject<WebSocketDataMessage<WebSocketPosition>>();

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
        protected virtual void HandlePositionUpdate(WebSocketPosition position)
        {
            try
            {
                Log.Trace($"{GetType().Name}.HandlePositionUpdate(): {position.InstrumentId} Position: {position.Quantity}, UPL: {position.UnrealizedPnL}");

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
                var message = jObject.ToObject<WebSocketDataMessage<WebSocketTicker>>();

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
        protected virtual void HandleTickerUpdate(WebSocketTicker ticker)
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
                var message = jObject.ToObject<WebSocketDataMessage<WebSocketTrade>>();

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
        protected virtual void HandleTradeUpdate(WebSocketTrade trade)
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
        /// Lightweight handler - writes to synchronizer which handles all processing
        /// </summary>
        protected virtual void HandleOrderBookChannel(JObject jObject)
        {
            try
            {
                var message = jObject.ToObject<WebSocketDataMessage<WebSocketOrderBook>>();

                if (message.Data == null || message.Data.Count == 0)
                {
                    return;
                }

                // Get instId and action from message
                var instId = message.Arg?.InstrumentId;
                var action = message.Action;

                // Write each orderbook update to the synchronizer
                foreach (var orderBook in message.Data)
                {
                    // Fill in the InstrumentId and Action from parent message
                    orderBook.InstrumentId = instId;
                    orderBook.Action = action;

                    // Write to synchronizer (automatic routing by symbol)
                    _orderBookSync?.Writer.TryWrite(orderBook);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.HandleOrderBookChannel(): Error: {ex.Message}");
            }
        }
    }
}
