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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.OKX.Tests
{
    /// <summary>
    /// Tests for PriceLimit DTO deserialization and TruncateByPriceLimit logic
    /// </summary>
    [TestFixture]
    public class OKXPriceLimitTests
    {
        private Symbol _symbol;

        [SetUp]
        public void SetUp()
        {
            _symbol = Symbol.Create("LINKUSDT", SecurityType.Crypto, Market.OKX);
        }

        #region PriceLimit DTO Deserialization

        [Test]
        public void Deserialize_EnabledPriceLimit_ParsesCorrectly()
        {
            var json = @"{
                ""instId"": ""LINK-USDT"",
                ""buyLmt"": ""9.267"",
                ""sellLmt"": ""8.123"",
                ""enabled"": true,
                ""ts"": ""1700000000000""
            }";

            var priceLimit = JsonConvert.DeserializeObject<PriceLimit>(json);

            Assert.AreEqual("LINK-USDT", priceLimit.InstrumentId);
            Assert.AreEqual("9.267", priceLimit.BuyLimit);
            Assert.AreEqual("8.123", priceLimit.SellLimit);
            Assert.IsTrue(priceLimit.Enabled);
            Assert.AreEqual("1700000000000", priceLimit.Timestamp);
        }

        [Test]
        public void Deserialize_DisabledPriceLimit_EmptyStrings()
        {
            var json = @"{
                ""instId"": ""BTC-USDT"",
                ""buyLmt"": """",
                ""sellLmt"": """",
                ""enabled"": false,
                ""ts"": ""1700000000000""
            }";

            var priceLimit = JsonConvert.DeserializeObject<PriceLimit>(json);

            Assert.IsFalse(priceLimit.Enabled);
            Assert.AreEqual("", priceLimit.BuyLimit);
            Assert.AreEqual("", priceLimit.SellLimit);
        }

        [Test]
        public void Deserialize_WebSocketDataMessage_ParsesCorrectly()
        {
            // Simulate full WS push: {"arg":{"channel":"price-limit","instId":"LINK-USDT"},"data":[{...}]}
            var json = @"{
                ""arg"": { ""channel"": ""price-limit"", ""instId"": ""LINK-USDT"" },
                ""data"": [{
                    ""instId"": ""LINK-USDT"",
                    ""buyLmt"": ""9.267"",
                    ""sellLmt"": ""8.123"",
                    ""enabled"": true,
                    ""ts"": ""1700000000000""
                }]
            }";

            var message = JsonConvert.DeserializeObject<WebSocketDataMessage<PriceLimit>>(json);

            Assert.AreEqual("price-limit", message.Arg.Channel);
            Assert.AreEqual(1, message.Data.Count);
            Assert.AreEqual("9.267", message.Data[0].BuyLimit);
            Assert.IsTrue(message.Data[0].Enabled);
        }

        #endregion

        #region TruncateByPriceLimit

        [Test]
        public void Truncate_AsksAboveBuyLimit_Removed()
        {
            // LINKUSDT case: best ask 9.31, buyLmt 9.267 → asks above 9.267 are phantom
            var orderbook = CreateOrderbook(
                bids: new[] { (9.25m, 100m), (9.24m, 200m), (9.23m, 300m) },
                asks: new[] { (9.26m, 100m), (9.267m, 50m), (9.31m, 200m), (9.35m, 300m) }
            );
            var priceLimit = new PriceLimit { BuyLimit = "9.267", SellLimit = "8.0", Enabled = true };

            var brokerage = CreateBrokerageWithPriceLimit(_symbol, priceLimit);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            // Only asks <= 9.267 should remain
            Assert.AreEqual(2, orderbook.Asks.Count);
            Assert.AreEqual(9.26m, orderbook.Asks[0].Price);
            Assert.AreEqual(9.267m, orderbook.Asks[1].Price);
        }

        [Test]
        public void Truncate_BidsBelowSellLimit_Removed()
        {
            var orderbook = CreateOrderbook(
                bids: new[] { (9.25m, 100m), (9.20m, 200m), (8.50m, 300m), (7.90m, 400m) },
                asks: new[] { (9.30m, 100m) }
            );
            var priceLimit = new PriceLimit { BuyLimit = "10.0", SellLimit = "9.0", Enabled = true };

            var brokerage = CreateBrokerageWithPriceLimit(_symbol, priceLimit);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            // Only bids >= 9.0 should remain
            Assert.AreEqual(2, orderbook.Bids.Count);
            Assert.AreEqual(9.25m, orderbook.Bids[0].Price);
            Assert.AreEqual(9.20m, orderbook.Bids[1].Price);
        }

        [Test]
        public void Truncate_AllLevelsWithinLimits_NoChange()
        {
            var orderbook = CreateOrderbook(
                bids: new[] { (100m, 1m), (99m, 2m) },
                asks: new[] { (101m, 1m), (102m, 2m) }
            );
            var priceLimit = new PriceLimit { BuyLimit = "105", SellLimit = "95", Enabled = true };

            var brokerage = CreateBrokerageWithPriceLimit(_symbol, priceLimit);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            Assert.AreEqual(2, orderbook.Bids.Count);
            Assert.AreEqual(2, orderbook.Asks.Count);
        }

        [Test]
        public void Truncate_DisabledPriceLimit_NoChange()
        {
            var orderbook = CreateOrderbook(
                bids: new[] { (100m, 1m) },
                asks: new[] { (101m, 1m) }
            );
            var priceLimit = new PriceLimit { BuyLimit = "50", SellLimit = "150", Enabled = false };

            var brokerage = CreateBrokerageWithPriceLimit(_symbol, priceLimit);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            Assert.AreEqual(1, orderbook.Bids.Count);
            Assert.AreEqual(1, orderbook.Asks.Count);
        }

        [Test]
        public void Truncate_NoPriceLimitState_NoChange()
        {
            var orderbook = CreateOrderbook(
                bids: new[] { (100m, 1m) },
                asks: new[] { (101m, 1m) }
            );

            // No price limit set for this symbol
            var brokerage = CreateBrokerageWithPriceLimit(_symbol, null);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            Assert.AreEqual(1, orderbook.Bids.Count);
            Assert.AreEqual(1, orderbook.Asks.Count);
        }

        [Test]
        public void Truncate_AllAsksAboveBuyLimit_EmptyAsks()
        {
            var orderbook = CreateOrderbook(
                bids: new[] { (9.25m, 100m) },
                asks: new[] { (9.31m, 200m), (9.35m, 300m) }
            );
            var priceLimit = new PriceLimit { BuyLimit = "9.267", SellLimit = "8.0", Enabled = true };

            var brokerage = CreateBrokerageWithPriceLimit(_symbol, priceLimit);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            Assert.AreEqual(0, orderbook.Asks.Count);
            Assert.AreEqual(1, orderbook.Bids.Count);
        }

        [Test]
        public void Truncate_BothSidesTruncated()
        {
            // Tight price limits that clip both sides
            var orderbook = CreateOrderbook(
                bids: new[] { (100m, 1m), (99m, 2m), (95m, 3m), (90m, 4m) },
                asks: new[] { (101m, 1m), (102m, 2m), (108m, 3m), (110m, 4m) }
            );
            var priceLimit = new PriceLimit { BuyLimit = "105", SellLimit = "96", Enabled = true };

            var brokerage = CreateBrokerageWithPriceLimit(_symbol, priceLimit);
            brokerage.TruncateByPriceLimit(orderbook, _symbol);

            // Asks: 101, 102, (108 > 105 removed), (110 > 105 removed)
            Assert.AreEqual(2, orderbook.Asks.Count);
            Assert.AreEqual(101m, orderbook.Asks[0].Price);
            Assert.AreEqual(102m, orderbook.Asks[1].Price);

            // Bids: 100, 99, (95 < 96 removed), (90 < 96 removed)
            Assert.AreEqual(2, orderbook.Bids.Count);
            Assert.AreEqual(100m, orderbook.Bids[0].Price);
            Assert.AreEqual(99m, orderbook.Bids[1].Price);
        }

        #endregion

        #region Helpers

        private static Orderbook CreateOrderbook(
            (decimal price, decimal size)[] bids,
            (decimal price, decimal size)[] asks)
        {
            var orderbook = new Orderbook();
            foreach (var (price, size) in bids)
                orderbook.Bids.Add(new OrderbookLevel(price, size));
            foreach (var (price, size) in asks)
                orderbook.Asks.Add(new OrderbookLevel(price, size));
            return orderbook;
        }

        /// <summary>
        /// Creates a minimal OKXBrokerage with a pre-seeded price limit state for testing.
        /// Uses the real synchronizer with a direct state injection.
        /// </summary>
        private static TestableOKXBrokerage CreateBrokerageWithPriceLimit(Symbol symbol, PriceLimit priceLimit)
        {
            var brokerage = new TestableOKXBrokerage();
            brokerage.CreatePriceLimitSynchronizer();

            if (priceLimit != null)
            {
                // Get/create the per-symbol synchronizer, then seed state
                var sync = brokerage.PriceLimitSync.GetSynchronizer(symbol);
                sync.SetStateSilent(priceLimit);
            }

            return brokerage;
        }

        /// <summary>
        /// Minimal concrete subclass of OKXBaseBrokerage for unit testing.
        /// Exposes protected members without requiring API credentials or WebSocket connections.
        /// </summary>
        private class TestableOKXBrokerage : OKXBaseBrokerage
        {
            public BrokerageMultiStateSynchronizer<Symbol, PriceLimit, PriceLimit> PriceLimitSync => _priceLimitSync;
            public new void CreatePriceLimitSynchronizer() => base.CreatePriceLimitSynchronizer();
            protected override void SubscribePrivateChannels() { }
            protected override void SendAuthenticationRequest() { }
        }

        #endregion
    }
}
