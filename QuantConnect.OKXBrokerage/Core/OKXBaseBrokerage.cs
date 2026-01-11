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
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using Timer = System.Timers.Timer;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Abstract base class for all OKX Brokerage implementations
    /// Provides common WebSocket management, order state tracking, and data handling
    /// Eliminates IsFuturesModel checks in favor of proper OOP via virtual methods
    /// </summary>
    public abstract partial class OKXBaseBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler, IDataQueueUniverseProvider
    {
        // ========================================
        // CONSTANTS
        // ========================================

        /// <summary>
        /// Maximum symbols per WebSocket connection
        /// </summary>
        protected const int MaximumSymbolsPerConnection = 512;

        // ========================================
        // PROTECTED FIELDS
        // ========================================

        /// <summary>
        /// Data aggregator for consolidating ticks
        /// </summary>
        protected readonly IDataAggregator _aggregator;

        /// <summary>
        /// Symbol mapper for converting between LEAN and broker symbols
        /// </summary>
        protected readonly ISymbolMapper _symbolMapper;

        /// <summary>
        /// Algorithm instance
        /// </summary>
        protected IAlgorithm _algorithm;

        /// <summary>
        /// Live job packet
        /// </summary>
        protected LiveNodePacket _job;

        /// <summary>
        /// Rate limiter for order operations (PlaceOrder, CancelOrder, UpdateOrder)
        /// Spot: 10 orders per second, Futures: 100 orders per second (overridden in subclass)
        /// </summary>
        protected virtual RateGate OrderRateLimiter { get; } = new(10, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Track pending orders by WebSocket request_id
        /// </summary>
        protected readonly System.Collections.Concurrent.ConcurrentDictionary<string, Order> _pendingOrdersByRequestId = new();

        /// <summary>
        /// Track cumulative fill quantities
        /// </summary>
        protected readonly System.Collections.Concurrent.ConcurrentDictionary<int, decimal> _fills = new();

        /// <summary>
        /// Fast reverse lookup: OKX Brokerage Order ID -> LEAN Order
        /// </summary>
        protected readonly System.Collections.Concurrent.ConcurrentDictionary<string, Order> _ordersByBrokerId = new();

        /// <summary>
        /// Track processed trade IDs to prevent duplicate fill events.
        /// Per OKX docs: for the same tradeId, only process the first push message.
        /// Uses MemoryCache with auto-expiration (5 minutes) to prevent unbounded growth.
        /// </summary>
        protected readonly IMemoryCache _processedTradeIds = new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// Login state tracking
        /// </summary>
        protected volatile bool _isAuthenticated = false;

        /// <summary>
        /// Tracks whether a Reconnect notification needs to be sent on next successful authentication.
        /// Set to true when Disconnect is sent, reset to false after Reconnect is sent.
        /// </summary>
        protected volatile bool _reconnectNotificationPending = false;

        /// <summary>
        /// Keep-alive timer for WebSocket heartbeat (Binance pattern)
        /// </summary>
        private Timer _keepAliveTimer;

        /// <summary>
        /// Periodic reconnection timer (23.5 hours)
        /// OKX has scheduled 24-hour disconnection, similar to Binance
        /// </summary>
        private Timer _reconnectTimer;

        // ========================================
        // PROTECTED FIELDS
        // ========================================

        /// <summary>
        /// API passphrase for authentication (OKX-specific)
        /// </summary>
        protected string Passphrase { get; set; }

        /// <summary>
        /// REST API client for OKX API v5
        /// </summary>
        protected OKXRestApiClient RestApiClient { get; private set; }

        // ========================================
        // PUBLIC PROPERTIES
        // ========================================

        /// <summary>
        /// Returns true if connected to the broker
        /// Checks if WebSocket is connected (for real-time updates)
        /// Note: REST API is always available after initialization, but WebSocket is needed for OrderEvents
        /// </summary>
        public override bool IsConnected => WebSocket?.IsOpen == true;

        /// <summary>
        /// Gets the account base currency (USDT for OKX)
        /// </summary>
        public new string AccountBaseCurrency => "USDT";

        // ========================================
        // CONSTRUCTORS
        // ========================================

        /// <summary>
        /// Parameterless constructor for brokerages implementing IDataQueueHandler
        /// Used when brokerage is created via SetJob() pattern
        /// </summary>
        protected OKXBaseBrokerage()
            : this(Composer.Instance.GetPart<IDataAggregator>())
        {
        }

        /// <summary>
        /// Creates a new brokerage instance with data aggregator
        /// Used for DataQueueHandler-only mode
        /// </summary>
        /// <param name="aggregator">Data aggregator for consolidating ticks</param>
        protected OKXBaseBrokerage(IDataAggregator aggregator)
            : base("OKXBaseBrokerage")
        {
            _aggregator = aggregator;
            _symbolMapper = new OKXSymbolMapper(Market.OKX);
        }

        /// <summary>
        /// Creates a new brokerage instance with full configuration
        /// Calls Initialize() to complete setup (Binance pattern)
        /// </summary>
        /// <param name="apiKey">OKX API key</param>
        /// <param name="apiSecret">OKX API secret</param>
        /// <param name="passphrase">OKX API passphrase</param>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="aggregator">Data aggregator</param>
        /// <param name="job">Live job packet</param>
        protected OKXBaseBrokerage(string apiKey, string apiSecret, string passphrase, IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
            : base("OKXBaseBrokerage")
        {
            _aggregator = aggregator;
            _symbolMapper = new OKXSymbolMapper(Market.OKX);

            // Call Initialize to complete setup
            Initialize(apiKey, apiSecret, passphrase, algorithm, aggregator, job);
        }

        /// <summary>
        /// Initializes the brokerage with credentials and configuration
        /// Following Binance pattern: called from constructor and SetJobInit()
        /// </summary>
        /// <param name="apiKey">OKX API key</param>
        /// <param name="apiSecret">OKX API secret</param>
        /// <param name="passphrase">OKX API passphrase</param>
        /// <param name="algorithm">Algorithm instance (can be null for DataQueueHandler mode)</param>
        /// <param name="aggregator">Data aggregator</param>
        /// <param name="job">Live job packet</param>
        protected virtual void Initialize(
            string apiKey,
            string apiSecret,
            string passphrase,
            IAlgorithm algorithm,
            IDataAggregator aggregator,
            LiveNodePacket job)
        {
            if (IsInitialized)
            {
                return;
            }

            // Get WebSocket URLs from OKXEnvironment
            // OKX requires separate WebSocket connections for public (market data) and private (orders/account) channels
            var privateWssUrl = OKXEnvironment.GetWebSocketPrivateUrl();  // For authentication and orders channel
            var publicWssUrl = OKXEnvironment.GetWebSocketPublicUrl();    // For market data subscriptions

            // 1. Call base initialization first (establishes WebSocket infrastructure, stores apiKey/apiSecret)
            // Use factory method for WebSocket to allow test injection
            // IMPORTANT: Use private URL for base WebSocket - it handles authentication and private channels (orders, account)
            base.Initialize(privateWssUrl, CreateWebSocket(), null, apiKey, apiSecret);

            // 2. Set instance variables (after base.Initialize, following Binance pattern)
            _algorithm = algorithm;
            _job = job;
            Passphrase = passphrase;

            // 3. Initialize SubscriptionManager for market data (public channels)
            // Note: Private channels (orders, balances) use BaseWebsocketsBrokerage.WebSocket directly
            var maximumWebSocketConnections = Config.GetInt("okx-maximum-websocket-connections", 0);

            var subscriptionManager = new BrokerageMultiWebSocketSubscriptionManager(
                publicWssUrl,
                MaximumSymbolsPerConnection,  // 512 (estimated from OKX 64KB limit)
                maximumWebSocketConnections,  // From config, default 0 = unlimited
                null,                         // symbolWeights (null = no weighting)
                () => new OKXWebSocketWrapper(null),  // WebSocket factory
                Subscribe,                    // Subscribe callback
                Unsubscribe,                  // Unsubscribe callback
                OnDataMessage,                // Message handler
                new TimeSpan(23, 45, 0));     // webSocketConnectionDuration (23h 45m, same as Binance)

            SubscriptionManager = subscriptionManager;

            // 4. Initialize REST API client
            RestApiClient = new OKXRestApiClient(apiKey, apiSecret, passphrase);

            // 5. Initialize timers (created once, controlled via Start/Stop - Binance pattern)
            // Send heartbeat every 15 seconds
            _keepAliveTimer = new Timer
            {
                Interval = 15 * 1000  // 15 seconds
            };
            _keepAliveTimer.Elapsed += (s, e) => SendHeartbeat();

            // OKX has 24-hour scheduled disconnect
            // Reconnect before that to maintain connection
            _reconnectTimer = new Timer
            {
                Interval = 23.5 * 60 * 60 * 1000  // 23.5 hours
            };
            _reconnectTimer.Elapsed += (s, e) =>
            {
                Log.Trace($"{GetType().Name}: Daily websocket restart: disconnect");
                Disconnect();

                Log.Trace($"{GetType().Name}: Daily websocket restart: connect");
                Connect();
            };

            // 6. Attach anonymous WebSocket event handlers (Binance pattern)
            WebSocket.Open += (sender, e) =>
            {
                Log.Trace($"{GetType().Name}.WebSocket.Open: Connection established");
                _keepAliveTimer.Start();

                // Reset auth state and send authentication request (non-blocking)
                // OnMessage will handle the response and call OnAuthenticationSuccess()
                _isAuthenticated = false;
                SendAuthenticationRequest();
            };

            WebSocket.Closed += (sender, e) =>
            {
                Log.Trace($"{GetType().Name}.WebSocket.Closed: Code={e.Code}, Reason={e.Reason}, WasClean={e.WasClean}");
                _keepAliveTimer.Stop();

                if (e.WasClean)
                {
                    OnMessage(new BrokerageMessageEvent(
                        BrokerageMessageType.Information,
                        e.Code,
                        $"{GetType().Name} WebSocket connection closed cleanly: {e.Reason}"
                    ));
                }
            };

            Log.Trace($"{GetType().Name}.Initialize(): Initialization complete (Environment: {OKXEnvironment.GetEnvironmentName()})");
        }
        /// <summary>
        /// Creates the WebSocket client instance
        /// Override in test classes to inject mock WebSocket
        /// </summary>
        /// <returns>WebSocket client instance</returns>
        protected virtual IWebSocket CreateWebSocket()
        {
            return new WebSocketClientWrapper();
        }

        // ========================================
        // CONNECTION LIFECYCLE (Template Method Pattern)
        // ========================================

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// Assumes Initialize() has already been called (via constructor or SetJob)
        /// Authentication is handled asynchronously: OnOpen sends auth request, OnMessage confirms it
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            // Validate unified account mode if configured (optional, override in subclass)
            ValidateAccountMode();

            // Connect to WebSocket (this triggers OnOpen which sends auth request)
            Log.Trace($"{GetType().Name}.Connect(): Connecting to WebSocket...");
            ConnectSync();

            // Start periodic reconnection timer (Binance pattern)
            _reconnectTimer.Start();

            Log.Trace($"{GetType().Name}.Connect(): WebSocket connected, authentication in progress...");
        }

        /// <summary>
        /// Validates account mode configuration (optional, can be overridden)
        /// </summary>
        protected virtual void ValidateAccountMode()
        {
            // Default: no validation
            // OKXUnifiedBrokerage will override this to validate unified account settings
        }

        /// <summary>
        /// Sends authentication request to WebSocket
        /// </summary>
        protected abstract void SendAuthenticationRequest();

        /// <summary>
        /// Subscribes to private channels (orders, trades, balances)
        /// Subclasses override this method to send subscription requests
        /// </summary>
        protected abstract void SubscribePrivateChannels();

        /// <summary>
        /// Sends a private channel subscription request
        /// This method is called by subclasses to send subscription requests
        /// </summary>
        /// <param name="channel">Channel name (e.g., "spot.orders", "futures.balances")</param>
        /// <param name="payload">Subscription payload (contract names or parameters)</param>
        protected void SendPrivateChannelSubscription(string channel, string[] payload)
        {
            // Generate authentication
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var auth = RestApiClient.GenerateWebSocketAuth(channel, "subscribe", timestamp);

            // Build subscription request
            var subscribeRequest = new
            {
                time = timestamp,
                channel = channel,
                @event = "subscribe",
                payload = payload,
                auth = auth
            };

            // Send request
            var message = JsonConvert.SerializeObject(subscribeRequest);
            WebSocket.Send(message);

            Log.Trace($"{GetType().Name}.SendPrivateChannelSubscription(): Sent subscription request for {channel}");
        }

        /// <summary>
        /// Disconnects from the broker
        /// </summary>
        public override void Disconnect()
        {
            Log.Trace($"{GetType().Name}.Disconnect(): Disconnecting...");

            // Stop timers (Binance pattern)
            _keepAliveTimer.Stop();
            _reconnectTimer.Stop();

            // Close WebSocket connection
            WebSocket?.Close();

            // Reset authentication state
            _isAuthenticated = false;

            Log.Trace($"{GetType().Name}.Disconnect(): Disconnected successfully");
        }

        /// <summary>
        /// Unsubscribes from private channels
        /// </summary>
        protected abstract void UnsubscribePrivateChannels();

        /// <summary>
        /// Unsubscribes from a specific channel
        /// Default implementation handles both market data and private channels
        /// Subclasses can override if they need special handling
        /// </summary>
        /// <param name="channel">Channel name (e.g., "spot.trades", "futures.orders")</param>
        /// <param name="payload">Payload parameter (currency pair, contract, "!all", or null for empty payload)</param>
        protected virtual void UnsubscribeChannel(string channel, string payload)
        {
            try
            {
                // Check WebSocket availability before attempting to unsubscribe
                // This prevents NullReferenceException during concurrent disconnect/reconnect
                if (WebSocket == null || !WebSocket.IsOpen)
                {
                    Log.Trace($"{GetType().Name}.UnsubscribeChannel({channel}): WebSocket not available, skipping unsubscribe");
                    return;
                }

                // Construct payload array based on parameter
                // null → empty array (for account-level channels like balances)
                // non-null → single-element array (for symbol-specific or "!all" channels)
                var payloadArray = payload == null ? Array.Empty<string>() : new[] { payload };

                var unsubscribeRequest = new
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    channel = channel,
                    @event = "unsubscribe",
                    payload = payloadArray
                };

                var message = JsonConvert.SerializeObject(unsubscribeRequest);

                // Double-check WebSocket is still available before sending
                // Guards against race condition where WebSocket closes between checks
                if (WebSocket != null && WebSocket.IsOpen)
                {
                    WebSocket.Send(message);
                    Log.Trace($"{GetType().Name}.UnsubscribeChannel(): Unsubscribed from {channel}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.UnsubscribeChannel({channel}): Error: {ex}");
            }
        }

        /// <summary>
        /// Sends heartbeat ping to WebSocket
        /// </summary>
        private void SendHeartbeat()
        {
            try
            {
                if (!IsConnected)
                {
                    return;
                }

                // Check for connection timeout
                var timeSinceLastMessage = DateTime.UtcNow - _lastMessageTime;
                if (timeSinceLastMessage.TotalSeconds > 60)
                {
                    // Stop the heartbeat timer to prevent repeated timeout detections
                    _keepAliveTimer.Stop();

                    TriggerReconnect("HeartbeatTimeout", $"WebSocket heartbeat timeout: No messages received for {timeSinceLastMessage.TotalSeconds}s");
                    return;
                }

                // OKX WebSocket expects simple "ping" string (not JSON)
                WebSocket.Send("ping");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.SendHeartbeat(): Error: {ex}");
            }
        }

        // ========================================
        // BROKERAGE INTERFACE IMPLEMENTATION
        // ========================================

        /// <summary>
        /// Gets account cash balances
        /// </summary>
        public override List<CashAmount> GetCashBalance()
        {
            return RestApiClient.GetCashBalance();
        }

        /// <summary>
        /// Gets account holdings
        /// </summary>
        public override List<Holding> GetAccountHoldings()
        {
            return RestApiClient.GetAccountHoldings();
        }

        /// <summary>
        /// Gets all open orders
        /// </summary>
        public override List<Order> GetOpenOrders()
        {
            return RestApiClient.GetOpenOrders();
        }


        // ========================================
        // SUBSCRIPTIONMANAGER CALLBACKS
        // ========================================

        /// <summary>
        /// Subscribe callback for BrokerageMultiWebSocketSubscriptionManager (public channels only)
        /// Called when market data subscription is requested
        /// Note: Private channels (orders, balances) use BaseWebsocketsBrokerage.WebSocket directly
        /// </summary>
        /// <param name="webSocket">WebSocket instance from SubscriptionManager (NOT BaseWebsocketsBrokerage.WebSocket)</param>
        /// <param name="symbol">Symbol to subscribe</param>
        /// <returns>True if subscription successful</returns>
        private bool Subscribe(IWebSocket webSocket, Symbol symbol)
        {
            try
            {
                var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);

                // Initialize order book context for this symbol (if not already exists)
                var orderBookContext = _orderBookContexts.GetOrAdd(symbol, _ =>
                {
                    var newContext = new OrderBookContext(this, symbol, brokerageSymbol, _orderBookDepth);
                    _orderBooks[symbol] = newContext.OrderBook;
                    return newContext;
                });

                // OKX v5 API subscription format: { "op": "subscribe", "args": [ { "channel": "...", "instId": "..." } ] }
                // Subscribe to books5 channel (5-level orderbook depth)
                var bookSubscribeMessage = new Messages.OKXWebSocketMessage
                {
                    Operation = "subscribe",
                    Arguments = new List<object>
                    {
                        new Messages.OKXWebSocketChannel
                        {
                            Channel = "books5",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(bookSubscribeMessage));

                // Subscribe to trades channel
                var tradesSubscribeMessage = new Messages.OKXWebSocketMessage
                {
                    Operation = "subscribe",
                    Arguments = new List<object>
                    {
                        new Messages.OKXWebSocketChannel
                        {
                            Channel = "trades",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(tradesSubscribeMessage));

                Log.Trace($"{GetType().Name}.Subscribe(): Subscribed {symbol} to books5 and trades");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.Subscribe({symbol}): {ex}");
                return false;
            }
        }

        /// <summary>
        /// Unsubscribe callback for BrokerageMultiWebSocketSubscriptionManager (public channels only)
        /// Called when market data unsubscription is requested
        /// </summary>
        /// <param name="webSocket">WebSocket instance from SubscriptionManager</param>
        /// <param name="symbol">Symbol to unsubscribe</param>
        /// <returns>True if unsubscription successful</returns>
        private bool Unsubscribe(IWebSocket webSocket, Symbol symbol)
        {
            try
            {
                var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);

                // OKX v5 API unsubscription format
                // Unsubscribe from books5 channel
                var bookUnsubscribeMessage = new Messages.OKXWebSocketMessage
                {
                    Operation = "unsubscribe",
                    Arguments = new List<object>
                    {
                        new Messages.OKXWebSocketChannel
                        {
                            Channel = "books5",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(bookUnsubscribeMessage));

                // Clean up order book contexts
                if (_orderBookContexts.TryRemove(symbol, out var context))
                {
                    try
                    {
                        // Signal cancellation
                        context.CancellationToken?.Cancel();

                        // Complete the Channel writer (no more messages)
                        context.MessageChannel?.Writer.Complete();

                        // Wait for consumer task to finish (with timeout)
                        context.ConsumerTask?.Wait(TimeSpan.FromSeconds(5));

                        // Dispose resources
                        context.CancellationToken?.Dispose();

                        Log.Trace($"{GetType().Name}.Unsubscribe(): Cleaned up OrderBookContext for {symbol}");
                    }
                    catch (Exception cleanupEx)
                    {
                        Log.Error($"{GetType().Name}.Unsubscribe({symbol}): Cleanup error: {cleanupEx.Message}");
                    }
                }
                _orderBooks.TryRemove(symbol, out _);

                // Unsubscribe from trades channel
                var tradesUnsubscribeMessage = new Messages.OKXWebSocketMessage
                {
                    Operation = "unsubscribe",
                    Arguments = new List<object>
                    {
                        new Messages.OKXWebSocketChannel
                        {
                            Channel = "trades",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(tradesUnsubscribeMessage));

                Log.Trace($"{GetType().Name}.Unsubscribe(): Unsubscribed {symbol} from books5 and trades");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.Unsubscribe({symbol}): {ex}");
                return false;
            }
        }

        /// <summary>
        /// Message handler for BrokerageMultiWebSocketSubscriptionManager (public channels only)
        /// Processes market data messages from SubscriptionManager WebSocket instances
        /// Note: Private channel messages are handled by OnMessage() → RouteMessage()
        /// </summary>
        /// <param name="webSocketMessage">Message from SubscriptionManager WebSocket</param>
        private void OnDataMessage(WebSocketMessage webSocketMessage)
        {
            try
            {
                var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;

                // Reuse existing ProcessMessage logic for message parsing and routing
                // This ensures consistent handling of all message types (errors, subscription confirmations, data updates)
                ProcessMessage(e.Message);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.OnDataMessage(): Error processing message: {ex}");
            }
        }

        // ========================================
        // HELPER METHODS
        // ========================================

        /// <summary>
        /// Checks if the specified symbol can be subscribed to
        /// </summary>
        protected virtual bool CanSubscribe(Symbol symbol)
        {
            if (symbol.Value.IndexOfInvariant("universe", true) != -1 || symbol.IsCanonical())
            {
                return false;
            }

            // OKX supports both Crypto (spot) and CryptoFuture (perpetual swaps, futures)
            return symbol.SecurityType == SecurityType.Crypto || symbol.SecurityType == SecurityType.CryptoFuture;
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public override void Dispose()
        {
            _keepAliveTimer?.DisposeSafely();
            _reconnectTimer?.DisposeSafely();
            OrderRateLimiter?.DisposeSafely();

            base.Dispose();
        }
    }
}
