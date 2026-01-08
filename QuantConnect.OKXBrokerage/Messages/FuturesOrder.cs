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
    /// OKX Futures Order message
    /// Represents futures.orders WebSocket channel messages
    /// </summary>
    [JsonConverter(typeof(FuturesOrderConverter))]
    public class FuturesOrder
    {
        /// <summary>
        /// Order ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Contract name (e.g., BTC_USDT)
        /// Note: This is called "contract" in Futures, not "currency_pair" as in Spot
        /// </summary>
        [JsonProperty("contract")]
        public string Contract { get; set; }

        /// <summary>
        /// Client Order ID (user-defined text)
        /// </summary>
        [JsonProperty("text")]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Order creation time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("create_time_ms")]
        public long CreateTimeMs { get; set; }

        /// <summary>
        /// Order creation time (Unix timestamp in seconds) - deprecated but kept for compatibility
        /// </summary>
        [JsonProperty("create_time")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Order finish time (Unix timestamp in milliseconds), 0 if not finished
        /// </summary>
        [JsonProperty("finish_time_ms")]
        public long FinishTimeMs { get; set; }

        /// <summary>
        /// Order finish time (Unix timestamp in seconds), 0 if not finished
        /// </summary>
        [JsonProperty("finish_time")]
        public long FinishTime { get; set; }

        /// <summary>
        /// Last update time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("update_time")]
        public long UpdateTime { get; set; }

        /// <summary>
        /// Order status: open, finished
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Order size. Positive for bids (long), negative for asks (short)
        /// Note: This is called "size" in Futures, representing number of contracts
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Order price. 0 indicates market order
        /// </summary>
        [JsonProperty("price")]
        public string Price { get; set; }

        /// <summary>
        /// Fill price of the order
        /// </summary>
        [JsonProperty("fill_price")]
        public string FillPrice { get; set; }

        /// <summary>
        /// Maker fee rate
        /// </summary>
        [JsonProperty("mkfr")]
        public string Mkfr { get; set; }

        /// <summary>
        /// Taker fee rate
        /// </summary>
        [JsonProperty("tkfr")]
        public string Tkfr { get; set; }

        /// <summary>
        /// Amount left to trade
        /// </summary>
        [JsonProperty("left")]
        public long Left { get; set; }

        /// <summary>
        /// Time in force: gtc, ioc, poc, fok
        /// </summary>
        [JsonProperty("tif")]
        public string TimeInForce { get; set; }

        /// <summary>
        /// Iceberg order display amount. 0 or unspecified means normal order
        /// </summary>
        [JsonProperty("iceberg")]
        public long Iceberg { get; set; }

        /// <summary>
        /// Whether this order is a close position order
        /// </summary>
        [JsonProperty("is_close")]
        public bool IsClose { get; set; }

        /// <summary>
        /// Whether this order is a liquidation order
        /// </summary>
        [JsonProperty("is_liq")]
        public bool IsLiq { get; set; }

        /// <summary>
        /// Whether this order is reduce-only
        /// </summary>
        [JsonProperty("is_reduce_only")]
        public bool IsReduceOnly { get; set; }

        /// <summary>
        /// How the order was finished:
        /// - filled: completely filled
        /// - cancelled: manually cancelled
        /// - liquidated: cancelled due to liquidation
        /// - ioc: IOC order not filled
        /// - auto_deleveraging: ADL finished
        /// - reduce_only: cancelled due to reduce-only setting
        /// - position_close: cancelled due to position close
        /// - stp: cancelled due to self-trade prevention
        /// - _new: new order
        /// - _update: order filled or partially filled or updated
        /// - reduce_out: reduce-only order excluded
        /// </summary>
        [JsonProperty("finish_as")]
        public string FinishAs { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

        /// <summary>
        /// Referrer user ID
        /// </summary>
        [JsonProperty("refu")]
        public long Refu { get; set; }

        /// <summary>
        /// Referrer rebate
        /// </summary>
        [JsonProperty("refr")]
        public string Refr { get; set; }

        /// <summary>
        /// STP group ID. Orders between users with same non-zero stp_id won't be self-traded
        /// </summary>
        [JsonProperty("stp_id")]
        public string StpId { get; set; }

        /// <summary>
        /// Self-trade prevention action: cn (cancel newest), co (cancel oldest), cb (cancel both)
        /// </summary>
        [JsonProperty("stp_act")]
        public string StpAct { get; set; }

        /// <summary>
        /// User-defined amendment text
        /// </summary>
        [JsonProperty("amend_text")]
        public string AmendText { get; set; }

        /// <summary>
        /// Update ID
        /// </summary>
        [JsonProperty("update_id")]
        public long UpdateId { get; set; }

        /// <summary>
        /// Business info text
        /// </summary>
        [JsonProperty("biz_info")]
        public string BizInfo { get; set; }

        /// <summary>
        /// Take profit price
        /// </summary>
        [JsonProperty("stop_profit_price")]
        public string StopProfitPrice { get; set; }

        /// <summary>
        /// Stop loss price
        /// </summary>
        [JsonProperty("stop_loss_price")]
        public string StopLossPrice { get; set; }

        /// <summary>
        /// Set side to close dual-mode position.
        /// close_long closes the long side; close_short closes the short one.
        /// Note: size also needs to be set to 0 when using auto_size
        /// </summary>
        [JsonProperty("auto_size")]
        public string AutoSize { get; set; }
    }
}
