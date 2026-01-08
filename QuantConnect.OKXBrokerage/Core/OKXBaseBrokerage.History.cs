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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - Historical Data Implementation
    /// Provides historical market data via REST API
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Gets historical market data for the specified request
        /// </summary>
        /// <param name="request">History request parameters</param>
        /// <returns>Enumerable of base data (TradeBars) or null if not supported</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            // Validate symbol is supported
            if (!CanSubscribe(request.Symbol))
            {
                Log.Trace($"{GetType().Name}.GetHistory(): Cannot subscribe to symbol {request.Symbol}");
                return null;
            }

            // Only support TradeBar for now
            if (request.DataType != typeof(TradeBar))
            {
                Log.Trace($"{GetType().Name}.GetHistory(): Unsupported data type {request.DataType.Name}");
                return null;
            }

            // Validate resolution is supported
            var interval = OKXUtility.ConvertResolution(request.Resolution);
            if (string.IsNullOrEmpty(interval))
            {
                Log.Trace($"{GetType().Name}.GetHistory(): Unsupported resolution {request.Resolution}");
                return null;
            }

            try
            {
                // Convert parameters
                var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
                var fromTimestamp = (long)Time.DateTimeToUnixTimeStamp(request.StartTimeUtc);
                var toTimestamp = (long)Time.DateTimeToUnixTimeStamp(request.EndTimeUtc);

                Log.Trace($"{GetType().Name}.GetHistory(): Fetching history for {brokerageSymbol} from {request.StartTimeUtc:yyyy-MM-dd HH:mm:ss} to {request.EndTimeUtc:yyyy-MM-dd HH:mm:ss} ({interval})");

                // Fetch candlesticks with pagination
                var allCandles = FetchCandlesticksWithPagination(brokerageSymbol, interval, fromTimestamp, toTimestamp);

                if (allCandles == null || allCandles.Count == 0)
                {
                    Log.Trace($"{GetType().Name}.GetHistory(): No candlestick data returned for {brokerageSymbol}");
                    return Enumerable.Empty<BaseData>();
                }

                // Convert to LEAN TradeBars
                var tradeBars = ConvertCandlesToTradeBars(allCandles, request.Symbol, request.Resolution);

                Log.Trace($"{GetType().Name}.GetHistory(): Returned {tradeBars.Count()} bars for {brokerageSymbol}");

                return tradeBars;
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.GetHistory(): Error fetching history for {request.Symbol}: {ex.Message}");
                return Enumerable.Empty<BaseData>();
            }
        }

        /// <summary>
        /// Fetches candlesticks with automatic pagination
        /// OKX limits each request to 1000 candles maximum
        /// </summary>
        /// <param name="brokerageSymbol">Brokerage symbol (e.g., "BTC_USDT")</param>
        /// <param name="interval">Interval string (e.g., "1m", "1h", "1d")</param>
        /// <param name="fromTimestamp">Start timestamp (Unix seconds)</param>
        /// <param name="toTimestamp">End timestamp (Unix seconds)</param>
        /// <returns>List of candlestick arrays</returns>
        private List<object[]> FetchCandlesticksWithPagination(
            string brokerageSymbol,
            string interval,
            long fromTimestamp,
            long toTimestamp)
        {
            var allCandles = new List<object[]>();
            const int maxLimit = 1000;
            var currentFrom = fromTimestamp;

            while (currentFrom < toTimestamp)
            {
                var candles = RestApiClient.GetCandlesticks(
                    brokerageSymbol,
                    interval,
                    currentFrom,
                    toTimestamp,
                    maxLimit
                );

                if (candles == null || candles.Count == 0)
                {
                    break;
                }

                allCandles.AddRange(candles);

                // Get timestamp of last candle
                var lastTimestamp = Convert.ToInt64(candles.Last()[0]);

                // If we got less than the limit, we've reached the end
                if (candles.Count < maxLimit)
                {
                    break;
                }

                // If last timestamp is at or past our target, we're done
                if (lastTimestamp >= toTimestamp)
                {
                    break;
                }

                // Move to next page (add 1 second to avoid duplicate)
                currentFrom = lastTimestamp + 1;

                // Safety check to prevent infinite loops
                if (currentFrom == fromTimestamp)
                {
                    Log.Error($"{GetType().Name}.FetchCandlesticksWithPagination(): Infinite loop detected, breaking");
                    break;
                }

                fromTimestamp = currentFrom;
            }

            return allCandles;
        }

        /// <summary>
        /// Converts OKX candlestick data to LEAN TradeBars
        /// OKX candlestick format: [timestamp, volume, close, high, low, open]
        /// (Futures has additional "amount" field at index 6)
        /// </summary>
        /// <param name="candles">Raw candlestick data from OKX</param>
        /// <param name="symbol">LEAN symbol</param>
        /// <param name="resolution">Resolution for the period</param>
        /// <returns>List of TradeBars</returns>
        private List<TradeBar> ConvertCandlesToTradeBars(
            List<object[]> candles,
            Symbol symbol,
            Resolution resolution)
        {
            var tradeBars = new List<TradeBar>();
            var period = resolution.ToTimeSpan();

            foreach (var candle in candles)
            {
                if (candle == null || candle.Length < 6)
                {
                    Log.Error($"{GetType().Name}.ConvertCandlesToTradeBars(): Invalid candle data (length: {candle?.Length ?? 0})");
                    continue;
                }

                try
                {
                    // Parse candlestick data
                    // OKX format: [timestamp, volume, close, high, low, open]
                    var timestamp = Convert.ToInt64(candle[0]);
                    var volume = Convert.ToDecimal(candle[1]);
                    var close = Convert.ToDecimal(candle[2]);
                    var high = Convert.ToDecimal(candle[3]);
                    var low = Convert.ToDecimal(candle[4]);
                    var open = Convert.ToDecimal(candle[5]);

                    // Convert timestamp to DateTime
                    var time = Time.UnixTimeStampToDateTime(timestamp);

                    tradeBars.Add(new TradeBar(
                        time,
                        symbol,
                        open,
                        high,
                        low,
                        close,
                        volume,
                        period
                    ));
                }
                catch (Exception ex)
                {
                    Log.Error($"{GetType().Name}.ConvertCandlesToTradeBars(): Error converting candle: {ex.Message}");
                }
            }

            return tradeBars;
        }
    }
}
