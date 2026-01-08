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
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Provides extension methods to convert OKXOrderBook to LEAN's Orderbook
    /// </summary>
    public static class OKXOrderbookConverter
    {
        /// <summary>
        /// Converts a OKXOrderBook to LEAN's Orderbook data type
        /// </summary>
        /// <param name="orderBook">The OKX order book to convert</param>
        /// <returns>Orderbook instance with properly sorted bid and ask levels</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderBook is null</exception>
        public static Orderbook ToOrderbook(this OKXOrderBook orderBook)
        {
            if (orderBook == null)
            {
                throw new ArgumentNullException(nameof(orderBook));
            }

            var now = DateTime.UtcNow;

            // Get bids: already sorted descending (best bid first) from OKXOrderBook.GetBids()
            var bids = orderBook.GetBids()
                .Select(level => new OrderbookLevel(level.price, level.size))
                .ToList();

            // Get asks: already sorted ascending (best ask first) from OKXOrderBook.GetAsks()
            var asks = orderBook.GetAsks()
                .Select(level => new OrderbookLevel(level.price, level.size))
                .ToList();

            return new Orderbook(orderBook.Symbol, now)
            {
                Bids = bids,
                Asks = asks,
                Levels = Math.Max(bids.Count, asks.Count),
                Value = bids.Count > 0 && asks.Count > 0
                    ? (bids[0].Price + asks[0].Price) / 2m  // Mid-price
                    : 0m
            };
        }
    }
}
