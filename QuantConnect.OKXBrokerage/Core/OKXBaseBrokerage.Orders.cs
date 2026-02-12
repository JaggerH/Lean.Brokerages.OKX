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
using System.Linq;
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
        /// <summary>
        /// Default price buffer for converting market buy orders to FOK limit orders (0.3%)
        /// </summary>
        private const decimal DefaultMarketBuyPriceBuffer = 0.003m;

        // ========================================
        // ORDER ROUTING
        // ========================================

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order.
        /// Uses WithLockedStream to synchronize REST API call with WebSocket message processing.
        /// This prevents race conditions where WebSocket receives fills before REST response sets BrokerId.
        /// Follows Binance pattern: always returns true, errors reported via events.
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True (errors are reported via OrderEvent and BrokerageMessageEvent)</returns>
        public override bool PlaceOrder(Order order)
        {
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                try
                {
                    // Rate limit order operations
                    OrderRateLimiter.WaitToProceed();

                    // Convert LEAN symbol to OKX instrument ID
                    var instId = _symbolMapper.GetBrokerageSymbol(order.Symbol);

                    // Determine trade mode based on account mode and security type
                    var tdMode = GetTradeMode(order.Symbol.SecurityType);

                    // Route to appropriate handler based on order type
                    if (IsSpotMarketBuy(order))
                    {
                        submitted = PlaceSpotMarketBuyAsFokLimit(order, instId, tdMode);
                    }
                    else
                    {
                        submitted = PlaceStandardOrder(order, instId, tdMode);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Exception placing order {order.Id}: {ex.Message}";
                    Log.Error($"OKXBaseBrokerage.PlaceOrder(): {message}");

                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_PLACEMENT_ERROR", message));

                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Invalid,
                        Message = ex.Message
                    });

                    submitted = true;  // Binance pattern: return true even on exception
                }
            });

            return submitted;
        }

        /// <summary>
        /// Determines if the order is a Spot market buy order
        /// These orders require special handling due to OKX's sz parameter semantics
        /// </summary>
        private bool IsSpotMarketBuy(Order order)
        {
            return order.Type == OrderType.Market
                && order.Quantity > 0  // Buy direction
                && order.Symbol.SecurityType == SecurityType.Crypto;
        }

        // ========================================
        // SPOT MARKET BUY - FOK LIMIT CONVERSION
        // ========================================

        /// <summary>
        /// Converts Spot market buy order to FOK (Fill-or-Kill) limit order.
        ///
        /// <para><b>Why this conversion is necessary:</b></para>
        /// <para>
        /// OKX Spot market orders have a semantic mismatch with LEAN:
        /// <list type="bullet">
        ///   <item>OKX Market BUY (cash mode): sz can be base or quote currency (controlled by tgtCcy)</item>
        ///   <item>OKX Market BUY (cross/isolated mode): sz is always quote currency (tgtCcy not supported)</item>
        ///   <item>OKX Market SELL: sz is always base currency</item>
        ///   <item>LEAN Market Order: Quantity is always in base currency</item>
        /// </list>
        /// </para>
        ///
        /// <para><b>Solution:</b></para>
        /// <para>
        /// Convert all Spot market buy orders to FOK limit orders. FOK limit orders:
        /// <list type="bullet">
        ///   <item>sz is always in base currency (matches LEAN's Order.Quantity)</item>
        ///   <item>Guarantees all-or-nothing execution - no partial fills left on the book</item>
        ///   <item>Works consistently across all account modes (cash/cross/isolated)</item>
        /// </list>
        /// </para>
        ///
        /// <para><b>Price calculation:</b></para>
        /// <para>
        /// limitPrice = BestAskPrice × (1 + buffer), where buffer defaults to 2%.
        /// This ensures immediate execution while protecting against excessive slippage.
        /// </para>
        /// </summary>
        private bool PlaceSpotMarketBuyAsFokLimit(Order order, string instId, string tdMode)
        {
            // 1. Get BestAsk price via REST API
            var ticker = RestApiClient.GetTicker(instId)?.FirstOrDefault();
            var bestAsk = ticker?.LowestAsk ?? 0m;

            if (bestAsk <= 0)
            {
                throw new InvalidOperationException($"No price data available for {instId}");
            }

            // 2. Calculate limit price with buffer (default 2%)
            var priceBuffer = DefaultMarketBuyPriceBuffer;
            var limitPrice = bestAsk * (1 + priceBuffer);

            Log.Trace($"OKXBaseBrokerage.PlaceSpotMarketBuyAsFokLimit(): {instId} bestAsk={bestAsk}, limitPrice={limitPrice} (buffer={priceBuffer:P0})");

            // 3. Add order to cache BEFORE placing
            CachedOrderIDs.TryAdd(order.Id, order);

            // 4. Build FOK limit order request
            var request = new Messages.PlaceOrderRequest
            {
                InstrumentId = instId,
                TradeMode = tdMode,
                Side = "buy",
                OrderType = "fok",  // Fill-or-Kill
                Size = Math.Abs(order.Quantity).ToStringInvariant(),
                Price = limitPrice.ToStringInvariant(),
                ClientOrderId = order.Id.ToStringInvariant(),
                Tag = string.IsNullOrEmpty(order.Tag) ? "" : order.Tag
            };

            // 5. Place order via REST API
            var result = RestApiClient.PlaceOrder(request);

            if (result.IsSuccess)
            {
                order.BrokerId.Add(result.Data.OrderId);

                Log.Trace($"OKXBaseBrokerage.PlaceSpotMarketBuyAsFokLimit(): Converted market buy to FOK limit @ {limitPrice} " +
                         $"(bestAsk={bestAsk}, buffer={priceBuffer:P0}) for {order.Quantity} {order.Symbol.ID}({order.Symbol.SecurityType}), OKX OrderId: {result.Data.OrderId}");
                return true;
            }

            // Error - throw to let outer catch handle
            throw new InvalidOperationException($"FOK limit @ {limitPrice} failed: {result.GetErrorMessage()}");
        }

        // ========================================
        // STANDARD ORDER PLACEMENT
        // ========================================

        /// <summary>
        /// Places a standard order (Limit, Market Sell, Futures/Swap orders)
        /// For these order types, sz is always in base currency or contract units
        /// </summary>
        private bool PlaceStandardOrder(Order order, string instId, string tdMode)
        {
            // Determine order side
            var side = order.Quantity > 0 ? "buy" : "sell";

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
                    throw new NotSupportedException("StopMarket orders are not supported by OKX v5 API. Use StopLimit instead.");

                case OrderType.StopLimit:
                    throw new NotSupportedException("StopLimit orders require algo order endpoint - not implemented");

                default:
                    throw new NotSupportedException($"Order type {order.Type} is not supported");
            }

            // Add order to cache BEFORE placing via REST API
            CachedOrderIDs.TryAdd(order.Id, order);

            // Create place order request
            var request = new Messages.PlaceOrderRequest
            {
                InstrumentId = instId,
                TradeMode = tdMode,
                Side = side,
                OrderType = ordType,
                Size = Math.Abs(order.Quantity).ToStringInvariant(),
                Price = price,
                ClientOrderId = order.Id.ToStringInvariant(),
                Tag = string.IsNullOrEmpty(order.Tag) ? "" : order.Tag
                // Note: tgtCcy removed - not needed for Limit orders, Market Sell, or Futures/Swap
            };

            // Place order via REST API
            var result = RestApiClient.PlaceOrder(request);

            if (result.IsSuccess)
            {
                order.BrokerId.Add(result.Data.OrderId);

                var orderDetails = order.Type == OrderType.Limit
                    ? $"{order.Type} {order.Direction} {order.Quantity} {order.Symbol.ID}({order.Symbol.SecurityType}) @ {((LimitOrder)order).LimitPrice}"
                    : $"{order.Type} {order.Direction} {order.Quantity} {order.Symbol.ID}({order.Symbol.SecurityType})";

                Log.Trace($"OKXBaseBrokerage.PlaceStandardOrder(): Order {order.Id} placed successfully - {orderDetails}, OKX OrderId: {result.Data.OrderId}");
                return true;
            }

            // Error - throw to let outer catch handle
            throw new InvalidOperationException($"Order failed: {result.GetErrorMessage()}");
        }



        /// <summary>
        /// Determines the trade mode (tdMode) based on account level and security type
        /// Config value has been validated against actual account in ValidateAccountMode()
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>Trade mode: "cash" or "cross"</returns>
        private string GetTradeMode(SecurityType securityType)
        {
            // Read account level from configuration (validated in ValidateAccountMode)
            // Values: "1" (Simple), "2" (Single-currency), "3" (Multi-currency), "4" (Portfolio)
            var accountLevel = ResolveAccountLevel(Configuration.Config.Get("okx-unified-account-mode", "spot"));

            // Simple mode (acctLv="1"):
            // - Spot (SecurityType.Crypto): tdMode="cash"
            // - Futures/Swap (SecurityType.CryptoFuture): tdMode="cross"
            if (accountLevel == "1")
            {
                return securityType == SecurityType.Crypto ? "cash" : "cross";
            }

            // Unified account modes (acctLv="2"/"3"/"4"):
            // - All securities (Spot, Futures, Swap): tdMode="cross"
            return "cross";
        }

        /// <summary>
        /// Updates the order with the same id.
        /// Uses WithLockedStream to synchronize REST API call with WebSocket message processing.
        /// Follows Binance pattern: always returns true, errors reported via events.
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True (errors are reported via OrderEvent and BrokerageMessageEvent)</returns>
        public override bool UpdateOrder(Order order)
        {
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                try
                {
                    // Rate limit order operations
                    OrderRateLimiter.WaitToProceed();

                    if (order.BrokerId.Count == 0)
                    {
                        throw new InvalidOperationException("Cannot update order: No broker ID found");
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
                    var request = new Messages.AmendOrderRequest
                    {
                        InstrumentId = instId,
                        OrderId = ordId,
                        NewSize = newSize,
                        NewPrice = newPrice
                    };

                    // Amend order via REST API
                    var result = RestApiClient.AmendOrder(request);

                    if (result.IsSuccess)
                    {
                        Log.Trace($"OKXBaseBrokerage.UpdateOrder(): Order {order.Id} (OKX: {ordId}) updated successfully");
                        submitted = true;
                        return;
                    }

                    throw new InvalidOperationException($"Update failed: {result.GetErrorMessage()}");
                }
                catch (Exception ex)
                {
                    var message = $"Exception updating order {order.Id}: {ex.Message}";
                    Log.Trace($"OKXBaseBrokerage.UpdateOrder(): {message}");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_UPDATE_ERROR", message));
                    submitted = true;
                }
            });

            return submitted;
        }

        /// <summary>
        /// Cancels the order with the specified ID.
        /// Uses WithLockedStream to synchronize REST API call with WebSocket message processing.
        /// Follows Binance pattern: always returns true, errors reported via events.
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True (errors are reported via OrderEvent and BrokerageMessageEvent)</returns>
        public override bool CancelOrder(Order order)
        {
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                try
                {
                    // Rate limit order operations
                    OrderRateLimiter.WaitToProceed();

                    if (order.BrokerId.Count == 0)
                    {
                        throw new InvalidOperationException("Cannot cancel order: No broker ID found");
                    }

                    var ordId = order.BrokerId[0];
                    var instId = _symbolMapper.GetBrokerageSymbol(order.Symbol);

                    // Create cancel request
                    var request = new Messages.CancelOrderRequest
                    {
                        InstrumentId = instId,
                        OrderId = ordId
                    };

                    // Cancel order via REST API
                    var result = RestApiClient.CancelOrder(request);

                    if (result.IsSuccess)
                    {
                        Log.Trace($"OKXBaseBrokerage.CancelOrder(): Order {order.Id} (OKX: {ordId}) canceled successfully");
                        submitted = true;
                        return;
                    }

                    throw new InvalidOperationException($"Cancel failed: {result.GetErrorMessage()}");
                }
                catch (Exception ex)
                {
                    var message = $"Exception canceling order {order.Id}: {ex.Message}";
                    Log.Trace($"OKXBaseBrokerage.CancelOrder(): {message}");
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ORDER_CANCEL_ERROR", message));
                    submitted = true;
                }
            });

            return submitted;
        }
    }
}
