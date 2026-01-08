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
    /// OKX Spot Order message
    /// </summary>
    [JsonConverter(typeof(SpotOrderConverter))]
    public class SpotOrder
    {
        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Client Order ID
        /// </summary>
        [JsonProperty("text")]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Currency pair (e.g., BTC_USDT)
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Order type: limit or market
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Order side: buy or sell
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Order amount
        /// </summary>
        [JsonProperty("amount")]
        public string Amount { get; set; }

        /// <summary>
        /// Order price
        /// </summary>
        [JsonProperty("price")]
        public string Price { get; set; }

        /// <summary>
        /// Order status: open, closed, cancelled
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Creation time (Unix timestamp in seconds)
        /// </summary>
        [JsonProperty("create_time")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Creation time in milliseconds
        /// </summary>
        [JsonProperty("create_time_ms")]
        public string CreateTimeMs { get; set; }

        /// <summary>
        /// Update time (Unix timestamp in seconds)
        /// </summary>
        [JsonProperty("update_time")]
        public long UpdateTime { get; set; }

        /// <summary>
        /// Update time in milliseconds
        /// </summary>
        [JsonProperty("update_time_ms")]
        public string UpdateTimeMs { get; set; }

        /// <summary>
        /// Event type: put, update, finish
        /// </summary>
        [JsonProperty("event")]
        public string Event { get; set; }

        /// <summary>
        /// Account type: spot, margin, etc.
        /// </summary>
        [JsonProperty("account")]
        public string Account { get; set; }

        /// <summary>
        /// Time in force: gtc, ioc, etc.
        /// </summary>
        [JsonProperty("time_in_force")]
        public string TimeInForce { get; set; }

        /// <summary>
        /// Filled amount
        /// </summary>
        [JsonProperty("filled_amount")]
        public string FilledAmount { get; set; }

        /// <summary>
        /// Total filled value (cumulative)
        /// </summary>
        [JsonProperty("filled_total")]
        public string FilledTotal { get; set; }

        /// <summary>
        /// Amount remaining
        /// </summary>
        [JsonProperty("left")]
        public string Left { get; set; }

        /// <summary>
        /// Trading fee
        /// </summary>
        [JsonProperty("fee")]
        public string Fee { get; set; }

        /// <summary>
        /// Fee currency
        /// </summary>
        [JsonProperty("fee_currency")]
        public string FeeCurrency { get; set; }

        /// <summary>
        /// Finish reason: filled, cancelled, ioc, etc.
        /// </summary>
        [JsonProperty("finish_as")]
        public string FinishAs { get; set; }
    }
}
