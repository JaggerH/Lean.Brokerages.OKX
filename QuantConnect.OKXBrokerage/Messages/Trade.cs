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
    /// OKX trade execution record
    /// </summary>
    [JsonConverter(typeof(TradeConverter))]
    public class Trade
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// Currency pair (e.g., BTC_USDT)
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

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
        /// Trade side: buy or sell
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Trade amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Trade price
        /// </summary>
        public decimal Price { get; set; }
    }
}