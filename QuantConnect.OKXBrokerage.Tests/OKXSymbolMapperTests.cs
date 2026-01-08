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

using NUnit.Framework;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXSymbolMapperTests
    {
        private OKXSymbolMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            _mapper = new OKXSymbolMapper(Market.OKX);
        }

        /// <summary>
        /// Tests that GetLeanSymbol can dynamically fetch and register an unknown futures symbol
        /// </summary>
        [Test]
        public void GetLeanSymbol_DynamicallyRegisters_UnknownFuturesSymbol()
        {
            // Arrange
            var brokerageSymbol = "ADA_USDT";
            var securityType = SecurityType.CryptoFuture;
            var market = Market.OKX;

            // Act
            var symbol = _mapper.GetLeanSymbol(brokerageSymbol, securityType, market);

            // Assert
            Assert.IsNotNull(symbol, "Symbol should not be null");
            Assert.AreEqual("ADAUSDT", symbol.Value, "Symbol value should be ADAUSDT");
            Assert.AreEqual(SecurityType.CryptoFuture, symbol.SecurityType, "Security type should be CryptoFuture");
            Assert.AreEqual(Market.OKX, symbol.ID.Market, "Market should be OKX");

            // Verify that SymbolProperties was registered
            var symbolProperties = SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolProperties(market, symbol, securityType, "USDT");

            Assert.IsNotNull(symbolProperties, "SymbolProperties should be registered");
            Assert.AreEqual(brokerageSymbol, symbolProperties.MarketTicker, "MarketTicker should match brokerage symbol");

            Log.Trace($"✓ Successfully registered {symbol.Value} with properties: " +
                $"ContractMultiplier={symbolProperties.ContractMultiplier}, " +
                $"LotSize={symbolProperties.LotSize}, " +
                $"MinimumPriceVariation={symbolProperties.MinimumPriceVariation}");
        }

        /// <summary>
        /// Tests that GetLeanSymbol can dynamically fetch and register an unknown spot symbol
        /// </summary>
        [Test]
        public void GetLeanSymbol_DynamicallyRegisters_UnknownSpotSymbol()
        {
            // Arrange - use a symbol that's unlikely to be in CSV
            var brokerageSymbol = "DOGE_USDT";
            var securityType = SecurityType.Crypto;
            var market = Market.OKX;

            // Act
            var symbol = _mapper.GetLeanSymbol(brokerageSymbol, securityType, market);

            // Assert
            Assert.IsNotNull(symbol, "Symbol should not be null");
            Assert.AreEqual("DOGEUSDT", symbol.Value, "Symbol value should be DOGEUSDT");
            Assert.AreEqual(SecurityType.Crypto, symbol.SecurityType, "Security type should be Crypto");
            Assert.AreEqual(Market.OKX, symbol.ID.Market, "Market should be OKX");

            Log.Trace($"✓ Successfully registered spot symbol: {symbol.Value}");
        }
    }
}
