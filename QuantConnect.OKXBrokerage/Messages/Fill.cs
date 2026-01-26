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
    /// User fill/execution record from OKX REST API
    /// https://www.okx.com/docs-v5/en/#rest-api-trade-get-transaction-details-last-3-days
    /// GET /api/v5/trade/fills
    /// </summary>
    public class Fill
    {
        /// <summary>
        /// Trade ID - maps to ExecutionId in LEAN
        /// </summary>
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }

        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Client Order ID (user-defined)
        /// </summary>
        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION
        /// </summary>
        [JsonProperty("instType")]
        public string InstrumentType { get; set; }

        /// <summary>
        /// Order side: buy or sell
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Fill size (quantity)
        /// </summary>
        [JsonProperty("fillSz")]
        public string FillSize { get; set; }

        /// <summary>
        /// Fill price
        /// </summary>
        [JsonProperty("fillPx")]
        public string FillPrice { get; set; }

        /// <summary>
        /// Fee amount (negative = fee paid, positive = rebate)
        /// </summary>
        [JsonProperty("fee")]
        public string Fee { get; set; }

        /// <summary>
        /// Fee currency
        /// </summary>
        [JsonProperty("feeCcy")]
        public string FeeCurrency { get; set; }

        /// <summary>
        /// Fill time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("fillTime")]
        public string FillTime { get; set; }

        /// <summary>
        /// Timestamp when the fill record was created (Unix milliseconds)
        /// </summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Order tag (user-defined)
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Bill ID (used for pagination)
        /// </summary>
        [JsonProperty("billId")]
        public string BillId { get; set; }

        /// <summary>
        /// Execution type: T = taker, M = maker
        /// </summary>
        [JsonProperty("execType")]
        public string ExecType { get; set; }
    }
}
