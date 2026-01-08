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

            try
            {
                // First try the base mapper (CSV database)
                return _baseMapper.GetBrokerageSymbol(symbol);
            }
            catch (ArgumentException)
            {
                // Fallback: Format based on security type
                return FormatOKXSymbol(symbol);
            }
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

            try
            {
                // First try the base mapper (CSV database)
                return _baseMapper.GetLeanSymbol(brokerageSymbol, securityType, market, expirationDate, strike, optionRight);
            }
            catch (ArgumentException)
            {
                // Fallback: Parse OKX symbol format
                return ParseOKXSymbol(brokerageSymbol, securityType, market);
            }
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

        /// <summary>
        /// Formats a LEAN Symbol into OKX v5 API format
        /// </summary>
        /// <param name="symbol">LEAN Symbol</param>
        /// <returns>OKX symbol string</returns>
        private string FormatOKXSymbol(Symbol symbol)
        {
            switch (symbol.SecurityType)
            {
                case SecurityType.Crypto:
                    return FormatSpotSymbol(symbol);

                case SecurityType.CryptoFuture:
                case SecurityType.Future:  // Symbol.CreateFuture() uses SecurityType.Future
                    return FormatFuturesSymbol(symbol);

                default:
                    throw new NotSupportedException($"OKX does not support security type: {symbol.SecurityType}");
            }
        }

        /// <summary>
        /// Formats a spot symbol: BTCUSDT → BTC-USDT
        /// </summary>
        private string FormatSpotSymbol(Symbol symbol)
        {
            // Extract base and quote currency from symbol value
            // Common quote currencies: USDT, USDC, USD, BTC, ETH
            var symbolValue = symbol.Value.ToUpperInvariant();
            var quoteCurrencies = new[] { "USDT", "USDC", "USD", "BTC", "ETH" };

            foreach (var quote in quoteCurrencies)
            {
                if (symbolValue.EndsWith(quote))
                {
                    var baseCurrency = symbolValue.Substring(0, symbolValue.Length - quote.Length);
                    if (!string.IsNullOrEmpty(baseCurrency))
                    {
                        return $"{baseCurrency}-{quote}";
                    }
                }
            }

            throw new ArgumentException($"Cannot parse spot symbol: {symbol.Value}. Unable to identify base/quote currency.");
        }

        /// <summary>
        /// Formats a futures symbol based on expiration
        /// Perpetual: /BTCUSDT → BTC-USDT-SWAP
        /// Delivery: BTCUSDT28H25 (expiry: 2025-03-28) → BTC-USDT-250328
        /// </summary>
        private string FormatFuturesSymbol(Symbol symbol)
        {
            // LEAN's Symbol.CreateFuture() generates symbols with special formatting:
            // - Perpetual (DefaultDate): /BTCUSDT (slash prefix)
            // - Delivery futures: BTCUSDT28H25 (includes expiry code suffix)
            // We need to extract just the base ticker part

            var symbolValue = symbol.Value.ToUpperInvariant();

            // Remove leading slash for perpetual contracts
            if (symbolValue.StartsWith("/"))
            {
                symbolValue = symbolValue.Substring(1);
            }

            // Remove LEAN's expiry suffix (e.g., "28H25" from "BTCUSDT28H25")
            // The suffix format is typically 2-5 characters at the end
            // We look for common quote currencies to find where the base ticker ends
            var quoteCurrencies = new[] { "USDT", "USDC", "USD", "BTC", "ETH" };

            string baseCurrency = null;
            string quoteCurrency = null;

            foreach (var quote in quoteCurrencies)
            {
                // Check if symbol contains this quote currency
                var quoteIndex = symbolValue.IndexOf(quote);
                if (quoteIndex > 0)
                {
                    baseCurrency = symbolValue.Substring(0, quoteIndex);
                    quoteCurrency = quote;
                    break;
                }
            }

            if (string.IsNullOrEmpty(baseCurrency) || string.IsNullOrEmpty(quoteCurrency))
            {
                throw new ArgumentException($"Cannot parse futures symbol: {symbol.Value}. Unable to identify base/quote currency.");
            }

            // Check if perpetual (no expiration date)
            if (symbol.ID.Date == SecurityIdentifier.DefaultDate)
            {
                return $"{baseCurrency}-{quoteCurrency}-SWAP";
            }

            // Delivery futures: format expiry as YYMMDD
            var expiryDate = symbol.ID.Date;
            var expiryString = expiryDate.ToString("yyMMdd", CultureInfo.InvariantCulture);

            return $"{baseCurrency}-{quoteCurrency}-{expiryString}";
        }

        /// <summary>
        /// Parses an OKX symbol into a LEAN Symbol
        /// Supports: BTC-USDT, BTC-USDT-SWAP, BTC-USDT-250328
        /// </summary>
        private Symbol ParseOKXSymbol(string brokerageSymbol, SecurityType securityType, string market)
        {
            var parts = brokerageSymbol.Split('-');

            if (parts.Length < 2)
            {
                throw new ArgumentException($"Invalid OKX symbol format: {brokerageSymbol}. Expected format: BASE-QUOTE or BASE-QUOTE-SWAP or BASE-QUOTE-YYMMDD");
            }

            var baseCurrency = parts[0];
            var quoteCurrency = parts[1];
            var leanSymbolValue = $"{baseCurrency}{quoteCurrency}";

            if (parts.Length == 2)
            {
                // Spot: BTC-USDT
                if (securityType != SecurityType.Crypto)
                {
                    throw new ArgumentException($"Symbol {brokerageSymbol} appears to be spot, but SecurityType is {securityType}");
                }

                return Symbol.Create(leanSymbolValue, SecurityType.Crypto, market);
            }
            else if (parts.Length == 3)
            {
                if (parts[2] == "SWAP")
                {
                    // Perpetual swap: BTC-USDT-SWAP
                    return Symbol.CreateFuture(leanSymbolValue, market, SecurityIdentifier.DefaultDate);
                }
                else
                {
                    // Delivery futures: BTC-USDT-250328
                    if (!DateTime.TryParseExact(parts[2], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiryDate))
                    {
                        throw new ArgumentException($"Invalid OKX futures expiry format: {parts[2]}. Expected YYMMDD.");
                    }

                    return Symbol.CreateFuture(leanSymbolValue, market, expiryDate);
                }
            }

            throw new ArgumentException($"Invalid OKX symbol format: {brokerageSymbol}");
        }
    }
}
