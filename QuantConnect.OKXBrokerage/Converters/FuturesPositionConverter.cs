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
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Converter for transforming OKX Futures positions to LEAN holdings
    /// </summary>
    public static class FuturesPositionConverter
    {
        /// <summary>
        /// Converts a OKX Futures position to a LEAN Holding
        /// </summary>
        /// <param name="position">OKX futures position</param>
        /// <param name="symbolMapper">Symbol mapper for converting OKX symbols to LEAN symbols</param>
        /// <returns>LEAN Holding object or null if conversion fails</returns>
        public static Holding ToHolding(this FuturesPosition position, ISymbolMapper symbolMapper)
        {
            if (position == null || string.IsNullOrEmpty(position.Contract))
            {
                return null;
            }

            // Convert OKX symbol to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(position.Contract, SecurityType.CryptoFuture, Market.OKX);

            // Parse numeric fields
            var entryPrice = decimal.Parse(position.EntryPrice);
            var markPrice = decimal.Parse(position.MarkPrice);
            var unrealisedPnl = decimal.Parse(position.UnrealisedPnl);

            // Position size (can be positive for long or negative for short)
            var quantity = position.Size;

            // Calculate market value (position value in settlement currency)
            var marketValue = decimal.Parse(position.Value);

            var holding = new Holding
            {
                Symbol = symbol,
                Quantity = quantity,
                AveragePrice = entryPrice,
                MarketPrice = markPrice,
                MarketValue = marketValue,
                UnrealizedPnL = unrealisedPnl,
                CurrencySymbol = "$" // USDT-margined futures
            };

            // Calculate unrealized PnL percentage if we have valid entry price
            if (entryPrice != 0 && marketValue != 0)
            {
                holding.UnrealizedPnLPercent = (unrealisedPnl / Math.Abs(marketValue)) * 100;
            }

            return holding;
        }
    }
}
