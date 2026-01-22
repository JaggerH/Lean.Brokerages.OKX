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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Brokerages.OKX;

namespace QuantConnect.Brokerages.OKX.Tests
{
    /// <summary>
    /// Unit tests for OKXOrderBook class
    /// </summary>
    [TestFixture]
    public class OKXOrderBookUnitTests
    {
        private Symbol _symbol;

        [SetUp]
        public void Setup()
        {
            _symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesOrderBook()
        {
            // Arrange & Act
            var orderBook = new OKXOrderBook(_symbol);

            // Assert
            Assert.IsNotNull(orderBook);
            Assert.AreEqual(_symbol, orderBook.Symbol);
            Assert.AreEqual(0, orderBook.BidCount);
            Assert.AreEqual(0, orderBook.AskCount);
        }

        [Test]
        public void UpdateBidRow_AddsSingleBid_UpdatesCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);

            // Act
            orderBook.UpdateBidRow(50000m, 1.5m);

            // Assert
            Assert.AreEqual(1, orderBook.BidCount);
            Assert.AreEqual(50000m, orderBook.BestBidPrice);
            Assert.AreEqual(1.5m, orderBook.BestBidSize);
        }

        [Test]
        public void UpdateAskRow_AddsSingleAsk_UpdatesCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);

            // Act
            orderBook.UpdateAskRow(50100m, 2.0m);

            // Assert
            Assert.AreEqual(1, orderBook.AskCount);
            Assert.AreEqual(50100m, orderBook.BestAskPrice);
            Assert.AreEqual(2.0m, orderBook.BestAskSize);
        }


        [Test]
        public void RemoveBidRow_RemovesExistingBid_UpdatesCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 1.5m);
            orderBook.UpdateBidRow(49980m, 2.0m);

            // Act
            orderBook.RemoveBidRow(49990m);

            // Assert
            Assert.AreEqual(2, orderBook.BidCount);
            var bids = orderBook.GetBids().ToList();
            Assert.IsFalse(bids.Any(b => b.Key == 49990m), "Removed bid should not exist");
        }

        [Test]
        public void RemoveAskRow_RemovesExistingAsk_UpdatesCorrectly()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateAskRow(51000m, 1.0m);
            orderBook.UpdateAskRow(51010m, 1.5m);
            orderBook.UpdateAskRow(51020m, 2.0m);

            // Act
            orderBook.RemoveAskRow(51010m);

            // Assert
            Assert.AreEqual(2, orderBook.AskCount);
            var asks = orderBook.GetAsks().ToList();
            Assert.IsFalse(asks.Any(a => a.Key == 51010m), "Removed ask should not exist");
        }

        [Test]
        public void GetBids_ReturnsDescendingOrder_BestBidFirst()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 1.5m);
            orderBook.UpdateBidRow(50010m, 0.5m);

            // Act
            var bids = orderBook.GetBids().ToList();

            // Assert
            Assert.AreEqual(3, bids.Count);
            Assert.AreEqual(50010m, bids[0].Key, "Best bid (highest) should be first");
            Assert.AreEqual(50000m, bids[1].Key);
            Assert.AreEqual(49990m, bids[2].Key, "Worst bid (lowest) should be last");
        }

        [Test]
        public void GetAsks_ReturnsAscendingOrder_BestAskFirst()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateAskRow(51000m, 1.0m);
            orderBook.UpdateAskRow(51020m, 1.5m);
            orderBook.UpdateAskRow(50990m, 0.5m);

            // Act
            var asks = orderBook.GetAsks().ToList();

            // Assert
            Assert.AreEqual(3, asks.Count);
            Assert.AreEqual(50990m, asks[0].Key, "Best ask (lowest) should be first");
            Assert.AreEqual(51000m, asks[1].Key);
            Assert.AreEqual(51020m, asks[2].Key, "Worst ask (highest) should be last");
        }

        [Test]
        public void BestBidAskUpdated_Event_FiresWhenBestPricesChange()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            var eventFired = false;
            decimal receivedBestBid = 0;
            decimal receivedBestAsk = 0;

            orderBook.BestBidAskUpdated += (sender, e) =>
            {
                eventFired = true;
                receivedBestBid = e.BestBidPrice;
                receivedBestAsk = e.BestAskPrice;
            };

            // Act - UpdateBidRow fires event automatically when both bid and ask are present
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateAskRow(51000m, 1.5m);

            // Assert
            Assert.IsTrue(eventFired, "BestBidAskUpdated event should fire");
            Assert.AreEqual(50000m, receivedBestBid);
            Assert.AreEqual(51000m, receivedBestAsk);
        }

        [Test]
        public void Clear_RemovesAllLevels()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 1.5m);
            orderBook.UpdateAskRow(51000m, 1.0m);
            orderBook.UpdateAskRow(51010m, 1.5m);

            // Act
            orderBook.Clear();

            // Assert
            Assert.AreEqual(0, orderBook.BidCount);
            Assert.AreEqual(0, orderBook.AskCount);
            Assert.AreEqual(0, orderBook.BestBidPrice);
            Assert.AreEqual(0, orderBook.BestAskPrice);
        }

        [Test]
        public void UpdateBidRow_UpdatesExistingPrice_ReplacesSize()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateBidRow(50000m, 1.0m);

            // Act
            orderBook.UpdateBidRow(50000m, 2.5m); // Update same price with new size

            // Assert
            Assert.AreEqual(1, orderBook.BidCount, "Should still have only 1 bid level");
            Assert.AreEqual(2.5m, orderBook.BestBidSize, "Size should be updated");
        }

        [Test]
        public void UpdateAskRow_UpdatesExistingPrice_ReplacesSize()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateAskRow(51000m, 1.0m);

            // Act
            orderBook.UpdateAskRow(51000m, 3.0m); // Update same price with new size

            // Assert
            Assert.AreEqual(1, orderBook.AskCount, "Should still have only 1 ask level");
            Assert.AreEqual(3.0m, orderBook.BestAskSize, "Size should be updated");
        }

        [Test]
        public void RemoveBidRow_RemovesBestBid_UpdatesBestBidPrice()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateBidRow(50000m, 1.0m);
            orderBook.UpdateBidRow(49990m, 1.5m);
            orderBook.UpdateBidRow(49980m, 2.0m);

            // Act - Remove best bid
            orderBook.RemoveBidRow(50000m);

            // Assert
            Assert.AreEqual(49990m, orderBook.BestBidPrice, "Best bid should update to next highest");
            Assert.AreEqual(2, orderBook.BidCount);
        }

        [Test]
        public void RemoveAskRow_RemovesBestAsk_UpdatesBestAskPrice()
        {
            // Arrange
            var orderBook = new OKXOrderBook(_symbol);
            orderBook.UpdateAskRow(51000m, 1.0m);
            orderBook.UpdateAskRow(51010m, 1.5m);
            orderBook.UpdateAskRow(51020m, 2.0m);

            // Act - Remove best ask
            orderBook.RemoveAskRow(51000m);

            // Assert
            Assert.AreEqual(51010m, orderBook.BestAskPrice, "Best ask should update to next lowest");
            Assert.AreEqual(2, orderBook.AskCount);
        }
    }
}
