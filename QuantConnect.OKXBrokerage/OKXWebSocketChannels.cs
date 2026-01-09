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
    /// Tracks channel subscriptions for a OKX v5 WebSocket connection
    /// Maps channel keys to LEAN Symbol objects for message routing
    /// Thread-safe dictionary for concurrent access from WebSocket threads
    /// </summary>
    /// <remarks>
    /// Channel key format: "{channel}:{instId}"
    /// Example: "tickers:BTC-USDT" or "books5:BTC-USDT-SWAP"
    ///
    /// This allows us to route incoming WebSocket messages to the correct Symbol
    /// since OKX sends messages with channel name and instId, but LEAN
    /// needs Symbol objects for data processing
    /// </remarks>
    public class OKXWebSocketChannels : ConcurrentDictionary<string, Symbol>
    {
        /// <summary>
        /// Creates a channel key for subscription tracking
        /// </summary>
        /// <param name="channel">OKX v5 channel name (e.g., "tickers", "trades", "books5", "orders")</param>
        /// <param name="instId">OKX instrument ID (e.g., "BTC-USDT", "BTC-USDT-SWAP")</param>
        /// <returns>Channel key for dictionary lookup</returns>
        public static string CreateChannelKey(string channel, string instId)
        {
            return $"{channel}:{instId}";
        }

        /// <summary>
        /// Adds a symbol subscription to this WebSocket
        /// </summary>
        /// <param name="channel">OKX v5 channel name</param>
        /// <param name="instId">OKX instrument ID</param>
        /// <param name="symbol">LEAN Symbol to route messages to</param>
        public void AddSubscription(string channel, string instId, Symbol symbol)
        {
            var key = CreateChannelKey(channel, instId);
            this[key] = symbol;
        }

        /// <summary>
        /// Removes a symbol subscription from this WebSocket
        /// </summary>
        /// <param name="channel">OKX v5 channel name</param>
        /// <param name="instId">OKX instrument ID</param>
        public void RemoveSubscription(string channel, string instId)
        {
            var key = CreateChannelKey(channel, instId);
            TryRemove(key, out _);
        }

        /// <summary>
        /// Gets the LEAN Symbol for a channel subscription
        /// </summary>
        /// <param name="channel">OKX v5 channel name</param>
        /// <param name="instId">OKX instrument ID</param>
        /// <param name="symbol">Output LEAN Symbol if found</param>
        /// <returns>True if subscription exists, false otherwise</returns>
        public bool TryGetSymbol(string channel, string instId, out Symbol symbol)
        {
            var key = CreateChannelKey(channel, instId);
            return TryGetValue(key, out symbol);
        }
    }
}
