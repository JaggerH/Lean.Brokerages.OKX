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
using QuantConnect.Brokerages;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Represents a full order book for OKX securities with depth limit management.
    /// Extends DefaultOrderBook to add depth limiting functionality for OKX's order book levels.
    /// </summary>
    public class OKXOrderBook : DefaultOrderBook
    {
        private readonly int _maxDepth;

        // Track best bid/ask locally to avoid triggering events during partial updates
        private decimal _bestBidPrice;
        private decimal _bestBidSize;
        private decimal _bestAskPrice;
        private decimal _bestAskSize;

        /// <summary>
        /// Event fired each time best bid or best ask changes.
        /// Hides the base class event to control trigger timing.
        /// </summary>
        public new event EventHandler<BestBidAskUpdatedEventArgs> BestBidAskUpdated;

        /// <summary>
        /// Gets the maximum depth (number of levels) for this order book
        /// </summary>
        public int MaxDepth => _maxDepth;

        /// <summary>
        /// The best bid price (overrides base class property to use local tracking)
        /// </summary>
        public new decimal BestBidPrice => _bestBidPrice;

        /// <summary>
        /// The best bid size (overrides base class property to use local tracking)
        /// </summary>
        public new decimal BestBidSize => _bestBidSize;

        /// <summary>
        /// The best ask price (overrides base class property to use local tracking)
        /// </summary>
        public new decimal BestAskPrice => _bestAskPrice;

        /// <summary>
        /// The best ask size (overrides base class property to use local tracking)
        /// </summary>
        public new decimal BestAskSize => _bestAskSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="OKXOrderBook"/> class
        /// </summary>
        /// <param name="symbol">The symbol for the order book</param>
        /// <param name="maxDepth">Maximum number of price levels to maintain (e.g., 5, 10, 20, 50, 100)</param>
        public OKXOrderBook(Symbol symbol, int maxDepth = 100) : base(symbol)
        {
            if (maxDepth <= 0)
            {
                throw new ArgumentException($"Max depth must be positive, got {maxDepth}", nameof(maxDepth));
            }

            _maxDepth = maxDepth;
        }

        /// <summary>
        /// Updates or inserts a bid price level in the order book with depth limiting.
        /// Does NOT trigger BestBidAskUpdated event - use TriggerBestBidAskUpdatedIfChanged() manually.
        /// </summary>
        /// <param name="price">The bid price level to be inserted or updated</param>
        /// <param name="size">The new size at the bid price level</param>
        public new void UpdateBidRow(decimal price, decimal size)
        {
            // Update Bids dictionary directly (inherited protected property)
            Bids[price] = size;

            // Update best bid tracking if this is the new best bid
            if (_bestBidPrice == 0 || price >= _bestBidPrice)
            {
                _bestBidPrice = price;
                _bestBidSize = size;
            }

            // Enforce depth limit: remove lowest bids beyond max depth
            // Bids are sorted ascending in SortedDictionary, so highest bid is at .Last()
            while (Bids.Count > _maxDepth)
            {
                // Remove the lowest bid (first in ascending sorted dictionary)
                var lowestBid = Bids.First();
                Bids.Remove(lowestBid.Key);
            }
        }

        /// <summary>
        /// Updates or inserts an ask price level in the order book with depth limiting.
        /// Does NOT trigger BestBidAskUpdated event - use TriggerBestBidAskUpdatedIfChanged() manually.
        /// </summary>
        /// <param name="price">The ask price level to be inserted or updated</param>
        /// <param name="size">The new size at the ask price level</param>
        public new void UpdateAskRow(decimal price, decimal size)
        {
            // Update Asks dictionary directly (inherited protected property)
            Asks[price] = size;

            // Update best ask tracking if this is the new best ask
            if (_bestAskPrice == 0 || price <= _bestAskPrice)
            {
                _bestAskPrice = price;
                _bestAskSize = size;
            }

            // Enforce depth limit: remove highest asks beyond max depth
            // Asks are sorted ascending in SortedDictionary, so lowest ask is at .First()
            while (Asks.Count > _maxDepth)
            {
                // Remove the highest ask (last in ascending sorted dictionary)
                var highestAsk = Asks.Last();
                Asks.Remove(highestAsk.Key);
            }
        }

        /// <summary>
        /// Removes a bid price level from the order book.
        /// Does NOT trigger BestBidAskUpdated event - use TriggerBestBidAskUpdatedIfChanged() manually.
        /// </summary>
        /// <param name="price">The bid price level to be removed</param>
        public new void RemoveBidRow(decimal price)
        {
            Bids.Remove(price);

            // If we removed the best bid, recalculate from remaining bids
            if (price == _bestBidPrice)
            {
                if (Bids.Count > 0)
                {
                    var priceLevel = Bids.Last(); // Highest bid in ascending sorted dict
                    _bestBidPrice = priceLevel.Key;
                    _bestBidSize = priceLevel.Value;
                }
                else
                {
                    _bestBidPrice = 0;
                    _bestBidSize = 0;
                }
            }
        }

        /// <summary>
        /// Removes an ask price level from the order book.
        /// Does NOT trigger BestBidAskUpdated event - use TriggerBestBidAskUpdatedIfChanged() manually.
        /// </summary>
        /// <param name="price">The ask price level to be removed</param>
        public new void RemoveAskRow(decimal price)
        {
            Asks.Remove(price);

            // If we removed the best ask, recalculate from remaining asks
            if (price == _bestAskPrice)
            {
                if (Asks.Count > 0)
                {
                    var priceLevel = Asks.First(); // Lowest ask in ascending sorted dict
                    _bestAskPrice = priceLevel.Key;
                    _bestAskSize = priceLevel.Value;
                }
                else
                {
                    _bestAskPrice = 0;
                    _bestAskSize = 0;
                }
            }
        }

        /// <summary>
        /// Manually triggers the BestBidAskUpdated event with current best bid/ask values.
        /// Call this after completing a batch of OrderBook updates.
        /// </summary>
        public void TriggerBestBidAskUpdatedIfChanged()
        {
            // Always trigger event with current state
            // The event handler can decide whether to act on it
            BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(
                Symbol, _bestBidPrice, _bestBidSize, _bestAskPrice, _bestAskSize));
        }

        /// <summary>
        /// Applies a full order book snapshot (from REST API or WebSocket full push).
        /// Clears existing data, applies new snapshot, and triggers event after completion.
        /// </summary>
        /// <param name="bids">List of bid price levels in format [[price, size], ...]</param>
        /// <param name="asks">List of ask price levels in format [[price, size], ...]</param>
        public void ApplyFullSnapshot(List<List<string>> bids, List<List<string>> asks)
        {
            // 1. Clear existing data
            Bids.Clear();
            Asks.Clear();
            _bestBidPrice = 0;
            _bestBidSize = 0;
            _bestAskPrice = 0;
            _bestAskSize = 0;

            // 2. Apply all bids
            int validBids = 0;
            foreach (var bid in bids ?? new List<List<string>>())
            {
                if (bid.Count >= 2 &&
                    decimal.TryParse(bid[0], out var price) &&
                    decimal.TryParse(bid[1], out var size) &&
                    size > 0)
                {
                    UpdateBidRow(price, size);
                    validBids++;
                }
            }

            // 3. Apply all asks
            int validAsks = 0;
            foreach (var ask in asks ?? new List<List<string>>())
            {
                if (ask.Count >= 2 &&
                    decimal.TryParse(ask[0], out var price) &&
                    decimal.TryParse(ask[1], out var size) &&
                    size > 0)
                {
                    UpdateAskRow(price, size);
                    validAsks++;
                }
            }

            // 4. Trigger event after complete snapshot is applied
            TriggerBestBidAskUpdatedIfChanged();
        }

        /// <summary>
        /// Applies incremental order book updates (from WebSocket incremental push).
        /// Updates or removes price levels based on size, and triggers event after completion.
        /// </summary>
        /// <param name="bids">List of bid updates in format [[price, size], ...]. Size=0 means remove.</param>
        /// <param name="asks">List of ask updates in format [[price, size], ...]. Size=0 means remove.</param>
        public void ApplyIncrementalUpdate(List<List<string>> bids, List<List<string>> asks)
        {
            // 1. Apply bid incremental updates
            foreach (var bid in bids ?? new List<List<string>>())
            {
                if (bid.Count >= 2 &&
                    decimal.TryParse(bid[0], out var price) &&
                    decimal.TryParse(bid[1], out var size))
                {
                    if (size == 0)
                    {
                        RemoveBidRow(price);
                    }
                    else
                    {
                        UpdateBidRow(price, size);
                    }
                }
            }

            // 2. Apply ask incremental updates
            foreach (var ask in asks ?? new List<List<string>>())
            {
                if (ask.Count >= 2 &&
                    decimal.TryParse(ask[0], out var price) &&
                    decimal.TryParse(ask[1], out var size))
                {
                    if (size == 0)
                    {
                        RemoveAskRow(price);
                    }
                    else
                    {
                        UpdateAskRow(price, size);
                    }
                }
            }

            // 3. Trigger event after all incremental updates are applied
            TriggerBestBidAskUpdatedIfChanged();
        }

        /// <summary>
        /// Gets all bid levels as a list sorted by price descending (best bid first)
        /// </summary>
        /// <returns>List of (price, size) tuples for all bid levels</returns>
        public List<(decimal price, decimal size)> GetBids()
        {
            return Bids
                .Reverse() // SortedDictionary is ascending, reverse for descending (best first)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Gets all ask levels as a list sorted by price ascending (best ask first)
        /// </summary>
        /// <returns>List of (price, size) tuples for all ask levels</returns>
        public List<(decimal price, decimal size)> GetAsks()
        {
            return Asks
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Gets the number of bid levels currently in the order book
        /// </summary>
        public int BidCount => Bids.Count;

        /// <summary>
        /// Gets the number of ask levels currently in the order book
        /// </summary>
        public int AskCount => Asks.Count;

        /// <summary>
        /// Clears all bid and ask levels from the order book and resets best bid/ask tracking
        /// </summary>
        public new void Clear()
        {
            Bids.Clear();
            Asks.Clear();
            _bestBidPrice = 0;
            _bestBidSize = 0;
            _bestAskPrice = 0;
            _bestAskSize = 0;
        }
    }
}
