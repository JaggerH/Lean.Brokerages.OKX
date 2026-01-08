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
    /// Cross Margin loan information from REST API
    /// Endpoint: GET /margin/loans
    /// https://www.okx.io/docs/developers/apiv4/en/#list-all-loans
    /// Returns loan records for cross margin trading
    /// Used to calculate net asset: NetAsset = available - left - interest
    /// </summary>
    public class CrossMarginLoan
    {
        /// <summary>
        /// Loan record ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Creation time (Unix timestamp in seconds)
        /// </summary>
        [JsonProperty("create_time")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Currency code of the borrowed asset (e.g., "BTC", "USDT")
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Original borrowed amount
        /// This is the initial principal when the loan was created
        /// </summary>
        [JsonProperty("amount")]
        public string Amount { get; set; }

        /// <summary>
        /// Unpaid principal amount (remaining debt)
        /// This is what still needs to be repaid (excluding interest)
        /// Used in NetAsset calculation: NetAsset = available - left - interest
        /// </summary>
        [JsonProperty("left")]
        public string Left { get; set; }

        /// <summary>
        /// Unpaid interest accrued on the loan
        /// Used in NetAsset calculation: NetAsset = available - left - interest
        /// </summary>
        [JsonProperty("interest")]
        public string Interest { get; set; }

        /// <summary>
        /// Loan status:
        /// - "open": Active loan with unpaid balance
        /// - "loaning": Loan in progress (being borrowed)
        /// - "closed": Fully repaid loan
        /// Only "open" loans should be included in balance calculations
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Loan type (margin mode):
        /// - "cross": Cross margin loan (shared across all positions)
        /// - "isolated": Isolated margin loan (per trading pair)
        /// For cross margin, this should always be "cross"
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Trading pair for isolated margin (e.g., "BTC_USDT")
        /// Empty for cross margin loans
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Repayment time (Unix timestamp in seconds)
        /// 0 if not yet repaid
        /// </summary>
        [JsonProperty("repaid_time")]
        public long RepaidTime { get; set; }

        /// <summary>
        /// Total repaid amount (principal + interest)
        /// 0 if not yet repaid
        /// </summary>
        [JsonProperty("repaid")]
        public string Repaid { get; set; }
    }
}
