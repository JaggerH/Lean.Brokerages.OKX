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
    /// OKX fee rate response from GET /api/v5/account/trade-fee.
    /// Negative string values indicate a fee charge; positive values indicate a rebate.
    /// </summary>
    /// <remarks>
    /// For SPOT:  use <see cref="Maker"/> / <see cref="Taker"/>.
    /// For SWAP:  use <see cref="MakerU"/> / <see cref="TakerU"/> (USDT-margined).
    /// </remarks>
    public class FeeRate
    {
        /// <summary>Maker fee rate for spot or crypto-margined contracts (e.g. "-0.0008").</summary>
        [JsonProperty("maker")]
        public string Maker { get; set; }

        /// <summary>Taker fee rate for spot or crypto-margined contracts (e.g. "-0.001").</summary>
        [JsonProperty("taker")]
        public string Taker { get; set; }

        /// <summary>Maker fee rate for USDT-margined perpetual/futures contracts (e.g. "-0.0002").</summary>
        [JsonProperty("makerU")]
        public string MakerU { get; set; }

        /// <summary>Taker fee rate for USDT-margined perpetual/futures contracts (e.g. "-0.0005").</summary>
        [JsonProperty("takerU")]
        public string TakerU { get; set; }

        /// <summary>Delivery fee rate.</summary>
        [JsonProperty("delivery")]
        public string Delivery { get; set; }

        /// <summary>Fee tier level (e.g. "Lv1").</summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>Instrument type this response applies to (SPOT, SWAP, FUTURES, OPTION).</summary>
        [JsonProperty("instType")]
        public string InstType { get; set; }
    }
}
