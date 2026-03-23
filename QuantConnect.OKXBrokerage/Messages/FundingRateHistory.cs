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
    /// OKX funding rate history entry.
    /// REST endpoint: GET /api/v5/public/funding-rate-history?instId={instId}&amp;limit=100
    /// </summary>
    /// <remarks>
    /// OKX field mapping:
    /// | OKX field    | Type             | Description                    |
    /// |--------------|------------------|--------------------------------|
    /// | instId       | string           | Instrument ID                  |
    /// | fundingRate  | string(decimal)  | Predicted funding rate at time  |
    /// | realizedRate | string(decimal)  | Actual settled rate             |
    /// | fundingTime  | string(ms epoch) | Settlement timestamp            |
    /// </remarks>
    public class FundingRateHistory
    {
        [JsonProperty("instId")]
        public string InstId { get; set; }

        /// <summary>Predicted funding rate at the time.</summary>
        [JsonProperty("fundingRate")]
        public string FundingRateValue { get; set; }

        /// <summary>Actual realized/settled funding rate.</summary>
        [JsonProperty("realizedRate")]
        public string RealizedRate { get; set; }

        /// <summary>Settlement timestamp (Unix ms).</summary>
        [JsonProperty("fundingTime")]
        public string FundingTime { get; set; }

        /// <summary>
        /// Converts to <see cref="BrokerageDataService.FundingRate"/> for history backfill.
        /// Uses realizedRate (actual settled rate) as CurrentRate.
        /// </summary>
        public BrokerageDataService.FundingRate ToFundingRate()
        {
            static decimal ParseDecimal(string raw) =>
                string.IsNullOrEmpty(raw) ? 0m :
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

            static DateTime ParseMs(string raw) =>
                string.IsNullOrEmpty(raw) ? default :
                long.TryParse(raw, out var ms) ?
                    DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime : default;

            var settlementTime = ParseMs(FundingTime);
            return new BrokerageDataService.FundingRate
            {
                CurrentRate = ParseDecimal(RealizedRate),
                SettlementTime = settlementTime,
                NextSettlementTime = default,
                UpdatedAt = settlementTime
            };
        }
    }
}
