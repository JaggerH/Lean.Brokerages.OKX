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
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Helper class for calculating OKX futures risk limits
    /// Provides a simple interface for algorithms to query available order capacity
    /// </summary>
    /// <remarks>
    /// Usage in algorithm:
    /// <code>
    /// var riskHelper = new OKXRiskLimitHelper(this);
    /// var availableLimit = riskHelper.GetAvailableLimit(futuresSymbol);
    /// </code>
    ///
    /// Configuration required in config.json:
    /// - okx-api-key: Your OKX API key
    /// - okx-api-secret: Your OKX API secret
    /// </remarks>
    public class OKXRiskLimitHelper
    {
        private readonly OKXRiskLimitCalculator _calculator;

        /// <summary>
        /// Creates a new risk limit helper
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <exception cref="ArgumentNullException">Thrown when algorithm is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when API credentials are not configured</exception>
        public OKXRiskLimitHelper(IAlgorithm algorithm)
        {
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm));

            // Read API credentials from configuration
            var apiKey = Config.Get("okx-api-key");
            var apiSecret = Config.Get("okx-api-secret");
            var passphrase = Config.Get("okx-passphrase");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret) || string.IsNullOrEmpty(passphrase))
            {
                throw new InvalidOperationException(
                    "OKX API credentials not configured. " +
                    "Please set 'okx-api-key', 'okx-api-secret', and 'okx-passphrase' in your configuration.");
            }

            // Create REST API client
            var restApiUrl = OKXEnvironment.GetRestApiUrl();
            var restClient = new OKXRestApiClient(apiKey, apiSecret, passphrase, restApiUrl);

            // Create symbol mapper for OKX market
            var symbolMapper = new OKXSymbolMapper(Market.OKX);

            // Create calculator
            _calculator = new OKXRiskLimitCalculator(restClient, symbolMapper, algorithm);
        }

        /// <summary>
        /// Gets the available risk limit for a futures contract
        /// </summary>
        /// <param name="symbol">LEAN Symbol (must be SecurityType.CryptoFuture)</param>
        /// <returns>Available order value in USD. Returns decimal.MaxValue for non-futures symbols.</returns>
        /// <exception cref="Exception">Thrown when API calls fail</exception>
        public decimal GetAvailableLimit(Symbol symbol)
        {
            return _calculator.GetAvailableRiskLimit(symbol);
        }

        /// <summary>
        /// Clears the internal tier cache (useful for testing or forced refresh)
        /// </summary>
        public void ClearCache()
        {
            _calculator.ClearCache();
        }
    }
}
