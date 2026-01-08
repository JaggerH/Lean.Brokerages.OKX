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
    /// Represents OKX v5 position information
    /// https://www.okx.com/docs-v5/en/#rest-api-account-get-positions
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Instrument type: MARGIN, SWAP, FUTURES, OPTION
        /// </summary>
        [JsonProperty("instType")]
        public string InstrumentType { get; set; }

        /// <summary>
        /// Margin mode: cross, isolated
        /// </summary>
        [JsonProperty("mgnMode")]
        public string MarginMode { get; set; }

        /// <summary>
        /// Position ID
        /// </summary>
        [JsonProperty("posId")]
        public string PositionId { get; set; }

        /// <summary>
        /// Position side: long, short (only for hedge mode)
        /// </summary>
        [JsonProperty("posSide")]
        public string PositionSide { get; set; }

        /// <summary>
        /// Quantity of positions (positive for long, negative for short in net mode)
        /// </summary>
        [JsonProperty("pos")]
        public string Quantity { get; set; }

        /// <summary>
        /// Base currency (for MARGIN only)
        /// </summary>
        [JsonProperty("baseCcy")]
        public string BaseCurrency { get; set; }

        /// <summary>
        /// Quote currency (for MARGIN only)
        /// </summary>
        [JsonProperty("quoteCcy")]
        public string QuoteCurrency { get; set; }

        /// <summary>
        /// Base currency balance (for MARGIN only)
        /// </summary>
        [JsonProperty("baseBal")]
        public string BaseBalance { get; set; }

        /// <summary>
        /// Quote currency balance (for MARGIN only)
        /// </summary>
        [JsonProperty("quoteBal")]
        public string QuoteBalance { get; set; }

        /// <summary>
        /// Base borrowed (for MARGIN only)
        /// </summary>
        [JsonProperty("baseBorrowed")]
        public string BaseBorrowed { get; set; }

        /// <summary>
        /// Base interest (for MARGIN only)
        /// </summary>
        [JsonProperty("baseInterest")]
        public string BaseInterest { get; set; }

        /// <summary>
        /// Quote borrowed (for MARGIN only)
        /// </summary>
        [JsonProperty("quoteBorrowed")]
        public string QuoteBorrowed { get; set; }

        /// <summary>
        /// Quote interest (for MARGIN only)
        /// </summary>
        [JsonProperty("quoteInterest")]
        public string QuoteInterest { get; set; }

        /// <summary>
        /// Instrument ID (e.g., BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Leverage (not applicable to OPTION seller)
        /// </summary>
        [JsonProperty("lever")]
        public string Leverage { get; set; }

        /// <summary>
        /// Liquidation price (not applicable to OPTION)
        /// </summary>
        [JsonProperty("liqPx")]
        public string LiquidationPrice { get; set; }

        /// <summary>
        /// Mark price
        /// </summary>
        [JsonProperty("markPx")]
        public string MarkPrice { get; set; }

        /// <summary>
        /// Initial margin requirement
        /// </summary>
        [JsonProperty("imr")]
        public string InitialMarginRequirement { get; set; }

        /// <summary>
        /// Margin
        /// </summary>
        [JsonProperty("margin")]
        public string Margin { get; set; }

        /// <summary>
        /// Margin ratio
        /// </summary>
        [JsonProperty("mgnRatio")]
        public string MarginRatio { get; set; }

        /// <summary>
        /// Maintenance margin requirement
        /// </summary>
        [JsonProperty("mmr")]
        public string MaintenanceMarginRequirement { get; set; }

        /// <summary>
        /// Liabilities (applicable to MARGIN)
        /// </summary>
        [JsonProperty("liab")]
        public string Liabilities { get; set; }

        /// <summary>
        /// Liabilities currency (applicable to MARGIN)
        /// </summary>
        [JsonProperty("liabCcy")]
        public string LiabilitiesCurrency { get; set; }

        /// <summary>
        /// Interest
        /// </summary>
        [JsonProperty("interest")]
        public string Interest { get; set; }

        /// <summary>
        /// Trade ID (last trade)
        /// </summary>
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }

        /// <summary>
        /// Option value (applicable to OPTION)
        /// </summary>
        [JsonProperty("optVal")]
        public string OptionValue { get; set; }

        /// <summary>
        /// Notional value in USD
        /// </summary>
        [JsonProperty("notionalUsd")]
        public string NotionalUsd { get; set; }

        /// <summary>
        /// Auto-deleveraging indicator: 1,2,3,4,5 (5 is most likely)
        /// </summary>
        [JsonProperty("adl")]
        public string AutoDeleveraging { get; set; }

        /// <summary>
        /// Currency (margin currency)
        /// </summary>
        [JsonProperty("ccy")]
        public string Currency { get; set; }

        /// <summary>
        /// Last traded price
        /// </summary>
        [JsonProperty("last")]
        public string Last { get; set; }

        /// <summary>
        /// USD price (applicable to OPTION)
        /// </summary>
        [JsonProperty("usdPx")]
        public string UsdPrice { get; set; }

        /// <summary>
        /// Unrealized profit and loss
        /// </summary>
        [JsonProperty("upl")]
        public string UnrealizedPnL { get; set; }

        /// <summary>
        /// Unrealized profit and loss ratio
        /// </summary>
        [JsonProperty("uplRatio")]
        public string UnrealizedPnLRatio { get; set; }

        /// <summary>
        /// Instrument family (for derivatives)
        /// </summary>
        [JsonProperty("instFamily")]
        public string InstrumentFamily { get; set; }

        /// <summary>
        /// Delta (for options)
        /// </summary>
        [JsonProperty("delta")]
        public string Delta { get; set; }

        /// <summary>
        /// Gamma (for options)
        /// </summary>
        [JsonProperty("gamma")]
        public string Gamma { get; set; }

        /// <summary>
        /// Vega (for options)
        /// </summary>
        [JsonProperty("vega")]
        public string Vega { get; set; }

        /// <summary>
        /// Theta (for options)
        /// </summary>
        [JsonProperty("theta")]
        public string Theta { get; set; }

        /// <summary>
        /// Position creation time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("cTime")]
        public string CreateTime { get; set; }

        /// <summary>
        /// Latest position update time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("uTime")]
        public string UpdateTime { get; set; }

        /// <summary>
        /// Push time of positions information (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("pTime")]
        public string PushTime { get; set; }

        /// <summary>
        /// Average open price
        /// </summary>
        [JsonProperty("avgPx")]
        public string AveragePrice { get; set; }

        /// <summary>
        /// Break-even price
        /// </summary>
        [JsonProperty("bePx")]
        public string BreakEvenPrice { get; set; }

        /// <summary>
        /// Quantity that can be closed
        /// </summary>
        [JsonProperty("closeOrderAlgo")]
        public string CloseOrderAlgo { get; set; }

        /// <summary>
        /// Position type: net(one-way), long/short(hedge)
        /// </summary>
        [JsonProperty("posMode")]
        public string PositionMode { get; set; }

        /// <summary>
        /// Realized profit and loss
        /// </summary>
        [JsonProperty("realizedPnl")]
        public string RealizedPnL { get; set; }

        /// <summary>
        /// Fee
        /// </summary>
        [JsonProperty("fee")]
        public string Fee { get; set; }

        /// <summary>
        /// Funding fee
        /// </summary>
        [JsonProperty("fundingFee")]
        public string FundingFee { get; set; }

        /// <summary>
        /// Latest price from orderbook
        /// </summary>
        [JsonProperty("bizRefId")]
        public string BizRefId { get; set; }

        /// <summary>
        /// Business type
        /// </summary>
        [JsonProperty("bizRefType")]
        public string BizRefType { get; set; }
    }
}
