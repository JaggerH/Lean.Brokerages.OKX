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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.Converters;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// OKX incremental order book update from WebSocket
    /// Used to maintain local order book with sequence validation
    /// </summary>
    [JsonConverter(typeof(OrderBookUpdateConverter))]
    public class OrderBookUpdate
    {
        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        [JsonProperty("t")]
        public long TimestampMs { get; set; }

        /// <summary>
        /// Timestamp as DateTime (UTC)
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Currency pair (e.g., BTC_USDT)
        /// </summary>
        [JsonProperty("s")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// First update ID in this message
        /// Used for sequence validation: should be equal to lastUpdateId + 1
        /// </summary>
        [JsonProperty("U")]
        public long FirstUpdateId { get; set; }

        /// <summary>
        /// Last update ID in this message
        /// Should be stored as the new lastUpdateId after processing
        /// </summary>
        [JsonProperty("u")]
        public long LastUpdateId { get; set; }

        /// <summary>
        /// true = full snapshot (replace entire order book), false = incremental update
        /// When true, the update contains a complete snapshot and should replace the local order book
        /// </summary>
        [JsonProperty("full")]
        public bool Full { get; set; }

        /// <summary>
        /// Depth level (e.g., "20" = 20 levels, "100" = 100 levels)
        /// </summary>
        [JsonProperty("l")]
        public string Level { get; set; }

        /// <summary>
        /// Bid updates (buy orders)
        /// Format: [["price", "amount"], ["price", "amount"], ...]
        /// When amount is "0", the price level should be removed
        /// Otherwise, replace the existing amount at this price level
        /// </summary>
        [JsonProperty("b")]
        public List<List<string>> Bids { get; set; }

        /// <summary>
        /// Ask updates (sell orders)
        /// Format: [["price", "amount"], ["price", "amount"], ...]
        /// When amount is "0", the price level should be removed
        /// Otherwise, replace the existing amount at this price level
        /// </summary>
        [JsonProperty("a")]
        public List<List<string>> Asks { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OrderBookUpdate()
        {
            Bids = new List<List<string>>();
            Asks = new List<List<string>>();
        }

        /// <summary>
        /// Validates if this update is the next expected update after the given lastUpdateId
        /// </summary>
        /// <param name="lastUpdateId">The last update ID that was processed</param>
        /// <returns>True if this is the next expected update, false if there's a gap or duplicate</returns>
        public bool IsNextUpdate(long lastUpdateId)
        {
            return FirstUpdateId == lastUpdateId + 1;
        }

        /// <summary>
        /// Checks if this update contains the base snapshot ID
        /// Used during initialization to find the starting point in the cached messages
        /// </summary>
        /// <param name="baseId">The base order book ID from REST API snapshot</param>
        /// <returns>True if this update contains the baseId</returns>
        public bool ContainsBaseId(long baseId)
        {
            return FirstUpdateId <= baseId + 1 && LastUpdateId >= baseId + 1;
        }

        /// <summary>
        /// Checks if this update is outdated compared to the base snapshot ID
        /// Updates where LastUpdateId < baseId + 1 should be discarded
        /// </summary>
        /// <param name="baseId">The base order book ID from REST API snapshot</param>
        /// <returns>True if this update is outdated and should be discarded</returns>
        public bool IsOutdated(long baseId)
        {
            return LastUpdateId < baseId + 1;
        }
    }
}
