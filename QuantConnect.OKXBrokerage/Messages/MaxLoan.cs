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
    /// Response record for GET /api/v5/account/max-loan (private endpoint).
    /// Returns the maximum borrowable amount considering leverage tier limits.
    /// Rate limit: 20 requests per 2 seconds.
    /// </summary>
    /// <remarks>
    /// OKX field mapping:
    /// | OKX field | Type            | Description                                      |
    /// |-----------|-----------------|--------------------------------------------------|
    /// | instId    | string          | Instrument ID (e.g. "LINK-USDT")                 |
    /// | mgnMode   | string          | Margin mode ("cross" or "isolated")              |
    /// | mgnCcy    | string          | Margin currency                                  |
    /// | maxLoan   | string(decimal) | Max borrowable amount at current leverage tier   |
    /// | ccy       | string          | Currency (e.g. "LINK")                           |
    /// | side      | string          | Order side ("buy" or "sell")                     |
    /// </remarks>
    public class MaxLoanRecord
    {
        /// <summary>Instrument ID (e.g. "LINK-USDT").</summary>
        [JsonProperty("instId")]
        public string InstId { get; set; }

        /// <summary>Margin mode ("cross" or "isolated").</summary>
        [JsonProperty("mgnMode")]
        public string MgnMode { get; set; }

        /// <summary>Margin currency.</summary>
        [JsonProperty("mgnCcy")]
        public string MgnCcy { get; set; }

        /// <summary>Maximum borrowable amount as a decimal string (coin units).</summary>
        [JsonProperty("maxLoan")]
        public string MaxLoanRaw { get; set; }

        /// <summary>Currency code (e.g. "LINK").</summary>
        [JsonProperty("ccy")]
        public string Ccy { get; set; }

        /// <summary>Order side ("buy" or "sell").</summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>Parses maxLoan to decimal (coin units). Returns 0 on parse failure.</summary>
        public decimal GetMaxLoan() => ParseHelper.ParseDecimal(MaxLoanRaw);
    }
}
