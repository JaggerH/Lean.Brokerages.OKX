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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Represents OKX v5 API account balance response
    /// https://www.okx.com/docs-v5/en/#rest-api-account-get-balance
    ///
    /// Response format:
    /// {
    ///   "code": "0",
    ///   "msg": "",
    ///   "data": [{
    ///     "totalEq": "41624.32",
    ///     "details": [
    ///       {
    ///         "ccy": "USDT",
    ///         "availBal": "1000.50",
    ///         "cashBal": "1200.50",
    ///         "frozenBal": "200"
    ///       }
    ///     ]
    ///   }]
    /// }
    /// </summary>
    public class AccountBalance
    {
        /// <summary>
        /// Total equity in USD
        /// </summary>
        [JsonProperty("totalEq")]
        public string TotalEquity { get; set; }

        /// <summary>
        /// List of balance details for each currency
        /// </summary>
        [JsonProperty("details")]
        public List<BalanceDetail> Details { get; set; }
    }

    /// <summary>
    /// Represents balance detail for a specific currency
    /// </summary>
    public class BalanceDetail
    {
        /// <summary>
        /// Currency (e.g., USDT, BTC, ETH)
        /// </summary>
        [JsonProperty("ccy")]
        public string Currency { get; set; }

        /// <summary>
        /// Available balance
        /// </summary>
        [JsonProperty("availBal")]
        public string AvailableBalance { get; set; }

        /// <summary>
        /// Cash balance
        /// </summary>
        [JsonProperty("cashBal")]
        public string CashBalance { get; set; }

        /// <summary>
        /// Frozen balance (locked in orders)
        /// </summary>
        [JsonProperty("frozenBal")]
        public string FrozenBalance { get; set; }

        /// <summary>
        /// Equity of the currency
        /// </summary>
        [JsonProperty("eq")]
        public string Equity { get; set; }
    }
}
