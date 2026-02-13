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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using OKXWsMessage = QuantConnect.Brokerages.OKX.Messages.WebSocketMessage;

namespace QuantConnect.Brokerages.OKX.WebSocket
{
    /// <summary>
    /// OKX v5 WebSocket client for real-time market data and order updates
    /// Handles connection, authentication, subscriptions, and message routing
    /// </summary>
    public class OKXWebSocketClient : IDisposable
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _passphrase;
        private readonly bool _isPrivateChannel;
        private readonly string _websocketUrl;

        private IWebSocket _webSocket;
        private readonly OKXWebSocketChannels _channelSubscriptions;
        private readonly object _connectionLock = new object();
        private volatile bool _isConnected;
        private volatile bool _isAuthenticated;
        private Timer _heartbeatTimer;

        private const int HeartbeatIntervalMs = 15000; // 15 seconds
        private const int ConnectionTimeoutMs = 10000; // 10 seconds

        /// <summary>
        /// Gets whether the WebSocket is currently connected
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Event fired when a ticker message is received
        /// </summary>
        public event EventHandler<WebSocketTicker> TickerReceived;

        /// <summary>
        /// Event fired when a trade message is received
        /// </summary>
        public event EventHandler<WebSocketTrade> TradeReceived;

        /// <summary>
        /// Event fired when an orderbook message is received
        /// </summary>
        public event EventHandler<WebSocketOrderBook> OrderBookReceived;

        /// <summary>
        /// Event fired when an order update is received
        /// </summary>
        public event EventHandler<WebSocketOrder> OrderReceived;

        /// <summary>
        /// Event fired when an account update is received
        /// </summary>
        public event EventHandler<WebSocketAccount> AccountReceived;

        /// <summary>
        /// Event fired when a position update is received
        /// </summary>
        public event EventHandler<WebSocketPosition> PositionReceived;

        /// <summary>
        /// Creates a new OKX WebSocket client
        /// </summary>
        /// <param name="websocketUrl">WebSocket URL (public or private channel)</param>
        /// <param name="apiKey">API key (required for private channels)</param>
        /// <param name="apiSecret">API secret (required for private channels)</param>
        /// <param name="passphrase">Passphrase (required for private channels)</param>
        /// <param name="isPrivateChannel">True if this is a private channel requiring authentication</param>
        public OKXWebSocketClient(
            string websocketUrl,
            string apiKey = null,
            string apiSecret = null,
            string passphrase = null,
            bool isPrivateChannel = false)
        {
            _websocketUrl = websocketUrl;
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
            _isPrivateChannel = isPrivateChannel;
            _channelSubscriptions = new OKXWebSocketChannels();

            // Validate credentials for private channels
            if (_isPrivateChannel && (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || string.IsNullOrEmpty(_passphrase)))
            {
                throw new ArgumentException("API credentials are required for private channels");
            }
        }

        /// <summary>
        /// Connects to the WebSocket
        /// </summary>
        public void Connect()
        {
            lock (_connectionLock)
            {
                if (_isConnected)
                {
                    Log.Trace("OKXWebSocketClient.Connect(): Already connected");
                    return;
                }

                try
                {
                    // Create WebSocket
                    _webSocket = new WebSocketClientWrapper();

                    // Subscribe to events
                    _webSocket.Open += OnOpen;
                    _webSocket.Message += OnMessage;
                    _webSocket.Error += OnError;
                    _webSocket.Closed += OnClosed;

                    // Connect
                    Log.Trace($"OKXWebSocketClient.Connect(): Connecting to {_websocketUrl}");
                    _webSocket.Initialize(_websocketUrl);
                    _webSocket.Connect();

                    // Wait for connection
                    var timeout = DateTime.UtcNow.AddMilliseconds(ConnectionTimeoutMs);
                    while (!_isConnected && DateTime.UtcNow < timeout)
                    {
                        Thread.Sleep(100);
                    }

                    if (!_isConnected)
                    {
                        throw new TimeoutException($"WebSocket connection timeout after {ConnectionTimeoutMs}ms");
                    }

                    // Authenticate for private channels
                    if (_isPrivateChannel)
                    {
                        Authenticate();

                        // Wait for authentication
                        timeout = DateTime.UtcNow.AddMilliseconds(ConnectionTimeoutMs);
                        while (!_isAuthenticated && DateTime.UtcNow < timeout)
                        {
                            Thread.Sleep(100);
                        }

                        if (!_isAuthenticated)
                        {
                            throw new Exception("WebSocket authentication failed or timed out");
                        }
                    }

                    // Start heartbeat
                    _heartbeatTimer = new Timer(SendHeartbeat, null, HeartbeatIntervalMs, HeartbeatIntervalMs);

                    Log.Trace($"OKXWebSocketClient.Connect(): Connected successfully (authenticated: {_isAuthenticated})");
                }
                catch (Exception ex)
                {
                    Log.Error($"OKXWebSocketClient.Connect(): Error connecting: {ex.Message}");
                    Disconnect();
                    throw;
                }
            }
        }

        /// <summary>
        /// Disconnects from the WebSocket
        /// </summary>
        public void Disconnect()
        {
            lock (_connectionLock)
            {
                if (!_isConnected && _webSocket == null)
                {
                    return;
                }

                Log.Trace("OKXWebSocketClient.Disconnect(): Disconnecting");

                // Stop heartbeat
                _heartbeatTimer?.DisposeSafely();
                _heartbeatTimer = null;

                // Close WebSocket
                if (_webSocket != null)
                {
                    try
                    {
                        _webSocket.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"OKXWebSocketClient.Disconnect(): Error closing WebSocket: {ex.Message}");
                    }

                    _webSocket = null;
                }

                _isConnected = false;
                _isAuthenticated = false;
                _channelSubscriptions.Clear();

                Log.Trace("OKXWebSocketClient.Disconnect(): Disconnected");
            }
        }

        /// <summary>
        /// Subscribes to a channel
        /// </summary>
        /// <param name="channel">Channel name (e.g., "tickers", "trades", "books5", "orders", "account")</param>
        /// <param name="instId">Instrument ID (e.g., "BTC-USDT"), can be null for account-wide channels</param>
        /// <param name="symbol">LEAN Symbol for message routing (can be null for private channels)</param>
        public void Subscribe(string channel, string instId, Symbol symbol)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("WebSocket not connected");
            }

            // Add to subscriptions (only if we have a valid instId)
            if (!string.IsNullOrEmpty(instId))
            {
                _channelSubscriptions.AddSubscription(channel, instId, symbol);
            }

            // Create subscription message
            var channelArg = new WebSocketChannel
            {
                Channel = channel
            };

            // Only add instId if provided (some private channels don't need it)
            if (!string.IsNullOrEmpty(instId))
            {
                channelArg.InstrumentId = instId;
            }

            var subscribeMessage = new OKXWsMessage
            {
                Operation = "subscribe",
                Arguments = new List<object> { channelArg }
            };

            // Send subscription
            var json = JsonConvert.SerializeObject(subscribeMessage);
            _webSocket.Send(json);
        }

        /// <summary>
        /// Unsubscribes from a channel
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="instId">Instrument ID</param>
        public void Unsubscribe(string channel, string instId)
        {
            if (!_isConnected)
            {
                return;
            }

            // Remove from subscriptions
            _channelSubscriptions.RemoveSubscription(channel, instId);

            // Create unsubscription message
            var unsubscribeMessage = new OKXWsMessage
            {
                Operation = "unsubscribe",
                Arguments = new List<object>
                {
                    new WebSocketChannel
                    {
                        Channel = channel,
                        InstrumentId = instId
                    }
                }
            };

            // Send unsubscription
            var json = JsonConvert.SerializeObject(unsubscribeMessage);
            _webSocket.Send(json);

            Log.Trace($"OKXWebSocketClient.Unsubscribe(): Unsubscribed from {channel}:{instId}");
        }

        /// <summary>
        /// Authenticates the WebSocket connection for private channels
        /// </summary>
        private void Authenticate()
        {
            // Create login signature
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "GET";
            var requestPath = "/users/self/verify";
            var signatureInput = timestamp + method + requestPath;

            var signature = Sign(signatureInput, _apiSecret);

            // Create login message
            var loginMessage = new OKXWsMessage
            {
                Operation = "login",
                Arguments = new List<object>
                {
                    new WebSocketLoginArgs
                    {
                        ApiKey = _apiKey,
                        Passphrase = _passphrase,
                        Timestamp = timestamp,
                        Sign = signature
                    }
                }
            };

            // Send login
            var json = JsonConvert.SerializeObject(loginMessage);
            _webSocket.Send(json);

            Log.Trace("OKXWebSocketClient.Authenticate(): Sent login request");
        }

        /// <summary>
        /// Sends a heartbeat (ping) message
        /// </summary>
        private void SendHeartbeat(object state)
        {
            if (!_isConnected || _webSocket == null)
            {
                return;
            }

            try
            {
                _webSocket.Send("ping");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXWebSocketClient.SendHeartbeat(): Error sending heartbeat: {ex.Message}");
            }
        }

        /// <summary>
        /// Signs a message using HMAC SHA256
        /// </summary>
        private string Sign(string message, string secret)
        {
            var encoding = Encoding.UTF8;
            var keyBytes = encoding.GetBytes(secret);
            var messageBytes = encoding.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        #region WebSocket Event Handlers

        private void OnOpen(object sender, EventArgs e)
        {
            Log.Trace("OKXWebSocketClient.OnOpen(): WebSocket opened");
            _isConnected = true;
        }

        private void OnMessage(object sender, WebSocketMessage message)
        {
            try
            {
                // Extract message text
                if (!(message.Data is WebSocketClientWrapper.TextMessage textMessage))
                {
                    return;
                }

                var json = textMessage.Message;

                // Handle plain text messages (ping/pong)
                if (json == "pong" || json == "ping")
                {
                    return;
                }

                // Skip empty or whitespace messages
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                // Try to parse as JSON
                JObject jObject;
                try
                {
                    jObject = JObject.Parse(json);
                }
                catch (JsonException)
                {
                    // Not JSON, log and ignore
                    Log.Trace($"OKXWebSocketClient.OnMessage(): Received non-JSON message: {json.Substring(0, Math.Min(50, json.Length))}");
                    return;
                }

                // Check for event responses (subscribe, login, error)
                if (jObject["event"] != null)
                {
                    HandleEventResponse(jObject);
                    return;
                }

                // Handle data messages
                if (jObject["arg"] != null && jObject["data"] != null)
                {
                    HandleDataMessage(jObject);
                    return;
                }

                Log.Trace($"OKXWebSocketClient.OnMessage(): Unknown message format: {json.Substring(0, Math.Min(100, json.Length))}");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXWebSocketClient.OnMessage(): Error processing message: {ex.Message}");
            }
        }

        private void OnError(object sender, WebSocketError error)
        {
            Log.Error($"OKXWebSocketClient.OnError(): WebSocket error: {error.Message}");
        }

        private void OnClosed(object sender, WebSocketCloseData closeData)
        {
            Log.Trace($"OKXWebSocketClient.OnClosed(): WebSocket closed (code: {closeData.Code}, reason: {closeData.Reason})");
            _isConnected = false;
            _isAuthenticated = false;
        }

        #endregion

        #region Message Handlers

        private void HandleEventResponse(JObject jObject)
        {
            var response = jObject.ToObject<WebSocketResponse>();

            if (response.Event == "login")
            {
                if (response.Code == "0")
                {
                    _isAuthenticated = true;
                    Log.Trace($"OKXWebSocketClient: Login successful (connId: {response.ConnectionId})");
                }
                else
                {
                    Log.Error($"OKXWebSocketClient: Login failed - Code: {response.Code}, Message: {response.Message}");
                }
            }
            else if (response.Event == "subscribe")
            {
                if (response.Code == "0")
                {
                    Log.Trace("OKXWebSocketClient: Subscription confirmed");
                }
                else
                {
                    Log.Error($"OKXWebSocketClient: Subscription failed - Code: {response.Code}, Message: {response.Message}");
                }
            }
            else if (response.Event == "unsubscribe")
            {
                Log.Trace("OKXWebSocketClient: Unsubscription confirmed");
            }
            else if (response.Event == "error")
            {
                Log.Error($"OKXWebSocketClient: Error event - Code: {response.Code}, Message: {response.Message}");
            }
        }

        private void HandleDataMessage(JObject jObject)
        {
            var arg = jObject["arg"].ToObject<WebSocketChannel>();
            var channel = arg.Channel;

            // Route to appropriate handler based on channel
            switch (channel)
            {
                case "tickers":
                    HandleTickerMessage(jObject);
                    break;
                case "trades":
                    HandleTradeMessage(jObject);
                    break;
                case "books":
                case "books5":
                case "books-l2-tbt":
                    HandleOrderBookMessage(jObject);
                    break;
                case "orders":
                    HandleOrderMessage(jObject);
                    break;
                case "account":
                    HandleAccountMessage(jObject);
                    break;
                case "positions":
                    HandlePositionMessage(jObject);
                    break;
                default:
                    Log.Trace($"OKXWebSocketClient: Unknown channel: {channel}");
                    break;
            }
        }

        private void HandleTickerMessage(JObject jObject)
        {
            var message = jObject.ToObject<WebSocketDataMessage<WebSocketTicker>>();
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var ticker in message.Data)
                {
                    TickerReceived?.Invoke(this, ticker);
                }
            }
        }

        private void HandleTradeMessage(JObject jObject)
        {
            var message = jObject.ToObject<WebSocketDataMessage<WebSocketTrade>>();
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var trade in message.Data)
                {
                    TradeReceived?.Invoke(this, trade);
                }
            }
        }

        private void HandleOrderBookMessage(JObject jObject)
        {
            var message = jObject.ToObject<WebSocketDataMessage<WebSocketOrderBook>>();
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var orderBook in message.Data)
                {
                    OrderBookReceived?.Invoke(this, orderBook);
                }
            }
        }

        private void HandleOrderMessage(JObject jObject)
        {
            var message = jObject.ToObject<WebSocketDataMessage<WebSocketOrder>>();
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var order in message.Data)
                {
                    OrderReceived?.Invoke(this, order);
                }
            }
        }

        private void HandleAccountMessage(JObject jObject)
        {
            var message = jObject.ToObject<WebSocketDataMessage<WebSocketAccount>>();
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var account in message.Data)
                {
                    AccountReceived?.Invoke(this, account);
                }
            }
        }

        private void HandlePositionMessage(JObject jObject)
        {
            var message = jObject.ToObject<WebSocketDataMessage<WebSocketPosition>>();
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var position in message.Data)
                {
                    PositionReceived?.Invoke(this, position);
                }
            }
        }

        #endregion

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }
    }
}
