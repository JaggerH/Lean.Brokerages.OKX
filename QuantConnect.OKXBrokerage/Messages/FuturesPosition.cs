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
    /// Represents a futures position from OKX API
    /// https://www.okx.io/docs/developers/futures/en/#list-all-positions-of-a-user
    /// </summary>
    public class FuturesPosition
    {
        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user")]
        public long User { get; set; }

        /// <summary>
        /// Futures contract (e.g., "BTC_USDT")
        /// </summary>
        [JsonProperty("contract")]
        public string Contract { get; set; }

        /// <summary>
        /// Position size (positive for long, negative for short)
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Position leverage (0 means cross margin; positive number means isolated margin)
        /// </summary>
        [JsonProperty("leverage")]
        public string Leverage { get; set; }

        /// <summary>
        /// Position risk limit
        /// </summary>
        [JsonProperty("risk_limit")]
        public string RiskLimit { get; set; }

        /// <summary>
        /// Maximum leverage under current risk limit
        /// </summary>
        [JsonProperty("leverage_max")]
        public string LeverageMax { get; set; }

        /// <summary>
        /// Maintenance rate under current risk limit
        /// </summary>
        [JsonProperty("maintenance_rate")]
        public string MaintenanceRate { get; set; }

        /// <summary>
        /// Position value calculated in settlement currency
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// Position margin
        /// </summary>
        [JsonProperty("margin")]
        public string Margin { get; set; }

        /// <summary>
        /// Entry price
        /// </summary>
        [JsonProperty("entry_price")]
        public string EntryPrice { get; set; }

        /// <summary>
        /// Liquidation price
        /// </summary>
        [JsonProperty("liq_price")]
        public string LiqPrice { get; set; }

        /// <summary>
        /// Current mark price
        /// </summary>
        [JsonProperty("mark_price")]
        public string MarkPrice { get; set; }

        /// <summary>
        /// Initial margin occupied by the position (applicable to portfolio margin account)
        /// </summary>
        [JsonProperty("initial_margin")]
        public string InitialMargin { get; set; }

        /// <summary>
        /// Maintenance margin required for the position (applicable to portfolio margin account)
        /// </summary>
        [JsonProperty("maintenance_margin")]
        public string MaintenanceMargin { get; set; }

        /// <summary>
        /// Unrealized PNL
        /// </summary>
        [JsonProperty("unrealised_pnl")]
        public string UnrealisedPnl { get; set; }

        /// <summary>
        /// Realized PnL
        /// </summary>
        [JsonProperty("realised_pnl")]
        public string RealisedPnl { get; set; }

        /// <summary>
        /// Total realized PNL from closed positions
        /// </summary>
        [JsonProperty("history_pnl")]
        public string HistoryPnl { get; set; }

        /// <summary>
        /// PNL of last position close
        /// </summary>
        [JsonProperty("last_close_pnl")]
        public string LastClosePnl { get; set; }

        /// <summary>
        /// Realized POINT PNL
        /// </summary>
        [JsonProperty("realised_point")]
        public string RealisedPoint { get; set; }

        /// <summary>
        /// History realized POINT PNL
        /// </summary>
        [JsonProperty("history_point")]
        public string HistoryPoint { get; set; }

        /// <summary>
        /// Auto deleveraging ranking (1-5, where 1 is highest, 5 is lowest, and 6 is special case with no position or in liquidation)
        /// </summary>
        [JsonProperty("adl_ranking")]
        public int AdlRanking { get; set; }

        /// <summary>
        /// Current pending order quantity
        /// </summary>
        [JsonProperty("pending_orders")]
        public int PendingOrders { get; set; }

        /// <summary>
        /// Close order details (can be null)
        /// </summary>
        [JsonProperty("close_order")]
        public object CloseOrder { get; set; }

        /// <summary>
        /// Position mode: single (Single position mode), dual_long (Long position in dual mode), or dual_short (Short position in dual mode)
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// Cross margin leverage
        /// </summary>
        [JsonProperty("cross_leverage_limit")]
        public string CrossLeverageLimit { get; set; }

        /// <summary>
        /// Last update time (Unix timestamp)
        /// </summary>
        [JsonProperty("update_time")]
        public long UpdateTime { get; set; }

        /// <summary>
        /// Update ID (increments on each update)
        /// </summary>
        [JsonProperty("update_id")]
        public long UpdateId { get; set; }
    }
}
