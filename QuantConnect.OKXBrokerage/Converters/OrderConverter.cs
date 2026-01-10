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
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Converter for transforming OKX v5 orders to LEAN orders
    /// </summary>
    public static class OrderConverter
    {
        /// <summary>
        /// Converts an OKX v5 order to a LEAN Order
        /// </summary>
        /// <param name="okxOrder">OKX order</param>
        /// <param name="symbolMapper">Symbol mapper for converting OKX symbols to LEAN symbols</param>
        /// <returns>LEAN Order object or null if conversion fails</returns>
        public static Order ToLeanOrder(this OKXOrder okxOrder, ISymbolMapper symbolMapper)
        {
            if (okxOrder == null || string.IsNullOrEmpty(okxOrder.InstrumentId))
            {
                Log.Error("OrderConverter.ToLeanOrder(): Invalid OKX order - null or missing InstrumentId");
                return null;
            }

            try
            {
                // Determine security type from instrument ID format
                var securityType = DetermineSecurityType(okxOrder.InstrumentId, okxOrder.InstrumentType);

                // Convert OKX symbol to LEAN symbol
                var symbol = symbolMapper.GetLeanSymbol(
                    okxOrder.InstrumentId,
                    securityType,
                    Market.OKX);

                // Parse numeric fields
                if (!decimal.TryParse(okxOrder.Size, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                {
                    Log.Error($"OrderConverter.ToLeanOrder(): Failed to parse Size: {okxOrder.Size}");
                    return null;
                }

                // Determine order direction from side
                if (okxOrder.Side.Equals("sell", StringComparison.OrdinalIgnoreCase))
                {
                    quantity = -quantity;
                }

                // Parse filled quantity
                decimal filledQuantity = 0;
                if (!string.IsNullOrEmpty(okxOrder.AccumulatedFillSize))
                {
                    decimal.TryParse(okxOrder.AccumulatedFillSize, NumberStyles.Any, CultureInfo.InvariantCulture, out filledQuantity);
                    if (okxOrder.Side.Equals("sell", StringComparison.OrdinalIgnoreCase))
                    {
                        filledQuantity = -filledQuantity;
                    }
                }

                // Parse order ID
                if (!int.TryParse(okxOrder.OrderId, out var orderId))
                {
                    // For large order IDs, use hash code
                    orderId = okxOrder.OrderId.GetHashCode();
                }

                // Create time
                var createTime = DateTimeOffset.FromUnixTimeMilliseconds(okxOrder.CreateTime).UtcDateTime;

                // Create appropriate order type using constructors
                Order order = null;

                switch (okxOrder.OrderType.ToLowerInvariant())
                {
                    case "market":
                        order = new MarketOrder(symbol, quantity, createTime);
                        break;

                    case "limit":
                    case "post_only":
                        if (!decimal.TryParse(okxOrder.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var limitPrice))
                        {
                            Log.Error($"OrderConverter.ToLeanOrder(): Failed to parse Price: {okxOrder.Price}");
                            return null;
                        }

                        order = new LimitOrder(symbol, quantity, limitPrice, createTime);
                        break;

                    case "ioc":
                    case "fok":
                        // IOC (Immediate or Cancel) and FOK (Fill or Kill) are treated as market orders
                        order = new MarketOrder(symbol, quantity, createTime);
                        break;

                    default:
                        Log.Error($"OrderConverter.ToLeanOrder(): Unsupported order type: {okxOrder.OrderType}");
                        return null;
                }

                // Set properties that can be modified
                if (order != null)
                {
                    order.BrokerId.Add(okxOrder.OrderId);
                    order.Status = ConvertOrderStatus(okxOrder.State);
                }

                return order;
            }
            catch (Exception ex)
            {
                Log.Error($"OrderConverter.ToLeanOrder(): Exception converting OKX order {okxOrder.OrderId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts OKX order state to LEAN OrderStatus
        /// </summary>
        /// <param name="okxState">OKX order state: live, partially_filled, filled, canceled</param>
        /// <returns>LEAN OrderStatus</returns>
        private static OrderStatus ConvertOrderStatus(string okxState)
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
                case "cancelled":
                    return OrderStatus.Canceled;

                default:
                    Log.Error($"OrderConverter.ConvertOrderStatus(): Unknown OKX order state: {okxState}");
                    return OrderStatus.None;
            }
        }

        /// <summary>
        /// Determines LEAN SecurityType from OKX instrument ID and type
        /// </summary>
        /// <param name="instrumentId">OKX instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)</param>
        /// <param name="instrumentType">OKX instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION</param>
        /// <returns>LEAN SecurityType</returns>
        private static SecurityType DetermineSecurityType(string instrumentId, string instrumentType)
        {
            // Use instrumentType if available
            if (!string.IsNullOrEmpty(instrumentType))
            {
                switch (instrumentType.ToUpperInvariant())
                {
                    case "SPOT":
                    case "MARGIN":
                        return SecurityType.Crypto;

                    case "SWAP":
                    case "FUTURES":
                        return SecurityType.CryptoFuture;

                    case "OPTION":
                        return SecurityType.Option;
                }
            }

            // Fallback: parse from instrument ID format
            var parts = instrumentId.Split('-');
            if (parts.Length >= 3)
            {
                // Has suffix: BTC-USDT-SWAP, BTC-USDT-230630
                return SecurityType.CryptoFuture;
            }

            // Default to spot: BTC-USDT
            return SecurityType.Crypto;
        }
    }

    /// <summary>
    /// Extension methods for converting OKX WebSocket order updates to LEAN OrderEvents
    /// </summary>
    public static class WebSocketOrderExtensions
    {
        /// <summary>
        /// Converts OKX WebSocket order update to LEAN OrderEvent
        /// OKX orders channel pushes a message for each fill (trade execution)
        /// </summary>
        /// <param name="order">OKX WebSocket order update</param>
        /// <param name="leanOrder">The LEAN order associated with this update</param>
        /// <param name="accumulatedFillSize">Total accumulated fill size for this order</param>
        /// <returns>LEAN OrderEvent or null if this is not a fill event</returns>
        public static OrderEvent ToOrderEvent(this OKXWebSocketOrder order, Order leanOrder, decimal accumulatedFillSize)
        {
            if (order == null || leanOrder == null)
            {
                return null;
            }

            // Parse last fill size
            if (!decimal.TryParse(order.LastFillSize ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var lastFillSize))
            {
                lastFillSize = 0;
            }

            // Map OKX state to LEAN OrderStatus
            var status = ConvertOrderStatus(order.State);

            // Check if this is a fill event (lastFillSize > 0)
            if (lastFillSize > 0)
            {
                // Parse last fill price (fallback to average price if not available)
                if (!decimal.TryParse(order.LastFillPrice ?? order.AveragePrice ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var lastFillPrice))
                {
                    lastFillPrice = 0;
                }

                // Parse last fill fee
                if (!decimal.TryParse(order.LastFillFee ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var lastFillFee))
                {
                    lastFillFee = 0;
                }

                // Adjust sign based on order direction
                var signedFillQty = order.Side == "buy" ? lastFillSize : -lastFillSize;

                // Determine fee currency
                var feeCurrency = !string.IsNullOrEmpty(order.LastFillFeeCurrency)
                    ? order.LastFillFeeCurrency
                    : order.FeeCurrency ?? "USDT";

                // Parse fill time (use UpdateTime if FillTime is not available)
                long fillTimeMs;
                if (!string.IsNullOrEmpty(order.LastFillTime) && long.TryParse(order.LastFillTime, out fillTimeMs))
                {
                    // Use fill time
                }
                else if (long.TryParse(order.UpdateTime, out fillTimeMs))
                {
                    // Fallback to update time
                }
                else
                {
                    fillTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                // Create fill event
                return new OrderEvent(
                    leanOrder.Id,
                    leanOrder.Symbol,
                    DateTimeOffset.FromUnixTimeMilliseconds(fillTimeMs).UtcDateTime,
                    status,
                    leanOrder.Direction,
                    lastFillPrice,
                    signedFillQty,
                    new OrderFee(new CashAmount(Math.Abs(lastFillFee), feeCurrency)),
                    $"OKX order {order.OrderId}: {order.State}, Fill: {lastFillSize} @ {lastFillPrice}"
                );
            }

            // Order state change without fill (e.g., canceled, submitted)
            long updateTimeMs;
            if (!long.TryParse(order.UpdateTime, out updateTimeMs))
            {
                updateTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            return new OrderEvent(
                leanOrder.Id,
                leanOrder.Symbol,
                DateTimeOffset.FromUnixTimeMilliseconds(updateTimeMs).UtcDateTime,
                status,
                leanOrder.Direction,
                0,
                0,
                OrderFee.Zero,
                $"OKX order {order.OrderId}: {order.State}"
            );
        }

        /// <summary>
        /// Converts OKX order state to LEAN OrderStatus
        /// </summary>
        /// <param name="okxState">OKX order state: live, partially_filled, filled, canceled</param>
        /// <returns>LEAN OrderStatus</returns>
        private static OrderStatus ConvertOrderStatus(string okxState)
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
                case "cancelled":
                    return OrderStatus.Canceled;

                case "canceling":
                    return OrderStatus.CancelPending;

                default:
                    Log.Error($"WebSocketOrderExtensions.ConvertOrderStatus(): Unknown OKX order state: {okxState}");
                    return OrderStatus.None;
            }
        }
    }
}
