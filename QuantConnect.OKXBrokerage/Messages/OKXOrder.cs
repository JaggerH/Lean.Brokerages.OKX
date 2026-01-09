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
    /// Represents an OKX v5 API order
    /// https://www.okx.com/docs-v5/en/#rest-api-trade-get-order-list
    ///
    /// Response format:
    /// {
    ///   "instId": "BTC-USDT",
    ///   "ordId": "312269865356374016",
    ///   "clOrdId": "b15",
    ///   "tag": "",
    ///   "px": "30000",
    ///   "sz": "0.001",
    ///   "ordType": "limit",
    ///   "side": "buy",
    ///   "posSide": "net",
    ///   "tdMode": "cash",
    ///   "state": "live",
    ///   "accFillSz": "0",
    ///   "fillPx": "",
    ///   "avgPx": "",
    ///   "cTime": "1597026383085",
    ///   "uTime": "1597026383085"
    /// }
    /// </summary>
    public class OKXOrder
    {
        /// <summary>
        /// Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Client Order ID as assigned by the client
        /// </summary>
        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Order tag (user-defined)
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        [JsonProperty("px")]
        public string Price { get; set; }

        /// <summary>
        /// Size (quantity)
        /// </summary>
        [JsonProperty("sz")]
        public string Size { get; set; }

        /// <summary>
        /// Order type: market, limit, post_only, fok, ioc
        /// </summary>
        [JsonProperty("ordType")]
        public string OrderType { get; set; }

        /// <summary>
        /// Order side: buy or sell
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Position side: long, short, or net
        /// </summary>
        [JsonProperty("posSide")]
        public string PositionSide { get; set; }

        /// <summary>
        /// Trade mode: cash, cross, isolated
        /// </summary>
        [JsonProperty("tdMode")]
        public string TradeMode { get; set; }

        /// <summary>
        /// Order state: live, partially_filled, filled, canceled
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Accumulated fill quantity
        /// </summary>
        [JsonProperty("accFillSz")]
        public string AccumulatedFillSize { get; set; }

        /// <summary>
        /// Last filled price (if partial fill occurred)
        /// </summary>
        [JsonProperty("fillPx")]
        public string FillPrice { get; set; }

        /// <summary>
        /// Average filled price
        /// </summary>
        [JsonProperty("avgPx")]
        public string AveragePrice { get; set; }

        /// <summary>
        /// Creation time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("cTime")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Update time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("uTime")]
        public long UpdateTime { get; set; }

        /// <summary>
        /// Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION
        /// </summary>
        [JsonProperty("instType")]
        public string InstrumentType { get; set; }

        /// <summary>
        /// Leverage (for margin/derivatives)
        /// </summary>
        [JsonProperty("lever")]
        public string Leverage { get; set; }
    }
}
