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
    /// Represents OKX v5 instrument (trading pair) information
    /// https://www.okx.com/docs-v5/en/#rest-api-public-data-get-instruments
    /// </summary>
    public class Instrument
    {
        /// <summary>
        /// Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION
        /// </summary>
        [JsonProperty("instType")]
        public string InstrumentType { get; set; }

        /// <summary>
        /// Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Base currency (e.g., BTC in BTC-USDT)
        /// </summary>
        [JsonProperty("baseCcy")]
        public string BaseCurrency { get; set; }

        /// <summary>
        /// Quote currency (e.g., USDT in BTC-USDT)
        /// </summary>
        [JsonProperty("quoteCcy")]
        public string QuoteCurrency { get; set; }

        /// <summary>
        /// Settlement currency (for derivatives)
        /// </summary>
        [JsonProperty("settleCcy")]
        public string SettlementCurrency { get; set; }

        /// <summary>
        /// Contract value (for derivatives)
        /// </summary>
        [JsonProperty("ctVal")]
        public string ContractValue { get; set; }

        /// <summary>
        /// Contract multiplier (for derivatives)
        /// </summary>
        [JsonProperty("ctMult")]
        public string ContractMultiplier { get; set; }

        /// <summary>
        /// Contract value currency
        /// </summary>
        [JsonProperty("ctValCcy")]
        public string ContractValueCurrency { get; set; }

        /// <summary>
        /// Option type: C (call) or P (put)
        /// </summary>
        [JsonProperty("optType")]
        public string OptionType { get; set; }

        /// <summary>
        /// Strike price (for options)
        /// </summary>
        [JsonProperty("stk")]
        public string StrikePrice { get; set; }

        /// <summary>
        /// Listing time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("listTime")]
        public string ListTime { get; set; }

        /// <summary>
        /// Expiry time (Unix timestamp in milliseconds, for derivatives)
        /// </summary>
        [JsonProperty("expTime")]
        public string ExpiryTime { get; set; }

        /// <summary>
        /// Maximum limit order size
        /// </summary>
        [JsonProperty("maxLmtSz")]
        public string MaxLimitSize { get; set; }

        /// <summary>
        /// Maximum market order size
        /// </summary>
        [JsonProperty("maxMktSz")]
        public string MaxMarketSize { get; set; }

        /// <summary>
        /// Minimum order size
        /// </summary>
        [JsonProperty("minSz")]
        public string MinSize { get; set; }

        /// <summary>
        /// Lot size (order quantity increment)
        /// </summary>
        [JsonProperty("lotSz")]
        public string LotSize { get; set; }

        /// <summary>
        /// Tick size (price increment)
        /// </summary>
        [JsonProperty("tickSz")]
        public string TickSize { get; set; }

        /// <summary>
        /// Contract type: linear or inverse (for derivatives)
        /// </summary>
        [JsonProperty("ctType")]
        public string ContractType { get; set; }

        /// <summary>
        /// Alias (e.g., this_week, next_week)
        /// </summary>
        [JsonProperty("alias")]
        public string Alias { get; set; }

        /// <summary>
        /// Instrument state: live, suspend, preopen
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }
    }
}
