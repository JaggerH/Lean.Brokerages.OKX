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
    /// Outer response wrapper for GET /api/v5/account/interest-limits (private endpoint).
    /// The API returns <c>data: [{ records: [...] }]</c> — this class maps the inner object.
    /// </summary>
    public class InterestLimitResponse
    {
        /// <summary>Per-currency borrow limit records.</summary>
        [JsonProperty("records")]
        public List<InterestLimitRecord> Records { get; set; }
    }

    /// <summary>
    /// Per-currency borrow limit record from the interest-limits API.
    /// </summary>
    /// <remarks>
    /// OKX field mapping (market borrowing, type=2):
    /// | OKX field  | Type            | Description                        |
    /// |------------|-----------------|------------------------------------|
    /// | ccy        | string          | Currency code (e.g. "XRP", "BTC")  |
    /// | loanQuota  | string(decimal) | Total borrowing limit (coin units) |
    /// | usedLmt    | string(decimal) | Currently used borrowing amount    |
    /// | surplusLmt | string(decimal) | Remaining = loanQuota - usedLmt    |
    /// | rate       | string(decimal) | Daily borrow interest rate         |
    ///
    /// Fields availLoan, posLoan, usedLoan, avgRate are deprecated (VIP lending) and always empty.
    /// </remarks>
    public class InterestLimitRecord
    {
        /// <summary>Currency code (e.g. "XRP", "BTC", "USDT").</summary>
        [JsonProperty("ccy")]
        public string Ccy { get; set; }

        /// <summary>Total borrowing limit as a decimal string (coin units).</summary>
        [JsonProperty("loanQuota")]
        public string LoanQuotaRaw { get; set; }

        /// <summary>Parses loanQuota to decimal (coin units). Returns 0 on parse failure.</summary>
        public decimal GetLoanQuota() => ParseHelper.ParseDecimal(LoanQuotaRaw);
    }
}
