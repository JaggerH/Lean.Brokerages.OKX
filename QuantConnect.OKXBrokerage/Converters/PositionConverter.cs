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
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Converter for transforming OKX v5 positions to LEAN holdings
    /// </summary>
    public static class PositionConverter
    {
        /// <summary>
        /// Converts an OKX v5 position to a LEAN Holding
        /// </summary>
        /// <param name="okxPosition">OKX position</param>
        /// <param name="symbolMapper">Symbol mapper for converting OKX symbols to LEAN symbols</param>
        /// <returns>LEAN Holding object or null if conversion fails or position is empty</returns>
        public static Holding ToHolding(this Position okxPosition, ISymbolMapper symbolMapper)
        {
            if (okxPosition == null || string.IsNullOrEmpty(okxPosition.InstrumentId))
            {
                Log.Error("PositionConverter.ToHolding(): Invalid OKX position - null or missing InstrumentId");
                return null;
            }

            try
            {
                // Parse position quantity
                if (!decimal.TryParse(okxPosition.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                {
                    Log.Error($"PositionConverter.ToHolding(): Failed to parse Position: {okxPosition.Quantity}");
                    return null;
                }

                // Skip empty positions (OKX returns positions with 0 quantity)
                if (quantity == 0)
                {
                    return null;
                }

                // Determine security type from instrument type
                var securityType = DetermineSecurityType(okxPosition.InstrumentType);

                // Convert OKX symbol to LEAN symbol
                var symbol = symbolMapper.GetLeanSymbol(
                    okxPosition.InstrumentId,
                    securityType,
                    Market.OKX);

                // Parse average price
                if (!decimal.TryParse(okxPosition.AveragePrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var averagePrice))
                {
                    Log.Error($"PositionConverter.ToHolding(): Failed to parse AveragePrice: {okxPosition.AveragePrice}");
                    return null;
                }

                // Parse market price (last price or mark price)
                decimal marketPrice = 0;
                if (!string.IsNullOrEmpty(okxPosition.LastPrice))
                {
                    decimal.TryParse(okxPosition.LastPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out marketPrice);
                }

                // If market price not available, use average price
                if (marketPrice == 0)
                {
                    marketPrice = averagePrice;
                }

                // Calculate market value
                // For crypto: market_value = quantity * market_price
                var marketValue = quantity * marketPrice;

                // Parse unrealized PnL
                decimal unrealizedPnL = 0;
                if (!string.IsNullOrEmpty(okxPosition.UnrealizedPnL))
                {
                    decimal.TryParse(okxPosition.UnrealizedPnL, NumberStyles.Any, CultureInfo.InvariantCulture, out unrealizedPnL);
                }

                // Create holding
                var holding = new Holding
                {
                    Symbol = symbol,
                    Quantity = quantity,
                    AveragePrice = averagePrice,
                    MarketPrice = marketPrice,
                    MarketValue = marketValue,
                    UnrealizedPnL = unrealizedPnL,
                    CurrencySymbol = "$" // OKX uses USDT/USD as quote currency
                };

                // Calculate unrealized PnL percentage
                if (marketValue != 0)
                {
                    holding.UnrealizedPnLPercent = (unrealizedPnL / Math.Abs(marketValue)) * 100;
                }

                return holding;
            }
            catch (Exception ex)
            {
                Log.Error($"PositionConverter.ToHolding(): Exception converting OKX position {okxPosition.InstrumentId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines LEAN SecurityType from OKX instrument type
        /// </summary>
        /// <param name="instrumentType">OKX instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION</param>
        /// <returns>LEAN SecurityType</returns>
        private static SecurityType DetermineSecurityType(string instrumentType)
        {
            switch (instrumentType?.ToUpperInvariant())
            {
                case "SPOT":
                case "MARGIN":
                    return SecurityType.Crypto;

                case "SWAP":
                case "FUTURES":
                    return SecurityType.CryptoFuture;

                case "OPTION":
                    return SecurityType.Option;

                default:
                    Log.Error($"PositionConverter.DetermineSecurityType(): Unknown instrument type: {instrumentType}, defaulting to Crypto");
                    return SecurityType.Crypto;
            }
        }
    }
}
