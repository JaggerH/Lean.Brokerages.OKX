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
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Symbol Mapper for OKX v5 API
    /// Supports OKX symbol formats:
    /// - Spot: BTC-USDT
    /// - Perpetual Swap: BTC-USDT-SWAP
    /// - Delivery Futures: BTC-USDT-250328 (YYMMDD expiry date)
    /// </summary>
    public class OKXSymbolMapper : ISymbolMapper
    {
        private readonly string _market;
        private readonly SymbolPropertiesDatabaseSymbolMapper _baseMapper;
        private OKXRestApiClient _restApiClient;

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
        /// Sets the REST API client for dynamic symbol lookup
        /// </summary>
        /// <param name="restApiClient">REST API client instance</param>
        public void SetRestApiClient(OKXRestApiClient restApiClient)
        {
            _restApiClient = restApiClient;
        }

        /// <summary>
        /// Converts a Lean symbol instance to an OKX brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The OKX brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrEmpty(symbol.Value))
            {
                throw new ArgumentException("Invalid symbol");
            }

            // Use CSV database exclusively - no fallback
            // All symbols must be explicitly mapped in symbol-properties-database.csv
            return _baseMapper.GetBrokerageSymbol(symbol);
        }

        /// <summary>
        /// Converts an OKX brokerage symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The OKX brokerage symbol (e.g., "BTC-USDT", "BTC-USDT-SWAP", "BTC-USDT-250328")</param>
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
            if (string.IsNullOrEmpty(brokerageSymbol))
            {
                throw new ArgumentException("Invalid brokerage symbol");
            }

            // Use CSV database exclusively - no fallback
            return _baseMapper.GetLeanSymbol(brokerageSymbol, securityType, market,
                expirationDate, strike, optionRight);
        }

        /// <summary>
        /// Checks if the Lean symbol is supported by the brokerage
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            try
            {
                GetBrokerageSymbol(symbol);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the security type for a brokerage symbol
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            try
            {
                return _baseMapper.GetBrokerageSecurityType(brokerageSymbol);
            }
            catch
            {
                // Infer from OKX symbol format
                if (brokerageSymbol.EndsWith("-SWAP"))
                {
                    return SecurityType.CryptoFuture; // Perpetual
                }
                else if (brokerageSymbol.Split('-').Length == 3 && !brokerageSymbol.EndsWith("-SWAP"))
                {
                    return SecurityType.CryptoFuture; // Delivery futures
                }
                else
                {
                    return SecurityType.Crypto; // Spot
                }
            }
        }

        /// <summary>
        /// Checks if the symbol is supported by the brokerage
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            try
            {
                GetBrokerageSecurityType(brokerageSymbol);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
