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
    /// OKX v5 API place order request
    /// https://www.okx.com/docs-v5/en/#rest-api-trade-place-order
    /// </summary>
    public class OKXPlaceOrderRequest
    {
        /// <summary>
        /// Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Trade mode: cash, cross, isolated
        /// </summary>
        [JsonProperty("tdMode")]
        public string TradeMode { get; set; }

        /// <summary>
        /// Order side: buy or sell
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Order type: market, limit, post_only, fok, ioc
        /// </summary>
        [JsonProperty("ordType")]
        public string OrderType { get; set; }

        /// <summary>
        /// Order size (quantity)
        /// </summary>
        [JsonProperty("sz")]
        public string Size { get; set; }

        /// <summary>
        /// Order price (required for limit orders)
        /// </summary>
        [JsonProperty("px", NullValueHandling = NullValueHandling.Ignore)]
        public string Price { get; set; }

        /// <summary>
        /// Client-supplied order ID (optional)
        /// </summary>
        [JsonProperty("clOrdId", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Order tag (optional, user-defined)
        /// </summary>
        [JsonProperty("tag", NullValueHandling = NullValueHandling.Ignore)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// OKX v5 API place order response
    /// </summary>
    public class OKXPlaceOrderResponse
    {
        /// <summary>
        /// Order ID assigned by OKX
        /// </summary>
        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Client order ID (if provided in request)
        /// </summary>
        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Order tag (if provided in request)
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Response code (0 = success)
        /// </summary>
        [JsonProperty("sCode")]
        public string StatusCode { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        [JsonProperty("sMsg")]
        public string StatusMessage { get; set; }
    }
}
