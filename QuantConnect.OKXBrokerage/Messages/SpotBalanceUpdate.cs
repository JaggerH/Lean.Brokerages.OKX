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
    /// Spot balance update from WebSocket channel: spot.balances
    /// Real-time notification of spot account balance changes
    /// https://www.okx.io/docs/developers/apiv4/ws/en/#balances-channel
    /// </summary>
    [JsonConverter(typeof(SpotBalanceUpdateConverter))]
    public class SpotBalanceUpdate
    {
        /// <summary>
        /// Balance change timestamp (seconds)
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Balance change timestamp (milliseconds)
        /// </summary>
        [JsonProperty("timestamp_ms")]
        public string TimestampMs { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

        /// <summary>
        /// Currency code (e.g., BTC, USDT, ETH)
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Change amount (delta)
        /// </summary>
        public decimal Change { get; set; }

        /// <summary>
        /// Total spot balance after change
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Available spot balance after change
        /// </summary>
        public decimal Available { get; set; }

        /// <summary>
        /// Frozen balance amount (locked in orders)
        /// </summary>
        public decimal Freeze { get; set; }

        /// <summary>
        /// Freeze change amount (delta)
        /// </summary>
        public decimal FreezeChange { get; set; }

        /// <summary>
        /// Balance change type (e.g., "order-create", "trade", "deposit", "withdraw")
        /// </summary>
        [JsonProperty("change_type")]
        public string ChangeType { get; set; }
    }
}
