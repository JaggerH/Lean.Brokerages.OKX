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
    /// Represents OKX v5 API trade data
    /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-trades
    ///
    /// Response format:
    /// {
    ///   "instId": "BTC-USDT",
    ///   "tradeId": "12345",
    ///   "px": "41000.5",
    ///   "sz": "0.01",
    ///   "side": "buy",
    ///   "ts": "1597026383085"
    /// }
    /// </summary>
    public class Trade
    {
        /// <summary>
        /// Instrument ID (e.g., BTC-USDT)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Trade ID
        /// </summary>
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }

        /// <summary>
        /// Trade price
        /// </summary>
        [JsonProperty("px")]
        public decimal Price { get; set; }

        /// <summary>
        /// Trade size (quantity)
        /// </summary>
        [JsonProperty("sz")]
        public decimal Size { get; set; }

        /// <summary>
        /// Trade side: "buy" or "sell"
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Trade timestamp in Unix milliseconds
        /// </summary>
        [JsonProperty("ts")]
        public long Timestamp { get; set; }
    }
}
