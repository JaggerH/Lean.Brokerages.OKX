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
using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// WebSocket wrapper for OKX data connections
    /// Used by BrokerageMultiWebSocketSubscriptionManager to manage multiple WebSocket connections
    /// Each wrapper tracks subscribed symbols and provides connection identification
    /// </summary>
    public class OKXWebSocketWrapper : WebSocketClientWrapper
    {
        /// <summary>
        /// Unique identifier for this WebSocket connection
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// Optional connection handler for advanced connection management
        /// </summary>
        public IConnectionHandler ConnectionHandler { get; }

        /// <summary>
        /// Creates a new OKX WebSocket wrapper
        /// </summary>
        /// <param name="connectionHandler">Optional connection handler</param>
        public OKXWebSocketWrapper(IConnectionHandler connectionHandler)
        {
            ConnectionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            ConnectionHandler = connectionHandler;
        }

        /// <summary>
        /// Returns a string representation of this WebSocket connection
        /// </summary>
        public override string ToString()
        {
            return $"OKXWebSocket[{ConnectionId}]";
        }
    }
}
