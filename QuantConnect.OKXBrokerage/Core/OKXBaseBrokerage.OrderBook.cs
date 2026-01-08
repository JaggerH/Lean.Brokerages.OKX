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
using System.Threading.Tasks;
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - OrderBook Management
    /// Handles full order book state synchronization and maintenance
    /// Copied from legacy OKXBrokerage.DataQueueHandler.cs
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Applies an order book update to the context
        /// Updates bid/ask levels and sequence tracking
        /// Single-consumer model ensures atomicity without locks
        /// </summary>
        protected void ApplyOrderBookUpdate(OrderBookContext context, Messages.OrderBookUpdate update)
        {
            // Deleokx to OKXOrderBook for incremental update logic
            context.OrderBook.ApplyIncrementalUpdate(update.Bids, update.Asks);

            // Update sequence ID and timestamp
            context.LastUpdateId = update.LastUpdateId;
            context.LastUpdateTime = update.Time;
        }

        /// <summary>
        /// Event handler for order book best bid/ask updates
        /// Emits Quote ticks when top of book changes
        /// </summary>
        protected void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            try
            {
                // Emit quote tick (best bid/ask)
                var quoteTick = new Tick
                {
                    Symbol = e.Symbol,
                    Time = DateTime.UtcNow,
                    Value = (e.BestBidPrice + e.BestAskPrice) / 2,
                    BidPrice = e.BestBidPrice,
                    AskPrice = e.BestAskPrice,
                    BidSize = e.BestBidSize,
                    AskSize = e.BestAskSize,
                    TickType = TickType.Quote,
                    Exchange = Market.OKX
                };

                lock (_aggregator)
                {
                    _aggregator.Update(quoteTick);
                }

                // Emit orderbook depth (full depth levels)
                if (_orderBooks.TryGetValue(e.Symbol, out var orderBook))
                {
                    var orderbook = orderBook.ToOrderbook();

                    lock (_aggregator)
                    {
                        _aggregator.Update(orderbook);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.OnBestBidAskUpdated(): {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes an order book asynchronously
        /// Fetches REST snapshot and applies it, then lets consumer handle incremental updates
        /// </summary>
        protected async Task InitializeOrderBookAsync(OrderBookContext context)
        {
            try
            {
                // Give WebSocket time to receive and buffer updates in Channel
                await Task.Delay(500);

                // Fetch REST API snapshot
                var snapshot = RestApiClient.GetOrderBookSnapshot(context.CurrencyPair, 100);
                if (snapshot == null)
                {
                    Log.Error($"{GetType().Name}.InitializeOrderBookAsync(): Failed to get snapshot for {context.Symbol}");
                    lock (context.Lock)
                    {
                        context.State = OrderBookState.Error;
                    }
                    return;
                }

                // Apply snapshot to order book
                lock (context.Lock)
                {
                    // Unsubscribe from events during rebuild
                    context.OrderBook.BestBidAskUpdated -= OnBestBidAskUpdated;

                    // Deleokx to OKXOrderBook for full snapshot logic (includes Clear, Update, and Trigger)
                    context.OrderBook.ApplyFullSnapshot(snapshot.Bids, snapshot.Asks);

                    // Update sequence tracking
                    context.BaseId = snapshot.Id;
                    context.LastUpdateId = snapshot.Id;
                    context.LastUpdateTime = DateTime.UtcNow;

                    // Mark as synchronized - consumer will now process buffered Channel messages
                    context.State = OrderBookState.Synchronized;
                    context.OrderBook.BestBidAskUpdated += OnBestBidAskUpdated;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.InitializeOrderBookAsync(): Error for {context.Symbol}: {ex.Message}");
                lock (context.Lock)
                {
                    context.State = OrderBookState.Error;
                }
            }
        }

        /// <summary>
        /// Initializes a Quote tick subscription asynchronously
        /// Fetches REST ticker data and emits initial quote tick if no WebSocket data arrived yet
        /// </summary>
        protected Task InitializeQuoteTickAsync(QuoteTickContext context)
        {
            try
            {
                // Fetch REST ticker data immediately (0ms delay as per user preference)
                var tickers = RestApiClient.GetTicker(context.CurrencyPair);
                var ticker = tickers?.FirstOrDefault();
                if (ticker == null)
                {
                    Log.Error($"{GetType().Name}.InitializeQuoteTickAsync(): Failed to get ticker for {context.Symbol}");
                    return Task.CompletedTask;
                }

                // Check if WebSocket data already arrived
                lock (context.Lock)
                {
                    if (context.HasReceivedWebSocketData)
                    {
                        Log.Trace($"{GetType().Name}.InitializeQuoteTickAsync(): WebSocket data already received for {context.Symbol}, discarding REST data");
                        return Task.CompletedTask;
                    }

                    // WebSocket data hasn't arrived yet, emit REST ticker as initial quote tick
                    var quoteTick = new Tick
                    {
                        Symbol = context.Symbol,
                        Time = DateTime.UtcNow,
                        Value = (ticker.HighestBid + ticker.LowestAsk) / 2,
                        BidPrice = ticker.HighestBid,
                        AskPrice = ticker.LowestAsk,
                        BidSize = 0, // Ticker doesn't provide size info
                        AskSize = 0,
                        TickType = TickType.Quote,
                        Exchange = Market.OKX
                    };

                    lock (_aggregator)
                    {
                        _aggregator.Update(quoteTick);
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.InitializeQuoteTickAsync(): Error for {context.Symbol}: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Reinitializes an order book synchronously after detecting a sequence gap
        /// BLOCKS the calling consumer thread to prevent message loss during reinitialization
        /// This is intentional design: acts as a barrier to ensure OrderBook consistency
        /// </summary>
        protected void ReinitializeOrderBookSync(OrderBookContext context)
        {
            try
            {
                lock (context.Lock)
                {
                    context.OrderBook.BestBidAskUpdated -= OnBestBidAskUpdated;
                    context.OrderBook.Clear();
                    context.State = OrderBookState.Initializing;
                    context.LastUpdateId = 0;
                    context.BaseId = 0;
                    context.LastUpdateTime = DateTime.UtcNow;
                }

                // Call async initialization and wait synchronously (blocks consumer)
                InitializeOrderBookAsync(context).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.ReinitializeOrderBookSync(): Error for {context.Symbol}: {ex.Message}");
                lock (context.Lock)
                {
                    context.State = OrderBookState.Error;
                }
            }
        }

        /// <summary>
        /// Consumer task that processes order book updates from the channel
        /// Single-threaded per symbol, runs for the lifetime of the OrderBookContext
        /// </summary>
        protected async Task ProcessOrderBookUpdatesAsync(OrderBookContext context)
        {
            try
            {
                // Defensive: Check for null CancellationToken
                if (context.CancellationToken == null)
                {
                    Log.Error($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): CancellationToken is null for {context.Symbol}, consumer exiting");
                    return;
                }

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    // State check - wait during initialization or syncing
                    while ((context.State == OrderBookState.Initializing ||
                            context.State == OrderBookState.Syncing) &&
                           !context.CancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                    }

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Wait for message from Channel
                    if (await context.MessageChannel.Reader.WaitToReadAsync(context.CancellationToken.Token))
                    {
                        while (context.MessageChannel.Reader.TryRead(out var update))
                        {
                            try
                            {
                                // Handle different update types
                                if (update.Full)
                                {
                                    // Full snapshot - reset order book
                                    Log.Trace($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): Full snapshot received for {context.Symbol}");

                                    lock (context.Lock)
                                    {
                                        // Deleokx to OKXOrderBook for full snapshot logic
                                        context.OrderBook.ApplyFullSnapshot(update.Bids, update.Asks);

                                        context.LastUpdateId = update.LastUpdateId;
                                        context.LastUpdateTime = update.Time;
                                    }
                                }
                                else
                                {
                                    // Incremental update - check sequence continuity
                                    if (context.State == OrderBookState.Synchronized)
                                    {
                                        // Gap detection with overflow protection
                                        // Note: Use checked arithmetic to detect overflow, or validate bounds first
                                        long expectedNextId;
                                        bool overflowDetected = false;

                                        try
                                        {
                                            expectedNextId = checked(context.LastUpdateId + 1);
                                        }
                                        catch (OverflowException)
                                        {
                                            // Overflow detected - treat as gap and reinitialize
                                            overflowDetected = true;
                                            expectedNextId = 0;
                                            Log.Error($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): Sequence ID overflow detected for {context.Symbol}! LastUpdateId={context.LastUpdateId}. Reinitializing...");
                                        }

                                        if (overflowDetected || update.FirstUpdateId > expectedNextId)
                                        {
                                            // Set state to Error to pause consumer, then reinitialize synchronously
                                            lock (context.Lock)
                                            {
                                                context.State = OrderBookState.Error;
                                            }

                                            // Call synchronous reinitialization (blocks consumer - intentional barrier)
                                            ReinitializeOrderBookSync(context);
                                            continue;
                                        }

                                        // Skip outdated messages
                                        if (update.LastUpdateId <= context.LastUpdateId)
                                        {
                                            continue;
                                        }

                                        // Apply incremental update
                                        ApplyOrderBookUpdate(context, update);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): Error processing update for {context.Symbol}: {ex.Message}");
                            }
                        }
                    }
                }

                Log.Trace($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): Consumer stopped for {context.Symbol}");
            }
            catch (OperationCanceledException)
            {
                Log.Trace($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): Consumer cancelled for {context.Symbol}");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.ProcessOrderBookUpdatesAsync(): Fatal error for {context.Symbol}: {ex.Message}");
            }
        }
    }
}
