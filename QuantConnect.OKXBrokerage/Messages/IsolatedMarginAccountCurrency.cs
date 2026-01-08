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
    /// Isolated Margin account currency balance from REST API
    /// Represents balance details for a single currency in an isolated margin trading pair
    /// Part of IsolatedMarginAccount response from GET /margin/accounts
    /// https://www.okx.io/docs/developers/apiv4/en/#query-user-39-s-margin-account
    /// Note: This is for ISOLATED margin, not CROSS margin
    /// </summary>
    public class IsolatedMarginAccountCurrency
    {
        /// <summary>
        /// Currency code (e.g., BTC, USDT, ETH)
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Available balance for margin trading
        /// IMPORTANT: This includes both owned funds AND borrowed funds
        /// To get net asset (real equity), must subtract borrowed and interest
        /// </summary>
        [JsonProperty("available")]
        public decimal Available { get; set; }

        /// <summary>
        /// Locked balance (in open orders)
        /// </summary>
        [JsonProperty("locked")]
        public decimal Locked { get; set; }

        /// <summary>
        /// Total borrowed amount
        /// This is the principal debt (excluding interest)
        /// </summary>
        [JsonProperty("borrowed")]
        public decimal Borrowed { get; set; }

        /// <summary>
        /// Total unpaid interest accrued on borrowed funds
        /// </summary>
        [JsonProperty("interest")]
        public decimal Interest { get; set; }

        /// <summary>
        /// Net asset (real user equity)
        /// Calculation: Available + Locked - Borrowed - Interest
        /// This represents what the user actually owns (after subtracting debt)
        /// </summary>
        [JsonIgnore]
        public decimal NetAsset => Available + Locked - Borrowed - Interest;

        /// <summary>
        /// Total holding (gross balance)
        /// Calculation: Available + Locked
        /// This represents the total balance under management (including borrowed funds)
        /// Used for risk calculations
        /// </summary>
        [JsonIgnore]
        public decimal TotalHolding => Available + Locked;
    }
}
