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

using System.Collections.Concurrent;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Tracks channel subscriptions for a OKX WebSocket connection
    /// Maps channel keys to LEAN Symbol objects for message routing
    /// Thread-safe dictionary for concurrent access from WebSocket threads
    /// </summary>
    /// <remarks>
    /// Channel key format: "{channel}:{currency_pair}"
    /// Example: "spot.trades:BTC_USDT" or "futures.order_book_update:BTC_USDT"
    ///
    /// This allows us to route incoming WebSocket messages to the correct Symbol
    /// since OKX sends messages with channel name and currency_pair, but LEAN
    /// needs Symbol objects for data processing
    /// </remarks>
    public class OKXWebSocketChannels : ConcurrentDictionary<string, Symbol>
    {
        /// <summary>
        /// Creates a channel key for subscription tracking
        /// </summary>
        /// <param name="channel">OKX channel name (e.g., "spot.trades", "futures.order_book_update")</param>
        /// <param name="currencyPair">OKX currency pair (e.g., "BTC_USDT")</param>
        /// <returns>Channel key for dictionary lookup</returns>
        public static string CreateChannelKey(string channel, string currencyPair)
        {
            return $"{channel}:{currencyPair}";
        }

        /// <summary>
        /// Adds a symbol subscription to this WebSocket
        /// </summary>
        /// <param name="channel">OKX channel name</param>
        /// <param name="currencyPair">OKX currency pair</param>
        /// <param name="symbol">LEAN Symbol to route messages to</param>
        public void AddSubscription(string channel, string currencyPair, Symbol symbol)
        {
            var key = CreateChannelKey(channel, currencyPair);
            this[key] = symbol;
        }

        /// <summary>
        /// Removes a symbol subscription from this WebSocket
        /// </summary>
        /// <param name="channel">OKX channel name</param>
        /// <param name="currencyPair">OKX currency pair</param>
        public void RemoveSubscription(string channel, string currencyPair)
        {
            var key = CreateChannelKey(channel, currencyPair);
            TryRemove(key, out _);
        }

        /// <summary>
        /// Gets the LEAN Symbol for a channel subscription
        /// </summary>
        /// <param name="channel">OKX channel name</param>
        /// <param name="currencyPair">OKX currency pair</param>
        /// <param name="symbol">Output LEAN Symbol if found</param>
        /// <returns>True if subscription exists, false otherwise</returns>
        public bool TryGetSymbol(string channel, string currencyPair, out Symbol symbol)
        {
            var key = CreateChannelKey(channel, currencyPair);
            return TryGetValue(key, out symbol);
        }
    }
}
