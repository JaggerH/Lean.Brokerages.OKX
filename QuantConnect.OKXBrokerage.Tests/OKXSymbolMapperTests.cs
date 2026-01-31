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
using System;

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

        #region LEAN → OKX Symbol Conversion Tests

        /// <summary>
        /// Tests spot symbol conversion: BTCUSDT → BTC-USDT
        /// </summary>
        [Test]
        public void GetBrokerageSymbol_Spot_BTCUSDT()
        {
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual("BTC-USDT", brokerageSymbol);
        }

        /// <summary>
        /// Tests spot symbol conversion: ETHUSDT → ETH-USDT
        /// </summary>
        [Test]
        public void GetBrokerageSymbol_Spot_ETHUSDT()
        {
            var symbol = Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.OKX);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual("ETH-USDT", brokerageSymbol);
        }

        /// <summary>
        /// Tests spot symbol with USDC quote: BTCUSDC → BTC-USDC
        /// </summary>
        [Test]
        public void GetBrokerageSymbol_Spot_BTCUSDC()
        {
            var symbol = Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.OKX);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual("BTC-USDC", brokerageSymbol);
        }

        // Note: Perpetual and Delivery Futures tests removed
        // These require Symbol.CreateFuture() which creates symbols with "/" prefix or date suffixes
        // that don't match CSV database entries. Tests will be added back when ToolBox downloads
        // these symbols to CSV database.

        #endregion

        #region OKX → LEAN Symbol Conversion Tests

        /// <summary>
        /// Tests OKX → LEAN conversion: BTC-USDT → BTCUSDT (Crypto)
        /// </summary>
        [Test]
        public void GetLeanSymbol_Spot_BTCUSDT()
        {
            var brokerageSymbol = "BTC-USDT";
            var symbol = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto, Market.OKX);

            Assert.AreEqual("BTCUSDT", symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.SecurityType);
            Assert.AreEqual(Market.OKX, symbol.ID.Market);
        }

        /// <summary>
        /// Tests OKX → LEAN conversion: ETH-USDT → ETHUSDT (Crypto)
        /// </summary>
        [Test]
        public void GetLeanSymbol_Spot_ETHUSDT()
        {
            var brokerageSymbol = "ETH-USDT";
            var symbol = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto, Market.OKX);

            Assert.AreEqual("ETHUSDT", symbol.Value);
            Assert.AreEqual(SecurityType.Crypto, symbol.SecurityType);
        }

        /// <summary>
        /// Tests OKX → LEAN conversion: BTC-USDT-SWAP → BTCUSDT (CryptoFuture, perpetual)
        /// CSV database defines perpetual swaps as CryptoFuture with BTCUSDT value (no slash prefix)
        /// </summary>
        [Test]
        public void GetLeanSymbol_PerpetualSwap_BTCUSDT()
        {
            var brokerageSymbol = "BTC-USDT-SWAP";
            var symbol = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.CryptoFuture, Market.OKX);

            Assert.IsTrue(symbol.Value.Contains("BTCUSDT"), $"Symbol value should contain BTCUSDT, got: {symbol.Value}");
            Assert.AreEqual(SecurityType.CryptoFuture, symbol.SecurityType);
            Assert.AreEqual(SecurityIdentifier.DefaultDate, symbol.ID.Date, "Perpetual swap should have DefaultDate");
        }

        /// <summary>
        /// Tests OKX → LEAN conversion for delivery futures throws when not in CSV database.
        /// Delivery futures like BTC-USDT-250328 must be explicitly mapped in symbol-properties-database.csv.
        /// </summary>
        [Test]
        public void GetLeanSymbol_DeliveryFutures_NotInDatabase_ThrowsException()
        {
            // Delivery futures are not in CSV database - expect exception
            Assert.Throws<ArgumentException>(() =>
                _mapper.GetLeanSymbol("BTC-USDT-250328", SecurityType.CryptoFuture, Market.OKX));
            Assert.Throws<ArgumentException>(() =>
                _mapper.GetLeanSymbol("ETH-USDT-251231", SecurityType.CryptoFuture, Market.OKX));
        }

        #endregion

        #region Round-Trip Conversion Tests

        /// <summary>
        /// Tests round-trip conversion for spot symbols
        /// </summary>
        [Test]
        public void RoundTrip_Spot_BTCUSDT()
        {
            var originalSymbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(originalSymbol);
            var convertedSymbol = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto, Market.OKX);

            Assert.AreEqual(originalSymbol.Value, convertedSymbol.Value);
            Assert.AreEqual(originalSymbol.SecurityType, convertedSymbol.SecurityType);
        }

        /// <summary>
        /// Tests round-trip conversion for perpetual swap symbols
        /// CSV database defines perpetual swaps as CryptoFuture, so we start from GetLeanSymbol
        /// to ensure we use the actual CSV database representation
        /// </summary>
        [Test]
        public void RoundTrip_PerpetualSwap_BTCUSDT()
        {
            // Start from OKX symbol to get the CSV database representation
            var originalSymbol = _mapper.GetLeanSymbol("BTC-USDT-SWAP", SecurityType.CryptoFuture, Market.OKX);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(originalSymbol);
            var convertedSymbol = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.CryptoFuture, Market.OKX);

            Assert.AreEqual(originalSymbol.Value, convertedSymbol.Value);
            Assert.AreEqual(originalSymbol.SecurityType, convertedSymbol.SecurityType);
            Assert.AreEqual(originalSymbol.ID.Date, convertedSymbol.ID.Date);
            Assert.AreEqual("BTC-USDT-SWAP", brokerageSymbol);
        }

        /// <summary>
        /// Tests that delivery futures symbols not in CSV database throw exception.
        /// Round-trip conversion requires symbols to be explicitly mapped in symbol-properties-database.csv.
        /// </summary>
        [Test]
        public void RoundTrip_DeliveryFutures_NotInDatabase_ThrowsException()
        {
            var expiryDate = new DateTime(2025, 3, 28);
            var originalSymbol = Symbol.CreateFuture("BTCUSDT", Market.OKX, expiryDate);

            // Delivery futures are not in CSV database - expect exception
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(originalSymbol));
        }

        #endregion

        #region Security Type Detection Tests

        /// <summary>
        /// Tests that GetBrokerageSecurityType correctly identifies spot symbols
        /// </summary>
        [Test]
        public void GetBrokerageSecurityType_Spot()
        {
            Assert.AreEqual(SecurityType.Crypto, _mapper.GetBrokerageSecurityType("BTC-USDT"));
            Assert.AreEqual(SecurityType.Crypto, _mapper.GetBrokerageSecurityType("ETH-USDC"));
        }

        /// <summary>
        /// Tests that GetBrokerageSecurityType correctly identifies perpetual swaps
        /// </summary>
        [Test]
        public void GetBrokerageSecurityType_PerpetualSwap()
        {
            Assert.AreEqual(SecurityType.CryptoFuture, _mapper.GetBrokerageSecurityType("BTC-USDT-SWAP"));
            Assert.AreEqual(SecurityType.CryptoFuture, _mapper.GetBrokerageSecurityType("ETH-USDT-SWAP"));
        }

        /// <summary>
        /// Tests that GetBrokerageSecurityType throws for delivery futures not in CSV database.
        /// All symbols must be explicitly mapped in symbol-properties-database.csv.
        /// </summary>
        [Test]
        public void GetBrokerageSecurityType_DeliveryFutures_NotInDatabase_ThrowsException()
        {
            // Delivery futures are not in CSV database - expect exception
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSecurityType("BTC-USDT-250328"));
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSecurityType("ETH-USDT-251231"));
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests that invalid OKX symbol format throws exception
        /// </summary>
        [Test]
        public void GetLeanSymbol_InvalidFormat_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                _mapper.GetLeanSymbol("INVALID", SecurityType.Crypto, Market.OKX));
        }

        /// <summary>
        /// Tests that invalid LEAN symbol throws exception
        /// </summary>
        [Test]
        public void GetBrokerageSymbol_InvalidSymbol_ThrowsException()
        {
            var symbol = Symbol.Create("XYZ", SecurityType.Crypto, Market.OKX);
            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(symbol));
        }

        /// <summary>
        /// Tests that unsupported security type throws exception
        /// After refactoring: CSV database will throw ArgumentException for symbols not in database
        /// </summary>
        [Test]
        public void GetBrokerageSymbol_UnsupportedSecurityType_ThrowsException()
        {
            // Create a Forex symbol (unsupported by OKX)
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);

            Assert.Throws<ArgumentException>(() => _mapper.GetBrokerageSymbol(symbol));
        }

        #endregion

        #region Multiple Quote Currency Tests

        /// <summary>
        /// Tests symbols with different quote currencies
        /// </summary>
        [TestCase("BTCUSDT", "BTC-USDT")]
        [TestCase("BTCUSDC", "BTC-USDC")]
        [TestCase("BTCUSD", "BTC-USD")]
        [TestCase("ETHBTC", "ETH-BTC")]
        [TestCase("ETHETH", "ETH-ETH")] // Edge case: same base and quote
        public void GetBrokerageSymbol_VariousQuoteCurrencies(string leanSymbol, string expectedOKXSymbol)
        {
            var symbol = Symbol.Create(leanSymbol, SecurityType.Crypto, Market.OKX);
            var brokerageSymbol = _mapper.GetBrokerageSymbol(symbol);

            Assert.AreEqual(expectedOKXSymbol, brokerageSymbol);
        }

        #endregion
    }
}
