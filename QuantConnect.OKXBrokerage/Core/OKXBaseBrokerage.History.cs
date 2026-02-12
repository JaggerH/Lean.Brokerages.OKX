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
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - Historical Data Implementation
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars/ticks covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            // Validate request
            if (request == null)
            {
                Log.Error($"{GetType().Name}.GetHistory(): Request cannot be null");
                yield break;
            }

            if (request.Symbol == null || string.IsNullOrEmpty(request.Symbol.Value))
            {
                Log.Error($"{GetType().Name}.GetHistory(): Invalid symbol in request");
                yield break;
            }

            Log.Trace($"{GetType().Name}.GetHistory(): Requesting {request.DataType.Name} history for {request.Symbol} " +
                     $"from {request.StartTimeUtc:yyyy-MM-dd HH:mm:ss} to {request.EndTimeUtc:yyyy-MM-dd HH:mm:ss} " +
                     $"at {request.Resolution} resolution");

            // Convert symbol to OKX format
            var instId = _symbolMapper.GetBrokerageSymbol(request.Symbol);

            // Handle different data types
            if (request.DataType == typeof(TradeBar) || request.DataType == typeof(QuoteBar))
            {
                // Get candles (OHLCV data)
                var bar = ConvertResolutionToBar(request.Resolution);
                var startMs = new DateTimeOffset(request.StartTimeUtc).ToUnixTimeMilliseconds();
                var endMs = new DateTimeOffset(request.EndTimeUtc).ToUnixTimeMilliseconds();
                var period = request.Resolution.ToTimeSpan();
                var isQuoteBar = request.DataType == typeof(QuoteBar);

                // OKX returns max 100 candles per request in descending order (newest first).
                // OKX 'after' (our endTime) returns records older than ts — suitable for backward pagination.
                // Collect all candles backwards, then reverse for chronological yield.
                var allCandles = new List<Candle>();
                long? currentEndTime = endMs;
                var reachedStart = false;

                while (!reachedStart)
                {
                    var candles = RestApiClient.GetCandles(instId, bar, endTime: currentEndTime, limit: 100);

                    if (candles == null || candles.Count == 0)
                    {
                        Log.Trace($"{GetType().Name}.GetHistory(): No more candles returned. Total fetched: {allCandles.Count}");
                        break;
                    }

                    // OKX returns descending: candles[0]=newest, candles[Count-1]=oldest
                    foreach (var candle in candles)
                    {
                        if (candle.Timestamp < startMs)
                        {
                            reachedStart = true;
                            break;
                        }
                        allCandles.Add(candle);
                    }

                    // Next page: get data older than the oldest candle in this batch
                    currentEndTime = candles[candles.Count - 1].Timestamp;

                    if (candles.Count < 100)
                    {
                        Log.Trace($"{GetType().Name}.GetHistory(): Received less than 100 candles. Total: {allCandles.Count}");
                        break;
                    }
                }

                // Collected in descending order, reverse for chronological yield
                allCandles.Reverse();
                Log.Trace($"{GetType().Name}.GetHistory(): Yielding {allCandles.Count} candles");

                foreach (var candle in allCandles)
                {
                    if (isQuoteBar)
                    {
                        yield return candle.ToQuoteBar(request.Symbol, period);
                    }
                    else
                    {
                        yield return candle.ToTradeBar(request.Symbol, period);
                    }
                }
            }
            else if (request.DataType == typeof(Tick) && request.TickType == TickType.Trade)
            {
                // Get recent trade ticks
                // Note: OKX /api/v5/market/trades endpoint only returns recent trades (max 500)
                // Historical trade data beyond this is not available via REST API
                Log.Trace($"{GetType().Name}.GetHistory(): Fetching recent trade ticks");

                var trades = RestApiClient.GetTrades(instId, 500);

                if (trades != null && trades.Count > 0)
                {
                    // Trades are returned in descending order (newest first)
                    // Reverse for chronological order
                    trades.Reverse();

                    foreach (var trade in trades)
                    {
                        var time = DateTimeOffset.FromUnixTimeMilliseconds(trade.Timestamp).UtcDateTime;

                        // Filter: only return trades within requested time range
                        if (time >= request.StartTimeUtc && time <= request.EndTimeUtc)
                        {
                            yield return trade.ToTick(_symbolMapper, request.Symbol.SecurityType);
                        }
                    }
                }
            }
            else if (request.DataType == typeof(Tick) && request.TickType == TickType.Quote)
            {
                // Quote ticks not supported for historical data
                Log.Trace($"{GetType().Name}.GetHistory(): Quote ticks not supported for historical data");
                yield break;
            }
            else
            {
                Log.Error($"{GetType().Name}.GetHistory(): Unsupported data type: {request.DataType.Name}");
                yield break;
            }
        }

        /// <summary>
        /// Converts LEAN Resolution to OKX bar interval format
        /// </summary>
        /// <param name="resolution">The LEAN resolution</param>
        /// <returns>OKX bar interval string (e.g., "1m", "1H", "1D")</returns>
        private string ConvertResolutionToBar(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Minute:
                    return "1m";
                case Resolution.Hour:
                    return "1H";
                case Resolution.Daily:
                    return "1D";
                case Resolution.Second:
                    throw new NotSupportedException($"{GetType().Name}.GetHistory(): Second resolution is not supported by OKX API");
                default:
                    throw new ArgumentException($"{GetType().Name}.GetHistory(): Unsupported resolution: {resolution}");
            }
        }
    }
}
