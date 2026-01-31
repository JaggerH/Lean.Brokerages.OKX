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
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - OrderBook Management
    /// Handles order book synchronization using BrokerageMultiStateSynchronizer pattern
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Order book synchronizer for managing multiple order book states
        /// </summary>
        protected BrokerageMultiStateSynchronizer<Symbol, OKXOrderBook, Messages.WebSocketOrderBook> _orderBookSync;

        /// <summary>
        /// Initializes the order book synchronizer
        /// Must be called after RestApiClient is initialized
        /// </summary>
        protected virtual void InitializeOrderBookSync()
        {
            // Initialize order book synchronizer
            // Uses BrokerageMultiStateSynchronizer for automatic message routing and gap recovery
            _orderBookSync = new BrokerageMultiStateSynchronizer<Symbol, OKXOrderBook, Messages.WebSocketOrderBook>(
                getKey: msg => _symbolMapper.GetLeanSymbol(msg.InstrumentId, GetSecurityType(msg.InstrumentId), Market.OKX),
                reducer: OrderBookReducer,
                capacity: 10000,
                initializer: InitializeOrderBookAsync
            );

            // Subscribe to state changes to emit orderbook data
            _orderBookSync.StateChanged += OnOrderBookStateChanged;

            // Subscribe to errors to trigger refresh on gap detection
            _orderBookSync.Error += OnOrderBookError;
        }

        /// <summary>
        /// Initializes OrderBook state with full control over the workflow.
        /// Called by BrokerageStateSynchronizer.ReinitializeAsync() which handles:
        /// - Reentrancy protection (prevents concurrent initializations)
        /// - Consumption pausing (messages buffer automatically)
        /// </summary>
        private async Task InitializeOrderBookAsync(Symbol symbol, BrokerageStateSynchronizer<OKXOrderBook, Messages.WebSocketOrderBook> sync)
        {
            // NOTE: PauseConsumption/ResumeConsumption is handled by ReinitializeAsync()
            // This method only focuses on the initialization logic itself

            // 1. Ensure State exists (silent, no event)
            var orderBook = sync.State;
            if (orderBook == null)
            {
                orderBook = new OKXOrderBook(symbol);
                orderBook.BestBidAskUpdated += OnBestBidAskUpdated;
                sync.SetStateSilent(orderBook);
                _orderBooks[symbol] = orderBook;
                Log.Trace($"{GetType().Name}.InitializeOrderBookAsync(): State created for {symbol}, waiting for WS snapshot");
            }
        }

        /// <summary>
        /// Reducer function for order book state updates
        /// Processes snapshots and incremental updates
        /// Throws exceptions on errors to trigger refresh via Error event
        /// </summary>
        protected OKXOrderBook OrderBookReducer(OKXOrderBook current, Messages.WebSocketOrderBook update)
        {
            // State should never be null after initialization
            if (current == null)
            {
                throw new InvalidOperationException(
                    $"OrderBook state is null for {update.InstrumentId} - initializer should have created it");
            }

            var time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(update.Timestamp)).UtcDateTime;

            // 1. Snapshot detection: action = "snapshot" or prevSeqId = -1
            if (update.Action == "snapshot" || update.PreviousSequenceId == -1)
            {
                Log.Trace($"{GetType().Name}.OrderBookReducer(): Snapshot received for {current.Symbol}, seqId={update.SequenceId}, prevSeqId={update.PreviousSequenceId}");

                // Apply full snapshot
                current.ApplyFullSnapshot(update.Bids, update.Asks);

                // Validate checksum if provided
                if (update.Checksum.HasValue)
                {
                    if (!OKXChecksumValidator.ValidateChecksum(current, update.Checksum.Value, out var calculatedChecksum))
                    {
                        Log.Error($"{GetType().Name}.OrderBookReducer(): Checksum validation FAILED for {current.Symbol} snapshot! Expected: {update.Checksum.Value}, Calculated: {calculatedChecksum}");
                        throw new InvalidOperationException($"Checksum validation failed for {current.Symbol} snapshot");
                    }
                    Log.Trace($"{GetType().Name}.OrderBookReducer(): Checksum validation passed for {current.Symbol} snapshot: {update.Checksum.Value}");
                }

                // Update sequence tracking
                current.LastUpdateId = update.SequenceId ?? 0;
                current.LastUpdateTime = time;
                return current;
            }

            // 2. Keepalive detection: prevSeqId == seqId means no update
            if (update.PreviousSequenceId == update.SequenceId)
            {
                return null; // Ignore keepalive
            }

            // 3. Sequence validation
            if (update.SequenceId.HasValue && update.PreviousSequenceId.HasValue)
            {
                var expectedPrevSeqId = current.LastUpdateId;
                var actualPrevSeqId = update.PreviousSequenceId.Value;
                var currentSeqId = update.SequenceId.Value;

                // Special case: sequence reset during maintenance (prevSeqId > seqId)
                if (actualPrevSeqId > currentSeqId)
                {
                    Log.Trace($"{GetType().Name}.OrderBookReducer(): Sequence reset detected for {current.Symbol}, prevSeqId={actualPrevSeqId} > seqId={currentSeqId}. Continuing with new sequence.");
                    // Continue processing with new sequence
                }
                // Normal case: validate sequence continuity
                else if (actualPrevSeqId != expectedPrevSeqId && expectedPrevSeqId > 0)
                {
                    throw new InvalidOperationException(
                        $"OrderBook sequence gap detected for {current.Symbol}: expected prevSeqId={expectedPrevSeqId}, got prevSeqId={actualPrevSeqId}, seqId={currentSeqId}");
                }

                // Apply incremental update
                current.ApplyIncrementalUpdate(update.Bids, update.Asks);

                // Validate checksum if provided
                if (update.Checksum.HasValue)
                {
                    if (!OKXChecksumValidator.ValidateChecksum(current, update.Checksum.Value, out var calculatedChecksum))
                    {
                        throw new InvalidOperationException(
                            $"Checksum validation failed for {current.Symbol} update: expected={update.Checksum.Value}, calculated={calculatedChecksum}, seqId={currentSeqId}");
                    }
                }

                current.LastUpdateId = currentSeqId;
                current.LastUpdateTime = time;
            }
            else
            {
                // No sequence IDs present (shouldn't happen for books channel, but handle gracefully)
                current.ApplyIncrementalUpdate(update.Bids, update.Asks);

                // Validate checksum if provided
                if (update.Checksum.HasValue)
                {
                    if (!OKXChecksumValidator.ValidateChecksum(current, update.Checksum.Value, out var calculatedChecksum))
                    {
                        throw new InvalidOperationException(
                            $"Checksum validation failed for {current.Symbol} update: expected={update.Checksum.Value}, calculated={calculatedChecksum}");
                    }
                }

                current.LastUpdateTime = time;
            }

            return current;
        }

        /// <summary>
        /// Handles order book state changes - emits Orderbook data
        /// </summary>
        private void OnOrderBookStateChanged(object sender, KeyedStateChangedEventArgs<Symbol, OKXOrderBook> e)
        {
            // Emit full orderbook data
            var orderBook = _orderBookSync?.GetState(e.Key);
            if (orderBook != null)
            {
                var orderbookData = orderBook.ToOrderbook();

                lock (_aggregator)
                {
                    _aggregator.Update(orderbookData);
                }
            }
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
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.OnBestBidAskUpdated(): {ex.Message}");
            }
        }

        /// <summary>
        /// Handles order book errors - re-initializes on gap detection.
        /// Uses ReinitializeAsync() which provides:
        /// - Reentrancy protection (skips if already initializing)
        /// - Automatic consumption pausing (set synchronously before returning)
        /// </summary>
        private void OnOrderBookError(object sender, KeyedErrorEventArgs<Symbol> e)
        {
            Log.Trace($"{GetType().Name}.OnOrderBookError(): {e.Key} - {e.Exception.Message}");

            var sync = _orderBookSync?.GetSynchronizer(e.Key);
            if (sync == null)
                return;

            // ReinitializeAsync handles reentrancy and consumption pausing
            // Fire-and-forget: _consumptionPaused is set synchronously before ReinitializeAsync returns
            _ = sync.ReinitializeAsync();
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
    }
}
