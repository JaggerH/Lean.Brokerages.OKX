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

using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.Converters;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// OKX order book snapshot from REST API
    /// Used to initialize the local order book with a base sequence ID
    /// Supports both Spot and Futures formats via OrderBookSnapshotConverter
    /// </summary>
    [JsonConverter(typeof(OrderBookSnapshotConverter))]
    public class OrderBookSnapshot
    {
        /// <summary>
        /// Order book ID - used as baseID for sequence validation
        /// This ID represents the state of the order book at this snapshot
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// Current timestamp (Unix timestamp in seconds)
        /// </summary>
        [JsonProperty("current")]
        public long Current { get; set; }

        /// <summary>
        /// Last update timestamp (Unix timestamp in seconds)
        /// </summary>
        [JsonProperty("update")]
        public long Update { get; set; }

        /// <summary>
        /// Ask orders (sell orders)
        /// Format: [["price", "amount"], ["price", "amount"], ...]
        /// Sorted from lowest to highest price
        /// </summary>
        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; }

        /// <summary>
        /// Bid orders (buy orders)
        /// Format: [["price", "amount"], ["price", "amount"], ...]
        /// Sorted from highest to lowest price
        /// </summary>
        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OrderBookSnapshot()
        {
            Asks = new List<List<string>>();
            Bids = new List<List<string>>();
        }
    }
}
