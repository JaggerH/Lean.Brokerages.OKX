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
    /// OKX premium index history entry.
    /// REST endpoint: GET /api/v5/public/premium-history?instId={instId}&amp;before={before}&amp;limit=100
    /// Returns minute-level premium_index data (timestamps may drift ±1min from exact minute marks).
    /// </summary>
    /// <remarks>
    /// OKX field mapping:
    /// | OKX field | Type             | Description                                |
    /// |-----------|------------------|--------------------------------------------|
    /// | instId    | string           | Instrument ID, e.g. "BTC-USDT-SWAP"       |
    /// | premium   | string(decimal)  | Premium index = (perp - index) / index     |
    /// | ts        | string(ms epoch) | Data timestamp (approx minute-level)       |
    /// </remarks>
    public class PremiumHistory
    {
        [JsonProperty("instId")]
        public string InstId { get; set; }

        /// <summary>Premium index value: (perp - index) / index.</summary>
        [JsonProperty("premium")]
        public string Premium { get; set; }

        /// <summary>Timestamp (Unix ms). Approximately minute-level, may drift ±1min.</summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }
    }
}
