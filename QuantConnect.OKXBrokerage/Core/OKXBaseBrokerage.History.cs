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
            if (!ValidateHistoryRequest(request))
            {
                yield break;
            }

            // Dispatch to appropriate handler based on data type
            foreach (var data in GetHistoryByType(request))
            {
                yield return data;
            }
        }

        /// <summary>
        /// Validates the history request
        /// </summary>
        /// <param name="request">The history request to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private bool ValidateHistoryRequest(HistoryRequest request)
        {
            if (request == null)
            {
                Log.Error($"{GetType().Name}.GetHistory(): Request cannot be null");
                return false;
            }

            if (request.Symbol != null && !string.IsNullOrEmpty(request.Symbol.Value))
            {
                return true;
            }

            Log.Error($"{GetType().Name}.GetHistory(): Invalid symbol in request");
            return false;
        }

        /// <summary>
        /// Dispatches history request to appropriate handler based on data type
        /// </summary>
        /// <param name="request">The history request</param>
        /// <returns>Historical data</returns>
        private IEnumerable<BaseData> GetHistoryByType(HistoryRequest request)
        {
            if (request.DataType == typeof(TradeBar) || request.DataType == typeof(QuoteBar))
            {
                return GetHistoricalBars(request);
            }

            if (request.DataType == typeof(Tick))
            {
                return GetHistoricalTicks(request);
            }

            Log.Error($"{GetType().Name}.GetHistory(): Unsupported data type: {request.DataType.Name} (Symbol: {request.Symbol})");
            return Enumerable.Empty<BaseData>();
        }

        /// <summary>
        /// Gets historical bars (TradeBar or QuoteBar)
        /// </summary>
        /// <param name="request">The history request</param>
        /// <returns>Historical bars</returns>
        private IEnumerable<BaseData> GetHistoricalBars(HistoryRequest request)
        {
            var instId = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            var bar = ConvertResolutionToBar(request.Resolution);
            var startMs = new DateTimeOffset(request.StartTimeUtc).ToUnixTimeMilliseconds();
            var endMs = new DateTimeOffset(request.EndTimeUtc).ToUnixTimeMilliseconds();
            var period = request.Resolution.ToTimeSpan();

            // Get all candles in the time range
            var allCandles = GetCandlesInTimeRange(instId, bar, startMs, endMs);

            // OKX candlesticks are trade data (OHLCV), always return TradeBar
            // QuoteBar would require real bid/ask data which OKX doesn't provide for historical data
            foreach (var candle in allCandles)
            {
                yield return candle.ToTradeBar(request.Symbol, period);
            }
        }

        /// <summary>
        /// Gets all candles in the specified time range
        /// OKX returns max 100 candles per request in descending order (newest first)
        /// </summary>
        /// <param name="instId">Instrument ID</param>
        /// <param name="bar">Bar interval (e.g., "1m", "1H")</param>
        /// <param name="startMs">Start time in milliseconds</param>
        /// <param name="endMs">End time in milliseconds</param>
        /// <returns>List of candles in chronological order</returns>
        private List<Candle> GetCandlesInTimeRange(string instId, string bar, long startMs, long endMs)
        {
            // OKX 'after' (our endTime) returns records older than ts — suitable for backward pagination.
            // Collect all candles backwards, then reverse for chronological order.
            var allCandles = new List<Candle>();
            long? currentEndTime = endMs;
            var reachedStart = false;

            while (!reachedStart)
            {
                var candles = RestApiClient.GetCandles(instId, bar, endTime: currentEndTime, limit: 100);

                if (candles == null || candles.Count == 0)
                {
                    Log.Trace($"{GetType().Name}.GetCandlesInTimeRange(): No more candles returned. Total: {allCandles.Count}");
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
                currentEndTime = candles[^1].Timestamp;

                if (candles.Count < 100)
                {
                    Log.Trace($"{GetType().Name}.GetCandlesInTimeRange(): Received less than 100 candles. Total: {allCandles.Count}");
                    break;
                }
            }

            // Collected in descending order, reverse for chronological order
            allCandles.Reverse();
            return allCandles;
        }

        /// <summary>
        /// Gets historical ticks based on tick type
        /// </summary>
        /// <param name="request">The history request</param>
        /// <returns>Historical ticks</returns>
        private IEnumerable<BaseData> GetHistoricalTicks(HistoryRequest request)
        {
            switch (request.TickType)
            {
                case TickType.Trade:
                    return GetTradeTicks(request);

                case TickType.Quote:
                case TickType.OpenInterest:
                case TickType.Orderbook:
                    // Quote, OpenInterest and Orderbook are not supported for historical data
                    // Warmup only reads Trade data (similar to Gate.io behavior)
                    return Enumerable.Empty<BaseData>();

                default:
                    Log.Error($"{GetType().Name}.GetHistory(): Unsupported tick type: {request.TickType} " +
                             $"(DataType: {request.DataType.Name}, Symbol: {request.Symbol})");
                    return Enumerable.Empty<BaseData>();
            }
        }

        /// <summary>
        /// Gets historical trade ticks
        /// Note: OKX /api/v5/market/trades endpoint only returns recent trades (max 500)
        /// </summary>
        /// <param name="request">The history request</param>
        /// <returns>Trade ticks</returns>
        private IEnumerable<BaseData> GetTradeTicks(HistoryRequest request)
        {
            var instId = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            Log.Trace($"{GetType().Name}.GetTradeTicks(): Fetching recent trade ticks for {request.Symbol}");

            var trades = RestApiClient.GetTrades(instId, 500);

            if (trades == null || trades.Count == 0)
            {
                yield break;
            }

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
                    throw new NotSupportedException($"{GetType().Name}.ConvertResolutionToBar(): Second resolution is not supported by OKX API");
                default:
                    throw new ArgumentException($"{GetType().Name}.ConvertResolutionToBar(): Unsupported resolution: {resolution}");
            }
        }
    }
}
