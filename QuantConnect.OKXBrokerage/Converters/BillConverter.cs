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
using QuantConnect.Securities;
using QuantConnect.TradingPairs;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Extension methods for converting OKX Bill records to LEAN InterestSettlement
    /// </summary>
    public static class BillExtensions
    {
        /// <summary>
        /// Converts an OKX Bill to a LEAN InterestSettlement.
        /// Maps instId → Symbol via symbolMapper, balChg+ccy → CashAmount.
        /// </summary>
        /// <param name="bill">OKX bill record</param>
        /// <param name="symbolMapper">Symbol mapper for converting OKX instrument IDs to LEAN symbols</param>
        /// <returns>LEAN InterestSettlement</returns>
        public static InterestSettlement ToInterestSettlement(this Bill bill, OKXSymbolMapper symbolMapper)
        {
            // Determine SecurityType from instrument type (same pattern as FillExtensions)
            var securityType = bill.InstType?.ToUpperInvariant() switch
            {
                "SPOT" or "MARGIN" => SecurityType.Crypto,
                "SWAP" or "FUTURES" => SecurityType.CryptoFuture,
                "OPTION" => SecurityType.Option,
                _ => SecurityType.Crypto
            };

            var symbol = symbolMapper.GetLeanSymbol(bill.InstId, securityType);
            var amount = ParseHelper.ParseDecimal(bill.BalanceChange);
            var currency = bill.Ccy ?? "USDT";
            var timestampMs = ParseHelper.ParseLong(bill.Ts);

            return new InterestSettlement
            {
                SettlementId = bill.BillId,
                Symbol = symbol,
                Amount = new CashAmount(amount, currency),
                TimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime
            };
        }
    }
}
