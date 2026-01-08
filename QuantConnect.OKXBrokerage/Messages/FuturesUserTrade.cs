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
    /// OKX Futures user trade execution (fill) record from futures.usertrades channel
    /// Note: Futures user trades have different fields than Spot user trades
    /// </summary>
    [JsonConverter(typeof(FuturesUserTradeConverter))]
    public class FuturesUserTrade
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Order ID that generated this trade
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Contract name (e.g., BTC_USDT)
        /// Note: This is called "contract" in Futures, not "currency_pair" as in Spot
        /// </summary>
        public string Contract { get; set; }

        /// <summary>
        /// Creation time (Unix timestamp in seconds)
        /// </summary>
        public long CreateTime { get; set; }

        /// <summary>
        /// Creation time in milliseconds
        /// </summary>
        public string CreateTimeMs { get; set; }

        /// <summary>
        /// Trade size (number of contracts, can be negative for sells)
        /// Note: This is called "size" in Futures, not "amount" as in Spot
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Trade price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// User role: taker or maker
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Trading fee (in quote currency, typically USDT for USDT-settled contracts)
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// Point fee (if applicable)
        /// </summary>
        public decimal PointFee { get; set; }

        /// <summary>
        /// User defined text (client order ID)
        /// </summary>
        public string Text { get; set; }

        // Note: The following fields from Spot are NOT present in Futures:
        // - UserId (user_id)
        // - FeeCurrency (fee_currency) - Futures fees are always in the settlement currency (USDT)
        // - IdMarket (id_market)
        // - GtFee (gt_fee) - May not be applicable to Futures
    }
}
