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

using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Brokerages.OKX.WebSocket;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - WebSocket integration for real-time data
    /// </summary>
    public partial class OKXBrokerage
    {
        private OKXWebSocketClient _publicWebSocket;
        private OKXWebSocketClient _privateWebSocket;
        private readonly Dictionary<Symbol, bool> _subscribedSymbols = new Dictionary<Symbol, bool>();

        /// <summary>
        /// Initializes WebSocket connections (public and private channels)
        /// </summary>
        private void InitializeWebSockets()
        {
            try
            {
                // Create public WebSocket for market data
                var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
                _publicWebSocket = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);
                _publicWebSocket.TickerReceived += OnTickerReceived;
                _publicWebSocket.TradeReceived += OnTradeReceived;
                _publicWebSocket.OrderBookReceived += OnOrderBookReceived;

                // Create private WebSocket for orders/account updates
                var privateUrl = OKXEnvironment.GetWebSocketPrivateUrl();
                _privateWebSocket = new OKXWebSocketClient(
                    privateUrl,
                    _apiKey,
                    _apiSecret,
                    _passphrase,
                    isPrivateChannel: true);
                _privateWebSocket.OrderReceived += OnOrderReceived;
                _privateWebSocket.AccountReceived += OnAccountReceived;
                _privateWebSocket.PositionReceived += OnPositionReceived;

                Log.Trace("OKXBrokerage.InitializeWebSockets(): WebSocket clients created");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.InitializeWebSockets(): Error creating WebSocket clients: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to market data for a symbol
        /// </summary>
        /// <param name="symbol">LEAN Symbol to subscribe to</param>
        public void SubscribeToSymbol(Symbol symbol)
        {
            if (_subscribedSymbols.ContainsKey(symbol))
            {
                return;
            }

            try
            {
                // Connect if not connected
                if (_publicWebSocket != null && !_publicWebSocket.IsConnected)
                {
                    _publicWebSocket.Connect();
                }

                // Get OKX instrument ID
                var instId = _symbolMapper.GetBrokerageSymbol(symbol);

                // Subscribe to tickers (quotes)
                _publicWebSocket.Subscribe("tickers", instId, symbol);

                // Subscribe to trades (tick data)
                _publicWebSocket.Subscribe("trades", instId, symbol);

                // Subscribe to orderbook (Level 2 data)
                _publicWebSocket.Subscribe("books5", instId, symbol);

                _subscribedSymbols[symbol] = true;

                Log.Trace($"OKXBrokerage.SubscribeToSymbol(): Subscribed to {symbol} (instId: {instId})");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.SubscribeToSymbol(): Error subscribing to {symbol}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unsubscribes from market data for a symbol
        /// </summary>
        /// <param name="symbol">LEAN Symbol to unsubscribe from</param>
        public void UnsubscribeFromSymbol(Symbol symbol)
        {
            if (!_subscribedSymbols.ContainsKey(symbol))
            {
                return;
            }

            try
            {
                var instId = _symbolMapper.GetBrokerageSymbol(symbol);

                _publicWebSocket?.Unsubscribe("tickers", instId);
                _publicWebSocket?.Unsubscribe("trades", instId);
                _publicWebSocket?.Unsubscribe("books5", instId);

                _subscribedSymbols.Remove(symbol);

                Log.Trace($"OKXBrokerage.UnsubscribeFromSymbol(): Unsubscribed from {symbol}");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.UnsubscribeFromSymbol(): Error unsubscribing from {symbol}: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribes to private channels (orders, account, positions)
        /// </summary>
        private void SubscribeToPrivateChannels()
        {
            try
            {
                // Connect private WebSocket
                if (_privateWebSocket != null)
                {
                    _privateWebSocket.Connect();

                    // Subscribe to orders channel (all instruments)
                    _privateWebSocket.Subscribe("orders", null, null);

                    // Subscribe to account channel
                    _privateWebSocket.Subscribe("account", null, null);

                    // Subscribe to positions channel (all instruments)
                    _privateWebSocket.Subscribe("positions", null, null);

                    Log.Trace("OKXBrokerage.SubscribeToPrivateChannels(): Subscribed to private channels");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.SubscribeToPrivateChannels(): Error subscribing: {ex.Message}");
            }
        }

        #region WebSocket Event Handlers

        private void OnTickerReceived(object sender, OKXWebSocketTicker ticker)
        {
            try
            {
                // Convert to LEAN Symbol
                var symbol = _symbolMapper.GetLeanSymbol(ticker.InstrumentId, GetSecurityType(ticker.InstrumentId), Market.OKX);

                // Parse values
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
                    AskSize = askSize
                };

                // TODO: Emit tick via IDataAggregator or directly
                // For now, just log
                // OnMessage(quote);
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.OnTickerReceived(): Error processing ticker: {ex.Message}");
            }
        }

        private void OnTradeReceived(object sender, OKXWebSocketTrade trade)
        {
            try
            {
                // Convert to LEAN Symbol
                var symbol = _symbolMapper.GetLeanSymbol(trade.InstrumentId, GetSecurityType(trade.InstrumentId), Market.OKX);

                // Parse values
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

                // TODO: Emit tick via IDataAggregator or directly
                // For now, just log
                // OnMessage(tick);
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.OnTradeReceived(): Error processing trade: {ex.Message}");
            }
        }

        private void OnOrderBookReceived(object sender, OKXWebSocketOrderBook orderBook)
        {
            try
            {
                // Convert to LEAN Symbol
                var symbol = _symbolMapper.GetLeanSymbol(orderBook.InstrumentId, GetSecurityType(orderBook.InstrumentId), Market.OKX);

                var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(orderBook.Timestamp)).UtcDateTime;

                // TODO: OrderBook data structure not available in LEAN base
                // Need to implement custom orderbook handling or wait for LEAN support
                // For now, we can convert top level to quote ticks

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
                        // Create quote tick from top of book
                        var quote = new Tick
                        {
                            Symbol = symbol,
                            Time = time,
                            TickType = TickType.Quote,
                            BidPrice = bidPrice,
                            AskPrice = askPrice,
                            BidSize = bidSize,
                            AskSize = askSize
                        };

                        // TODO: Emit tick
                        // OnMessage(quote);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.OnOrderBookReceived(): Error processing orderbook: {ex.Message}");
            }
        }

        private void OnOrderReceived(object sender, OKXWebSocketOrder order)
        {
            try
            {
                // Parse order ID
                if (!int.TryParse(order.ClientOrderId ?? "0", out var orderId))
                {
                    // If we can't parse client order ID, log and return
                    Log.Trace($"OKXBrokerage.OnOrderReceived(): Cannot parse client order ID: {order.ClientOrderId}");
                    return;
                }

                // Map OKX state to LEAN OrderStatus
                var status = MapOrderState(order.State);

                // Parse filled quantity and average price
                decimal.TryParse(order.FilledSize ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var filledQty);
                decimal.TryParse(order.AveragePrice ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var avgPrice);
                decimal.TryParse(order.Fee ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var feeAmount);

                var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(order.UpdateTime)).UtcDateTime;

                // Create order event
                var orderEvent = new OrderEvent
                {
                    OrderId = orderId,
                    Status = status,
                    FillPrice = avgPrice,
                    FillQuantity = order.Side == "buy" ? filledQty : -filledQty,
                    OrderFee = new OrderFee(new CashAmount(Math.Abs(feeAmount), order.FeeCurrency ?? "USDT")),
                    UtcTime = time,
                    Message = $"OKX order {order.OrderId}: {order.State}"
                };

                // Emit order event
                OnOrderEvent(orderEvent);

                Log.Trace($"OKXBrokerage.OnOrderReceived(): Order update - {order.OrderId} ({order.ClientOrderId}): {order.State}");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.OnOrderReceived(): Error processing order update: {ex.Message}");
            }
        }

        private void OnAccountReceived(object sender, OKXWebSocketAccount account)
        {
            try
            {
                Log.Trace($"OKXBrokerage.OnAccountReceived(): Account update - TotalEq: {account.TotalEquity}");

                // TODO: Update account holdings/cash in algorithm
                // This would typically update the algorithm's portfolio cash amounts
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.OnAccountReceived(): Error processing account update: {ex.Message}");
            }
        }

        private void OnPositionReceived(object sender, OKXWebSocketPosition position)
        {
            try
            {
                Log.Trace($"OKXBrokerage.OnPositionReceived(): Position update - {position.InstrumentId}: {position.Position}");

                // TODO: Update position holdings in algorithm
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.OnPositionReceived(): Error processing position update: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Maps OKX order state to LEAN OrderStatus
        /// </summary>
        private OrderStatus MapOrderState(string okxState)
        {
            switch (okxState?.ToLowerInvariant())
            {
                case "live":
                    return OrderStatus.Submitted;
                case "partially_filled":
                    return OrderStatus.PartiallyFilled;
                case "filled":
                    return OrderStatus.Filled;
                case "canceled":
                    return OrderStatus.Canceled;
                case "canceling":
                    return OrderStatus.CancelPending;
                default:
                    return OrderStatus.None;
            }
        }

        /// <summary>
        /// Gets security type from instrument ID
        /// </summary>
        private SecurityType GetSecurityType(string instId)
        {
            if (instId.Contains("-SWAP") || instId.Contains("-FUTURES"))
            {
                return SecurityType.CryptoFuture;
            }
            return SecurityType.Crypto;
        }

        #endregion

        /// <summary>
        /// Disposes WebSocket connections
        /// </summary>
        private void DisposeWebSockets()
        {
            _publicWebSocket?.Dispose();
            _publicWebSocket = null;

            _privateWebSocket?.Dispose();
            _privateWebSocket = null;
        }
    }
}
