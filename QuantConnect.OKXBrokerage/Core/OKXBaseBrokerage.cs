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
using System.Collections.Concurrent;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.TradingPairs;
using QuantConnect.Util;
using Timer = System.Timers.Timer;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Abstract base class for all OKX Brokerage implementations
    /// Provides common WebSocket management, order state tracking, and data handling
    /// Eliminates IsFuturesModel checks in favor of proper OOP via virtual methods
    /// </summary>
    public abstract partial class OKXBaseBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler, IDataQueueUniverseProvider, IExecutionHistoryProvider
    {
        // ========================================
        // CONSTANTS
        // ========================================

        /// <summary>
        /// Maximum symbols per WebSocket connection
        /// </summary>
        protected const int MaximumSymbolsPerConnection = 15;

        /// <summary>
        /// Maps readable account mode names to OKX API acctLv values.
        /// </summary>
        private static readonly Dictionary<string, string> AccountLevelMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "spot", "1" },
            { "single_currency", "2" },
            { "multi_currency", "3" },
            { "portfolio", "4" },
        };

        /// <summary>
        /// Resolves a config account mode value (e.g. "portfolio", "spot") to the OKX API acctLv ("1"-"4").
        /// </summary>
        protected static string ResolveAccountLevel(string configValue)
        {
            if (AccountLevelMap.TryGetValue(configValue, out var level))
            {
                return level;
            }
            throw new ArgumentException(
                $"Invalid okx-unified-account-mode value: '{configValue}'. " +
                $"Valid values: spot, single_currency, multi_currency, portfolio.");
        }

        // ========================================
        // PROTECTED FIELDS
        // ========================================

        /// <summary>
        /// Data aggregator for consolidating ticks
        /// </summary>
        protected IDataAggregator _aggregator;

        /// <summary>
        /// Symbol mapper for converting between LEAN and broker symbols
        /// </summary>
        protected readonly OKXSymbolMapper _symbolMapper;

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
        /// Rate limiter for WebSocket connections (public + private share the same IP limit).
        /// OKX limit: 3 connections per IP per second.
        /// </summary>
        private readonly RateGate _webSocketConnectionRateLimiter = new(3, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Track processed trade IDs to prevent duplicate fill events.
        /// Per OKX docs: for the same tradeId, only process the first push message.
        /// Value is the UTC expiration time. Entries older than 5 minutes are purged when count exceeds 500.
        /// </summary>
        protected readonly ConcurrentDictionary<string, DateTime> _processedTradeIds = new();

        /// <summary>
        /// Login state tracking
        /// </summary>
        protected volatile bool _isAuthenticated = false;

        /// <summary>
        /// Keep-alive timer for WebSocket heartbeat (Binance pattern)
        /// </summary>
        private Timer _keepAliveTimer;

        /// <summary>
        /// Periodic reconnection timer (23.5 hours)
        /// OKX has scheduled 24-hour disconnection, similar to Binance
        /// </summary>
        private Timer _reconnectTimer;

        /// <summary>
        /// Message handler for synchronizing REST API calls with WebSocket message processing.
        /// Prevents race conditions where WebSocket receives order updates before REST response sets BrokerId.
        /// Following Binance pattern.
        /// </summary>
        private BrokerageConcurrentMessageHandler<WebSocketMessage> _messageHandler;

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
                MaximumSymbolsPerConnection,  // 15 symbols × 2 channels = 30 channels (OKX recommends < 30 for books)
                maximumWebSocketConnections,  // From config, default 0 = unlimited
                null,                         // symbolWeights (null = no weighting)
                () => new OKXWebSocketWrapper(null),  // WebSocket factory
                Subscribe,                    // Subscribe callback
                Unsubscribe,                  // Unsubscribe callback
                OnDataMessage,                // Message handler
                new TimeSpan(23, 45, 0),      // webSocketConnectionDuration (23h 45m, same as Binance)
                _webSocketConnectionRateLimiter);  // shared with private WS: OKX limit 3 connections/IP/second

            SubscriptionManager = subscriptionManager;

            // 4. Initialize REST API client
            RestApiClient = new OKXRestApiClient(apiKey, apiSecret, passphrase);

            // 5. Initialize message handler for REST/WebSocket synchronization (Binance pattern)
            // This prevents race conditions where WebSocket receives fills before REST response sets BrokerId
            _messageHandler = new BrokerageConcurrentMessageHandler<WebSocketMessage>(ProcessPrivateMessage);

            // OKX is USDT-settled; tell BrokerageSetupHandler to auto-set account currency
            AccountBaseCurrency = "USDT";

            // 6. Initialize timers (created once, controlled via Start/Stop - Binance pattern)
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

            // 6. Initialize order book synchronizer
            CreateOrderBookSynchronizer();

            // 7. Initialize price limit synchronizer
            CreatePriceLimitSynchronizer();

            // 8. Attach anonymous WebSocket event handlers (Binance pattern)
            WebSocket.Open += (sender, e) =>
            {
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

            // Connect to private WebSocket (this triggers OnOpen which sends auth request)
            // Rate-limit together with public WS connections — OKX enforces 3 connections/IP/second
            _webSocketConnectionRateLimiter.WaitToProceed();
            ConnectSync();

            // Start periodic reconnection timer (Binance pattern)
            _reconnectTimer.Start();
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
        /// Following Binance pattern: try API first, fallback to base class cached data
        /// </summary>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = RestApiClient.GetAccountHoldings();
            if (holdings.Count > 0)
            {
                return holdings;
            }
            return base.GetAccountHoldings(_job?.BrokerageData, _algorithm?.Securities.Values);
        }

        /// <summary>
        /// Gets all open orders
        /// </summary>
        public override List<Order> GetOpenOrders()
        {
            return RestApiClient.GetOpenOrders();
        }

        /// <summary>
        /// Gets execution history for reconciliation
        /// </summary>
        /// <param name="startTimeUtc">Start time UTC</param>
        /// <param name="endTimeUtc">End time UTC</param>
        /// <returns>List of execution records</returns>
        public List<ExecutionRecord> GetExecutionHistory(DateTime startTimeUtc, DateTime endTimeUtc)
        {
            try
            {
                // Add 5-minute buffer to account for potential timing differences
                var beginMs = new DateTimeOffset(startTimeUtc).ToUnixTimeMilliseconds() - 300000;
                var endMs = new DateTimeOffset(endTimeUtc).ToUnixTimeMilliseconds() + 300000;

                // Get fills for all instrument types
                var fills = RestApiClient.GetExecutionHistory(null, null, beginMs, endMs);

                // Convert to ExecutionRecord
                return fills
                    .Select(fill => fill.ToExecutionRecord(_symbolMapper))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBaseBrokerage.GetExecutionHistory(): Error: {ex.Message}");
                return new List<ExecutionRecord>();
            }
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

                // Pre-create synchronizers to start initialization
                // This creates the OrderBook state before any WebSocket messages arrive
                _orderBookSync?.GetSynchronizer(symbol);
                // Pre-create price limit synchronizer (triggers REST init)
                _priceLimitSync?.GetSynchronizer(symbol);

                // OKX v5 API subscription format: { "op": "subscribe", "args": [ { "channel": "...", "instId": "..." } ] }
                // Subscribe to books channel (400-level orderbook depth)
                // books: 首次推400档快照数据，以后增量推送，每100毫秒推送一次变化的数据
                var bookSubscribeMessage = new Messages.WebSocketMessage
                {
                    Operation = "subscribe",
                    Arguments = new List<object>
                    {
                        new Messages.WebSocketChannel
                        {
                            Channel = "books",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(bookSubscribeMessage));

                // Subscribe to trades channel
                var tradesSubscribeMessage = new Messages.WebSocketMessage
                {
                    Operation = "subscribe",
                    Arguments = new List<object>
                    {
                        new Messages.WebSocketChannel
                        {
                            Channel = "trades",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(tradesSubscribeMessage));

                // Subscribe to price-limit channel (dynamic price limits)
                var priceLimitSubscribeMessage = new Messages.WebSocketMessage
                {
                    Operation = "subscribe",
                    Arguments = new List<object>
                    {
                        new Messages.WebSocketChannel
                        {
                            Channel = "price-limit",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(priceLimitSubscribeMessage));

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
                // Unsubscribe from books channel
                var bookUnsubscribeMessage = new Messages.WebSocketMessage
                {
                    Operation = "unsubscribe",
                    Arguments = new List<object>
                    {
                        new Messages.WebSocketChannel
                        {
                            Channel = "books",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(bookUnsubscribeMessage));

                // Clean up order book synchronizer state
                _orderBookSync?.RemoveState(symbol);
                _orderBooks.TryRemove(symbol, out _);

                // Clean up price limit sync state
                _priceLimitSync?.RemoveState(symbol);

                // Unsubscribe from trades channel
                var tradesUnsubscribeMessage = new Messages.WebSocketMessage
                {
                    Operation = "unsubscribe",
                    Arguments = new List<object>
                    {
                        new Messages.WebSocketChannel
                        {
                            Channel = "trades",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(tradesUnsubscribeMessage));

                // Unsubscribe from price-limit channel
                var priceLimitUnsubscribeMessage = new Messages.WebSocketMessage
                {
                    Operation = "unsubscribe",
                    Arguments = new List<object>
                    {
                        new Messages.WebSocketChannel
                        {
                            Channel = "price-limit",
                            InstrumentId = brokerageSymbol
                        }
                    }
                };
                webSocket.Send(JsonConvert.SerializeObject(priceLimitUnsubscribeMessage));

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
            _messageHandler?.DisposeSafely();
            OrderRateLimiter?.DisposeSafely();
            _orderBookSync?.Dispose();
            _priceLimitSync?.Dispose();

            base.Dispose();
        }
    }
}
