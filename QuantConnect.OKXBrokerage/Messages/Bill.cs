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
    /// OKX account bill record from GET /api/v5/account/bills.
    /// Used to pull funding fee (type=8) and interest deduction (type=7) records.
    /// </summary>
    /// <remarks>
    /// OKX field mapping:
    /// | OKX field | Type             | Description                          |
    /// |-----------|------------------|--------------------------------------|
    /// | billId    | string           | Bill ID (unique, descending)         |
    /// | instType  | string           | SPOT, MARGIN, SWAP, FUTURES, OPTION  |
    /// | instId    | string           | Instrument ID (e.g. BTC-USDT-SWAP)   |
    /// | type      | string           | Bill type: 7=interest, 8=funding     |
    /// | subType   | string           | Sub bill type                        |
    /// | ccy       | string           | Currency (e.g. USDT)                 |
    /// | balChg    | string(decimal)  | Balance change (positive=income)     |
    /// | bal       | string(decimal)  | Balance after this bill              |
    /// | sz        | string(decimal)  | Quantity/size                        |
    /// | ts        | string(ms epoch) | Timestamp                            |
    /// </remarks>
    public class Bill
    {
        /// <summary>Bill ID (unique identifier, used for pagination cursor).</summary>
        [JsonProperty("billId")]
        public string BillId { get; set; }

        /// <summary>Instrument type (SPOT, MARGIN, SWAP, FUTURES, OPTION).</summary>
        [JsonProperty("instType")]
        public string InstType { get; set; }

        /// <summary>Instrument ID (e.g. "BTC-USDT-SWAP").</summary>
        [JsonProperty("instId")]
        public string InstId { get; set; }

        /// <summary>Bill type: "7" = interest deduction, "8" = funding fee.</summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>Sub bill type for finer categorization.</summary>
        [JsonProperty("subType")]
        public string SubType { get; set; }

        /// <summary>Currency (e.g. "USDT", "BTC").</summary>
        [JsonProperty("ccy")]
        public string Ccy { get; set; }

        /// <summary>Balance change amount. Positive = income, negative = expense.</summary>
        [JsonProperty("balChg")]
        public string BalanceChange { get; set; }

        /// <summary>Balance after this bill.</summary>
        [JsonProperty("bal")]
        public string Balance { get; set; }

        /// <summary>Quantity or position size related to this bill.</summary>
        [JsonProperty("sz")]
        public string Size { get; set; }

        /// <summary>Timestamp in Unix milliseconds.</summary>
        [JsonProperty("ts")]
        public string Ts { get; set; }
    }
}
