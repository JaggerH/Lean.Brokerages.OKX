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
    /// Futures balance update from WebSocket channel: futures.balances
    /// Real-time notification of futures balance changes
    /// Note: Futures balance structure differs from Spot balance structure
    /// </summary>
    [JsonConverter(typeof(FuturesBalanceUpdateConverter))]
    public class FuturesBalanceUpdate
    {
        /// <summary>
        /// Final balance amount after change
        /// Note: This is called "balance" in Futures, not "available" as in Spot
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Balance change amount
        /// </summary>
        public decimal Change { get; set; }

        /// <summary>
        /// Attached information/text
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Timestamp in seconds
        /// Note: This is called "time" in Futures, not "timestamp" as in Spot
        /// </summary>
        [JsonProperty("time")]
        public long Time { get; set; }

        /// <summary>
        /// Timestamp in milliseconds
        /// Note: This is called "time_ms" in Futures, not "timestamp_ms" as in Spot
        /// </summary>
        [JsonProperty("time_ms")]
        public long TimeMs { get; set; }

        /// <summary>
        /// Balance change type
        /// Note: This is called "type" in Futures, not "change_type" as in Spot
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

        /// <summary>
        /// Currency code (e.g., USDT, BTC)
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        // Note: The following fields from Spot are NOT present in Futures:
        // - Available (available) - Futures uses "balance" instead
        // - Locked/Freeze (freeze) - Not present in Futures balance updates
        // - Total (total) - Not present in Futures
        // - FreezeChange (freeze_change) - Not present in Futures
    }
}
