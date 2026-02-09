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
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX-specific order book that extends DefaultOrderBook with OKX WebSocket format parsing.
    /// Provides methods to apply snapshots and deltas from OKX's string-based price level format.
    /// </summary>
    public class OKXOrderBook : DefaultOrderBook
    {
        private readonly object _readLocker = new object();

        /// <summary>
        /// Last processed sequence ID for validation
        /// </summary>
        public long LastUpdateId { get; set; }

        /// <summary>
        /// Timestamp of last update
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OKXOrderBook"/> class
        /// </summary>
        /// <param name="symbol">The symbol for the order book</param>
        public OKXOrderBook(Symbol symbol) : base(symbol)
        {
        }

        /// <summary>
        /// Gets the number of bid price levels
        /// </summary>
        public int BidCount => Bids.Count;

        /// <summary>
        /// Gets the number of ask price levels
        /// </summary>
        public int AskCount => Asks.Count;

        /// <summary>
        /// Applies a full order book snapshot from OKX WebSocket format.
        /// Clears existing data and applies new snapshot atomically.
        /// </summary>
        /// <param name="bids">List of bid price levels in OKX format [[price, size, ...], ...]</param>
        /// <param name="asks">List of ask price levels in OKX format [[price, size, ...], ...]</param>
        public void ApplyFullSnapshot(List<List<string>> bids, List<List<string>> asks)
        {
            lock (_readLocker)
            {
                ApplySnapshot(ParseLevels(bids), ParseLevels(asks));
            }
        }

        /// <summary>
        /// Applies incremental order book updates from OKX WebSocket format.
        /// Size=0 means remove the price level.
        /// </summary>
        /// <param name="bids">List of bid updates in OKX format [[price, size, ...], ...]</param>
        /// <param name="asks">List of ask updates in OKX format [[price, size, ...], ...]</param>
        public void ApplyIncrementalUpdate(List<List<string>> bids, List<List<string>> asks)
        {
            lock (_readLocker)
            {
                ApplyDelta(ParseLevels(bids), ParseLevels(asks));
            }
        }

        /// <summary>
        /// Parses OKX string-based price levels to decimal tuples
        /// </summary>
        private static IEnumerable<(decimal Price, decimal Size)> ParseLevels(List<List<string>> levels)
        {
            if (levels == null)
            {
                yield break;
            }

            foreach (var level in levels)
            {
                if (level.Count >= 2 &&
                    decimal.TryParse(level[0], out var price) &&
                    decimal.TryParse(level[1], out var size))
                {
                    yield return (price, size);
                }
            }
        }

        /// <summary>
        /// Gets all bid levels sorted by price descending (best bid first).
        /// Thread-safe snapshot.
        /// </summary>
        public IEnumerable<KeyValuePair<decimal, decimal>> GetBids()
        {
            lock (_readLocker)
            {
                return Bids.Reverse().ToList();
            }
        }

        /// <summary>
        /// Gets all ask levels sorted by price ascending (best ask first).
        /// Thread-safe snapshot.
        /// </summary>
        public IEnumerable<KeyValuePair<decimal, decimal>> GetAsks()
        {
            lock (_readLocker)
            {
                return Asks.ToList();
            }
        }

        /// <summary>
        /// Converts this OKXOrderBook to LEAN's Orderbook data type.
        /// Thread-safe: uses lock to ensure consistent snapshot.
        /// </summary>
        public Orderbook ToOrderbook()
        {
            lock (_readLocker)
            {
                var now = DateTime.UtcNow;

                var bidsList = Bids.Reverse()
                    .Select(level => new OrderbookLevel(level.Key, level.Value))
                    .ToList();

                var asksList = Asks
                    .Select(level => new OrderbookLevel(level.Key, level.Value))
                    .ToList();

                return new Orderbook(Symbol, now)
                {
                    Bids = bidsList,
                    Asks = asksList,
                    Levels = Math.Max(bidsList.Count, asksList.Count),
                    Value = bidsList.Count > 0 && asksList.Count > 0
                        ? (bidsList[0].Price + asksList[0].Price) / 2m
                        : 0m
                };
            }
        }
    }
}
