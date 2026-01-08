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
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Symbol Mapper with runtime symbol registration support
    /// Wraps SymbolPropertiesDatabaseSymbolMapper and extends GetLeanSymbol to support runtime-registered symbols
    /// </summary>
    public class OKXSymbolMapper : ISymbolMapper
    {
        private readonly string _market;
        private readonly SymbolPropertiesDatabaseSymbolMapper _baseMapper;

        /// <summary>
        /// Creates a new instance of the <see cref="OKXSymbolMapper"/> class
        /// </summary>
        /// <param name="market">The Lean market (should be Market.OKX)</param>
        public OKXSymbolMapper(string market)
        {
            _market = market;
            _baseMapper = new SymbolPropertiesDatabaseSymbolMapper(market);
        }

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// Supports both CSV-loaded symbols and runtime-registered symbols
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            try
            {
                // First try the base mapper (local cache from CSV)
                return _baseMapper.GetBrokerageSymbol(symbol);
            }
            catch (ArgumentException)
            {
                // Fallback: Check global database for runtime-registered symbols
                var symbolProperties = SymbolPropertiesDatabase.FromDataFolder()
                    .GetSymbolProperties(_market, symbol, symbol.SecurityType, string.Empty);

                if (symbolProperties != null && !string.IsNullOrWhiteSpace(symbolProperties.MarketTicker))
                {
                    return symbolProperties.MarketTicker;
                }

                // If still not found, throw original error
                throw new ArgumentException(
                    $"Unknown symbol: {symbol.Value}/{symbol.SecurityType}/{symbol.ID.Market}. " +
                    $"Symbol not found in CSV database or runtime registration.");
            }
        }

        /// <summary>
        /// Converts a brokerage symbol to a Lean symbol instance
        /// Supports both CSV-loaded symbols and runtime-registered symbols
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol (e.g., "BTC_USDT")</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security (if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(
            string brokerageSymbol,
            SecurityType securityType,
            string market,
            DateTime expirationDate = default(DateTime),
            decimal strike = 0,
            OptionRight optionRight = OptionRight.Call)
        {
            try
            {
                // First try the base mapper (local cache from CSV)
                return _baseMapper.GetLeanSymbol(brokerageSymbol, securityType, market, expirationDate, strike, optionRight);
            }
            catch (ArgumentException)
            {
                // Fallback: Check global database for runtime-registered symbols
                // Query all symbols for this market and security type
                var symbolPropertiesList = SymbolPropertiesDatabase.FromDataFolder()
                    .GetSymbolPropertiesList(_market, securityType);

                // Find the symbol with matching MarketTicker (brokerage symbol)
                var match = symbolPropertiesList
                    .FirstOrDefault(x => x.Value.MarketTicker == brokerageSymbol);

                if (match.Value != null)
                {
                    // Found a runtime-registered symbol, create LEAN symbol
                    return Symbol.Create(match.Key.Symbol, securityType, market);
                }

                // Third fallback: Fetch from OKX API and register dynamically
                var symbol = TryFetchAndRegisterSymbol(brokerageSymbol, securityType, market);
                if (symbol != null)
                {
                    return symbol;
                }

                // If all fallbacks fail, throw error
                throw new ArgumentException(
                    $"Unknown brokerage symbol: {brokerageSymbol} for {securityType}/{market}. " +
                    $"Symbol not found in CSV database, runtime registration, or OKX API.");
            }
        }

        /// <summary>
        /// Checks if the Lean symbol is supported by the brokerage
        /// Deleokxs to the base mapper
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            return _baseMapper.IsKnownLeanSymbol(symbol);
        }

        /// <summary>
        /// Returns the security type for a brokerage symbol
        /// Deleokxs to the base mapper
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            return _baseMapper.GetBrokerageSecurityType(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by the brokerage
        /// Deleokxs to the base mapper
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            return _baseMapper.IsKnownBrokerageSymbol(brokerageSymbol);
        }

        /// <summary>
        /// Attempts to fetch symbol information from OKX API and register it dynamically
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol (e.g., "ADA_USDT")</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <returns>The LEAN Symbol if successful, null otherwise</returns>
        private Symbol TryFetchAndRegisterSymbol(string brokerageSymbol, SecurityType securityType, string market)
        {
            try
            {
                // Only support Crypto and CryptoFuture
                if (securityType != SecurityType.Crypto && securityType != SecurityType.CryptoFuture)
                {
                    return null;
                }

                // Validate brokerage symbol format
                if (string.IsNullOrEmpty(brokerageSymbol) || !brokerageSymbol.Contains("_"))
                {
                    return null;
                }

                // Convert OKX format to LEAN format: "BTC_USDT" → "BTCUSDT"
                var leanSymbolValue = brokerageSymbol.Replace("_", "").ToUpperInvariant();

                SymbolProperties symbolProperties;

                if (securityType == SecurityType.CryptoFuture)
                {
                    symbolProperties = FetchFuturesContractProperties(brokerageSymbol);
                }
                else
                {
                    symbolProperties = FetchSpotPairProperties(brokerageSymbol);
                }

                if (symbolProperties == null)
                {
                    return null;
                }

                // Register to SymbolPropertiesDatabase
                var db = SymbolPropertiesDatabase.FromDataFolder();
                db.SetEntry(market, leanSymbolValue, securityType, symbolProperties);

                Log.Trace($"OKXSymbolMapper.TryFetchAndRegisterSymbol(): Dynamically registered {leanSymbolValue} ({securityType})");

                // Create and return the Symbol
                return Symbol.Create(leanSymbolValue, securityType, market);
            }
            catch (Exception ex)
            {
                Log.Error($"OKXSymbolMapper.TryFetchAndRegisterSymbol(): Failed to fetch {brokerageSymbol}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches futures contract properties from OKX API
        /// </summary>
        private SymbolProperties FetchFuturesContractProperties(string brokerageSymbol)
        {
            var apiUrl = $"{OKXEnvironment.ProductionApiUrl}/futures/usdt/contracts/{brokerageSymbol}";

            var json = apiUrl.DownloadData();
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var contract = JsonConvert.DeserializeObject<FuturesContract>(json);
            if (contract == null || string.IsNullOrEmpty(contract.Name))
            {
                return null;
            }

            // Extract quote currency from contract name (e.g., "ADA_USDT" → "USDT")
            var parts = contract.Name.Split('_');
            var baseCurrency = parts.Length > 0 ? parts[0] : "";
            var quoteCurrency = parts.Length > 1 ? parts[1] : "USDT";

            return new SymbolProperties(
                description: $"{baseCurrency} Perpetual",
                quoteCurrency: quoteCurrency,
                contractMultiplier: contract.QuantoMultiplier,
                minimumPriceVariation: contract.OrderPriceRound,
                lotSize: contract.OrderSizeMin,
                marketTicker: contract.Name
            );
        }

        /// <summary>
        /// Fetches spot currency pair properties from OKX API
        /// </summary>
        private SymbolProperties FetchSpotPairProperties(string brokerageSymbol)
        {
            var apiUrl = $"{OKXEnvironment.ProductionApiUrl}/spot/currency_pairs/{brokerageSymbol}";

            var json = apiUrl.DownloadData();
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var pair = JsonConvert.DeserializeObject<SpotCurrencyPair>(json);
            if (pair == null || string.IsNullOrEmpty(pair.Id))
            {
                return null;
            }

            // Calculate minimum price variation from precision
            var minimumPriceVariation = (decimal)Math.Pow(10, -pair.Precision);

            return new SymbolProperties(
                description: pair.BaseName ?? pair.Base,
                quoteCurrency: pair.Quote,
                contractMultiplier: 1m,
                minimumPriceVariation: minimumPriceVariation,
                lotSize: pair.MinBaseAmount,
                marketTicker: pair.Id,
                minimumOrderSize: pair.MinQuoteAmount
            );
        }
    }
}
