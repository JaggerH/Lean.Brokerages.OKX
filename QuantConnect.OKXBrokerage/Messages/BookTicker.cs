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
using QuantConnect.Brokerages.OKX.Converters;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// OKX book ticker data - best bid/ask prices from order book
    /// Used by Futures market for real-time quote data
    /// </summary>
    [JsonConverter(typeof(BookTickerConverter))]
    public class BookTicker
    {
        /// <summary>
        /// Contract name (e.g., BTC_USDT for Futures) or Currency pair (for Spot)
        /// </summary>
        public string Contract { get; set; }

        /// <summary>
        /// Best bid price
        /// </summary>
        public decimal BestBid { get; set; }

        /// <summary>
        /// Best bid size
        /// </summary>
        public string BestBidSize { get; set; }

        /// <summary>
        /// Best ask price
        /// </summary>
        public decimal BestAsk { get; set; }

        /// <summary>
        /// Best ask size
        /// </summary>
        public string BestAskSize { get; set; }

        /// <summary>
        /// Book ticker timestamp (milliseconds)
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Order book update ID
        /// </summary>
        public string UpdateId { get; set; }
    }
}
