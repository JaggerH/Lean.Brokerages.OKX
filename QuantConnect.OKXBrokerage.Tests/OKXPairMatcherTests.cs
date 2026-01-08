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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Brokerages.OKX;

namespace QuantConnect.OKXBrokerage.Tests
{
    /// <summary>
    /// Tests for OKXPairMatcher
    /// </summary>
    [TestFixture]
    public class OKXPairMatcherTests
    {
        [Test]
        public void DefaultMinVolumeUsdt_IsCorrect()
        {
            // Assert
            Assert.AreEqual(300000m, OKXPairMatcher.DefaultMinVolumeUsdt,
                "Default threshold should match Python okx.py (300,000 USDT)");
        }

        [Test]
        public void SymbolPair_SupportsIndexerAccess()
        {
            // Arrange
            var spot = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var futures = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX);
            var pair = new SymbolPair(spot, futures);

            // Assert
            Assert.AreEqual(spot, pair[0]);
            Assert.AreEqual(futures, pair[1]);
        }

        [Test]
        public void SymbolPair_SupportsDeconstruction()
        {
            // Arrange
            var spot = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var futures = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX);
            var pair = new SymbolPair(spot, futures);

            // Act
            var (first, second) = pair;

            // Assert
            Assert.AreEqual(spot, first);
            Assert.AreEqual(futures, second);
        }

        [Test]
        public void SymbolPair_SupportsEnumeration()
        {
            // Arrange
            var spot = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var futures = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX);
            var pair = new SymbolPair(spot, futures);

            // Act
            var symbols = pair.ToList();

            // Assert
            Assert.AreEqual(2, symbols.Count);
            Assert.AreEqual(spot, symbols[0]);
            Assert.AreEqual(futures, symbols[1]);
        }

        [Test]
        public void SymbolPair_ToString_ReturnsExpectedFormat()
        {
            // Arrange
            var spot = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var futures = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX);
            var pair = new SymbolPair(spot, futures);

            // Act
            var result = pair.ToString();

            // Assert
            Assert.AreEqual("BTCUSDT <-> BTCUSDT", result);
        }

        /// <summary>
        /// Integration test - requires API access
        /// </summary>
        [Test]
        [Category("Integration")]
        public void GetSpotFuturePairs_ReturnsValidPairs()
        {
            // Act
            var pairs = OKXPairMatcher.GetSpotFuturePairs(minVolumeUsdt: 1000000m);

            // Assert
            Assert.IsNotNull(pairs);

            foreach (var pair in pairs)
            {
                Assert.AreEqual(SecurityType.Crypto, pair[0].SecurityType, "First should be Spot");
                Assert.AreEqual(SecurityType.CryptoFuture, pair[1].SecurityType, "Second should be Futures");
                Assert.AreEqual(pair[0].Value, pair[1].Value, "Symbol names should match");
                Assert.AreEqual(Market.OKX, pair[0].ID.Market);
                Assert.AreEqual(Market.OKX, pair[1].ID.Market);
            }
        }

        /// <summary>
        /// Integration test - requires API access
        /// </summary>
        [Test]
        [Category("Integration")]
        public void GetTokenizedStockPairs_SpotOnly_ReturnsValidPairs()
        {
            // Act
            var pairs = OKXPairMatcher.GetTokenizedStockPairs(type: "spot", minVolumeUsdt: 0m);

            // Assert
            Assert.IsNotNull(pairs);

            foreach (var pair in pairs)
            {
                Assert.AreEqual(SecurityType.Crypto, pair[0].SecurityType, "First should be Spot TokenizedStock");
                Assert.AreEqual(SecurityType.Equity, pair[1].SecurityType, "Second should be Equity");
                Assert.AreEqual(Market.OKX, pair[0].ID.Market);
                Assert.AreEqual(Market.USA, pair[1].ID.Market);
            }
        }

        /// <summary>
        /// Integration test - requires API access
        /// </summary>
        [Test]
        [Category("Integration")]
        public void GetTokenizedStockPairs_FutureOnly_ReturnsValidPairs()
        {
            // Act
            var pairs = OKXPairMatcher.GetTokenizedStockPairs(type: "future", minVolumeUsdt: 0m);

            // Assert
            Assert.IsNotNull(pairs);

            foreach (var pair in pairs)
            {
                Assert.AreEqual(SecurityType.CryptoFuture, pair[0].SecurityType, "First should be Futures TokenizedStock");
                Assert.AreEqual(SecurityType.Equity, pair[1].SecurityType, "Second should be Equity");
                Assert.AreEqual(Market.OKX, pair[0].ID.Market);
                Assert.AreEqual(Market.USA, pair[1].ID.Market);
            }
        }

        /// <summary>
        /// Integration test - requires API access
        /// </summary>
        [Test]
        [Category("Integration")]
        [Explicit("Requires API access")]        public void GetAllQualifiedPairs_ReturnsValidPairs()
        {
            // Act
            var pairs = OKXPairMatcher.GetAllQualifiedPairs(minVolumeUsdt: 1000000m);

            // Assert
            Assert.IsNotNull(pairs);

            foreach (var pair in pairs)
            {
                // First symbol is always from OKX market
                Assert.AreEqual(Market.OKX, pair[0].ID.Market);

                // Second symbol can be OKX (Futures) or USA (Equity)
                Assert.That(pair[1].ID.Market, Is.EqualTo(Market.OKX).Or.EqualTo(Market.USA));
            }
        }
    }
}
