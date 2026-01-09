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
    /// Represents an OKX v5 API position
    /// https://www.okx.com/docs-v5/en/#rest-api-account-get-positions
    ///
    /// Response format:
    /// {
    ///   "instId": "BTC-USDT-SWAP",
    ///   "pos": "10",
    ///   "availPos": "10",
    ///   "avgPx": "50000",
    ///   "upl": "500.5",
    ///   "posSide": "long",
    ///   "mgnMode": "cross",
    ///   "lever": "10"
    /// }
    /// </summary>
    public class OKXPosition
    {
        /// <summary>
        /// Instrument ID (e.g., BTC-USDT-SWAP, BTC-USDT-230630)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Instrument type: MARGIN, SWAP, FUTURES, OPTION
        /// </summary>
        [JsonProperty("instType")]
        public string InstrumentType { get; set; }

        /// <summary>
        /// Position quantity
        /// Positive for long positions, negative for short positions
        /// </summary>
        [JsonProperty("pos")]
        public string Position { get; set; }

        /// <summary>
        /// Available position that can be closed
        /// </summary>
        [JsonProperty("availPos")]
        public string AvailablePosition { get; set; }

        /// <summary>
        /// Average open price
        /// </summary>
        [JsonProperty("avgPx")]
        public string AveragePrice { get; set; }

        /// <summary>
        /// Unrealized profit and loss
        /// </summary>
        [JsonProperty("upl")]
        public string UnrealizedPnL { get; set; }

        /// <summary>
        /// Position side: long, short, or net
        /// </summary>
        [JsonProperty("posSide")]
        public string PositionSide { get; set; }

        /// <summary>
        /// Margin mode: cross or isolated
        /// </summary>
        [JsonProperty("mgnMode")]
        public string MarginMode { get; set; }

        /// <summary>
        /// Leverage
        /// </summary>
        [JsonProperty("lever")]
        public string Leverage { get; set; }

        /// <summary>
        /// Last price (mark price for derivatives)
        /// </summary>
        [JsonProperty("last")]
        public string LastPrice { get; set; }

        /// <summary>
        /// Currency for position
        /// </summary>
        [JsonProperty("ccy")]
        public string Currency { get; set; }

        /// <summary>
        /// Update time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("uTime")]
        public long UpdateTime { get; set; }
    }
}
