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
    /// OKX ticker data - high level overview of market state for a specified pair
    /// </summary>
    [JsonConverter(typeof(TickerConverter))]
    public class Ticker
    {
        /// <summary>
        /// Currency pair (e.g., BTC_USDT)
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Last trade price
        /// </summary>
        [JsonProperty("last")]
        public string Last { get; set; }

        /// <summary>
        /// Lowest ask price
        /// </summary>
        [JsonProperty("lowest_ask")]
        public decimal LowestAsk { get; set; }

        /// <summary>
        /// Highest bid price
        /// </summary>
        [JsonProperty("highest_bid")]
        public decimal HighestBid { get; set; }

        /// <summary>
        /// Change percentage in last 24 hours
        /// </summary>
        [JsonProperty("change_percentage")]
        public string ChangePercentage { get; set; }

        /// <summary>
        /// Base currency volume in last 24 hours
        /// </summary>
        [JsonProperty("base_volume")]
        public string BaseVolume { get; set; }

        /// <summary>
        /// Quote currency volume in last 24 hours
        /// </summary>
        [JsonProperty("quote_volume")]
        public string QuoteVolume { get; set; }

        /// <summary>
        /// Highest price in last 24 hours
        /// </summary>
        [JsonProperty("high_24h")]
        public string High24h { get; set; }

        /// <summary>
        /// Lowest price in last 24 hours
        /// </summary>
        [JsonProperty("low_24h")]
        public string Low24h { get; set; }
    }
}