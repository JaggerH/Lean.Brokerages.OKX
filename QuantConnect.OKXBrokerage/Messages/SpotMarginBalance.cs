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
using QuantConnect.Brokerages.OKX.Converters;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Spot margin balance update from WebSocket channel: spot.margin_balances
    /// Real-time notification of margin balance changes (margin trading)
    /// Triggered by margin funding deposits or borrowing
    /// </summary>
    [JsonConverter(typeof(SpotMarginBalanceConverter))]
    public class SpotMarginBalance
    {
        /// <summary>
        /// Margin balance update timestamp (seconds)
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Margin balance update timestamp (milliseconds)
        /// </summary>
        [JsonProperty("timestamp_ms")]
        public string TimestampMs { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

        /// <summary>
        /// Currency pair (e.g., "BTC_USDT")
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Changed currency
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Change amount (delta)
        /// </summary>
        [JsonProperty("change")]
        public decimal Change { get; set; }

        /// <summary>
        /// Available margin balance after change
        /// </summary>
        [JsonProperty("available")]
        public decimal Available { get; set; }

        /// <summary>
        /// Frozen amount (locked for funding account)
        /// </summary>
        [JsonProperty("freeze")]
        public decimal Freeze { get; set; }

        /// <summary>
        /// Total borrowed amount
        /// </summary>
        [JsonProperty("borrowed")]
        public decimal Borrowed { get; set; }

        /// <summary>
        /// Total unpaid interest
        /// </summary>
        [JsonProperty("interest")]
        public decimal Interest { get; set; }
    }
}
