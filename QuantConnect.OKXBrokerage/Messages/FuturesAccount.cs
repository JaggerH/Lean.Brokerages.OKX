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
    /// Futures account details from REST API
    /// Endpoint: GET /futures/{settle}/accounts
    /// https://www.okx.io/docs/developers/futures/en/#list-futures-account
    /// Note: This endpoint returns a SINGLE object (not an array)
    /// </summary>
    public class FuturesAccount
    {
        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user")]
        public long User { get; set; }

        /// <summary>
        /// Settlement currency (e.g., "USDT", "BTC")
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Wallet balance (accumulated deposits/withdrawals and realized PnL)
        /// Does NOT include unrealized PnL
        /// total = SUM(history_dnw, history_pnl, history_fee, history_refr, history_fund)
        /// </summary>
        [JsonProperty("total")]
        public string Total { get; set; }

        /// <summary>
        /// Unrealized PnL
        /// </summary>
        [JsonProperty("unrealised_pnl")]
        public string UnrealisedPnl { get; set; }

        /// <summary>
        /// Position margin (occupied by open positions)
        /// </summary>
        [JsonProperty("position_margin")]
        public string PositionMargin { get; set; }

        /// <summary>
        /// Order margin (occupied by open orders)
        /// </summary>
        [JsonProperty("order_margin")]
        public string OrderMargin { get; set; }

        /// <summary>
        /// Available margin for trading or withdrawal
        /// In isolated margin mode, this includes credit amount
        /// **This is the key field to return in GetCashBalance()**
        /// </summary>
        [JsonProperty("available")]
        public string Available { get; set; }

        /// <summary>
        /// Point card amount
        /// </summary>
        [JsonProperty("point")]
        public string Point { get; set; }

        /// <summary>
        /// Bonus amount (demo funds, cannot be withdrawn)
        /// </summary>
        [JsonProperty("bonus")]
        public string Bonus { get; set; }

        /// <summary>
        /// Whether dual position mode is enabled
        /// </summary>
        [JsonProperty("in_dual_mode")]
        public bool InDualMode { get; set; }

        /// <summary>
        /// Position mode: "single" (one-way), "dual" (hedge), "split" (split)
        /// </summary>
        [JsonProperty("position_mode")]
        public string PositionMode { get; set; }

        /// <summary>
        /// Whether unified account mode is enabled
        /// </summary>
        [JsonProperty("enable_credit")]
        public bool EnableCredit { get; set; }

        /// <summary>
        /// Position initial margin (unified account mode)
        /// </summary>
        [JsonProperty("position_initial_margin")]
        public string PositionInitialMargin { get; set; }

        /// <summary>
        /// Position maintenance margin (unified and new classic modes)
        /// </summary>
        [JsonProperty("maintenance_margin")]
        public string MaintenanceMargin { get; set; }

        /// <summary>
        /// Whether evolved classic margin mode is enabled
        /// true = new mode, false = old mode
        /// </summary>
        [JsonProperty("enable_evolved_classic")]
        public bool EnableEvolvedClassic { get; set; }

        /// <summary>
        /// Cross order margin (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_order_margin")]
        public string CrossOrderMargin { get; set; }

        /// <summary>
        /// Cross initial margin (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_initial_margin")]
        public string CrossInitialMargin { get; set; }

        /// <summary>
        /// Cross maintenance margin (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_maintenance_margin")]
        public string CrossMaintenanceMargin { get; set; }

        /// <summary>
        /// Cross unrealized PnL (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_unrealised_pnl")]
        public string CrossUnrealisedPnl { get; set; }

        /// <summary>
        /// Cross available margin (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_available")]
        public string CrossAvailable { get; set; }

        /// <summary>
        /// Cross margin balance (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_margin_balance")]
        public string CrossMarginBalance { get; set; }

        /// <summary>
        /// Cross maintenance margin rate (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_mmr")]
        public string CrossMmr { get; set; }

        /// <summary>
        /// Cross initial margin rate (new classic margin mode)
        /// </summary>
        [JsonProperty("cross_imr")]
        public string CrossImr { get; set; }

        /// <summary>
        /// Isolated position margin (new classic margin mode)
        /// </summary>
        [JsonProperty("isolated_position_margin")]
        public string IsolatedPositionMargin { get; set; }

        /// <summary>
        /// Whether new dual position mode is enabled
        /// </summary>
        [JsonProperty("enable_new_dual_mode")]
        public bool EnableNewDualMode { get; set; }

        /// <summary>
        /// Margin mode:
        /// 0 = classic margin mode
        /// 1 = cross-currency margin mode
        /// 2 = portfolio margin mode
        /// </summary>
        [JsonProperty("margin_mode")]
        public int MarginMode { get; set; }

        /// <summary>
        /// Whether tiered maintenance margin calculation is enabled
        /// </summary>
        [JsonProperty("enable_tiered_mm")]
        public bool EnableTieredMm { get; set; }

        /// <summary>
        /// Historical statistics
        /// </summary>
        [JsonProperty("history")]
        public FuturesAccountHistory History { get; set; }
    }

    /// <summary>
    /// Historical statistics for Futures account
    /// </summary>
    public class FuturesAccountHistory
    {
        /// <summary>
        /// Accumulated deposits and withdrawals
        /// </summary>
        [JsonProperty("dnw")]
        public string Dnw { get; set; }

        /// <summary>
        /// Accumulated trading PnL
        /// </summary>
        [JsonProperty("pnl")]
        public string Pnl { get; set; }

        /// <summary>
        /// Accumulated fees
        /// </summary>
        [JsonProperty("fee")]
        public string Fee { get; set; }

        /// <summary>
        /// Accumulated referral rebates
        /// </summary>
        [JsonProperty("refr")]
        public string Refr { get; set; }

        /// <summary>
        /// Accumulated funding fees
        /// </summary>
        [JsonProperty("fund")]
        public string Fund { get; set; }

        /// <summary>
        /// Accumulated point card deposits/withdrawals
        /// </summary>
        [JsonProperty("point_dnw")]
        public string PointDnw { get; set; }

        /// <summary>
        /// Accumulated point card fee deductions
        /// </summary>
        [JsonProperty("point_fee")]
        public string PointFee { get; set; }

        /// <summary>
        /// Accumulated point card referral rebates
        /// </summary>
        [JsonProperty("point_refr")]
        public string PointRefr { get; set; }

        /// <summary>
        /// Accumulated bonus deposits/withdrawals
        /// </summary>
        [JsonProperty("bonus_dnw")]
        public string BonusDnw { get; set; }

        /// <summary>
        /// Accumulated bonus deductions
        /// </summary>
        [JsonProperty("bonus_offset")]
        public string BonusOffset { get; set; }
    }
}
