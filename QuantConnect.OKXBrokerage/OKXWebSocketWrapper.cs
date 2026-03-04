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
using System.Timers;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// WebSocket wrapper for OKX public data connections (orderbook/trades/price-limit channels).
    /// Used by BrokerageMultiWebSocketSubscriptionManager to manage multiple WebSocket connections.
    /// Each wrapper tracks subscribed symbols, provides connection identification, and manages its
    /// own heartbeat timer to comply with OKX's 30-second inactivity disconnect requirement.
    /// </summary>
    public class OKXWebSocketWrapper : WebSocketClientWrapper
    {
        /// <summary>
        /// OKX requires a client ping every &lt;30 seconds to keep a connection alive.
        /// 15 seconds provides a comfortable margin below the 30-second timeout.
        /// </summary>
        private const int HeartbeatIntervalMs = 15_000;

        private readonly Timer _heartbeatTimer;

        /// <summary>
        /// Unique identifier for this WebSocket connection
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// Optional connection handler for advanced connection management
        /// </summary>
        public IConnectionHandler ConnectionHandler { get; }

        /// <summary>
        /// Creates a new OKX WebSocket wrapper with a self-contained heartbeat timer.
        /// The timer starts on Open and stops on Closed, matching the connection lifecycle.
        /// </summary>
        /// <param name="connectionHandler">Optional connection handler</param>
        public OKXWebSocketWrapper(IConnectionHandler connectionHandler)
        {
            ConnectionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            ConnectionHandler = connectionHandler;

            _heartbeatTimer = new Timer(HeartbeatIntervalMs) { AutoReset = true };
            _heartbeatTimer.Elapsed += OnHeartbeatElapsed;

            Open   += (_, _) => _heartbeatTimer.Start();
            Closed += (_, _) => _heartbeatTimer.Stop();
        }

        /// <summary>
        /// Sends a "ping" to OKX to keep this connection alive.
        /// OKX responds with "pong" (handled silently by OnDataMessage).
        /// Skips if the connection is not open (e.g. mid-reconnect).
        /// </summary>
        private void OnHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsOpen)
                return;

            try
            {
                Send("ping");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXWebSocketWrapper[{ConnectionId}].Heartbeat(): Error sending ping: {ex.Message}");
            }
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
