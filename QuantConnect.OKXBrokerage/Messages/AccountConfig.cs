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
    /// Represents OKX v5 account configuration
    /// https://www.okx.com/docs-v5/en/#rest-api-account-get-account-configuration
    /// </summary>
    public class AccountConfig
    {
        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("uid")]
        public string UserId { get; set; }

        /// <summary>
        /// Account level: 1(Simple), 2(Single-currency margin), 3(Multi-currency margin), 4(Portfolio margin)
        /// </summary>
        [JsonProperty("acctLv")]
        public string AccountLevel { get; set; }

        /// <summary>
        /// Position mode: long_short_mode(hedged), net_mode(one-way)
        /// </summary>
        [JsonProperty("posMode")]
        public string PositionMode { get; set; }

        /// <summary>
        /// Auto borrow: true or false
        /// </summary>
        [JsonProperty("autoLoan")]
        public bool AutoLoan { get; set; }

        /// <summary>
        /// Greeks display type: PA(parity), BS(BlackScholes)
        /// </summary>
        [JsonProperty("greeksType")]
        public string GreeksType { get; set; }

        /// <summary>
        /// Current account level's effective leverage
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        /// Temporary increase in leverage: true or false
        /// </summary>
        [JsonProperty("levelTmp")]
        public string LevelTmp { get; set; }

        /// <summary>
        /// Contract isolated margin trading settings
        /// </summary>
        [JsonProperty("ctIsoMode")]
        public string ContractIsoMode { get; set; }

        /// <summary>
        /// Margin isolated mode under Multi-currency margin
        /// </summary>
        [JsonProperty("mgnIsoMode")]
        public string MarginIsoMode { get; set; }
    }
}
