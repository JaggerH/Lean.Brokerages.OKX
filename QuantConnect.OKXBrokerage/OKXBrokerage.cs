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
using System.Collections.Generic;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage implementation for OKX v5 API
    /// Supports spot and derivatives trading on OKX exchange
    /// </summary>
    public partial class OKXBrokerage : Brokerage
    {
        private readonly OKXRestApiClient _restApiClient;
        private readonly ISymbolMapper _symbolMapper;
        private readonly IAlgorithm _algorithm;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _passphrase;

        /// <summary>
        /// Creates a new instance of OKXBrokerage
        /// </summary>
        /// <param name="apiKey">OKX API key</param>
        /// <param name="apiSecret">OKX API secret</param>
        /// <param name="passphrase">OKX API passphrase</param>
        /// <param name="algorithm">The algorithm instance</param>
        public OKXBrokerage(
            string apiKey,
            string apiSecret,
            string passphrase,
            IAlgorithm algorithm)
            : base("OKX")
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
            _algorithm = algorithm;

            _symbolMapper = new OKXSymbolMapper(Market.OKX);
            _restApiClient = new OKXRestApiClient(apiKey, apiSecret, passphrase);

            // Initialize WebSocket connections
            InitializeWebSockets();

            Log.Trace($"OKXBrokerage(): Initialized for {OKXEnvironment.GetEnvironmentName()} environment");
        }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _restApiClient != null;

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders</returns>
        public override List<Order> GetOpenOrders()
        {
            // TODO: Implement in Phase 4
            return new List<Order>();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            // TODO: Implement in Phase 4
            return new List<Holding>();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            // TODO: Implement in Phase 4
            return new List<CashAmount>();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            try
            {
                // Convert LEAN symbol to OKX instrument ID
                var instId = _symbolMapper.GetBrokerageSymbol(order.Symbol);

                // Determine order side (buy/sell) from quantity
                var side = order.Quantity > 0 ? "buy" : "sell";

                // Determine trade mode based on security type
                // SPOT: cash, FUTURES/SWAP: cross (we use cross margin by default)
                var tdMode = order.Symbol.SecurityType == SecurityType.Crypto ? "cash" : "cross";

                // Convert LEAN order type to OKX order type
                string ordType;
                string price = null;

                switch (order.Type)
                {
                    case OrderType.Market:
                        ordType = "market";
                        break;

                    case OrderType.Limit:
                        ordType = "limit";
                        price = ((LimitOrder)order).LimitPrice.ToStringInvariant();
                        break;

                    case OrderType.StopMarket:
                        // OKX doesn't have a direct "stop market" - use conditional order instead
                        // For now, we'll reject this as unsupported
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UNSUPPORTED_ORDER_TYPE",
                            $"StopMarket orders are not supported by OKX v5 API. Use StopLimit instead."));
                        return false;

                    case OrderType.StopLimit:
                        // OKX stop-limit orders require different endpoint (algo orders)
                        // For simplicity in Phase 5, we'll reject these
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UNSUPPORTED_ORDER_TYPE",
                            $"StopLimit orders require algo order endpoint - not implemented in Phase 5"));
                        return false;

                    default:
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UNSUPPORTED_ORDER_TYPE",
                            $"Order type {order.Type} is not supported"));
                        return false;
                }

                // Create place order request
                var request = new Messages.OKXPlaceOrderRequest
                {
                    InstrumentId = instId,
                    TradeMode = tdMode,
                    Side = side,
                    OrderType = ordType,
                    Size = Math.Abs(order.Quantity).ToStringInvariant(),
                    Price = price,
                    ClientOrderId = order.Id.ToStringInvariant(),
                    Tag = "LEAN"
                };

                // Place order via REST API
                var response = _restApiClient.PlaceOrder(request);

                if (response == null)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_PLACEMENT_FAILED",
                        $"Failed to place order {order.Id}"));
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Invalid,
                        Message = "Order placement failed"
                    });
                    return false;
                }

                // Store broker ID
                order.BrokerId.Add(response.OrderId);

                // Send order submitted event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = OrderStatus.Submitted
                });

                Log.Trace($"OKXBrokerage.PlaceOrder(): Order {order.Id} placed successfully, OKX OrderId: {response.OrderId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.PlaceOrder(): Exception placing order {order.Id}: {ex.Message}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ORDER_PLACEMENT_ERROR",
                    $"Exception placing order: {ex.Message}"));
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = OrderStatus.Invalid,
                    Message = ex.Message
                });
                return false;
            }
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            try
            {
                // OKX v5 supports amending orders (price and quantity modification)
                // Check if order has broker ID
                if (order.BrokerId.Count == 0)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UPDATE_ORDER_FAILED",
                        $"Cannot update order {order.Id}: No broker ID found"));
                    return false;
                }

                var ordId = order.BrokerId[0];
                var instId = _symbolMapper.GetBrokerageSymbol(order.Symbol);

                // Determine what to update based on order type
                string newPrice = null;
                string newSize = Math.Abs(order.Quantity).ToStringInvariant();

                if (order.Type == OrderType.Limit)
                {
                    newPrice = ((LimitOrder)order).LimitPrice.ToStringInvariant();
                }

                // Create amend request
                var request = new Messages.OKXAmendOrderRequest
                {
                    InstrumentId = instId,
                    OrderId = ordId,
                    NewSize = newSize,
                    NewPrice = newPrice
                };

                // Amend order via REST API
                var response = _restApiClient.AmendOrder(request);

                if (response == null)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_UPDATE_FAILED",
                        $"Failed to update order {order.Id}"));
                    return false;
                }

                // Send order update event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = order.Status,
                    Message = "Order updated"
                });

                Log.Trace($"OKXBrokerage.UpdateOrder(): Order {order.Id} (OKX: {ordId}) updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.UpdateOrder(): Exception updating order {order.Id}: {ex.Message}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ORDER_UPDATE_ERROR",
                    $"Exception updating order: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            try
            {
                // Check if order has broker ID
                if (order.BrokerId.Count == 0)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "CANCEL_ORDER_FAILED",
                        $"Cannot cancel order {order.Id}: No broker ID found"));
                    return false;
                }

                var ordId = order.BrokerId[0];
                var instId = _symbolMapper.GetBrokerageSymbol(order.Symbol);

                // Create cancel request
                var request = new Messages.OKXCancelOrderRequest
                {
                    InstrumentId = instId,
                    OrderId = ordId
                };

                // Cancel order via REST API
                var response = _restApiClient.CancelOrder(request);

                if (response == null)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_CANCEL_FAILED",
                        $"Failed to cancel order {order.Id}"));
                    return false;
                }

                // Send order canceled event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = OrderStatus.Canceled,
                    Message = "Order canceled"
                });

                Log.Trace($"OKXBrokerage.CancelOrder(): Order {order.Id} (OKX: {ordId}) canceled successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.CancelOrder(): Exception canceling order {order.Id}: {ex.Message}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ORDER_CANCEL_ERROR",
                    $"Exception canceling order: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            // Connection is established through REST API client initialization
            Log.Trace("OKXBrokerage.Connect(): Connected to OKX REST API");

            // Subscribe to private channels for real-time order/account updates
            SubscribeToPrivateChannels();
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            // Disconnect WebSocket connections
            _publicWebSocket?.Disconnect();
            _privateWebSocket?.Disconnect();

            Log.Trace("OKXBrokerage.Disconnect(): Disconnected from OKX REST API and WebSockets");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            // Dispose WebSocket connections
            DisposeWebSockets();

            Log.Trace("OKXBrokerage.Dispose(): Disposing OKXBrokerage");
        }
    }
}
