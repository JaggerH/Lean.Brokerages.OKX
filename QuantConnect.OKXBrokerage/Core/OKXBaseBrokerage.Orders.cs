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
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - Order Management
    /// Implements PlaceOrder, UpdateOrder, CancelOrder using REST API
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        // ========================================
        // ORDER MANAGEMENT METHODS
        // ========================================

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// Follows Binance pattern: always returns true, errors reported via events
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True (errors are reported via OrderEvent and BrokerageMessageEvent)</returns>
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
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UNSUPPORTED_ORDER_TYPE",
                            $"StopMarket orders are not supported by OKX v5 API. Use StopLimit instead."));
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                        {
                            Status = OrderStatus.Invalid,
                            Message = "StopMarket orders not supported"
                        });
                        return true;  // Binance pattern: always return true

                    case OrderType.StopLimit:
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UNSUPPORTED_ORDER_TYPE",
                            $"StopLimit orders require algo order endpoint - not implemented"));
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                        {
                            Status = OrderStatus.Invalid,
                            Message = "StopLimit orders not implemented"
                        });
                        return true;  // Binance pattern: always return true

                    default:
                        var unsupportedMsg = $"Order type {order.Type} is not supported";
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UNSUPPORTED_ORDER_TYPE", unsupportedMsg));
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                        {
                            Status = OrderStatus.Invalid,
                            Message = unsupportedMsg
                        });
                        return true;  // Binance pattern: always return true
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
                var result = RestApiClient.PlaceOrder(request);

                // SUCCESS PATH
                if (result.IsSuccess)
                {
                    // Store broker ID
                    order.BrokerId.Add(result.Data.OrderId);

                    // Send order submitted event
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Submitted
                    });

                    Log.Trace($"OKXBaseBrokerage.PlaceOrder(): Order {order.Id} placed successfully, OKX OrderId: {result.Data.OrderId}");
                    return true;
                }

                // ERROR PATH - Include full error details
                var errorMessage = result.GetErrorMessage();
                var fullMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} " +
                                 $"quantity: {order.Quantity} symbol: {order.Symbol} - {errorMessage}";

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = OrderStatus.Invalid,
                    Message = errorMessage  // Specific error reason visible to user
                });

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_PLACEMENT_FAILED", fullMessage));

                Log.Error($"OKXBaseBrokerage.PlaceOrder(): {fullMessage}");
                return true;  // Binance pattern: always return true
            }
            catch (Exception ex)
            {
                var message = $"Exception placing order {order.Id}: {ex.Message}";
                Log.Error($"OKXBaseBrokerage.PlaceOrder(): {message}");

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ORDER_PLACEMENT_ERROR", message));

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    Status = OrderStatus.Invalid,
                    Message = ex.Message
                });

                return true;  // Binance pattern: return true even on exception
            }
        }

        /// <summary>
        /// Updates the order with the same id
        /// Follows Binance pattern: always returns true, errors reported via events
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True (errors are reported via OrderEvent and BrokerageMessageEvent)</returns>
        public override bool UpdateOrder(Order order)
        {
            try
            {
                // OKX v5 supports amending orders (price and quantity modification)
                // Check if order has broker ID
                if (order.BrokerId.Count == 0)
                {
                    var errorMsg = "Cannot update order: No broker ID found";
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UPDATE_ORDER_FAILED", errorMsg));
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Invalid,
                        Message = errorMsg
                    });
                    return true;  // Binance pattern: always return true
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
                var result = RestApiClient.AmendOrder(request);

                // SUCCESS PATH
                if (result.IsSuccess)
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = order.Status,
                        Message = "Order updated"
                    });
                    Log.Trace($"OKXBaseBrokerage.UpdateOrder(): Order {order.Id} (OKX: {ordId}) updated successfully");
                    return true;
                }

                // ERROR PATH - Include full error details
                var errorMessage = result.GetErrorMessage();
                var fullMessage = $"Update failed for Order {order.Id} (OKX: {ordId}) - {errorMessage}";

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_UPDATE_FAILED", fullMessage));
                Log.Error($"OKXBaseBrokerage.UpdateOrder(): {fullMessage}");

                return true;  // Binance pattern: always return true
            }
            catch (Exception ex)
            {
                var message = $"Exception updating order {order.Id}: {ex.Message}";
                Log.Error($"OKXBaseBrokerage.UpdateOrder(): {message}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ORDER_UPDATE_ERROR", message));
                return true;  // Binance pattern: return true even on exception
            }
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// Follows Binance pattern: always returns true, errors reported via events
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True (errors are reported via OrderEvent and BrokerageMessageEvent)</returns>
        public override bool CancelOrder(Order order)
        {
            try
            {
                // Check if order has broker ID
                if (order.BrokerId.Count == 0)
                {
                    var errorMsg = "Cannot cancel order: No broker ID found";
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "CANCEL_ORDER_FAILED", errorMsg));
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Invalid,
                        Message = errorMsg
                    });
                    return true;  // Binance pattern: always return true
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
                var result = RestApiClient.CancelOrder(request);

                // SUCCESS PATH
                if (result.IsSuccess)
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Canceled,
                        Message = "Order canceled"
                    });
                    Log.Trace($"OKXBaseBrokerage.CancelOrder(): Order {order.Id} (OKX: {ordId}) canceled successfully");
                    return true;
                }

                // ERROR PATH - Include full error details
                var errorMessage = result.GetErrorMessage();
                var fullMessage = $"Cancel failed for Order {order.Id} (OKX: {ordId}) - {errorMessage}";

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_CANCEL_FAILED", fullMessage));
                Log.Error($"OKXBaseBrokerage.CancelOrder(): {fullMessage}");

                return true;  // Binance pattern: always return true
            }
            catch (Exception ex)
            {
                var message = $"Exception canceling order {order.Id}: {ex.Message}";
                Log.Error($"OKXBaseBrokerage.CancelOrder(): {message}");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ORDER_CANCEL_ERROR", message));
                return true;  // Binance pattern: return true even on exception
            }
        }
    }
}
