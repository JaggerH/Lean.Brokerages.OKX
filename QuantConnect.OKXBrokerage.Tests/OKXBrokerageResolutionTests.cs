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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;
using Moq;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.OKX.Tests
{
    /// <summary>
    /// Tests for OKX Resolution-based subscriptions
    /// Validates Orderbook subscriptions with simple table output
    /// </summary>
    [TestFixture]
    [Explicit("Requires valid OKX credentials and active market connection")]
    public class OKXBrokerageResolutionTests
    {
        private IDataQueueHandler _brokerage;
        private readonly List<Orderbook> _receivedOrderbooks = new();
        private readonly object _dataLock = new object();

        [SetUp]
        public void Setup()
        {
            _receivedOrderbooks.Clear();
        }

        /// <summary>
        /// Creates and initializes brokerage for Spot or Futures testing
        /// </summary>
        private void SetupBrokerage(string marketType, Symbol symbol = null)
        {
            var apiKey = Config.Get("okx-api-key");
            var apiSecret = Config.Get("okx-api-secret");
            var passphrase = Config.Get("okx-passphrase");

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret) || string.IsNullOrWhiteSpace(passphrase))
            {
                Assert.Ignore("API credentials not configured in config.json");
            }

            symbol ??= marketType == "Futures"
                ? Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX)
                : Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);

            var security = new Security(
                symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash("USDT", 100000, 1m),
                SymbolProperties.GetDefault("USDT"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                {symbol, security}
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());
            var algorithmSettings = new AlgorithmSettings();

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new OKXBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(
                new SecurityPortfolioManager(securities, transactions, algorithmSettings)
            );
            algorithm.Setup(a => a.Securities).Returns(securities);

            var aggregator = new AggregationManager();

            _brokerage = new OKXBrokerage(
                apiKey,
                apiSecret,
                passphrase,
                algorithm.Object,
                aggregator
            );

            Log.Trace($"OKXBrokerageResolutionTests.SetupBrokerage(): Created {marketType} brokerage");
        }

        [TearDown]
        public void TearDown()
        {
            if (_brokerage is IBrokerage brokerage)
            {
                brokerage.Disconnect();
            }
            (_brokerage as IDisposable)?.Dispose();
            Thread.Sleep(1000);
        }

        #region Spot OrderBook Tests

        [Test]
        public void Spot_OrderBook()
        {
            SetupBrokerage("Spot");

            TestOrderBook(
                symbolValue: "BTCUSDT",
                securityType: SecurityType.Crypto,
                maxUpdates: 20,
                maxDurationSeconds: 60,
                marketType: "Spot"
            );
        }

        #endregion

        #region Futures OrderBook Tests

        [Test]
        public void Futures_OrderBook()
        {
            SetupBrokerage("Futures");

            TestOrderBook(
                symbolValue: "BTCUSDT",
                securityType: SecurityType.CryptoFuture,
                maxUpdates: 20,
                maxDurationSeconds: 60,
                marketType: "Futures"
            );
        }

        #endregion

        #region Core Test Logic

        /// <summary>
        /// Core test method for OrderBook updates with simple table output
        /// </summary>
        private void TestOrderBook(
            string symbolValue,
            SecurityType securityType,
            int maxUpdates,
            int maxDurationSeconds,
            string marketType)
        {
            var symbol = Symbol.Create(symbolValue, securityType, Market.OKX);
            var startTime = DateTime.UtcNow;
            var timeoutTime = startTime.AddSeconds(maxDurationSeconds);
            int updateCount = 0;

            Log.Trace("");
            Log.Trace($"===== {marketType} OrderBook Test - {symbolValue} =====");
            Log.Trace($"Max Updates: {maxUpdates}, Max Duration: {maxDurationSeconds}s");
            Log.Trace("");

            // Connect
            ConnectBrokerage();
            Thread.Sleep(1000);

            Assert.IsTrue(_brokerage.IsConnected, "Brokerage should be connected");
            Log.Trace("[OK] Connected");

            // Subscribe
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                symbol,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false, false, false, false,
                tickType: null
            );

            var enumerator = _brokerage.Subscribe(config, (sender, args) => { });
            Assert.IsNotNull(enumerator, "Enumerator should not be null");
            Log.Trace($"[OK] Subscribed to {symbolValue}");

            // Wait for initial orderbook
            var orderBook = WaitForOrderBook(symbol, 30);
            Assert.IsNotNull(orderBook, "OrderBook should be initialized within 30 seconds");
            Log.Trace($"[OK] Initial OrderBook: Bid={orderBook.BestBidPrice:F2}, Ask={orderBook.BestAskPrice:F2}");
            Log.Trace("");

            // Table header
            Log.Trace("+--------+-----------+-------------+-------------+------------+-------+");
            Log.Trace("| Update |   Time    |  Bid Price  |  Ask Price  |   Spread   | Depth |");
            Log.Trace("+--------+-----------+-------------+-------------+------------+-------+");

            // Collect updates
            while (updateCount < maxUpdates && DateTime.UtcNow < timeoutTime)
            {
                if (enumerator.MoveNext() && enumerator.Current is Orderbook ob)
                {
                    updateCount++;
                    lock (_dataLock)
                    {
                        _receivedOrderbooks.Add(ob);
                    }

                    var bidPrice = ob.Bids.Count > 0 ? ob.Bids[0].Price : 0m;
                    var askPrice = ob.Asks.Count > 0 ? ob.Asks[0].Price : 0m;
                    var spread = askPrice - bidPrice;
                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

                    Log.Trace($"| {updateCount,6} | {elapsed,8:F2}s | {bidPrice,11:F2} | {askPrice,11:F2} | {spread,10:F2} | {ob.Levels,5} |");

                    // Basic validation
                    Assert.Greater(bidPrice, 0, "Bid price should be positive");
                    Assert.Greater(askPrice, 0, "Ask price should be positive");
                    Assert.Greater(askPrice, bidPrice, "Ask should be greater than Bid");
                }

                Thread.Sleep(100);
            }

            Log.Trace("+--------+-----------+-------------+-------------+------------+-------+");
            Log.Trace("");

            // Summary
            var totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            Assert.Greater(updateCount, 0, "Should have received at least one update");

            lock (_dataLock)
            {
                var lastOb = _receivedOrderbooks.Last();
                Log.Trace($"===== Summary =====");
                Log.Trace($"Updates: {updateCount}, Duration: {totalElapsed:F2}s, Rate: {updateCount / totalElapsed:F2}/s");
                Log.Trace($"Last: Bid={lastOb.Bids[0].Price:F2}, Ask={lastOb.Asks[0].Price:F2}, Levels={lastOb.Levels}");
            }

            // Cleanup
            _brokerage.Unsubscribe(config);
            Log.Trace("[OK] Test completed");
        }

        #endregion

        #region Helper Methods

        private OKXOrderBook WaitForOrderBook(Symbol symbol, int timeoutSeconds)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var ob = GetBrokerageOrderBook(symbol);
                if (ob != null && ob.BestBidPrice > 0 && ob.BestAskPrice > 0)
                {
                    return ob;
                }
                Thread.Sleep(500);
            }
            return null;
        }

        private OKXOrderBook GetBrokerageOrderBook(Symbol symbol)
        {
            if (_brokerage is OKXBrokerage okx)
            {
                return okx.GetOrderBook(symbol);
            }
            return null;
        }

        private void ConnectBrokerage()
        {
            if (_brokerage is IBrokerage b)
            {
                b.Connect();
            }
        }

        #endregion
    }
}
