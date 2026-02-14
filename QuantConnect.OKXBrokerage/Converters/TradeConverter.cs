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
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Domain converter extensions for Trade
    /// Converts OKX Trade messages to LEAN Tick objects (Trade type)
    /// </summary>
    public static class TradeExtensions
    {
        /// <summary>
        /// Converts an OKX Trade to a LEAN Trade Tick
        /// </summary>
        /// <param name="trade">OKX trade data</param>
        /// <param name="symbolMapper">Symbol mapper for converting symbols</param>
        /// <param name="securityType">Security type (Crypto or CryptoFuture)</param>
        /// <returns>LEAN Trade Tick object</returns>
        public static Tick ToTick(
            this Trade trade,
            OKXSymbolMapper symbolMapper,
            SecurityType securityType)
        {
            // Convert to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(
                trade.InstrumentId,
                securityType);

            // Convert Unix milliseconds to DateTime
            var time = DateTimeOffset.FromUnixTimeMilliseconds(trade.Timestamp).UtcDateTime;

            return new Tick
            {
                Symbol = symbol,
                Time = time,
                Value = trade.Price,
                Quantity = trade.Size,
                TickType = TickType.Trade
            };
        }
    }
}
