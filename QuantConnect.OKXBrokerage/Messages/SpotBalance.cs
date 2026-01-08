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

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Spot account balance from REST API
    /// Endpoint: GET /spot/accounts
    /// https://www.okx.io/docs/developers/apiv4/en/#list-spot-accounts
    /// Returns an array of balances for each currency in the spot account
    /// </summary>
    public class SpotBalance
    {
        /// <summary>
        /// Currency code (e.g., BTC, USDT, ETH)
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Available balance for trading or withdrawal
        /// </summary>
        [JsonProperty("available")]
        public string Available { get; set; }

        /// <summary>
        /// Locked balance (in open orders)
        /// </summary>
        [JsonProperty("locked")]
        public string Locked { get; set; }

        /// <summary>
        /// Update ID for this balance record
        /// </summary>
        [JsonProperty("update_id")]
        public long UpdateId { get; set; }
    }
}
