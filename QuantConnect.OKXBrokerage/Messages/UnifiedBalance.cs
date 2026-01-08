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
    /// Unified account balance for a single currency
    /// From GET /unified/accounts response
    /// </summary>
    /// <remarks>
    /// Business-oriented model - fields use appropriate types (decimal, not string)
    /// Parsing handled in UnifiedBalanceConverter
    /// </remarks>
    [JsonConverter(typeof(Converters.UnifiedBalanceConverter))]
    public class UnifiedBalance
    {
        /// <summary>
        /// Available balance
        /// Different calculation per mode (single/multi/portfolio)
        /// </summary>
        public decimal Available { get; set; }

        /// <summary>
        /// Frozen/locked balance
        /// </summary>
        public decimal Freeze { get; set; }

        /// <summary>
        /// Borrowed amount
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal Borrowed { get; set; }

        /// <summary>
        /// Negative balance liability
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal NegativeLiability { get; set; }

        /// <summary>
        /// Equity (net asset value)
        /// Valid in all modes (single_currency/multi_currency/portfolio)
        /// </summary>
        public decimal Equity { get; set; }

        /// <summary>
        /// Total liability (borrowed + interest)
        /// Valid in multi_currency/portfolio modes, 0 in single_currency mode
        /// </summary>
        public decimal TotalLiability { get; set; }

        /// <summary>
        /// Spot hedge quantity
        /// Valid in portfolio mode, 0 in other modes
        /// </summary>
        public decimal SpotInUse { get; set; }

        /// <summary>
        /// Funding account balance (余币宝理财)
        /// Valid when funding is enabled as margin
        /// </summary>
        public decimal Funding { get; set; }

        /// <summary>
        /// Cross margin balance
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal CrossBalance { get; set; }

        /// <summary>
        /// Isolated margin balance
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal IsolatedBalance { get; set; }

        /// <summary>
        /// Initial margin (IM)
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal InitialMargin { get; set; }

        /// <summary>
        /// Maintenance margin (MM)
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal MaintenanceMargin { get; set; }

        /// <summary>
        /// Initial margin rate (IMR)
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal InitialMarginRate { get; set; }

        /// <summary>
        /// Maintenance margin rate (MMR)
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal MaintenanceMarginRate { get; set; }

        /// <summary>
        /// Margin balance
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal MarginBalance { get; set; }

        /// <summary>
        /// Available margin
        /// Valid in single_currency mode, 0 in multi_currency/portfolio modes
        /// </summary>
        public decimal AvailableMargin { get; set; }

        /// <summary>
        /// Whether currency is enabled as collateral
        /// </summary>
        public bool EnabledCollateral { get; set; }
    }
}
