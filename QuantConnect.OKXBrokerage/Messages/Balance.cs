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
using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Represents OKX v5 account balance
    /// https://www.okx.com/docs-v5/en/#rest-api-account-get-balance
    /// </summary>
    public class AccountBalance
    {
        /// <summary>
        /// Total equity in USD
        /// </summary>
        [JsonProperty("totalEq")]
        public string TotalEquity { get; set; }

        /// <summary>
        /// Isolated margin equity in USD
        /// </summary>
        [JsonProperty("isoEq")]
        public string IsoEquity { get; set; }

        /// <summary>
        /// Adjusted/Effective equity in USD
        /// </summary>
        [JsonProperty("adjEq")]
        public string AdjEquity { get; set; }

        /// <summary>
        /// Margin frozen for pending orders in USD
        /// </summary>
        [JsonProperty("ordFroz")]
        public string OrderFrozen { get; set; }

        /// <summary>
        /// Initial margin requirement in USD
        /// </summary>
        [JsonProperty("imr")]
        public string InitialMarginRequirement { get; set; }

        /// <summary>
        /// Maintenance margin requirement in USD
        /// </summary>
        [JsonProperty("mmr")]
        public string MaintenanceMarginRequirement { get; set; }

        /// <summary>
        /// Margin ratio
        /// </summary>
        [JsonProperty("mgnRatio")]
        public string MarginRatio { get; set; }

        /// <summary>
        /// Notional value of positions in USD
        /// </summary>
        [JsonProperty("notionalUsd")]
        public string NotionalUsd { get; set; }

        /// <summary>
        /// Update time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("uTime")]
        public string UpdateTime { get; set; }

        /// <summary>
        /// Details of each currency
        /// </summary>
        [JsonProperty("details")]
        public List<BalanceDetail> Details { get; set; }
    }

    /// <summary>
    /// Balance detail for a specific currency
    /// </summary>
    public class BalanceDetail
    {
        /// <summary>
        /// Currency (e.g., USDT, BTC)
        /// </summary>
        [JsonProperty("ccy")]
        public string Currency { get; set; }

        /// <summary>
        /// Equity of the currency
        /// </summary>
        [JsonProperty("eq")]
        public string Equity { get; set; }

        /// <summary>
        /// Cash balance
        /// </summary>
        [JsonProperty("cashBal")]
        public string CashBalance { get; set; }

        /// <summary>
        /// Update time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("uTime")]
        public string UpdateTime { get; set; }

        /// <summary>
        /// Isolated margin equity
        /// </summary>
        [JsonProperty("isoEq")]
        public string IsoEquity { get; set; }

        /// <summary>
        /// Available equity
        /// </summary>
        [JsonProperty("availEq")]
        public string AvailableEquity { get; set; }

        /// <summary>
        /// Discount equity in USD
        /// </summary>
        [JsonProperty("disEq")]
        public string DiscountEquity { get; set; }

        /// <summary>
        /// Available balance
        /// </summary>
        [JsonProperty("availBal")]
        public string AvailableBalance { get; set; }

        /// <summary>
        /// Frozen balance
        /// </summary>
        [JsonProperty("frozenBal")]
        public string FrozenBalance { get; set; }

        /// <summary>
        /// Margin frozen for open orders
        /// </summary>
        [JsonProperty("ordFrozen")]
        public string OrderFrozen { get; set; }

        /// <summary>
        /// Liabilities (borrowed)
        /// </summary>
        [JsonProperty("liab")]
        public string Liabilities { get; set; }

        /// <summary>
        /// Unrealized profit and loss
        /// </summary>
        [JsonProperty("upl")]
        public string UnrealizedPnL { get; set; }

        /// <summary>
        /// Liabilities due to Unrealized loss
        /// </summary>
        [JsonProperty("uplLiab")]
        public string UnrealizedPnLLiabilities { get; set; }

        /// <summary>
        /// Cross liabilities
        /// </summary>
        [JsonProperty("crossLiab")]
        public string CrossLiabilities { get; set; }

        /// <summary>
        /// Isolated liabilities
        /// </summary>
        [JsonProperty("isoLiab")]
        public string IsoLiabilities { get; set; }

        /// <summary>
        /// Margin ratio
        /// </summary>
        [JsonProperty("mgnRatio")]
        public string MarginRatio { get; set; }

        /// <summary>
        /// Interest
        /// </summary>
        [JsonProperty("interest")]
        public string Interest { get; set; }

        /// <summary>
        /// Risk indicator: twap(Time-weighted average price)
        /// </summary>
        [JsonProperty("twap")]
        public string Twap { get; set; }

        /// <summary>
        /// Max loan
        /// </summary>
        [JsonProperty("maxLoan")]
        public string MaxLoan { get; set; }

        /// <summary>
        /// Equity in USD
        /// </summary>
        [JsonProperty("eqUsd")]
        public string EquityUsd { get; set; }

        /// <summary>
        /// Notional leverage
        /// </summary>
        [JsonProperty("notionalLever")]
        public string NotionalLeverage { get; set; }

        /// <summary>
        /// Strategy equity
        /// </summary>
        [JsonProperty("stgyEq")]
        public string StrategyEquity { get; set; }

        /// <summary>
        /// Isolated unrealized profit and loss
        /// </summary>
        [JsonProperty("isoUpl")]
        public string IsoUnrealizedPnL { get; set; }
    }
}
