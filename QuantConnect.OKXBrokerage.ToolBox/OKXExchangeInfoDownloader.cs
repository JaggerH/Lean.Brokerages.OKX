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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.ToolBox;

namespace QuantConnect.OKXBrokerage.ToolBox
{
    /// <summary>
    /// OKX implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class OKXExchangeInfoDownloader : IExchangeInfoDownloader
    {
        private readonly string _apiBaseUrl;
        private readonly HashSet<string> _symbolFilter;

        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.OKX;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OKXExchangeInfoDownloader() : this(null)
        {
        }

        /// <summary>
        /// Creates a new instance with symbol filter
        /// </summary>
        /// <param name="symbolFilter">Optional set of symbols to filter (e.g., {"BTC-USDT", "ETH-USDT"}).
        /// If provided, only these symbols will be downloaded. If null, all symbols are downloaded.</param>
        public OKXExchangeInfoDownloader(HashSet<string> symbolFilter)
        {
            _apiBaseUrl = OKXEnvironment.RestApiUrl;
            _symbolFilter = symbolFilter;
        }

        /// <summary>
        /// Pulls symbol properties data from OKX API
        /// </summary>
        /// <returns>Enumerable of CSV-formatted symbol properties</returns>
        public IEnumerable<string> Get()
        {
            // Download spot instruments
            var spotData = Extensions.DownloadData($"{_apiBaseUrl}/api/v5/public/instruments?instType=SPOT");
            var spotResponse = JsonConvert.DeserializeObject<InstrumentsResponse>(spotData);

            // Build tokenized stock registry for futures descriptions
            var tokenizedStockNames = new Dictionary<string, string>();
            foreach (var instrument in spotResponse.Data)
            {
                // Check if this might be a tokenized stock (you may need to adjust this logic)
                if (!string.IsNullOrEmpty(instrument.BaseCurrency))
                {
                    // For now, store base currency name
                    if (!tokenizedStockNames.ContainsKey(instrument.BaseCurrency))
                    {
                        tokenizedStockNames[instrument.BaseCurrency] = instrument.BaseCurrency;
                    }
                }
            }

            // Apply symbol filter to spot instruments if provided
            var filteredSpotInstruments = spotResponse.Data;
            if (_symbolFilter != null && _symbolFilter.Count > 0)
            {
                filteredSpotInstruments = spotResponse.Data.Where(i => _symbolFilter.Contains(i.InstrumentId)).ToList();
            }

            // Generate spot CSV entries
            foreach (var instrument in filteredSpotInstruments)
            {
                if (string.IsNullOrEmpty(instrument.InstrumentId) ||
                    string.IsNullOrEmpty(instrument.BaseCurrency) ||
                    string.IsNullOrEmpty(instrument.QuoteCurrency))
                {
                    continue;
                }

                var symbol = $"{instrument.BaseCurrency}{instrument.QuoteCurrency}"; // BTCUSDT (no separator)
                var description = instrument.BaseCurrency; // Use base currency as description
                var tickSz = decimal.Parse(instrument.TickSize);
                var lotSz = decimal.Parse(instrument.LotSize);
                var minSz = decimal.Parse(instrument.MinSize);

                yield return $"{Market.ToLowerInvariant()},{symbol},crypto,{description},{instrument.QuoteCurrency},1,{tickSz.NormalizeToStr()},{lotSz.NormalizeToStr()},{instrument.InstrumentId},{minSz.NormalizeToStr()},,";
            }

            // Download swap/perpetual futures instruments
            var futuresData = Extensions.DownloadData($"{_apiBaseUrl}/api/v5/public/instruments?instType=SWAP");
            var futuresResponse = JsonConvert.DeserializeObject<InstrumentsResponse>(futuresData);

            // Apply symbol filter to futures if provided
            var filteredFuturesInstruments = futuresResponse.Data;
            if (_symbolFilter != null && _symbolFilter.Count > 0)
            {
                filteredFuturesInstruments = futuresResponse.Data.Where(i => _symbolFilter.Contains(i.InstrumentId)).ToList();
            }

            // Generate futures CSV entries
            foreach (var instrument in filteredFuturesInstruments)
            {
                if (string.IsNullOrEmpty(instrument.InstrumentId))
                {
                    continue;
                }

                // Only include linear contracts to avoid duplicates (e.g., BTC-USD-SWAP vs BTC-USD_UM-SWAP)
                // Linear contracts are settled in quote currency and more commonly traded
                if (!instrument.ContractType.Equals("linear", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                // For SWAP instruments, InstrumentId is like "BTC-USDT-SWAP" or "BTC-USD_UM-SWAP"
                // We need to extract base and quote from the instrument ID
                var parts = instrument.InstrumentId.Split('-');
                if (parts.Length < 2)
                {
                    continue;
                }

                var baseCcy = parts[0];
                var quoteCcy = parts[1].Replace("_UM", ""); // Remove "_UM" suffix if present
                var symbol = $"{baseCcy}{quoteCcy}"; // BTCUSDT or BTCUSD

                // Use tokenized stock name if available, otherwise use base currency
                var description = tokenizedStockNames.TryGetValue(baseCcy, out var baseName)
                    ? $"{baseName} Perpetual"
                    : $"{baseCcy} Perpetual";

                // Contract multiplier: ctVal defines the face value per contract (e.g., 0.01 BTC per contract)
                // ctMult is an additional scaling factor (typically 1 for linear contracts)
                var ctVal = string.IsNullOrEmpty(instrument.ContractValue) ? 1m : decimal.Parse(instrument.ContractValue);
                var ctMult = string.IsNullOrEmpty(instrument.ContractMultiplier) ? 1m : decimal.Parse(instrument.ContractMultiplier);
                var contractMultiplier = ctVal * ctMult;
                var tickSz = decimal.Parse(instrument.TickSize);
                var lotSz = decimal.Parse(instrument.LotSize);
                var minSz = decimal.Parse(instrument.MinSize);

                yield return $"{Market.ToLowerInvariant()},{symbol},cryptofuture,{description},{quoteCcy},{contractMultiplier.NormalizeToStr()},{tickSz.NormalizeToStr()},{lotSz.NormalizeToStr()},{instrument.InstrumentId},{minSz.NormalizeToStr()},,";
            }
        }

        /// <summary>
        /// OKX API response wrapper for instruments
        /// </summary>
        private class InstrumentsResponse
        {
            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("data")]
            public List<Instrument> Data { get; set; }

            [JsonProperty("msg")]
            public string Message { get; set; }
        }
    }
}
