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
    /// Risk limit tier information from OKX futures API
    /// Represents a single tier in the risk limit ladder
    /// </summary>
    /// <remarks>
    /// OKX uses tiered risk limits to manage position sizes.
    /// Higher tiers allow larger positions but require higher margin rates.
    ///
    /// API: GET /futures/{settle}/risk_limit_tiers?contract={contract}
    ///
    /// Example response:
    /// {
    ///   "tier": 1,
    ///   "risk_limit": "20000",
    ///   "initial_rate": "0.02",
    ///   "maintenance_rate": "0.01",
    ///   "leverage_max": "50"
    /// }
    /// </remarks>
    [JsonConverter(typeof(RiskLimitTierConverter))]
    public class RiskLimitTier
    {
        /// <summary>
        /// Tier number (1-based, higher tier = larger position allowed)
        /// </summary>
        public int Tier { get; set; }

        /// <summary>
        /// Maximum position value in USD for this tier
        /// </summary>
        public decimal RiskLimit { get; set; }

        /// <summary>
        /// Initial margin rate for this tier (e.g., 0.02 = 2%)
        /// </summary>
        public decimal InitialRate { get; set; }

        /// <summary>
        /// Maintenance margin rate for this tier (e.g., 0.01 = 1%)
        /// </summary>
        public decimal MaintenanceRate { get; set; }

        /// <summary>
        /// Maximum leverage allowed for this tier
        /// </summary>
        public decimal LeverageMax { get; set; }

        /// <summary>
        /// Contract name this tier applies to (e.g., "BTC_USDT")
        /// </summary>
        public string Contract { get; set; }
    }
}
