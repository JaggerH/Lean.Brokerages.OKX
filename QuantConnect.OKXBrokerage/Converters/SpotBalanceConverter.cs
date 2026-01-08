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

using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Domain converter for Spot account balances (REST API)
    /// Converts OKX Spot balance models to LEAN models
    /// </summary>
    public static class SpotBalanceConverter
    {
        /// <summary>
        /// Converts Spot balance to LEAN CashAmount
        /// Returns the available balance (not locked) as the cash amount
        /// </summary>
        /// <param name="balance">OKX spot balance from REST API</param>
        /// <returns>LEAN CashAmount or null if conversion fails</returns>
        public static CashAmount? ToCashAmount(this SpotBalance balance)
        {
            if (balance == null || string.IsNullOrEmpty(balance.Currency))
            {
                return null;
            }

            // Parse available balance
            if (!decimal.TryParse(balance.Available, out var amount))
            {
                return null;
            }

            // Return available balance as CashAmount
            return new CashAmount(amount, balance.Currency.ToUpperInvariant());
        }
    }
}
