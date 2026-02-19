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
using QuantConnect.Securities;

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
        /// Verifies that requireMarginTrading filters out pairs without MARGIN instruments
        /// </summary>
        [Test]
        [Category("Integration")]
        public void GetSpotFuturePairs_RequireMarginTrading_ReturnsSubsetOfAll()
        {
            // Act
            var allPairs = OKXPairMatcher.GetSpotFuturePairs(minVolumeUsdt: 0m, requireMarginTrading: false);
            var marginPairs = OKXPairMatcher.GetSpotFuturePairs(minVolumeUsdt: 0m, requireMarginTrading: true);

            // Assert
            Assert.IsNotNull(marginPairs);
            Assert.Greater(marginPairs.Count, 0, "Should find at least one pair with margin trading");
            Assert.LessOrEqual(marginPairs.Count, allPairs.Count, "Margin filter should not increase pair count");

            TestContext.WriteLine($"All pairs: {allPairs.Count}, Margin pairs: {marginPairs.Count}, Excluded: {allPairs.Count - marginPairs.Count}");

            var marginSymbols = new System.Collections.Generic.HashSet<string>(marginPairs.Select(p => p[0].Value));
            foreach (var pair in marginPairs)
            {
                Assert.AreEqual(SecurityType.Crypto, pair[0].SecurityType, "First should be Spot");
                Assert.AreEqual(SecurityType.CryptoFuture, pair[1].SecurityType, "Second should be Futures");
            }

            // Verify the filter actually excluded something (there should be pairs without margin)
            if (allPairs.Count > marginPairs.Count)
            {
                var excluded = allPairs.Where(p => !marginSymbols.Contains(p[0].Value)).ToList();
                TestContext.WriteLine($"Excluded pairs (no margin trading):");
                foreach (var pair in excluded.Take(10))
                {
                    TestContext.WriteLine($"  {pair[0].Value}");
                }
            }
        }

        [Test]
        public void GetSpotFuturePairs_MatchingAndVolumeFilter()
        {
            // 1) No volume filter - verify matching works
            var allPairs = OKXPairMatcher.GetSpotFuturePairs(minVolumeUsdt: 0m);

            Assert.IsNotNull(allPairs);
            Assert.Greater(allPairs.Count, 0, "Should find at least one spot-future pair");

            TestContext.WriteLine($"=== No Volume Filter: {allPairs.Count} pairs ===");
            foreach (var pair in allPairs.Take(5))
            {
                TestContext.WriteLine($"  Spot: {pair[0].Value} ({pair[0].SecurityType})  <->  Future: {pair[1].Value} ({pair[1].SecurityType})");
                Assert.AreEqual(SecurityType.Crypto, pair[0].SecurityType);
                Assert.AreEqual(SecurityType.CryptoFuture, pair[1].SecurityType);
                Assert.AreEqual(pair[0].Value, pair[1].Value);
            }

            // 2) Default volume filter - verify filtering works
            var filteredPairs = OKXPairMatcher.GetSpotFuturePairs();

            Assert.IsNotNull(filteredPairs);
            Assert.Less(filteredPairs.Count, allPairs.Count, "Volume filter should reduce pair count");

            TestContext.WriteLine($"\n=== Volume Filter ({OKXPairMatcher.DefaultMinVolumeUsdt:N0} USDT): {filteredPairs.Count} pairs ===");
            foreach (var pair in filteredPairs.Take(5))
            {
                TestContext.WriteLine($"  Spot: {pair[0].Value} ({pair[0].SecurityType})  <->  Future: {pair[1].Value} ({pair[1].SecurityType})");
            }
        }
    }
}
