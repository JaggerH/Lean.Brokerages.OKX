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
}
