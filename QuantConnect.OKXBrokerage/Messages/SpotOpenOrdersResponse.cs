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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// OKX Spot Open Orders Response wrapper
    /// The /spot/open_orders endpoint returns a nested structure grouping orders by currency pair
    ///
    /// Example response:
    /// [
    ///   {
    ///     "currency_pair": "BTC_USDT",
    ///     "total": 2,
    ///     "orders": [...]
    ///   }
    /// ]
    ///
    /// Note: This is ONLY used for Spot market. Futures uses a different response format (flat array).
    /// </summary>
    public class SpotOpenOrdersResponse
    {
        /// <summary>
        /// Currency pair for these orders (e.g., BTC_USDT)
        /// </summary>
        [JsonProperty("currency_pair")]
        public string CurrencyPair { get; set; }

        /// <summary>
        /// Total number of open orders for this pair
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }

        /// <summary>
        /// List of open orders for this currency pair
        /// </summary>
        [JsonProperty("orders")]
        public List<SpotOrder> Orders { get; set; }
    }
}
