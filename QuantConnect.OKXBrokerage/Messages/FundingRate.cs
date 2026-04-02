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
    /// OKX funding rate message — shared by WS push and REST snapshot.
    /// WS channel: funding-rate (public, SWAP only).
    /// REST endpoint: GET /api/v5/public/funding-rate?instId={instId}
    /// </summary>
    /// <remarks>
    /// OKX field mapping:
    /// | OKX field       | Type             | Maps to                              |
    /// |-----------------|------------------|--------------------------------------|
    /// | instId          | string           | key (instrument ID)                  |
    /// | fundingRate     | string(decimal)  | CurrentRate                          |
    /// | nextFundingRate | string(decimal)  | NextRate (empty string = keep prior) |
    /// | fundingTime     | string(ms epoch) | SettlementTime                       |
    /// | nextFundingTime | string(ms epoch) | NextSettlementTime                   |
    /// | ts              | string(ms epoch) | UpdatedAt                            |
    /// </remarks>
    public class FundingRate
    {
        /// <summary>Instrument ID (e.g. "BTC-USDT-SWAP").</summary>
        [JsonProperty("instId")]
        public string InstId { get; set; }

        /// <summary>Current period funding rate as a decimal string (e.g. "0.0001").</summary>
        [JsonProperty("fundingRate")]
        public string FundingRateValue { get; set; }

        /// <summary>Predicted next period funding rate. Empty string when not yet published.</summary>
        [JsonProperty("nextFundingRate")]
        public string NextFundingRate { get; set; }

        /// <summary>Settlement timestamp of the current funding period (Unix ms).</summary>
        [JsonProperty("fundingTime")]
        public string FundingTime { get; set; }

        /// <summary>Settlement timestamp of the next funding period (Unix ms).</summary>
        [JsonProperty("nextFundingTime")]
        public string NextFundingTime { get; set; }

        /// <summary>Message timestamp (Unix ms).</summary>
        [JsonProperty("ts")]
        public string Ts { get; set; }

        /// <summary>Current premium index (decimal string, e.g. "-0.0002").</summary>
        [JsonProperty("premium")]
        public string Premium { get; set; }

        /// <summary>Interest rate per period (decimal string, e.g. "0.0001").</summary>
        [JsonProperty("interestRate")]
        public string InterestRateValue { get; set; }

        /// <summary>Minimum funding rate (decimal string, e.g. "-0.00375").</summary>
        [JsonProperty("minFundingRate")]
        public string MinFundingRate { get; set; }

        /// <summary>Maximum funding rate (decimal string, e.g. "0.00375").</summary>
        [JsonProperty("maxFundingRate")]
        public string MaxFundingRate { get; set; }

        /// <summary>Settlement state: "processing" (settling now) or "settled" (done).</summary>
        [JsonProperty("settState")]
        public string SettState { get; set; }

        /// <summary>
        /// If settState=processing: the rate used for this settlement.
        /// If settState=settled: the rate used for the last settlement.
        /// </summary>
        [JsonProperty("settFundingRate")]
        public string SettFundingRate { get; set; }

        /// <summary>
        /// Converts this message to a <see cref="BrokerageDataService.FundingRate"/>.
        /// When <see cref="NextFundingRate"/> is empty, <c>NextRate</c> is set to zero
        /// (callers should preserve the prior value if needed).
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

            var updatedAt = ParseMs(Ts);
            return new BrokerageDataService.FundingRate
            {
                CurrentRate = ParseDecimal(FundingRateValue),
                NextRate = ParseDecimal(NextFundingRate),
                SettlementTime = ParseMs(FundingTime),
                NextSettlementTime = ParseMs(NextFundingTime),
                UpdatedAt = updatedAt == default ? DateTime.UtcNow : updatedAt,
                PremiumIndex = ParseDecimal(Premium),
                InterestRate = ParseDecimal(InterestRateValue),
                MinFundingRate = ParseDecimal(MinFundingRate),
                MaxFundingRate = ParseDecimal(MaxFundingRate)
            };
        }
    }
}
