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
    /// OKX price limit DTO from REST /api/v5/public/price-limit and WS price-limit channel.
    /// Used as both DTO and synchronizer state (TState = TMessage).
    /// Parsing to decimal is done at the consumption site (TruncateByPriceLimit).
    /// </summary>
    public class PriceLimit
    {
        /// <summary>
        /// Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Maximum buy price limit. Empty string when not enabled.
        /// </summary>
        [JsonProperty("buyLmt")]
        public string BuyLimit { get; set; }

        /// <summary>
        /// Minimum sell price limit. Empty string when not enabled.
        /// </summary>
        [JsonProperty("sellLmt")]
        public string SellLimit { get; set; }

        /// <summary>
        /// Whether price limit is enabled for this instrument
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }
    }
}
