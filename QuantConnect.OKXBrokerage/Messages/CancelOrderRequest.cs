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
    /// OKX v5 API cancel order request
    /// https://www.okx.com/docs-v5/en/#rest-api-trade-cancel-order
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>
        /// Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Order ID (either ordId or clOrdId is required)
        /// </summary>
        [JsonProperty("ordId", NullValueHandling = NullValueHandling.Ignore)]
        public string OrderId { get; set; }

        /// <summary>
        /// Client order ID (either ordId or clOrdId is required)
        /// </summary>
        [JsonProperty("clOrdId", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientOrderId { get; set; }
    }

    /// <summary>
    /// OKX v5 API cancel order response
    /// </summary>
    public class CancelOrderResponse
    {
        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Client order ID (if provided)
        /// </summary>
        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

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
