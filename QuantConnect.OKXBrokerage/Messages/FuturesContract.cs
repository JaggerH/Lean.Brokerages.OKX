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
    /// Futures contract information from OKX
    /// Represents a futures contract available for trading
    /// </summary>
    [JsonConverter(typeof(FuturesContractConverter))]
    public class FuturesContract
    {
        /// <summary>
        /// Contract name (e.g., "BTC_USDT")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Contract type ("direct" for linear contracts, "inverse" for inverse contracts)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Whether the contract is being delisted
        /// </summary>
        public bool InDelisting { get; set; }

        /// <summary>
        /// Quanto multiplier (contract size multiplier)
        /// </summary>
        public decimal QuantoMultiplier { get; set; }

        /// <summary>
        /// Minimum allowed leverage
        /// </summary>
        public decimal LeverageMin { get; set; }

        /// <summary>
        /// Maximum allowed leverage
        /// </summary>
        public decimal LeverageMax { get; set; }

        /// <summary>
        /// Maintenance margin rate
        /// </summary>
        public decimal MaintenanceRate { get; set; }

        /// <summary>
        /// Mark price type (e.g., "index")
        /// </summary>
        public string MarkType { get; set; }

        /// <summary>
        /// Minimum order size in contracts
        /// Maps to lot_size in SymbolProperties
        /// </summary>
        public long OrderSizeMin { get; set; }

        /// <summary>
        /// Order price rounding precision
        /// Maps to minimum_price_variation in SymbolProperties
        /// </summary>
        public decimal OrderPriceRound { get; set; }
    }
}
