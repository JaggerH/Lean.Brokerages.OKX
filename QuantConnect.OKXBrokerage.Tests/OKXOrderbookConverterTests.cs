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
using NUnit.Framework;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.OKX.Tests
{
    /// <summary>
    /// Unit tests for OKXOrderbookConverter
    /// </summary>
    [TestFixture]
    public class OKXOrderbookConverterTests
    {
        private Symbol _symbol;

        [SetUp]
        public void Setup()
        {
            _symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
        }

        [Test]
        public void ToOrderbook_WithNullOrderBook_ThrowsArgumentNullException()
        {
            // Arrange
            OKXOrderBook orderBook = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => orderBook.ToOrderbook());
        }

        [Test]
        public void ToOrderbook_WithEmptyOrderBook_ReturnsEmptyDepth()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.IsNotNull(depth);
            Assert.AreEqual(_symbol, depth.Symbol);
            Assert.AreEqual(0, depth.Bids.Count);
            Assert.AreEqual(0, depth.Asks.Count);
            Assert.AreEqual(0, depth.Levels);
            Assert.AreEqual(0m, depth.Value);
        }

        [Test]
        public void ToOrderbook_WithSingleBidAndAsk_ConvertsCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.5m);
            orderBook.UpdateAskRow(50100m, 2.0m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.IsNotNull(depth);
            Assert.AreEqual(_symbol, depth.Symbol);
            Assert.AreEqual(1, depth.Bids.Count);
            Assert.AreEqual(1, depth.Asks.Count);
            Assert.AreEqual(1, depth.Levels);

            // Check bid
            Assert.AreEqual(50000m, depth.Bids[0].Price);
            Assert.AreEqual(1.5m, depth.Bids[0].Size);

            // Check ask
            Assert.AreEqual(50100m, depth.Asks[0].Price);
            Assert.AreEqual(2.0m, depth.Asks[0].Size);

            // Check mid-price
            Assert.AreEqual(50050m, depth.Value);
        }

        [Test]
        public void ToOrderbook_BidsAreSortedDescending_BestBidFirst()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(49990m, 1.0m);
            orderBook.UpdateBidRow(50000m, 1.5m);
            orderBook.UpdateBidRow(49980m, 2.0m);
            orderBook.UpdateBidRow(50010m, 0.5m);
            orderBook.UpdateAskRow(50100m, 1.0m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(4, depth.Bids.Count);

            // Verify descending order (best bid first)
            Assert.AreEqual(50010m, depth.Bids[0].Price, "Best bid (highest price) should be first");
            Assert.AreEqual(50000m, depth.Bids[1].Price);
            Assert.AreEqual(49990m, depth.Bids[2].Price);
            Assert.AreEqual(49980m, depth.Bids[3].Price, "Worst bid (lowest price) should be last");

            // Verify sizes match
            Assert.AreEqual(0.5m, depth.Bids[0].Size);
            Assert.AreEqual(1.5m, depth.Bids[1].Size);
            Assert.AreEqual(1.0m, depth.Bids[2].Size);
            Assert.AreEqual(2.0m, depth.Bids[3].Size);
        }

        [Test]
        public void ToOrderbook_AsksAreSortedAscending_BestAskFirst()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateAskRow(51010m, 1.5m);
            orderBook.UpdateAskRow(51000m, 1.0m);
            orderBook.UpdateAskRow(51030m, 2.5m);
            orderBook.UpdateAskRow(50990m, 0.5m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(4, depth.Asks.Count);

            // Verify ascending order (best ask first)
            Assert.AreEqual(50990m, depth.Asks[0].Price, "Best ask (lowest price) should be first");
            Assert.AreEqual(51000m, depth.Asks[1].Price);
            Assert.AreEqual(51010m, depth.Asks[2].Price);
            Assert.AreEqual(51030m, depth.Asks[3].Price, "Worst ask (highest price) should be last");

            // Verify sizes match
            Assert.AreEqual(0.5m, depth.Asks[0].Size);
            Assert.AreEqual(1.0m, depth.Asks[1].Size);
            Assert.AreEqual(1.5m, depth.Asks[2].Size);
            Assert.AreEqual(2.5m, depth.Asks[3].Size);
        }

        [Test]
        public void ToOrderbook_WithMultipleLevels_ConvertsAllLevels()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Add 10 bid levels
            for (int i = 0; i < 10; i++)
            {
                orderBook.UpdateBidRow(50000m - i * 10, 1.0m + i * 0.1m);
            }

            // Add 10 ask levels
            for (int i = 0; i < 10; i++)
            {
                orderBook.UpdateAskRow(50100m + i * 10, 1.5m + i * 0.1m);
            }

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(10, depth.Bids.Count);
            Assert.AreEqual(10, depth.Asks.Count);
            Assert.AreEqual(10, depth.Levels);
        }

        [Test]
        public void ToOrderbook_Levels_ReflectsMaxCount()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Add 5 bids
            for (int i = 0; i < 5; i++)
            {
                orderBook.UpdateBidRow(50000m - i * 10, 1.0m);
            }

            // Add 3 asks
            for (int i = 0; i < 3; i++)
            {
                orderBook.UpdateAskRow(50100m + i * 10, 1.0m);
            }

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(5, depth.Bids.Count);
            Assert.AreEqual(3, depth.Asks.Count);
            Assert.AreEqual(5, depth.Levels, "Levels should be max of bid and ask counts");
        }

        [Test]
        public void ToOrderbook_MidPrice_CalculatedCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateAskRow(50200m, 1.0m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(50100m, depth.Value, "Mid-price should be (50000 + 50200) / 2");
        }

        [Test]
        public void ToOrderbook_WithOnlyBids_ValueIsZero()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 1.5m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(2, depth.Bids.Count);
            Assert.AreEqual(0, depth.Asks.Count);
            Assert.AreEqual(0m, depth.Value, "Value should be 0 when asks are empty");
        }

        [Test]
        public void ToOrderbook_WithOnlyAsks_ValueIsZero()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateAskRow(51000m, 1.0m);
            orderBook.UpdateAskRow(51010m, 1.5m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(0, depth.Bids.Count);
            Assert.AreEqual(2, depth.Asks.Count);
            Assert.AreEqual(0m, depth.Value, "Value should be 0 when bids are empty");
        }

        [Test]
        public void ToOrderbook_TimestampIsRecent()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateAskRow(50100m, 1.0m);

            var beforeConversion = DateTime.UtcNow;

            // Act
            var depth = orderBook.ToOrderbook();

            var afterConversion = DateTime.UtcNow;

            // Assert
            Assert.GreaterOrEqual(depth.Time, beforeConversion);
            Assert.LessOrEqual(depth.Time, afterConversion);
        }

        [Test]
        public void ToOrderbook_SymbolIsPreserved()
        {
            // Arrange
            var symbol = Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.OKX);
            var orderBook = new OKXOrderBook(symbol, 100);
            orderBook.UpdateBidRow(3000m, 10.0m);
            orderBook.UpdateAskRow(3010m, 12.0m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(symbol, depth.Symbol);
        }

        [Test]
        public void ToOrderbook_HelperMethods_WorkCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 1.5m);
            orderBook.UpdateAskRow(50100m, 2.0m);
            orderBook.UpdateAskRow(50110m, 2.5m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(100m, depth.GetSpread(), "Spread should be 50100 - 50000");
            Assert.AreEqual(50050m, depth.GetMidPrice(), "Mid price should be (50000 + 50100) / 2");

            var (bidPrice, bidSize) = depth.GetBestBid();
            Assert.AreEqual(50000m, bidPrice);
            Assert.AreEqual(1.0m, bidSize);

            var (askPrice, askSize) = depth.GetBestAsk();
            Assert.AreEqual(50100m, askPrice);
            Assert.AreEqual(2.0m, askSize);
        }

        [Test]
        public void ToOrderbook_MaxDepth100_ConvertsAllLevels()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Add 100 bid and ask levels
            for (int i = 0; i < 100; i++)
            {
                orderBook.UpdateBidRow(50000m - i, 1.0m);
                orderBook.UpdateAskRow(50100m + i, 1.0m);
            }

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(100, depth.Bids.Count);
            Assert.AreEqual(100, depth.Asks.Count);
            Assert.AreEqual(100, depth.Levels);

            // Verify first and last bids
            Assert.AreEqual(50000m, depth.Bids[0].Price, "Best bid");
            Assert.AreEqual(49901m, depth.Bids[99].Price, "Worst bid");

            // Verify first and last asks
            Assert.AreEqual(50100m, depth.Asks[0].Price, "Best ask");
            Assert.AreEqual(50199m, depth.Asks[99].Price, "Worst ask");
        }

        #region New Architecture Tests

        [Test]
        public void ApplyFullSnapshot_WithValidData_ConvertsToDepthCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            var bids = new List<List<string>>
            {
                new List<string> { "50000", "1.5" },
                new List<string> { "49990", "2.0" },
                new List<string> { "49980", "1.0" }
            };
            var asks = new List<List<string>>
            {
                new List<string> { "50100", "1.0" },
                new List<string> { "50110", "1.5" },
                new List<string> { "50120", "2.0" }
            };

            // Act
            orderBook.ApplyFullSnapshot(bids, asks);
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(3, depth.Bids.Count);
            Assert.AreEqual(3, depth.Asks.Count);
            Assert.AreEqual(3, depth.Levels);

            // Verify bids are sorted descending (best first)
            Assert.AreEqual(50000m, depth.Bids[0].Price);
            Assert.AreEqual(1.5m, depth.Bids[0].Size);
            Assert.AreEqual(49990m, depth.Bids[1].Price);
            Assert.AreEqual(2.0m, depth.Bids[1].Size);
            Assert.AreEqual(49980m, depth.Bids[2].Price);
            Assert.AreEqual(1.0m, depth.Bids[2].Size);

            // Verify asks are sorted ascending (best first)
            Assert.AreEqual(50100m, depth.Asks[0].Price);
            Assert.AreEqual(1.0m, depth.Asks[0].Size);
            Assert.AreEqual(50110m, depth.Asks[1].Price);
            Assert.AreEqual(1.5m, depth.Asks[1].Size);
            Assert.AreEqual(50120m, depth.Asks[2].Price);
            Assert.AreEqual(2.0m, depth.Asks[2].Size);

            // Verify mid-price
            Assert.AreEqual(50050m, depth.Value);
        }

        [Test]
        public void ApplyFullSnapshot_WithEmptyData_ReturnsEmptyDepth()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            var bids = new List<List<string>>();
            var asks = new List<List<string>>();

            // Act
            orderBook.ApplyFullSnapshot(bids, asks);
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(0, depth.Bids.Count);
            Assert.AreEqual(0, depth.Asks.Count);
            Assert.AreEqual(0, depth.Levels);
            Assert.AreEqual(0m, depth.Value);
        }

        [Test]
        public void ApplyFullSnapshot_WithNullData_ReturnsEmptyDepth()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Act
            orderBook.ApplyFullSnapshot(null, null);
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(0, depth.Bids.Count);
            Assert.AreEqual(0, depth.Asks.Count);
            Assert.AreEqual(0, depth.Levels);
            Assert.AreEqual(0m, depth.Value);
        }

        [Test]
        public void ApplyFullSnapshot_ReplacesExistingData()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Initial snapshot
            var initialBids = new List<List<string>>
            {
                new List<string> { "50000", "1.0" },
                new List<string> { "49990", "2.0" }
            };
            var initialAsks = new List<List<string>>
            {
                new List<string> { "50100", "1.5" }
            };
            orderBook.ApplyFullSnapshot(initialBids, initialAsks);

            // New snapshot (completely different data)
            var newBids = new List<List<string>>
            {
                new List<string> { "60000", "5.0" }
            };
            var newAsks = new List<List<string>>
            {
                new List<string> { "60100", "3.0" },
                new List<string> { "60110", "4.0" }
            };

            // Act
            orderBook.ApplyFullSnapshot(newBids, newAsks);
            var depth = orderBook.ToOrderbook();

            // Assert - should only have new data
            Assert.AreEqual(1, depth.Bids.Count);
            Assert.AreEqual(2, depth.Asks.Count);
            Assert.AreEqual(60000m, depth.Bids[0].Price);
            Assert.AreEqual(5.0m, depth.Bids[0].Size);
            Assert.AreEqual(60100m, depth.Asks[0].Price);
            Assert.AreEqual(3.0m, depth.Asks[0].Size);
        }

        [Test]
        public void ApplyIncrementalUpdate_AfterSnapshot_ConvertsCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Initial snapshot
            var bids = new List<List<string>>
            {
                new List<string> { "50000", "1.0" },
                new List<string> { "49990", "2.0" }
            };
            var asks = new List<List<string>>
            {
                new List<string> { "50100", "1.5" },
                new List<string> { "50110", "2.0" }
            };
            orderBook.ApplyFullSnapshot(bids, asks);

            // Incremental update: update existing level and add new level
            var updateBids = new List<List<string>>
            {
                new List<string> { "50000", "3.0" }, // Update existing
                new List<string> { "49980", "1.5" }  // Add new
            };
            var updateAsks = new List<List<string>>
            {
                new List<string> { "50100", "4.0" }  // Update existing
            };

            // Act
            orderBook.ApplyIncrementalUpdate(updateBids, updateAsks);
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(3, depth.Bids.Count);
            Assert.AreEqual(2, depth.Asks.Count);

            // Verify updated bid
            Assert.AreEqual(50000m, depth.Bids[0].Price);
            Assert.AreEqual(3.0m, depth.Bids[0].Size, "Bid size should be updated");

            // Verify new bid
            Assert.AreEqual(49980m, depth.Bids[2].Price);
            Assert.AreEqual(1.5m, depth.Bids[2].Size);

            // Verify updated ask
            Assert.AreEqual(50100m, depth.Asks[0].Price);
            Assert.AreEqual(4.0m, depth.Asks[0].Size, "Ask size should be updated");
        }

        [Test]
        public void ApplyIncrementalUpdate_RemovesLevelsWithZeroSize()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Initial snapshot
            var bids = new List<List<string>>
            {
                new List<string> { "50000", "1.0" },
                new List<string> { "49990", "2.0" },
                new List<string> { "49980", "1.5" }
            };
            var asks = new List<List<string>>
            {
                new List<string> { "50100", "1.5" },
                new List<string> { "50110", "2.0" }
            };
            orderBook.ApplyFullSnapshot(bids, asks);

            // Incremental update: remove levels by setting size to 0
            var updateBids = new List<List<string>>
            {
                new List<string> { "49990", "0" }  // Remove this level
            };
            var updateAsks = new List<List<string>>
            {
                new List<string> { "50110", "0" }  // Remove this level
            };

            // Act
            orderBook.ApplyIncrementalUpdate(updateBids, updateAsks);
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(2, depth.Bids.Count, "One bid should be removed");
            Assert.AreEqual(1, depth.Asks.Count, "One ask should be removed");

            // Verify remaining bids
            Assert.AreEqual(50000m, depth.Bids[0].Price);
            Assert.AreEqual(49980m, depth.Bids[1].Price);

            // Verify remaining asks
            Assert.AreEqual(50100m, depth.Asks[0].Price);
        }

        [Test]
        public void GetBids_ReturnsSortedDescending()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 2.0m);
            orderBook.UpdateBidRow(50010m, 0.5m);
            orderBook.UpdateBidRow(49980m, 1.5m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert - should match GetBids() sorting
            Assert.AreEqual(4, depth.Bids.Count);
            Assert.AreEqual(50010m, depth.Bids[0].Price, "Best bid (highest price) first");
            Assert.AreEqual(50000m, depth.Bids[1].Price);
            Assert.AreEqual(49990m, depth.Bids[2].Price);
            Assert.AreEqual(49980m, depth.Bids[3].Price, "Worst bid (lowest price) last");
        }

        [Test]
        public void GetAsks_ReturnsSortedAscending()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            orderBook.UpdateAskRow(50100m, 1.0m);
            orderBook.UpdateAskRow(50120m, 2.0m);
            orderBook.UpdateAskRow(50090m, 0.5m);
            orderBook.UpdateAskRow(50110m, 1.5m);

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert - should match GetAsks() sorting
            Assert.AreEqual(4, depth.Asks.Count);
            Assert.AreEqual(50090m, depth.Asks[0].Price, "Best ask (lowest price) first");
            Assert.AreEqual(50100m, depth.Asks[1].Price);
            Assert.AreEqual(50110m, depth.Asks[2].Price);
            Assert.AreEqual(50120m, depth.Asks[3].Price, "Worst ask (highest price) last");
        }

        [Test]
        public void ApplyFullSnapshot_IgnoresInvalidData()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);
            var bids = new List<List<string>>
            {
                new List<string> { "50000", "1.5" },      // Valid
                new List<string> { "invalid", "2.0" },    // Invalid price
                new List<string> { "49990", "invalid" },  // Invalid size
                new List<string> { "49980" },             // Missing size
                new List<string> { "49970", "0" },        // Zero size (should be ignored)
                new List<string> { "49960", "-1" }        // Negative size (should be ignored)
            };
            var asks = new List<List<string>>
            {
                new List<string> { "50100", "1.0" },      // Valid
                new List<string> { "50110", "0" }         // Zero size (should be ignored)
            };

            // Act
            orderBook.ApplyFullSnapshot(bids, asks);
            var depth = orderBook.ToOrderbook();

            // Assert - only valid entries should be present
            Assert.AreEqual(1, depth.Bids.Count, "Only one valid bid");
            Assert.AreEqual(1, depth.Asks.Count, "Only one valid ask");
            Assert.AreEqual(50000m, depth.Bids[0].Price);
            Assert.AreEqual(1.5m, depth.Bids[0].Size);
            Assert.AreEqual(50100m, depth.Asks[0].Price);
            Assert.AreEqual(1.0m, depth.Asks[0].Size);
        }

        [Test]
        public void ApplyIncrementalUpdate_IgnoresInvalidData()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Initial valid snapshot
            orderBook.ApplyFullSnapshot(
                new List<List<string>> { new List<string> { "50000", "1.0" } },
                new List<List<string>> { new List<string> { "50100", "1.0" } }
            );

            // Incremental update with invalid data
            var updateBids = new List<List<string>>
            {
                new List<string> { "49990", "2.0" },      // Valid
                new List<string> { "invalid", "3.0" },    // Invalid price
                new List<string> { "49980", "invalid" }   // Invalid size
            };
            var updateAsks = new List<List<string>>
            {
                new List<string> { "50110", "2.0" }       // Valid
            };

            // Act
            orderBook.ApplyIncrementalUpdate(updateBids, updateAsks);
            var depth = orderBook.ToOrderbook();

            // Assert - only valid updates should be applied
            Assert.AreEqual(2, depth.Bids.Count, "Original bid + one valid update");
            Assert.AreEqual(2, depth.Asks.Count, "Original ask + one valid update");
        }

        [Test]
        public void MultipleIncrementalUpdates_AfterSnapshot_MaintainsCorrectState()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol, 100);

            // Initial snapshot
            orderBook.ApplyFullSnapshot(
                new List<List<string>>
                {
                    new List<string> { "50000", "1.0" },
                    new List<string> { "49990", "2.0" }
                },
                new List<List<string>>
                {
                    new List<string> { "50100", "1.0" }
                }
            );

            // First incremental update
            orderBook.ApplyIncrementalUpdate(
                new List<List<string>> { new List<string> { "50010", "0.5" } },
                new List<List<string>> { new List<string> { "50110", "1.5" } }
            );

            // Second incremental update
            orderBook.ApplyIncrementalUpdate(
                new List<List<string>> { new List<string> { "50000", "0" } }, // Remove best bid
                new List<List<string>> { new List<string> { "50100", "2.0" } } // Update best ask
            );

            // Act
            var depth = orderBook.ToOrderbook();

            // Assert
            Assert.AreEqual(2, depth.Bids.Count, "One bid removed, two remaining");
            Assert.AreEqual(2, depth.Asks.Count, "Two asks");

            // Best bid should now be 50010
            Assert.AreEqual(50010m, depth.Bids[0].Price);
            Assert.AreEqual(0.5m, depth.Bids[0].Size);

            // Best ask should have updated size
            Assert.AreEqual(50100m, depth.Asks[0].Price);
            Assert.AreEqual(2.0m, depth.Asks[0].Size);

            // Mid-price calculation
            Assert.AreEqual(50055m, depth.Value, "(50010 + 50100) / 2");
        }

        #endregion
    }
}
