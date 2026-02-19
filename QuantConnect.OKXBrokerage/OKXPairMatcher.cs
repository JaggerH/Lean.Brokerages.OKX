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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Represents a pair of symbols for arbitrage trading
    /// Implements IEnumerable for Python tuple unpacking: for spot, futures in pairs
    /// </summary>
    public readonly struct SymbolPair : IEnumerable<Symbol>
    {
        private readonly Symbol _first;
        private readonly Symbol _second;

        /// <summary>
        /// Creates a new SymbolPair
        /// </summary>
        /// <param name="first">First symbol (e.g., Spot)</param>
        /// <param name="second">Second symbol (e.g., Futures)</param>
        public SymbolPair(Symbol first, Symbol second)
        {
            _first = first;
            _second = second;
        }

        /// <summary>
        /// Indexer access: pair[0], pair[1]
        /// </summary>
        public Symbol this[int index] => index == 0 ? _first : _second;

        /// <summary>
        /// Deconstruct for C# pattern matching: var (spot, futures) = pair;
        /// </summary>
        public void Deconstruct(out Symbol first, out Symbol second)
        {
            first = _first;
            second = _second;
        }

        /// <summary>
        /// Enumerator for Python tuple unpacking: for spot, futures in pairs
        /// </summary>
        public IEnumerator<Symbol> GetEnumerator()
        {
            yield return _first;
            yield return _second;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString() => $"{_first?.Value} <-> {_second?.Value}";
    }

    /// <summary>
    /// Helper class for matching OKX Spot-Future pairs with volume filtering
    /// Provides methods to identify and query qualified trading pairs based on 24h volume
    /// </summary>
    public static class OKXPairMatcher
    {
        /// <summary>
        /// Default minimum 24h volume threshold in USDT
        /// Matches Python okx.py default (300,000 USDT)
        /// </summary>
        public const decimal DefaultMinVolumeUsdt = 300000m;

        /// <summary>
        /// Gets regular crypto Spot-Future pairs with dual-leg volume filtering
        /// Automatically creates API clients from configuration
        /// Both Spot and Futures must meet minimum volume threshold
        /// </summary>
        /// <param name="minVolumeUsdt">Minimum 24h USDT volume threshold</param>
        /// <param name="requireMarginTrading">If true, excludes pairs whose spot leg has no MARGIN instrument
        /// (required for portfolio margin mode where OKX routes spot orders through margin account)</param>
        /// <returns>List of SymbolPair sorted by combined volume descending</returns>
        public static List<SymbolPair> GetSpotFuturePairs(
            decimal minVolumeUsdt = DefaultMinVolumeUsdt,
            bool requireMarginTrading = false)
        {
            try
            {
                Log.Trace($"OKXPairMatcher.GetSpotFuturePairs(): Starting with minVolume={minVolumeUsdt} USDT, requireMarginTrading={requireMarginTrading}");

                // Get all spot-future pairs from database
                var allPairs = GetAllSpotFuturePairsInternal();
                if (allPairs.Count == 0)
                {
                    Log.Trace("OKXPairMatcher.GetSpotFuturePairs(): No spot-future pairs found in database");
                    return new List<SymbolPair>();
                }

                Log.Trace($"OKXPairMatcher.GetSpotFuturePairs(): Found {allPairs.Count} pairs in database");

                // Apply volume filter (both legs must meet threshold since both are Market.OKX)
                var filteredPairs = FilterByVolume(allPairs, minVolumeUsdt, requireMarginTrading);

                Log.Trace($"OKXPairMatcher.GetSpotFuturePairs(): {filteredPairs.Count} pairs passed all filters");

                return filteredPairs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OKXPairMatcher.GetSpotFuturePairs(): Error getting spot-future pairs");
                return new List<SymbolPair>();
            }
        }

        /// <summary>
        /// Gets all Spot-Future pairs from SymbolPropertiesDatabase
        /// Matches symbols with the same name in both Crypto and CryptoFuture security types
        /// </summary>
        /// <returns>List of SymbolPair (SpotSymbol, FuturesSymbol)</returns>
        private static List<SymbolPair> GetAllSpotFuturePairsInternal()
        {
            var result = new List<SymbolPair>();
            var database = SymbolPropertiesDatabase.FromDataFolder();

            // Get all Spot symbols (Crypto)
            var spotSymbols = database.GetSymbolPropertiesList(Market.OKX, SecurityType.Crypto)
                .Select(kvp => kvp.Key.Symbol)
                .ToHashSet();

            // Get all Futures symbols (CryptoFuture)
            var futuresSymbols = database.GetSymbolPropertiesList(Market.OKX, SecurityType.CryptoFuture)
                .Select(kvp => kvp.Key.Symbol)
                .ToHashSet();

            // Find common symbols (exist in both Spot and Futures)
            var commonSymbols = spotSymbols.Intersect(futuresSymbols);

            foreach (var symbolValue in commonSymbols)
            {
                var spotSymbol = Symbol.Create(symbolValue, SecurityType.Crypto, Market.OKX);
                var futuresSymbol = Symbol.Create(symbolValue, SecurityType.CryptoFuture, Market.OKX);
                result.Add(new SymbolPair(spotSymbol, futuresSymbol));
            }

            return result;
        }

        /// <summary>
        /// Filters SymbolPairs by volume threshold and optionally by margin trading availability.
        /// Uses OKX v5 /market/tickers and /public/instruments endpoints.
        /// </summary>
        /// <param name="pairs">List of SymbolPairs to filter</param>
        /// <param name="minVolumeUsdt">Minimum 24h USDT volume threshold</param>
        /// <param name="requireMarginTrading">If true, excludes pairs whose spot leg has no MARGIN instrument</param>
        /// <returns>Filtered list of SymbolPairs sorted by volume descending</returns>
        private static List<SymbolPair> FilterByVolume(
            List<SymbolPair> pairs,
            decimal minVolumeUsdt,
            bool requireMarginTrading = false)
        {
            var client = CreateApiClient();
            var symbolMapper = new OKXSymbolMapper(Market.OKX);

            // Fetch all tickers (SPOT + SWAP)
            var allTickers = client.GetTicker()
                .Where(t => !string.IsNullOrEmpty(t.CurrencyPair))
                .ToDictionary(t => t.CurrencyPair, t => t);

            if (allTickers.Count == 0)
            {
                Log.Error("OKXPairMatcher.FilterByVolume(): Failed to fetch any tickers");
                return new List<SymbolPair>();
            }

            Log.Trace($"OKXPairMatcher.FilterByVolume(): Fetched {allTickers.Count} tickers");

            // Fetch margin instruments if needed (for portfolio margin mode filtering)
            HashSet<string> marginInstIds = null;
            if (requireMarginTrading)
            {
                var marginInstruments = client.GetInstruments("MARGIN");
                if (marginInstruments.Count == 0)
                {
                    Log.Error("OKXPairMatcher.FilterByVolume(): Failed to fetch MARGIN instruments");
                    return new List<SymbolPair>();
                }
                marginInstIds = new HashSet<string>(marginInstruments.Select(i => i.InstrumentId));
                Log.Trace($"OKXPairMatcher.FilterByVolume(): Fetched {marginInstIds.Count} MARGIN instruments");
            }

            return pairs
                .Select(pair => (
                    Pair: pair,
                    // InstIds order matches SymbolPair order: [0]=spot, [1]=futures
                    InstIds: pair
                        .Select(s => { try { return symbolMapper.GetBrokerageSymbol(s); } catch (ArgumentException) { return null; } })
                        .ToList()
                ))
                // All legs must resolve to a valid instId
                .Where(x => x.InstIds.All(id => id != null))
                // Margin filter: spot leg (InstIds[0]) must exist in MARGIN instruments
                .Where(x => marginInstIds == null || marginInstIds.Contains(x.InstIds[0]))
                // Volume filter: all legs must meet minimum volume
                .Select(x => (
                    x.Pair,
                    Volumes: x.InstIds
                        .Select(id => allTickers.TryGetValue(id, out var t) &&
                                      !string.IsNullOrEmpty(t.QuoteVolume) &&
                                      decimal.TryParse(t.QuoteVolume, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                                      ? v : -1m)
                        .ToList()
                ))
                .Where(x => x.Volumes.All(v => v >= minVolumeUsdt))
                .OrderByDescending(x => x.Volumes.Max())
                .Select(x => x.Pair)
                .ToList();
        }

        /// <summary>
        /// Creates an API client for accessing OKX public ticker endpoints
        /// Reads configuration from Config: okx-api-key, okx-api-secret
        /// Uses OKXEnvironment to determine REST API URL based on environment
        /// </summary>
        /// <returns>OKXRestApiClient instance</returns>
        private static OKXRestApiClient CreateApiClient()
        {
            var apiKey = Config.Get("okx-api-key", string.Empty);
            var apiSecret = Config.Get("okx-api-secret", string.Empty);
            var passphrase = Config.Get("okx-passphrase", string.Empty);
            var restApiUrl = OKXEnvironment.GetRestApiUrl();

            return new OKXRestApiClient(apiKey, apiSecret, passphrase, restApiUrl);
        }
    }
}
