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
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Logging;
using QuantConnect.ToolBox;

namespace QuantConnect.OKXBrokerage.ToolBox
{
    /// <summary>
    /// OKX implementation of <see cref="IExchangeInfoDownloader"/>
    /// Downloads symbol properties from OKX API and generates CSV entries
    /// </summary>
    public class OKXExchangeInfoDownloader : IExchangeInfoDownloader
    {
        private readonly string _apiBaseUrl;
        private readonly Dictionary<string, string> _tokenizedStockBaseNames; // base -> base_name mapping for tokenized stocks
        private readonly HashSet<string> _symbolFilter; // Optional symbol filter

        private const string SpotApiPath = "/spot/currency_pairs";
        private const string FuturesApiPath = "/futures/usdt/contracts";

        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.OKX;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <remarks>
        /// Always uses production API URL for symbol properties updates
        /// </remarks>
        public OKXExchangeInfoDownloader() : this(null)
        {
        }

        /// <summary>
        /// Creates a new instance with symbol filter
        /// </summary>
        /// <param name="symbolFilter">Optional set of symbols to filter (e.g., {"BTC_USDT", "ETH_USDT"}).
        /// If provided, only these symbols will be downloaded. If null, all symbols are downloaded.</param>
        /// <remarks>
        /// Always uses production API URL for symbol properties updates
        /// </remarks>
        public OKXExchangeInfoDownloader(HashSet<string> symbolFilter)
        {
            _apiBaseUrl = OKXEnvironment.RestApiUrl;
            _tokenizedStockBaseNames = new Dictionary<string, string>();
            _symbolFilter = symbolFilter;
        }

        /// <summary>
        /// Pulls symbol properties data from OKX API
        /// </summary>
        /// <returns>Enumerable of CSV-formatted symbol properties</returns>
        public IEnumerable<string> Get()
        {
            Log.Trace("OKXExchangeInfoDownloader.Get(): Starting symbol properties download...");

            // 1. Fetch spot data and build tokenized stock registry
            var spotPairs = FetchSpotCurrencyPairs();
            BuildTokenizedStockRegistry(spotPairs);

            // Apply symbol filter to spot pairs if provided
            if (_symbolFilter != null && _symbolFilter.Count > 0)
            {
                var originalCount = spotPairs.Count;
                spotPairs = spotPairs.Where(p => _symbolFilter.Contains(p.Id)).ToList();
                Log.Trace($"OKXExchangeInfoDownloader.Get(): Filtered spot pairs from {originalCount} to {spotPairs.Count}");
            }

            // 2. Generate spot CSV entries
            var spotCount = 0;
            foreach (var entry in GenerateSpotEntries(spotPairs))
            {
                spotCount++;
                yield return entry;
            }

            Log.Trace($"OKXExchangeInfoDownloader.Get(): Generated {spotCount} spot entries");

            // 3. Fetch futures data and generate CSV entries
            var futuresContracts = FetchFuturesContracts();

            // Apply symbol filter to futures contracts if provided
            if (_symbolFilter != null && _symbolFilter.Count > 0)
            {
                var originalCount = futuresContracts.Count;
                futuresContracts = futuresContracts.Where(c => _symbolFilter.Contains(c.Name)).ToList();
                Log.Trace($"OKXExchangeInfoDownloader.Get(): Filtered futures contracts from {originalCount} to {futuresContracts.Count}");
            }

            var futuresCount = 0;
            foreach (var entry in GenerateFuturesEntries(futuresContracts))
            {
                futuresCount++;
                yield return entry;
            }

            Log.Trace($"OKXExchangeInfoDownloader.Get(): Generated {futuresCount} futures entries");
            Log.Trace($"OKXExchangeInfoDownloader.Get(): Total entries: {spotCount + futuresCount}");
        }

        /// <summary>
        /// Fetches spot currency pairs from OKX API
        /// </summary>
        private List<SpotCurrencyPair> FetchSpotCurrencyPairs()
        {
            var endpoint = $"{_apiBaseUrl}{SpotApiPath}";
            Log.Trace($"OKXExchangeInfoDownloader.FetchSpotCurrencyPairs(): Fetching from {endpoint}");

            var json = Extensions.DownloadData(endpoint);
            var pairs = JsonConvert.DeserializeObject<List<SpotCurrencyPair>>(json);

            Log.Trace($"OKXExchangeInfoDownloader.FetchSpotCurrencyPairs(): Received {pairs.Count} spot pairs");
            return pairs;
        }

        /// <summary>
        /// Fetches futures contracts from OKX API
        /// </summary>
        private List<FuturesContract> FetchFuturesContracts()
        {
            var endpoint = $"{_apiBaseUrl}{FuturesApiPath}";
            Log.Trace($"OKXExchangeInfoDownloader.FetchFuturesContracts(): Fetching from {endpoint}");

            var json = Extensions.DownloadData(endpoint);
            var contracts = JsonConvert.DeserializeObject<List<FuturesContract>>(json);

            Log.Trace($"OKXExchangeInfoDownloader.FetchFuturesContracts(): Received {contracts.Count} futures contracts");
            return contracts;
        }

        /// <summary>
        /// Builds registry of tokenized stock base currencies (base -> base_name mapping)
        /// Used to provide correct descriptions for tokenized stock futures
        /// </summary>
        private void BuildTokenizedStockRegistry(List<SpotCurrencyPair> spotPairs)
        {
            foreach (var pair in spotPairs)
            {
                if (!string.IsNullOrEmpty(pair.BaseName) &&
                    (pair.BaseName.Contains("xStock") || pair.BaseName.Contains("Ondo Tokenized")))
                {
                    _tokenizedStockBaseNames[pair.Base] = pair.BaseName;
                }
            }

            if (_tokenizedStockBaseNames.Count > 0)
            {
                Log.Trace($"OKXExchangeInfoDownloader.BuildTokenizedStockRegistry(): Found {_tokenizedStockBaseNames.Count} tokenized stocks");
            }
        }

        /// <summary>
        /// Generates CSV entries for spot currency pairs
        /// </summary>
        private IEnumerable<string> GenerateSpotEntries(List<SpotCurrencyPair> pairs)
        {
            foreach (var pair in pairs)
            {
                // Skip if missing required fields
                if (string.IsNullOrEmpty(pair.Id) || string.IsNullOrEmpty(pair.Base) || string.IsNullOrEmpty(pair.Quote))
                {
                    Log.Error($"OKXExchangeInfoDownloader.GenerateSpotEntries(): Skipping pair with missing fields: {pair.Id}");
                    continue;
                }

                // Calculate fields
                var symbol = $"{pair.Base}{pair.Quote}"; // BTCUSDT (no underscore)
                var minimumPriceVariation = CalculateMinimumPriceVariation(pair.Precision);

                // Determine description - always use base_name for spot
                // (Tokenized stocks will have names like "Apple xStock", regular cryptos like "Bitcoin")
                var description = pair.BaseName;

                // Format CSV: 12 fields
                // market,symbol,type,description,quote_currency,contract_multiplier,
                // minimum_price_variation,lot_size,market_ticker,minimum_order_size,
                // price_magnifier,strike_multiplier
                var csv = string.Join(",", new[]
                {
                    "okx",                                                              // market
                    symbol,                                                               // symbol
                    "crypto",                                                             // type
                    description,                                                          // description
                    pair.Quote,                                                           // quote_currency
                    "1",                                                                  // contract_multiplier
                    minimumPriceVariation.ToString(CultureInfo.InvariantCulture),        // minimum_price_variation
                    pair.MinBaseAmount.ToStringInvariant(),                              // lot_size
                    pair.Id,                                                              // market_ticker (BTC_USDT)
                    pair.MinQuoteAmount.ToStringInvariant(),                             // minimum_order_size
                    "",                                                                   // price_magnifier (empty)
                    ""                                                                    // strike_multiplier (empty)
                });

                yield return csv;
            }
        }

        /// <summary>
        /// Generates CSV entries for futures contracts
        /// </summary>
        private IEnumerable<string> GenerateFuturesEntries(List<FuturesContract> contracts)
        {
            foreach (var contract in contracts)
            {
                // Skip if missing required fields
                if (string.IsNullOrEmpty(contract.Name))
                {
                    Log.Error($"OKXExchangeInfoDownloader.GenerateFuturesEntries(): Skipping contract with missing name");
                    continue;
                }

                // Extract base and quote from name (e.g., "BTC_USDT")
                var parts = contract.Name.Split('_');
                if (parts.Length != 2)
                {
                    Log.Error($"OKXExchangeInfoDownloader.GenerateFuturesEntries(): Invalid contract name format: {contract.Name}");
                    continue;
                }

                var baseCurrency = parts[0];
                var quoteCurrency = parts[1];
                var symbol = $"{baseCurrency}{quoteCurrency}"; // BTCUSDT

                // Determine description
                // For tokenized stock futures, use spot's base_name (e.g., "Apple xStock Perpetual")
                // For regular crypto futures, use base currency (e.g., "BTC Perpetual")
                var description = _tokenizedStockBaseNames.TryGetValue(baseCurrency, out var baseName)
                    ? $"{baseName} Perpetual"
                    : $"{baseCurrency} Perpetual";

                // Format CSV
                var csv = string.Join(",", new[]
                {
                    "okx",                                                              // market
                    symbol,                                                               // symbol
                    "cryptofuture",                                                       // type
                    description,                                                          // description
                    quoteCurrency,                                                        // quote_currency
                    contract.QuantoMultiplier.ToStringInvariant(),                       // contract_multiplier
                    contract.OrderPriceRound.ToStringInvariant(),                        // minimum_price_variation
                    contract.OrderSizeMin.ToString(CultureInfo.InvariantCulture),        // lot_size
                    contract.Name,                                                        // market_ticker (BTC_USDT)
                    "",                                                                   // minimum_order_size (empty)
                    "",                                                                   // price_magnifier (empty)
                    ""                                                                    // strike_multiplier (empty)
                });

                yield return csv;
            }
        }

        /// <summary>
        /// Calculates minimum price variation from precision
        /// </summary>
        private decimal CalculateMinimumPriceVariation(int precision)
        {
            // precision = 2 → MPV = 0.01
            // precision = 4 → MPV = 0.0001
            return (decimal)Math.Pow(10, -precision);
        }
    }
}
