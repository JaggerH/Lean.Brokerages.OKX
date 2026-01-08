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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - Data Queue Handler Implementation
    /// Provides market data subscription management
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        // ========================================
        // BALANCE CACHE
        // ========================================

        /// <summary>
        /// Cache for account balances (updated via WebSocket)
        /// Key: Currency symbol (e.g., "USDT", "BTC")
        /// Value: Available balance amount
        /// Note: Subclasses populate this cache in their HandleBalancesMessage/OnBalanceUpdate implementations
        /// </summary>
        protected readonly ConcurrentDictionary<string, decimal> _balanceCache = new();

        // ========================================
        // ORDER BOOK MANAGEMENT
        // ========================================

        /// <summary>
        /// Order book management for each symbol
        /// </summary>
        protected readonly ConcurrentDictionary<Symbol, OKXOrderBook> _orderBooks = new();

        /// <summary>
        /// Order book depth configuration (default: 100 levels)
        /// </summary>
        protected int _orderBookDepth = 100;

        /// <summary>
        /// Order book state management (for incremental update synchronization)
        /// </summary>
        protected readonly ConcurrentDictionary<Symbol, OrderBookContext> _orderBookContexts = new();

        /// <summary>
        /// Ticker quote cache for lightweight bid/ask from spot.tickers/futures.tickers channel
        /// Used as fallback when full order book is not subscribed
        /// </summary>
        protected readonly ConcurrentDictionary<Symbol, TickerQuote> _lastTickers = new();

        /// <summary>
        /// Cached ticker quote data from spot.tickers/futures.tickers channel
        /// Stores the most recent bid/ask prices for symbols without full order book subscription
        /// </summary>
        protected class TickerQuote
        {
            public decimal BidPrice { get; set; }
            public decimal AskPrice { get; set; }
            public DateTime UpdateTime { get; set; }
        }

        /// <summary>
        /// Enum representing the state of order book initialization and synchronization
        /// </summary>
        protected enum OrderBookState
        {
            /// <summary>
            /// Initial state - WebSocket subscribed, caching messages, waiting for REST snapshot
            /// </summary>
            Initializing,

            /// <summary>
            /// REST snapshot received, applying cached messages to synchronize with live stream
            /// </summary>
            Syncing,

            /// <summary>
            /// Fully synchronized - applying incremental updates in real-time
            /// </summary>
            Synchronized,

            /// <summary>
            /// Error detected (e.g., sequence gap) - needs reinitialization
            /// </summary>
            Error
        }

        /// <summary>
        /// Context for managing order book state, sequence IDs, and cached updates
        /// </summary>
        protected class OrderBookContext
        {
            /// <summary>
            /// Reference to the parent brokerage instance
            /// </summary>
            public OKXBaseBrokerage Brokerage { get; set; }

            /// <summary>
            /// The order book instance
            /// </summary>
            public OKXOrderBook OrderBook { get; set; }

            /// <summary>
            /// Current synchronization state
            /// </summary>
            public OrderBookState State { get; set; }

            /// <summary>
            /// Last processed update ID (u field from WebSocket messages)
            /// Used to validate sequence continuity
            /// </summary>
            public long LastUpdateId { get; set; }

            /// <summary>
            /// Base ID from REST API snapshot
            /// Set when snapshot is retrieved
            /// </summary>
            public long BaseId { get; set; }

            /// <summary>
            /// Channel for message passing between WebSocket handler and processor
            /// Replaces CachedUpdates - Channel naturally buffers messages
            /// </summary>
            public System.Threading.Channels.Channel<Messages.OrderBookUpdate> MessageChannel { get; set; }

            /// <summary>
            /// Currency pair for this order book (e.g., BTC_USDT)
            /// </summary>
            public string CurrencyPair { get; set; }

            /// <summary>
            /// LEAN Symbol for this order book
            /// </summary>
            public Symbol Symbol { get; set; }

            /// <summary>
            /// Timestamp of the last update (for monitoring)
            /// </summary>
            public DateTime LastUpdateTime { get; set; }

            /// <summary>
            /// Lock for thread-safe state transitions
            /// </summary>
            public readonly object Lock = new object();

            /// <summary>
            /// Cancellation token for consumer task
            /// </summary>
            public CancellationTokenSource CancellationToken { get; set; }

            /// <summary>
            /// Consumer task for processing order book updates
            /// </summary>
            public Task ConsumerTask { get; set; }

            /// <summary>
            /// Initialization task (async snapshot fetch)
            /// </summary>
            public Task InitializationTask { get; set; }

            /// <summary>
            /// Maximum number of updates to cache during initialization
            /// </summary>
            public const int MaxCacheSize = 100;

            /// <summary>
            /// Constructor
            /// </summary>
            public OrderBookContext(OKXBaseBrokerage brokerage, Symbol symbol, string currencyPair, int maxDepth = 100)
            {
                Brokerage = brokerage;
                Symbol = symbol;
                CurrencyPair = currencyPair;
                OrderBook = new OKXOrderBook(symbol, maxDepth);
                State = OrderBookState.Initializing;
                LastUpdateId = 0;
                BaseId = 0;
                LastUpdateTime = DateTime.UtcNow;

                // Create bounded Channel with DropOldest policy
                MessageChannel = System.Threading.Channels.Channel.CreateBounded<Messages.OrderBookUpdate>(
                    new BoundedChannelOptions(500)
                    {
                        FullMode = BoundedChannelFullMode.DropOldest,
                        SingleReader = true,
                        SingleWriter = true
                    });

                CancellationToken = new CancellationTokenSource();

                // Start consumer task immediately (will wait for State change)
                ConsumerTask = System.Threading.Tasks.Task.Run(() => Brokerage.ProcessOrderBookUpdatesAsync(this))
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Log.Error($"OKXBaseBrokerage.OrderBookContext: Consumer task faulted for {Symbol}: {task.Exception?.GetBaseException()?.Message}");
                        }
                    }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

                // Start async initialization (does not block subscription flow)
                InitializationTask = System.Threading.Tasks.Task.Run(() => Brokerage.InitializeOrderBookAsync(this))
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Log.Error($"OKXBaseBrokerage.OrderBookContext: Initialization task faulted for {Symbol}: {task.Exception?.GetBaseException()?.Message}");
                            lock (Lock)
                            {
                                State = OrderBookState.Error;
                            }
                        }
                    }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// Context for managing Quote tick initialization via REST API
        /// Tracks whether initial REST ticker data has been fetched and applied
        /// </summary>
        protected class QuoteTickContext
        {
            /// <summary>
            /// Lock for thread-safe state updates
            /// </summary>
            public readonly object Lock = new object();

            /// <summary>
            /// LEAN symbol
            /// </summary>
            public Symbol Symbol { get; }

            /// <summary>
            /// OKX currency pair or contract name (e.g., BTC_USDT)
            /// </summary>
            public string CurrencyPair { get; }

            /// <summary>
            /// Whether WebSocket has received any quote data yet
            /// </summary>
            public bool HasReceivedWebSocketData { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            public QuoteTickContext(Symbol symbol, string currencyPair)
            {
                Symbol = symbol;
                CurrencyPair = currencyPair;
                HasReceivedWebSocketData = false;
            }
        }

        /// <summary>
        /// Quote tick initialization tracking for each symbol
        /// </summary>
        protected readonly ConcurrentDictionary<Symbol, QuoteTickContext> _quoteTickContexts = new();

        // ========================================
        // IDataQueueHandler IMPLEMENTATION
        // ========================================

        /// <summary>
        /// Sets the job we're subscribing for
        /// Following Binance pattern: calls SetJobInit() then Connect()
        /// </summary>
        /// <param name="job">Job we're processing</param>
        public void SetJob(LiveNodePacket job)
        {
            SetJobInit(job, _aggregator);

            if (!IsConnected)
            {
                Connect();
            }
        }

        /// <summary>
        /// Initializes the brokerage from job packet
        /// Following Binance pattern: reads credentials from BrokerageData and calls Initialize()
        /// Can be overridden by subclasses for additional initialization
        /// </summary>
        /// <param name="job">Live job packet containing brokerage configuration</param>
        /// <param name="aggregator">Data aggregator</param>
        protected virtual void SetJobInit(LiveNodePacket job, IDataAggregator aggregator)
        {
            Initialize(
                apiKey: job.BrokerageData["okx-api-key"],
                apiSecret: job.BrokerageData["okx-api-secret"],
                algorithm: null,  // DataQueueHandler mode, no algorithm
                aggregator: aggregator,
                job: job
            );
        }

        
        /// <summary>
        /// Not used - subscriptions are handled by BrokerageMultiWebSocketSubscriptionManager
        /// Required override from BaseWebsocketsBrokerage abstract method
        /// </summary>
        /// <param name="symbols">Symbols to subscribe</param>
        /// <returns>True always</returns>
        protected override bool Subscribe(IEnumerable<Symbol> symbols)
        {
            // NOP - subscriptions handled by SubscriptionManager
            return true;
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription configuration</param>
        /// <param name="newDataAvailableHandler">Handler for new data</param>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return null;
            }

            // Add to aggregator
            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);

            // Deleokx to SubscriptionManager (triggers Subscribe callback synchronously)
            SubscriptionManager.Subscribe(dataConfig);

            Log.Trace($"{GetType().Name}.Subscribe(): Subscribed {dataConfig.Symbol} via SubscriptionManager");

            return enumerator;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription configuration to remove</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return;
            }

            Log.Trace($"{GetType().Name}.Unsubscribe(): {dataConfig.Symbol}");

            // Deleokx to SubscriptionManager (triggers Unsubscribe callback)
            SubscriptionManager.Unsubscribe(dataConfig);

            Log.Trace($"{GetType().Name}.Unsubscribe(): Unsubscribed {dataConfig.Symbol} via SubscriptionManager");
        }

        // ========================================
        // IDataQueueUniverseProvider IMPLEMENTATION
        // ========================================

        /// <summary>
        /// Returns whether selection can take place or not
        /// </summary>
        /// <returns>True if selection can be performed</returns>
        public bool CanPerformSelection()
        {
            // LookupSymbols uses REST API (doesn't require WebSocket)
            // Verify brokerage is initialized
            return RestApiClient != null;
        }

        /// <summary>
        /// Lookup symbols matching specified criteria
        /// </summary>
        /// <param name="symbol">The symbol to lookup</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency</param>
        /// <returns>Matching symbols</returns>
        public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
        {
            Log.Trace($"{GetType().Name}.LookupSymbols(): {symbol} includeExpired={includeExpired} securityCurrency={securityCurrency}");

            // Use REST API to fetch available symbols
            return RestApiClient.LookupSymbols(symbol, includeExpired, securityCurrency);
        }

        // ========================================
        // ORDER BOOK ACCESSORS
        // ========================================

        /// <summary>
        /// Gets the order book for a specific symbol
        /// This is useful for arbitrage strategies to access full depth data
        /// </summary>
        /// <param name="symbol">The symbol to get the order book for</param>
        /// <returns>The order book if available, null otherwise</returns>
        public OKXOrderBook GetOrderBook(Symbol symbol)
        {
            return _orderBooks.TryGetValue(symbol, out var orderBook) ? orderBook : null;
        }

        /// <summary>
        /// Gets the best bid/ask prices from the order book
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="bestBid">Output: best bid price</param>
        /// <param name="bestAsk">Output: best ask price</param>
        /// <param name="bidSize">Output: best bid size</param>
        /// <param name="askSize">Output: best ask size</param>
        /// <returns>True if order book data is available</returns>
        public bool TryGetBestBidAsk(Symbol symbol, out decimal bestBid, out decimal bestAsk,
            out decimal bidSize, out decimal askSize)
        {
            bestBid = bestAsk = bidSize = askSize = 0;

            // First, try to get data from order book (full depth - preferred when available)
            if (_orderBooks.TryGetValue(symbol, out var orderBook))
            {
                bestBid = orderBook.BestBidPrice;
                bestAsk = orderBook.BestAskPrice;
                bidSize = orderBook.BestBidSize;
                askSize = orderBook.BestAskSize;

                if (bestBid > 0 && bestAsk > 0)
                {
                    return true;
                }
            }

            // Fallback to ticker data if order book not available
            if (_lastTickers.TryGetValue(symbol, out var ticker))
            {
                bestBid = ticker.BidPrice;
                bestAsk = ticker.AskPrice;
                // Note: Ticker data doesn't include sizes, keep as 0
                return bestBid > 0 && bestAsk > 0;
            }

            return false;
        }
    }
}
