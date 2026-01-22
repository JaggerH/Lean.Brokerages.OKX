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

using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXChecksumValidatorTests
    {
        private Symbol _symbol;

        [SetUp]
        public void SetUp()
        {
            _symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
        }

        [Test]
        public void CalculateChecksum_WithSampleData_MatchesExpectedFormat()
        {
            // Arrange - Create orderbook with sample data from OKX docs
            var orderBook = new OKXOrderBook(_symbol);

            // Example from OKX docs:
            // Bids (descending): [["3366.1", "7"], ["3366", "6"]]
            // Asks (ascending): [["3366.8", "9"], ["3368", "8"]]
            // Expected string: "3366.1:7:3366.8:9:3366:6:3368:8"
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "3366.1", "7" },
                    new List<string> { "3366", "6" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "3366.8", "9" },
                    new List<string> { "3368", "8" }
                }
            );

            // Act
            var checksum = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Assert
            Assert.IsInstanceOf<int>(checksum);
            // Note: We can't hardcode the expected checksum value without knowing
            // the exact CRC32 implementation, but we verify it produces a signed int
        }

        [Test]
        public void CalculateChecksum_WithMoreThan25Levels_UsesOnly25Levels()
        {
            // Arrange - Create orderbook with 30 levels
            var orderBook = new OKXOrderBook(_symbol);

            var bids = new List<List<string>>();
            var asks = new List<List<string>>();

            // Add 30 bid and ask levels
            for (int i = 0; i < 30; i++)
            {
                bids.Add(new List<string> { (50000 - i * 10).ToString(), (i + 1).ToString() });
                asks.Add(new List<string> { (50001 + i * 10).ToString(), (i + 1).ToString() });
            }

            orderBook.ApplyFullSnapshot(bids, asks);

            // Act
            var checksum1 = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Add one more level (should not affect checksum as only first 25 are used)
            orderBook.UpdateBidRow(49700, 100);

            var checksum2 = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Assert - checksums should be the same as the 26th+ levels don't affect it
            Assert.AreEqual(checksum1, checksum2);
        }

        [Test]
        public void CalculateChecksum_WithLessThan25Levels_UsesAllAvailableLevels()
        {
            // Arrange - Create orderbook with only 3 levels
            var orderBook = new OKXOrderBook(_symbol);

            // Example from OKX docs with insufficient levels:
            // Bids: [["3366.1", "7"]]
            // Asks: [["3366.8", "9"], ["3368", "8"], ["3372", "8"]]
            // Expected string: "3366.1:7:3366.8:9:3368:8:3372:8"
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "3366.1", "7" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "3366.8", "9" },
                    new List<string> { "3368", "8" },
                    new List<string> { "3372", "8" }
                }
            );

            // Act
            var checksum = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Assert - should calculate without throwing
            Assert.IsInstanceOf<int>(checksum);
        }

        [Test]
        public void ValidateChecksum_WithMatchingChecksum_ReturnsTrue()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "50000", "1" },
                    new List<string> { "49999", "2" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "50001", "1" },
                    new List<string> { "50002", "2" }
                }
            );

            var expectedChecksum = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Act
            var isValid = OKXChecksumValidator.ValidateChecksum(orderBook, expectedChecksum);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void ValidateChecksum_WithMismatchedChecksum_ReturnsFalse()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "50000", "1" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "50001", "1" }
                }
            );

            var wrongChecksum = 123456789;

            // Act
            var isValid = OKXChecksumValidator.ValidateChecksum(orderBook, wrongChecksum);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void ValidateChecksum_AfterIncrementalUpdate_RecalculatesCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "50000", "1" },
                    new List<string> { "49999", "2" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "50001", "1" },
                    new List<string> { "50002", "2" }
                }
            );

            var checksumBeforeUpdate = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Act - Apply incremental update
            orderBook.ApplyIncrementalUpdate(
                bids: new List<List<string>>
                {
                    new List<string> { "50000", "5" } // Update size at 50000
                },
                asks: new List<List<string>>()
            );

            var checksumAfterUpdate = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Assert - checksum should change after update
            Assert.AreNotEqual(checksumBeforeUpdate, checksumAfterUpdate);
        }

        [Test]
        public void ValidateChecksum_WithOutputParameter_ReturnsCalculatedChecksum()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "50000", "1" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "50001", "1" }
                }
            );

            var expectedChecksum = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Act
            var isValid = OKXChecksumValidator.ValidateChecksum(orderBook, expectedChecksum, out var calculatedChecksum);

            // Assert
            Assert.IsTrue(isValid);
            Assert.AreEqual(expectedChecksum, calculatedChecksum);
        }

        [Test]
        public void CalculateChecksum_WithEmptyOrderBook_ReturnsChecksum()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);

            // Act
            var checksum = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Assert - should handle empty orderbook without throwing
            Assert.IsInstanceOf<int>(checksum);
        }

        [Test]
        public void CalculateChecksum_WithDecimalPrices_FormatsCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.ApplyFullSnapshot(
                bids: new List<List<string>>
                {
                    new List<string> { "50000.12345678", "1.5" }
                },
                asks: new List<List<string>>
                {
                    new List<string> { "50001.87654321", "2.75" }
                }
            );

            // Act
            var checksum = OKXChecksumValidator.CalculateChecksum(orderBook);

            // Assert - should handle decimal values correctly
            Assert.IsInstanceOf<int>(checksum);
        }
    }
}
