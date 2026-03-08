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

using System;
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Securities.UnifiedMargin;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// OKX interest rate response from GET /api/v5/account/interest-rate (private endpoint).
    /// Returns the current daily borrow interest rate per currency.
    /// </summary>
    /// <remarks>
    /// OKX field mapping:
    /// | OKX field    | Type            | Maps to               |
    /// |--------------|-----------------|-----------------------|
    /// | ccy          | string          | currency key          |
    /// | interestRate | string(decimal) | BorrowRate.DailyRate |
    /// </remarks>
    public class InterestRate
    {
        /// <summary>Currency (e.g. "BTC", "ETH", "USDT").</summary>
        [JsonProperty("ccy")]
        public string Ccy { get; set; }

        /// <summary>Daily interest rate as a decimal string (e.g. "0.0001" for 0.01%/day).</summary>
        [JsonProperty("interestRate")]
        public string InterestRateValue { get; set; }

        /// <summary>
        /// Converts this response item to a <see cref="BrokerageDataService.BorrowRate"/>.
        /// </summary>
        public BrokerageDataService.BorrowRate ToBorrowRate()
        {
            var rate = string.IsNullOrEmpty(InterestRateValue) ? 0m :
                decimal.TryParse(InterestRateValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

            // OKX interest-rate response does not include a server timestamp;
            // use the local receive time as the best available proxy.
            return new BrokerageDataService.BorrowRate
            {
                DailyRate = rate,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
