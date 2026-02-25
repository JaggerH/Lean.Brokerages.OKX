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
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.OKX
{
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Gets the history for the requested security.
        /// Supports:
        ///   - Resolution.Tick + TickType.Trade → Trade Ticks via /market/trades
        ///   - Resolution.Minute/Hour/Daily + DataType=TradeBar → TradeBar via /market/candles
        /// Returns null for everything else (LEAN skips null gracefully).
        /// </summary>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            if (!CanSubscribe(request.Symbol) || request.StartTimeUtc >= request.EndTimeUtc)
                return null;

            if (request.Resolution == Resolution.Tick && request.TickType == TickType.Trade)
                return GetTradeHistory(request);

            if (request.Resolution >= Resolution.Minute && request.DataType == typeof(TradeBar))
                return GetCandleHistory(request);

            return null;
        }

        /// <summary>
        /// Fetches recent trades from /market/trades.
        /// OKX returns up to 500 most recent trades (newest-first, no time-based pagination).
        /// We sort ascending and return all trades without time filtering.
        /// </summary>
        private IEnumerable<BaseData> GetTradeHistory(HistoryRequest request)
        {
            var instId = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            var trades = RestApiClient.GetTrades(instId, 500);

            // OKX returns trades newest-first; sort ascending so LEAN processes them chronologically
            trades.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            foreach (var trade in trades)
            {
                yield return trade.ToTick(_symbolMapper, request.Symbol.SecurityType);
            }
        }

        /// <summary>
        /// Fetches historical candles and converts to TradeBars.
        /// OKX limit: max 1440 candles per granularity (1m=24h, 1H=60d, 1D~4y).
        /// </summary>
        private IEnumerable<BaseData> GetCandleHistory(HistoryRequest request)
        {
            var instId = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            var bar = ConvertResolutionToBar(request.Resolution);
            var startMs = new DateTimeOffset(request.StartTimeUtc).ToUnixTimeMilliseconds();
            var endMs = new DateTimeOffset(request.EndTimeUtc).ToUnixTimeMilliseconds();
            var period = request.Resolution.ToTimeSpan();

            foreach (var candle in RestApiClient.GetCandles(instId, bar, startMs, endMs))
            {
                yield return candle.ToTradeBar(request.Symbol, period);
            }
        }

        private static string ConvertResolutionToBar(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Minute: return "1m";
                case Resolution.Hour: return "1H";
                case Resolution.Daily: return "1D";
                default:
                    throw new ArgumentException($"Unsupported resolution: {resolution}");
            }
        }
    }
}
