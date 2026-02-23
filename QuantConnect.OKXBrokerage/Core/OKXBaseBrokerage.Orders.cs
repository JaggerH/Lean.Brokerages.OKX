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
                    OrderRateLimiter.WaitToProceed();

                    var instId = _symbolMapper.GetBrokerageSymbol(order.Symbol);
                    var tdMode = GetTradeMode(order.Symbol.SecurityType);

                    // Build request based on order type
                    var request = IsSpotMarketBuy(order)
                        ? BuildSpotMarketBuyAsFokLimitRequest(order, instId, tdMode)
                        : BuildStandardOrderRequest(order, instId, tdMode);

                    // Add to cache before REST call so WithLockedStream-queued WS fills can find it
                    CachedOrderIDs.TryAdd(order.Id, order);

                    // Place order via REST API (throws on failure)
                    var response = RestApiClient.PlaceOrder(request);

                    order.BrokerId.Add(response.OrderId);

                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Submitted
                    });

                    submitted = true;
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

                    submitted = true;
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
        /// Walks orderbook ask levels to find the worst price needed to fill order.Quantity,
        /// then caps at OKX buyLmt (price limit) if enabled.
        /// </para>
        /// </summary>
        private Messages.PlaceOrderRequest BuildSpotMarketBuyAsFokLimitRequest(Order order, string instId, string tdMode)
        {
            if (!_orderBooks.TryGetValue(order.Symbol, out var orderBook))
            {
                throw new InvalidOperationException($"No order book available for {instId}");
            }

            var limitPrice = CalculateFokLimitPrice(orderBook, order.Symbol, Math.Abs(order.Quantity));

            Log.Trace($"OKXBaseBrokerage.BuildSpotMarketBuyAsFokLimitRequest(): " +
                $"{instId} qty={Math.Abs(order.Quantity)} limitPrice={limitPrice}");

            return new Messages.PlaceOrderRequest
            {
                InstrumentId = instId,
                TradeMode = tdMode,
                Side = "buy",
                OrderType = "fok",
                Size = Math.Abs(order.Quantity).ToStringInvariant(),
                Price = limitPrice.ToStringInvariant(),
                ClientOrderId = order.Id.ToStringInvariant(),
                Tag = HashOrderTag(order.Tag)
            };
        }

        /// <summary>
        /// Calculates FOK limit price by walking orderbook depth to cover the required quantity.
        /// Takes the worst (highest) ask price needed, then caps at OKX buyLmt if enabled.
        /// </summary>
        public decimal CalculateFokLimitPrice(OKXOrderBook orderBook, Symbol symbol, decimal quantity)
        {
            var asks = orderBook.GetAsks().ToList();

            if (asks.Count == 0)
            {
                throw new InvalidOperationException($"No ask levels in order book for {symbol}");
            }

            var accumulated = 0m;
            var worstPrice = asks[0].Key;

            foreach (var level in asks)
            {
                worstPrice = level.Key;
                accumulated += level.Value;
                if (accumulated >= quantity) break;
            }

            // Apply PriceLimit ceiling
            var limit = _priceLimitSync?.GetState(symbol);
            if (limit?.Enabled == true)
            {
                var buyLmt = ParseHelper.ParseDecimal(limit.BuyLimit);
                if (buyLmt > 0 && worstPrice > buyLmt)
                {
                    worstPrice = buyLmt;
                }
            }

            return worstPrice;
        }

        // ========================================
        // STANDARD ORDER PLACEMENT
        // ========================================

        /// <summary>
        /// Places a standard order (Limit, Market Sell, Futures/Swap orders)
        /// For these order types, sz is always in base currency or contract units
        /// </summary>
        private Messages.PlaceOrderRequest BuildStandardOrderRequest(Order order, string instId, string tdMode)
        {
            var side = order.Quantity > 0 ? "buy" : "sell";

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

            return new Messages.PlaceOrderRequest
            {
                InstrumentId = instId,
                TradeMode = tdMode,
                Side = side,
                OrderType = ordType,
                Size = Math.Abs(order.Quantity).ToStringInvariant(),
                Price = price,
                ClientOrderId = order.Id.ToStringInvariant(),
                Tag = HashOrderTag(order.Tag)
            };
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

                    // Amend order via REST API (throws on failure)
                    RestApiClient.AmendOrder(request);

                    Log.Trace($"OKXBaseBrokerage.UpdateOrder(): Order {order.Id} (OKX: {ordId}) updated successfully");
                    submitted = true;
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

                    // Cancel order via REST API (throws on failure)
                    RestApiClient.CancelOrder(request);

                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Canceled
                    });

                    Log.Trace($"OKXBaseBrokerage.CancelOrder(): Order {order.Id} (OKX: {ordId}) canceled successfully");
                    submitted = true;
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
