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
    /// Unified account information from GET /unified/accounts
    /// </summary>
    /// <remarks>
    /// Business-oriented model - fields use appropriate types (decimal, not string)
    /// Parsing handled in UnifiedAccountConverter
    /// </remarks>
    [JsonConverter(typeof(Converters.UnifiedAccountConverter))]
    public class UnifiedAccount
    {
        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Last refresh time (Unix timestamp)
        /// </summary>
        public long RefreshTime { get; set; }

        /// <summary>
        /// Whether account is locked
        /// Valid in multi_currency/portfolio modes
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// Unified account mode
        /// Values: classic, single_currency, multi_currency, portfolio
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Per-currency balances
        /// Key: Currency code (e.g., "BTC", "USDT")
        /// Value: UnifiedBalance object
        /// </summary>
        public Dictionary<string, UnifiedBalance> Balances { get; set; }

        /// <summary>
        /// Total borrowed in USD
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalBorrowed { get; set; }

        /// <summary>
        /// Total initial margin
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalInitialMargin { get; set; }

        /// <summary>
        /// Total margin balance
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalMarginBalance { get; set; }

        /// <summary>
        /// Total maintenance margin
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalMaintenanceMargin { get; set; }

        /// <summary>
        /// Total initial margin rate
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalInitialMarginRate { get; set; }

        /// <summary>
        /// Total maintenance margin rate
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalMaintenanceMarginRate { get; set; }

        /// <summary>
        /// Total available margin
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalAvailableMargin { get; set; }

        /// <summary>
        /// Unified account total assets (all modes)
        /// </summary>
        public decimal UnifiedAccountTotal { get; set; }

        /// <summary>
        /// Unified account total liability
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal UnifiedAccountTotalLiability { get; set; }

        /// <summary>
        /// Unified account total equity (all modes)
        /// </summary>
        public decimal UnifiedAccountTotalEquity { get; set; }

        /// <summary>
        /// Actual leverage
        /// Valid in multi_currency/portfolio modes
        /// </summary>
        public decimal Leverage { get; set; }

        /// <summary>
        /// Total spot order loss in USDT
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal SpotOrderLoss { get; set; }

        /// <summary>
        /// Spot hedge status
        /// </summary>
        public bool SpotHedge { get; set; }

        /// <summary>
        /// Whether funding account (余币宝) is used as margin
        /// </summary>
        public bool UseFunding { get; set; }

        /// <summary>
        /// Whether all currencies are enabled as collateral
        /// </summary>
        public bool IsAllCollateral { get; set; }
    }
}
