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
    /// <remarks>
    /// Supports two pairing types:
    /// 1. Crypto-Stock: Tokenized stock Spot-Future matching (filters Spot volume only)
    /// 2. Spot-Future: Regular crypto Spot-Future matching (filters both legs' volume)
    /// </remarks>
    public static class OKXPairMatcher
    {
        /// <summary>
        /// Default minimum 24h volume threshold in USDT
        /// Matches Python okx.py default (300,000 USDT)
        /// </summary>
        public const decimal DefaultMinVolumeUsdt = 300000m;

        /// <summary>
        /// Supported tokenized stock providers
        /// </summary>
        private static readonly string[] TokenizedProviders = { "xStock", "Ondo Tokenized" };

        /// <summary>
        /// Gets tokenized stock pairs with their underlying equity symbols (Simplified version)
        /// Automatically creates API clients from configuration for volume filtering
        /// </summary>
        /// <param name="type">Filter type: "spot", "future", or "all" (default)</param>
        /// <param name="minVolumeUsdt">Minimum 24h USDT volume threshold</param>
        /// <returns>List of SymbolPair (TokenizedStock, EquitySymbol) sorted by volume descending</returns>
        public static List<SymbolPair> GetTokenizedStockPairs(
            string type = "all",
            decimal minVolumeUsdt = DefaultMinVolumeUsdt)
        {
            try
            {
                Log.Trace($"OKXPairMatcher.GetTokenizedStockPairs(): Starting with type={type}, minVolume={minVolumeUsdt} USDT");

                // Get all tokenized stock pairs from database
                var allTokenizedPairs = GetAllTokenizedStockPairsInternal(type);
                if (allTokenizedPairs.Count == 0)
                {
                    Log.Trace("OKXPairMatcher.GetTokenizedStockPairs(): No tokenized stock pairs found in database");
                    return new List<SymbolPair>();
                }

                Log.Trace($"OKXPairMatcher.GetTokenizedStockPairs(): Found {allTokenizedPairs.Count} tokenized pairs in database");

                // Apply volume filter
                var filteredPairs = FilterByVolume(allTokenizedPairs, minVolumeUsdt);

                Log.Trace($"OKXPairMatcher.GetTokenizedStockPairs(): {filteredPairs.Count} pairs passed volume filter");

                return filteredPairs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OKXPairMatcher.GetTokenizedStockPairs(): Error getting tokenized stock pairs");
                return new List<SymbolPair>();
            }
        }

        /// <summary>
        /// Gets regular crypto Spot-Future pairs with dual-leg volume filtering (Simplified version)
        /// Automatically creates API clients from configuration
        /// Both Spot and Futures must meet minimum volume threshold
        /// </summary>
        /// <param name="minVolumeUsdt">Minimum 24h USDT volume threshold</param>
        /// <returns>List of SymbolPair sorted by combined volume descending</returns>
        public static List<SymbolPair> GetSpotFuturePairs(
            decimal minVolumeUsdt = DefaultMinVolumeUsdt)
        {
            try
            {
                Log.Trace($"OKXPairMatcher.GetSpotFuturePairs(): Starting with minVolume={minVolumeUsdt} USDT");

                // Get all spot-future pairs from database
                var allPairs = GetAllSpotFuturePairsInternal();
                if (allPairs.Count == 0)
                {
                    Log.Trace("OKXPairMatcher.GetSpotFuturePairs(): No spot-future pairs found in database");
                    return new List<SymbolPair>();
                }

                Log.Trace($"OKXPairMatcher.GetSpotFuturePairs(): Found {allPairs.Count} pairs in database");

                // Apply volume filter (both legs must meet threshold since both are Market.OKX)
                var filteredPairs = FilterByVolume(allPairs, minVolumeUsdt);

                Log.Trace($"OKXPairMatcher.GetSpotFuturePairs(): {filteredPairs.Count} pairs passed volume filter");

                return filteredPairs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OKXPairMatcher.GetSpotFuturePairs(): Error getting spot-future pairs");
                return new List<SymbolPair>();
            }
        }

        /// <summary>
        /// Gets all qualified pairs (both tokenized stocks and regular crypto) (Simplified version)
        /// Automatically creates API clients from configuration
        /// </summary>
        /// <param name="minVolumeUsdt">Minimum 24h USDT volume threshold</param>
        /// <returns>Combined list of all qualified SymbolPairs sorted by volume</returns>
        public static List<SymbolPair> GetAllQualifiedPairs(
            decimal minVolumeUsdt = DefaultMinVolumeUsdt)
        {
            try
            {
                Log.Trace($"OKXPairMatcher.GetAllQualifiedPairs(): Starting with minVolume={minVolumeUsdt} USDT");

                var tokenizedPairs = GetTokenizedStockPairs("all", minVolumeUsdt);
                var regularPairs = GetSpotFuturePairs(minVolumeUsdt);

                // Combine and remove duplicates (tokenized stocks might also appear in regular pairs)
                var tokenizedSymbols = new HashSet<string>(tokenizedPairs.Select(p => p[0].Value));
                var uniquePairs = tokenizedPairs
                    .Concat(regularPairs.Where(p => !tokenizedSymbols.Contains(p[0].Value)))
                    .ToList();

                Log.Trace($"OKXPairMatcher.GetAllQualifiedPairs(): Total {uniquePairs.Count} qualified pairs ({tokenizedPairs.Count} tokenized + {regularPairs.Count - (regularPairs.Count - (uniquePairs.Count - tokenizedPairs.Count))} regular)");

                return uniquePairs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OKXPairMatcher.GetAllQualifiedPairs(): Error getting all qualified pairs");
                return new List<SymbolPair>();
            }
        }

        /// <summary>
        /// Gets all Spot-Future pairs from SymbolPropertiesDatabase
        /// Matches symbols with the same name in both Crypto and CryptoFuture security types
        /// Excludes tokenized stocks (those are handled by GetTokenizedStockPairs)
        /// </summary>
        /// <returns>List of SymbolPair (SpotSymbol, FuturesSymbol)</returns>
        private static List<SymbolPair> GetAllSpotFuturePairsInternal()
        {
            var result = new List<SymbolPair>();
            var database = SymbolPropertiesDatabase.FromDataFolder();

            // Get all Spot symbols (Crypto)
            var spotSymbols = database.GetSymbolPropertiesList(Market.OKX, SecurityType.Crypto)
                .Where(kvp => !IsTokenizedStockDescription(kvp.Value.Description))
                .Select(kvp => kvp.Key.Symbol)
                .ToHashSet();

            // Get all Futures symbols (CryptoFuture)
            var futuresSymbols = database.GetSymbolPropertiesList(Market.OKX, SecurityType.CryptoFuture)
                .Where(kvp => !IsTokenizedStockDescription(kvp.Value.Description))
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
        /// Gets all tokenized stock pairs with their underlying equity symbols from SymbolPropertiesDatabase
        /// </summary>
        /// <param name="type">Filter type: "spot", "future", or "all"</param>
        /// <returns>List of SymbolPair (TokenizedStock, EquitySymbol)</returns>
        private static List<SymbolPair> GetAllTokenizedStockPairsInternal(string type = "all")
        {
            var result = new List<SymbolPair>();
            var database = SymbolPropertiesDatabase.FromDataFolder();
            var normalizedType = type?.ToLowerInvariant() ?? "all";

            // Get Spot tokenized stocks if type is "spot" or "all"
            if (normalizedType == "spot" || normalizedType == "all")
            {
                var spotSymbols = database.GetSymbolPropertiesList(Market.OKX, SecurityType.Crypto)
                    .Where(kvp => IsTokenizedStockDescription(kvp.Value.Description));

                foreach (var kvp in spotSymbols)
                {
                    var leanSymbolValue = kvp.Key.Symbol; // e.g., "AAPLXUSDT"
                    var description = kvp.Value.Description;

                    if (TryExtractEquityTicker(leanSymbolValue, description, out var equityTicker))
                    {
                        var tokenizedSymbol = Symbol.Create(leanSymbolValue, SecurityType.Crypto, Market.OKX);
                        var equitySymbol = CreateEquitySymbol(equityTicker);
                        result.Add(new SymbolPair(tokenizedSymbol, equitySymbol));
                    }
                }
            }

            // Get Futures tokenized stocks if type is "future" or "all"
            if (normalizedType == "future" || normalizedType == "all")
            {
                var futuresSymbols = database.GetSymbolPropertiesList(Market.OKX, SecurityType.CryptoFuture)
                    .Where(kvp => IsTokenizedStockDescription(kvp.Value.Description));

                foreach (var kvp in futuresSymbols)
                {
                    var leanSymbolValue = kvp.Key.Symbol; // e.g., "AAPLXUSDT"
                    var description = kvp.Value.Description;

                    if (TryExtractEquityTicker(leanSymbolValue, description, out var equityTicker))
                    {
                        var tokenizedSymbol = Symbol.Create(leanSymbolValue, SecurityType.CryptoFuture, Market.OKX);
                        var equitySymbol = CreateEquitySymbol(equityTicker);
                        result.Add(new SymbolPair(tokenizedSymbol, equitySymbol));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a description indicates a tokenized stock
        /// </summary>
        /// <param name="description">Symbol description</param>
        /// <returns>True if description contains tokenized stock keywords</returns>
        private static bool IsTokenizedStockDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return false;
            }

            foreach (var provider in TokenizedProviders)
            {
                if (description.Contains(provider))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to extract the underlying equity ticker from a TokenizedStock symbol
        /// </summary>
        /// <param name="symbolValue">Symbol.Value (e.g., AAPLXUSDT)</param>
        /// <param name="description">SymbolProperties.Description</param>
        /// <param name="equityTicker">Output: equity ticker (e.g., AAPL)</param>
        /// <returns>True if this is a TokenizedStock and extraction succeeded</returns>
        private static bool TryExtractEquityTicker(
            string symbolValue,
            string description,
            out string equityTicker)
        {
            equityTicker = null;

            if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(symbolValue))
                return false;

            // Step 1: Determine provider type from description
            string separator;
            if (description.Contains("xStock"))
            {
                separator = "X";      // xStock: AAPLXUSDT -> AAPL
            }
            else if (description.Contains("Ondo Tokenized"))
            {
                separator = "ON";     // Ondo: AAPLONUSDT -> AAPL
            }
            else
            {
                return false;         // Not a TokenizedStock
            }

            // Step 2: Extract equity ticker from Symbol.Value using separator
            var index = symbolValue.IndexOf(separator);
            if (index > 0)
            {
                equityTicker = symbolValue.Substring(0, index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates an Equity Symbol for the underlying stock
        /// </summary>
        /// <param name="ticker">Stock ticker (e.g., AAPL)</param>
        /// <returns>Equity Symbol in Market.USA</returns>
        private static Symbol CreateEquitySymbol(string ticker)
        {
            return Symbol.Create(ticker, SecurityType.Equity, Market.USA);
        }

        /// <summary>
        /// Filters SymbolPairs by volume threshold
        /// Only checks Market.OKX symbols, skips non-OKX symbols (e.g., EquitySymbol)
        /// Uses OKX v5 /market/tickers endpoint with instType parameter
        /// </summary>
        /// <param name="pairs">List of SymbolPairs to filter</param>
        /// <param name="minVolumeUsdt">Minimum 24h USDT volume threshold</param>
        /// <returns>Filtered list of SymbolPairs with volume data, sorted by volume descending</returns>
        private static List<SymbolPair> FilterByVolume(
            List<SymbolPair> pairs,
            decimal minVolumeUsdt)
        {
            var client = CreateApiClient();
            var symbolMapper = new OKXSymbolMapper(Market.OKX);

            // Fetch all tickers (SPOT + SWAP) in one call
            var allTickers = client.GetTicker()
                .Where(t => !string.IsNullOrEmpty(t.CurrencyPair))
                .ToDictionary(t => t.CurrencyPair, t => t);

            if (allTickers.Count == 0)
            {
                Log.Error("OKXPairMatcher.FilterByVolume(): Failed to fetch any tickers");
                return new List<SymbolPair>();
            }

            Log.Trace($"OKXPairMatcher.FilterByVolume(): Fetched {allTickers.Count} tickers");

            var qualifiedPairs = new List<(SymbolPair Pair, decimal MaxVolume)>();

            foreach (var pair in pairs)
            {
                bool allOKXLegsQualified = true;
                decimal maxVolume = 0m;

                foreach (var symbol in pair)
                {
                    // Skip non-OKX market symbols (e.g., EquitySymbol in Market.USA)
                    if (symbol.ID.Market != Market.OKX)
                        continue;

                    // Use SymbolMapper to get the correct OKX instId (e.g., BTC-USDT, BTC-USDT-SWAP)
                    string instId;
                    try
                    {
                        instId = symbolMapper.GetBrokerageSymbol(symbol);
                    }
                    catch (ArgumentException)
                    {
                        allOKXLegsQualified = false;
                        break;
                    }

                    if (allTickers.TryGetValue(instId, out var ticker) &&
                        !string.IsNullOrEmpty(ticker.QuoteVolume) &&
                        decimal.TryParse(ticker.QuoteVolume, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                    {
                        if (volume < minVolumeUsdt)
                        {
                            allOKXLegsQualified = false;
                            break;
                        }
                        maxVolume = Math.Max(maxVolume, volume);
                    }
                    else
                    {
                        // No ticker data available for this OKX symbol
                        allOKXLegsQualified = false;
                        break;
                    }
                }

                if (allOKXLegsQualified)
                {
                    qualifiedPairs.Add((pair, maxVolume));
                }
            }

            // Sort by volume descending
            return qualifiedPairs
                .OrderByDescending(p => p.MaxVolume)
                .Select(p => p.Pair)
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
