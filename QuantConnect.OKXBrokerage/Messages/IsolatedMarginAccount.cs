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

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Isolated Margin account information from REST API
    /// Endpoint: GET /margin/accounts
    /// https://www.okx.io/docs/developers/apiv4/en/#query-user-39-s-margin-account
    /// Returns isolated margin account information for a specific trading pair
    /// Note: This is for ISOLATED margin, not CROSS margin
    /// </summary>
    public class IsolatedMarginAccount
    {
        /// <summary>
        /// Currency pair (e.g., "BTC_USDT")
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Account type:
        /// - "risk": Risk-based margin account
        /// - "mmr": Maintenance margin ratio account
        /// - "inactive": Inactive margin account
        /// </summary>
        [JsonProperty("account_type")]
        public string AccountType { get; set; }

        /// <summary>
        /// Leverage ratio (e.g., "3" means 3x leverage)
        /// </summary>
        [JsonProperty("leverage")]
        public string Leverage { get; set; }

        /// <summary>
        /// Whether the account is locked (true if restricted from trading)
        /// </summary>
        [JsonProperty("locked")]
        public bool Locked { get; set; }

        /// <summary>
        /// Risk rate (returned when account_type is "risk")
        /// Risk = (borrowed + interest) / (total_equity)
        /// Lower is safer. Value approaching 1.0 indicates liquidation risk
        /// </summary>
        [JsonProperty("risk")]
        public string Risk { get; set; }

        /// <summary>
        /// Maintenance margin ratio (returned when account_type is "mmr")
        /// MMR = (position_margin + order_margin) / total_margin
        /// </summary>
        [JsonProperty("mmr")]
        public string Mmr { get; set; }

        /// <summary>
        /// Base currency balance details (e.g., BTC in BTC_USDT pair)
        /// Contains available, locked, borrowed, and interest information
        /// </summary>
        [JsonProperty("base")]
        public IsolatedMarginAccountCurrency Base { get; set; }

        /// <summary>
        /// Quote currency balance details (e.g., USDT in BTC_USDT pair)
        /// Contains available, locked, borrowed, and interest information
        /// </summary>
        [JsonProperty("quote")]
        public IsolatedMarginAccountCurrency Quote { get; set; }
    }
}
