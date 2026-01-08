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
    /// OKX spot user trade execution (fill) record from spot.usertrades channel
    /// </summary>
    [JsonConverter(typeof(SpotUserTradeConverter))]
    public class SpotUserTrade
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Order ID that generated this trade
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Currency pair (e.g., BTC_USDT)
        /// </summary>
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Creation time (Unix timestamp in seconds)
        /// </summary>
        public long CreateTime { get; set; }

        /// <summary>
        /// Creation time in milliseconds
        /// </summary>
        public string CreateTimeMs { get; set; }

        /// <summary>
        /// Trade side: buy or sell
        /// </summary>
        public string Side { get; set; }

        /// <summary>
        /// Trade amount (base currency)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// User role: taker or maker
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Trade price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Trading fee
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// Fee currency (e.g., USDT, BTC)
        /// </summary>
        public string FeeCurrency { get; set; }

        /// <summary>
        /// Point fee (if applicable)
        /// </summary>
        public decimal PointFee { get; set; }

        /// <summary>
        /// GT fee (if applicable)
        /// </summary>
        public decimal GtFee { get; set; }

        /// <summary>
        /// User defined text (client order ID)
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Market ID
        /// </summary>
        public int IdMarket { get; set; }
    }
}
