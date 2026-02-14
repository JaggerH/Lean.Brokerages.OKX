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
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Symbol Mapper - wraps SymbolPropertiesDatabaseSymbolMapper
    /// </summary>
    public class OKXSymbolMapper : ISymbolMapper
    {
        private readonly SymbolPropertiesDatabaseSymbolMapper _baseMapper;
        private readonly string _market;

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
        /// Infers the <see cref="SecurityType"/> from an OKX instrument ID.
        /// Spot: "BTC-USDT" (2 parts). Swap/Futures: "BTC-USDT-SWAP", "BTC-USD-230630" (3+ parts).
        /// </summary>
        public static SecurityType InferSecurityType(string instId)
        {
            if (string.IsNullOrEmpty(instId))
            {
                return SecurityType.Crypto;
            }

            if (instId.Contains("-SWAP") || instId.Contains("-FUTURES") ||
                (instId.Split('-').Length == 3 && !instId.EndsWith("-SWAP")))
            {
                return SecurityType.CryptoFuture;
            }

            return SecurityType.Crypto;
        }

        /// <summary>
        /// Converts an OKX instrument ID to a Lean symbol, using the stored market.
        /// </summary>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType)
            => GetLeanSymbol(brokerageSymbol, securityType, _market);

        /// <summary>
        /// Converts an OKX instrument ID to a Lean symbol, inferring both security type and market.
        /// </summary>
        public Symbol GetLeanSymbol(string brokerageSymbol)
            => GetLeanSymbol(brokerageSymbol, InferSecurityType(brokerageSymbol), _market);

        /// <summary>
        /// Converts a Lean symbol instance to an OKX brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The OKX brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
            => _baseMapper.GetBrokerageSymbol(symbol);

        /// <summary>
        /// Converts an OKX brokerage symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The OKX brokerage symbol</param>
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
            DateTime expirationDate = default,
            decimal strike = 0,
            OptionRight optionRight = OptionRight.Call)
            => _baseMapper.GetLeanSymbol(brokerageSymbol, securityType, market, expirationDate, strike, optionRight);

        /// <summary>
        /// Checks if the Lean symbol is supported by the brokerage
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
            => _baseMapper.IsKnownLeanSymbol(symbol);

        /// <summary>
        /// Returns the security type for a brokerage symbol
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
            => _baseMapper.GetBrokerageSecurityType(brokerageSymbol);

        /// <summary>
        /// Checks if the symbol is supported by the brokerage
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
            => _baseMapper.IsKnownBrokerageSymbol(brokerageSymbol);

        /// <summary>
        /// Lookup symbols matching specified criteria from Symbol Properties Database
        /// </summary>
        /// <param name="symbol">The symbol to lookup (used for SecurityType and Market)</param>
        /// <param name="includeExpired">Include expired contracts (not applicable for crypto)</param>
        /// <param name="securityCurrency">Expected security currency (quote currency filter)</param>
        /// <returns>Matching symbols</returns>
        public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
        {
            var database = SymbolPropertiesDatabase.FromDataFolder();
            var symbolPropertiesList = database.GetSymbolPropertiesList(Market.OKX, symbol.SecurityType);

            foreach (var kvp in symbolPropertiesList)
            {
                // Filter by quote currency if specified
                if (!string.IsNullOrEmpty(securityCurrency) &&
                    !kvp.Value.QuoteCurrency.Equals(securityCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return Symbol.Create(kvp.Key.Symbol, symbol.SecurityType, Market.OKX);
            }
        }
    }
}
