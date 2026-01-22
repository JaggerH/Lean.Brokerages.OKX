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
    /// Represents a full order book for OKX securities.
    /// Implements IOrderBookUpdater with batch update support.
    ///
    /// Key Design:
    /// - Single lock (_locker) protects all operations
    /// - Single-row methods (UpdateBidRow, etc.) trigger events immediately when best price changes
    /// - Batch methods (ApplyFullSnapshot, ApplyIncrementalUpdate) trigger event only once if best prices changed
    /// - Readers are blocked during batch updates to prevent inconsistent reads (e.g., Bid > Ask)
    /// </summary>
    public class OKXOrderBook : IOrderBookUpdater<decimal, decimal>
    {
        private readonly object _locker = new object();
        private readonly SortedDictionary<decimal, decimal> Bids = new SortedDictionary<decimal, decimal>();
        private readonly SortedDictionary<decimal, decimal> Asks = new SortedDictionary<decimal, decimal>();

        /// <summary>
        /// The symbol for the order book. Set by caller when raising events.
        /// </summary>
        public Symbol Symbol { get; set; }

        private decimal _bestBidPrice;
        private decimal _bestBidSize;
        private decimal _bestAskPrice;
        private decimal _bestAskSize;

        /// <summary>
        /// Event fired each time BestBidPrice or BestAskPrice are changed
        /// </summary>
        public event EventHandler<BestBidAskUpdatedEventArgs> BestBidAskUpdated;

        public OKXOrderBook(Symbol symbol)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// The best bid price
        /// </summary>
        public decimal BestBidPrice { get { lock (_locker) { return _bestBidPrice; } } }

        /// <summary>
        /// The best bid size
        /// </summary>
        public decimal BestBidSize { get { lock (_locker) { return _bestBidSize; } } }

        /// <summary>
        /// The best ask price
        /// </summary>
        public decimal BestAskPrice { get { lock (_locker) { return _bestAskPrice; } } }

        /// <summary>
        /// The best ask size
        /// </summary>
        public decimal BestAskSize { get { lock (_locker) { return _bestAskSize; } } }

        /// <summary>
        /// Gets the number of bid levels currently in the order book
        /// </summary>
        public int BidCount { get { lock (_locker) { return Bids.Count; } } }

        /// <summary>
        /// Gets the number of ask levels currently in the order book
        /// </summary>
        public int AskCount { get { lock (_locker) { return Asks.Count; } } }

        #region IOrderBookUpdater Implementation (Single-row operations with immediate events)

        /// <summary>
        /// Updates or inserts a bid price level in the order book.
        /// Triggers BestBidAskUpdated if this becomes the new best bid.
        /// </summary>
        public void UpdateBidRow(decimal price, decimal size)
        {
            lock (_locker)
            {
                Bids[price] = size;

                if (_bestBidPrice == 0 || price >= _bestBidPrice)
                {
                    _bestBidPrice = price;
                    _bestBidSize = size;
                    RaiseBestBidAskUpdated();
                }
            }
        }

        /// <summary>
        /// Updates or inserts an ask price level in the order book.
        /// Triggers BestBidAskUpdated if this becomes the new best ask.
        /// </summary>
        public void UpdateAskRow(decimal price, decimal size)
        {
            lock (_locker)
            {
                Asks[price] = size;

                if (_bestAskPrice == 0 || price <= _bestAskPrice)
                {
                    _bestAskPrice = price;
                    _bestAskSize = size;
                    RaiseBestBidAskUpdated();
                }
            }
        }

        /// <summary>
        /// Removes a bid price level from the order book.
        /// Triggers BestBidAskUpdated if the best bid was removed.
        /// </summary>
        public void RemoveBidRow(decimal price)
        {
            lock (_locker)
            {
                Bids.Remove(price);

                if (price == _bestBidPrice)
                {
                    var priceLevel = Bids.LastOrDefault();
                    _bestBidPrice = priceLevel.Key;
                    _bestBidSize = priceLevel.Value;
                    RaiseBestBidAskUpdated();
                }
            }
        }

        /// <summary>
        /// Removes an ask price level from the order book.
        /// Triggers BestBidAskUpdated if the best ask was removed.
        /// </summary>
        public void RemoveAskRow(decimal price)
        {
            lock (_locker)
            {
                Asks.Remove(price);

                if (price == _bestAskPrice)
                {
                    var priceLevel = Asks.FirstOrDefault();
                    _bestAskPrice = priceLevel.Key;
                    _bestAskSize = priceLevel.Value;
                    RaiseBestBidAskUpdated();
                }
            }
        }

        #endregion

        #region Silent Operations (No events - for batch updates)

        private void UpdateBidRowSilent(decimal price, decimal size)
        {
            Bids[price] = size;
        }

        private void UpdateAskRowSilent(decimal price, decimal size)
        {
            Asks[price] = size;
        }

        private void RemoveBidRowSilent(decimal price)
        {
            Bids.Remove(price);
        }

        private void RemoveAskRowSilent(decimal price)
        {
            Asks.Remove(price);
        }

        private void ClearSilent()
        {
            Bids.Clear();
            Asks.Clear();
            _bestBidPrice = 0;
            _bestBidSize = 0;
            _bestAskPrice = 0;
            _bestAskSize = 0;
        }

        /// <summary>
        /// Recalculates best bid/ask prices from the dictionaries.
        /// Returns true if best prices changed, false otherwise.
        /// </summary>
        private bool RecalculateBestPrices()
        {
            var updated = false;

            if (Bids.Count > 0)
            {
                var bestBid = Bids.Last(); // Highest price
                if (_bestBidPrice != bestBid.Key || _bestBidSize != bestBid.Value)
                {
                    _bestBidPrice = bestBid.Key;
                    _bestBidSize = bestBid.Value;
                    updated = true;
                }
            }
            else if (_bestBidPrice != 0 || _bestBidSize != 0)
            {
                _bestBidPrice = 0;
                _bestBidSize = 0;
                updated = true;
            }

            if (Asks.Count > 0)
            {
                var bestAsk = Asks.First(); // Lowest price
                if (_bestAskPrice != bestAsk.Key || _bestAskSize != bestAsk.Value)
                {
                    _bestAskPrice = bestAsk.Key;
                    _bestAskSize = bestAsk.Value;
                    updated = true;
                }
            }
            else if (_bestAskPrice != 0 || _bestAskSize != 0)
            {
                _bestAskPrice = 0;
                _bestAskSize = 0;
                updated = true;
            }

            return updated;
        }

        #endregion

        #region Batch Operations (Single event after all updates, only if best prices changed)

        /// <summary>
        /// Clears all bid/ask levels and prices.
        /// </summary>
        public void Clear()
        {
            lock (_locker)
            {
                ClearSilent();
            }
        }

        /// <summary>
        /// Applies a full order book snapshot (from WebSocket).
        /// Clears existing data and applies new snapshot atomically.
        /// Triggers BestBidAskUpdated only if best prices changed.
        /// </summary>
        /// <param name="bids">List of bid price levels in OKX format [[price, size], ...]</param>
        /// <param name="asks">List of ask price levels in OKX format [[price, size], ...]</param>
        public void ApplyFullSnapshot(List<List<string>> bids, List<List<string>> asks)
        {
            lock (_locker)
            {
                ClearSilent();

                // Apply all bids
                foreach (var bid in bids ?? new List<List<string>>())
                {
                    if (bid.Count >= 2 &&
                        decimal.TryParse(bid[0], out var price) &&
                        decimal.TryParse(bid[1], out var size) &&
                        size > 0)
                    {
                        UpdateBidRowSilent(price, size);
                    }
                }

                // Apply all asks
                foreach (var ask in asks ?? new List<List<string>>())
                {
                    if (ask.Count >= 2 &&
                        decimal.TryParse(ask[0], out var price) &&
                        decimal.TryParse(ask[1], out var size) &&
                        size > 0)
                    {
                        UpdateAskRowSilent(price, size);
                    }
                }

                if (RecalculateBestPrices())
                {
                    RaiseBestBidAskUpdated();
                }
            }
        }

        /// <summary>
        /// Applies incremental order book updates (from WebSocket).
        /// Size=0 means remove the price level.
        /// Triggers BestBidAskUpdated only if best prices changed.
        /// </summary>
        /// <param name="bids">List of bid updates in OKX format [[price, size], ...]</param>
        /// <param name="asks">List of ask updates in OKX format [[price, size], ...]</param>
        public void ApplyIncrementalUpdate(List<List<string>> bids, List<List<string>> asks)
        {
            lock (_locker)
            {
                // Apply bid incremental updates
                foreach (var bid in bids ?? new List<List<string>>())
                {
                    if (bid.Count >= 2 &&
                        decimal.TryParse(bid[0], out var price) &&
                        decimal.TryParse(bid[1], out var size))
                    {
                        if (size == 0)
                            RemoveBidRowSilent(price);
                        else
                            UpdateBidRowSilent(price, size);
                    }
                }

                // Apply ask incremental updates
                foreach (var ask in asks ?? new List<List<string>>())
                {
                    if (ask.Count >= 2 &&
                        decimal.TryParse(ask[0], out var price) &&
                        decimal.TryParse(ask[1], out var size))
                    {
                        if (size == 0)
                            RemoveAskRowSilent(price);
                        else
                            UpdateAskRowSilent(price, size);
                    }
                }

                if (RecalculateBestPrices())
                {
                    RaiseBestBidAskUpdated();
                }
            }
        }

        #endregion

        #region Event Helpers

        /// <summary>
        /// Raises the BestBidAskUpdated event with current best prices.
        /// Only fires if both best bid and best ask are valid (> 0).
        /// </summary>
        private void RaiseBestBidAskUpdated()
        {
            if (_bestBidPrice > 0 && _bestAskPrice > 0)
            {
                BestBidAskUpdated?.Invoke(this, new BestBidAskUpdatedEventArgs(
                    Symbol, _bestBidPrice, _bestBidSize, _bestAskPrice, _bestAskSize));
            }
        }

        #endregion

        #region Read Operations

        /// <summary>
        /// Gets all bid levels sorted by price descending (best bid first).
        /// Thread-safe snapshot.
        /// </summary>
        public IEnumerable<KeyValuePair<decimal, decimal>> GetBids()
        {
            lock (_locker)
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
            lock (_locker)
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
            lock (_locker)
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

        #endregion
    }
}
