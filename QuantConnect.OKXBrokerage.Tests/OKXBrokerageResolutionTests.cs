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
    /// Tests for OKX Resolution-based subscriptions.
    /// Validates different Resolution types (Tick, Orderbook) use appropriate channels:
    /// - Resolution.Tick with TickType.Quote uses tickers channel (lightweight)
    /// - Resolution.Tick with TickType.Trade uses trades channel
    /// - Resolution.Tick with typeof(Orderbook) uses order_book channel (full depth)
    /// </summary>
    [TestFixture]
    [Explicit("Requires valid OKX credentials and active market connection")]
    public class OKXBrokerageResolutionTests
    {
        private IDataQueueHandler _brokerage;
        private readonly List<Orderbook> _receivedOrderbooks = new();
        private readonly List<Tick> _receivedTicks = new();
        private readonly object _dataLock = new object();

        [SetUp]
        public void Setup()
        {
            _receivedOrderbooks.Clear();
            _receivedTicks.Clear();
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

        #region Spot Tests

        [Test]
        public void Spot_OrderBook()
        {
            SetupBrokerage("Spot");

            TestOrderBook(
                symbolValue: "SHIBUSDT",
                securityType: SecurityType.Crypto,
                maxUpdates: 50,
                maxDurationSeconds: 90,
                marketType: "Spot"
            );
        }

        [Test]
        public void Spot_Tick()
        {
            SetupBrokerage("Spot");
            TestTickerChannelSubscription("BTCUSDT", SecurityType.Crypto, expectedQuoteTicks: 10, maxDurationSeconds: 60, "Spot");
        }

        [Test]
        public void Spot_Tick_Trade()
        {
            SetupBrokerage("Spot");
            TestTradeChannelSubscription("BTCUSDT", SecurityType.Crypto, expectedTradeTicks: 10, maxDurationSeconds: 60, "Spot");
        }

        #endregion

        #region Futures Tests

        [Test]
        public void Futures_OrderBook()
        {
            SetupBrokerage("Futures");

            TestOrderBook(
                symbolValue: "SHIBUSDT",
                securityType: SecurityType.CryptoFuture,
                maxUpdates: 50,
                maxDurationSeconds: 120,
                marketType: "Futures"
            );
        }

        [Test]
        public void Futures_Tick()
        {
            SetupBrokerage("Futures");
            TestTickerChannelSubscription("BTCUSDT", SecurityType.CryptoFuture, expectedQuoteTicks: 10, maxDurationSeconds: 60, "Futures");
        }

        [Test]
        public void Futures_Tick_Trade()
        {
            SetupBrokerage("Futures");
            TestTradeChannelSubscription("BTCUSDT", SecurityType.CryptoFuture, expectedTradeTicks: 10, maxDurationSeconds: 60, "Futures");
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
            Log.Trace($"[OK] Initial OrderBook: Bid={FmtPrice(orderBook.BestBidPrice)}, Ask={FmtPrice(orderBook.BestAskPrice)}");
            Log.Trace("");

            // Table header
            Log.Trace("+--------+-----------+----------------+----------------+----------------+----------------+-------+");
            Log.Trace("| Update |   Time    |   Bid Price    |   Bid Size     |   Ask Price    |   Ask Size     | Depth |");
            Log.Trace("+--------+-----------+----------------+----------------+----------------+----------------+-------+");

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
                    var bidSize = ob.Bids.Count > 0 ? ob.Bids[0].Size : 0m;
                    var askPrice = ob.Asks.Count > 0 ? ob.Asks[0].Price : 0m;
                    var askSize = ob.Asks.Count > 0 ? ob.Asks[0].Size : 0m;
                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

                    Log.Trace($"| {updateCount,6} | {elapsed,8:F2}s | {FmtPrice(bidPrice),14} | {FmtPrice(bidSize),14} | {FmtPrice(askPrice),14} | {FmtPrice(askSize),14} | {ob.Levels,5} |");

                    // Basic validation
                    Assert.Greater(bidPrice, 0, "Bid price should be positive");
                    Assert.Greater(askPrice, 0, "Ask price should be positive");
                    Assert.Greater(askPrice, bidPrice, "Ask should be greater than Bid");
                }

                Thread.Sleep(100);
            }

            Log.Trace("+--------+-----------+----------------+----------------+----------------+----------------+-------+");
            Log.Trace("");

            // Summary
            var totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            Assert.Greater(updateCount, 0, "Should have received at least one update");

            lock (_dataLock)
            {
                var lastOb = _receivedOrderbooks.Last();
                Log.Trace($"===== Summary =====");
                Log.Trace($"Updates: {updateCount}, Duration: {totalElapsed:F2}s, Rate: {updateCount / totalElapsed:F2}/s");
                Log.Trace($"Last: Bid={FmtPrice(lastOb.Bids[0].Price)} x {FmtPrice(lastOb.Bids[0].Size)}, Ask={FmtPrice(lastOb.Asks[0].Price)} x {FmtPrice(lastOb.Asks[0].Size)}, Levels={lastOb.Levels}");
            }

            // Cleanup
            _brokerage.Unsubscribe(config);
            Log.Trace("[OK] Test completed");
        }

        /// <summary>
        /// Tests that Resolution.Tick subscriptions use tickers channel for Quote data.
        /// </summary>
        private void TestTickerChannelSubscription(
            string symbolValue,
            SecurityType securityType,
            int expectedQuoteTicks,
            int maxDurationSeconds,
            string marketType)
        {
            var symbol = Symbol.Create(symbolValue, securityType, Market.OKX);
            var startTime = DateTime.UtcNow;
            var timeoutTime = startTime.AddSeconds(maxDurationSeconds);
            var quoteTicksReceived = new List<Tick>();

            LogTestHeader($"{marketType} Ticker Channel Test", symbolValue, expectedQuoteTicks, maxDurationSeconds);

            ConnectBrokerage();
            Thread.Sleep(1000);
            Assert.IsTrue(_brokerage.IsConnected, "Brokerage should be connected");
            Log.Trace("[OK] Connected");

            var tickConfig = CreateQuoteTickConfig(symbol);
            var enumerator = _brokerage.Subscribe(tickConfig, (_, _) => { });
            Assert.IsNotNull(enumerator, "Enumerator should not be null");

            Log.Trace($"[INFO] Subscribed to {symbolValue} with Resolution.Tick + TickType.Quote");
            Log.Trace("+---------+-----------+----------------+----------------+---------------+----------------+");
            Log.Trace("|  Tick # |   Time    |   Bid Price    |   Ask Price    |    Spread     |     Value      |");
            Log.Trace("+---------+-----------+----------------+----------------+---------------+----------------+");

            while (DateTime.UtcNow < timeoutTime && quoteTicksReceived.Count < expectedQuoteTicks)
            {
                if (!enumerator.MoveNext())
                {
                    Thread.Sleep(100);
                    continue;
                }

                var tick = enumerator.Current as Tick;
                if (tick == null || tick.TickType != TickType.Quote)
                {
                    continue;
                }

                quoteTicksReceived.Add(tick);
                var spread = tick.AskPrice - tick.BidPrice;
                var elapsed = DateTime.UtcNow - startTime;

                Log.Trace($"| {quoteTicksReceived.Count,7} | {elapsed.TotalSeconds,8:F2}s | {FmtPrice(tick.BidPrice),14} | {FmtPrice(tick.AskPrice),14} | {FmtPrice(spread),13} | {FmtPrice(tick.Value),14} |");

                Assert.Greater(tick.BidPrice, 0, "Bid price should be positive");
                Assert.Greater(tick.AskPrice, 0, "Ask price should be positive");
                Assert.AreEqual((tick.BidPrice + tick.AskPrice) / 2, tick.Value, "Tick value should be mid-price");

                Thread.Sleep(100);
            }

            Log.Trace("+---------+-----------+----------------+----------------+---------------+----------------+");

            var totalElapsed = DateTime.UtcNow - startTime;
            Assert.Greater(quoteTicksReceived.Count, 0, "Should have received quote ticks from tickers channel");

            LogTickerSummary(symbolValue, marketType, quoteTicksReceived, totalElapsed);

            _brokerage.Unsubscribe(tickConfig);
            Log.Trace("[OK] Test completed");
        }

        /// <summary>
        /// Tests that Resolution.Tick subscriptions use trades channel for Trade data.
        /// </summary>
        private void TestTradeChannelSubscription(
            string symbolValue,
            SecurityType securityType,
            int expectedTradeTicks,
            int maxDurationSeconds,
            string marketType)
        {
            var symbol = Symbol.Create(symbolValue, securityType, Market.OKX);
            var startTime = DateTime.UtcNow;
            var timeoutTime = startTime.AddSeconds(maxDurationSeconds);
            var tradeTicksReceived = new List<Tick>();

            LogTestHeader($"{marketType} Trade Channel Test", symbolValue, expectedTradeTicks, maxDurationSeconds);

            ConnectBrokerage();
            Thread.Sleep(1000);
            Assert.IsTrue(_brokerage.IsConnected, "Brokerage should be connected");
            Log.Trace("[OK] Connected");

            var tickConfig = CreateTradeTickConfig(symbol);
            var enumerator = _brokerage.Subscribe(tickConfig, (_, _) => { });
            Assert.IsNotNull(enumerator, "Enumerator should not be null");

            Log.Trace($"[INFO] Subscribed to {symbolValue} with Resolution.Tick + TickType.Trade");
            Log.Trace("+---------+-----------+----------------+----------------+----------------+");
            Log.Trace("|  Tick # |   Time    |     Price      |   Quantity     |     Value      |");
            Log.Trace("+---------+-----------+----------------+----------------+----------------+");

            while (DateTime.UtcNow < timeoutTime && tradeTicksReceived.Count < expectedTradeTicks)
            {
                if (!enumerator.MoveNext())
                {
                    Thread.Sleep(100);
                    continue;
                }

                var tick = enumerator.Current as Tick;
                if (tick == null || tick.TickType != TickType.Trade)
                {
                    continue;
                }

                tradeTicksReceived.Add(tick);
                var elapsed = DateTime.UtcNow - startTime;

                Log.Trace($"| {tradeTicksReceived.Count,7} | {elapsed.TotalSeconds,8:F2}s | {FmtPrice(tick.Value),14} | {FmtPrice(tick.Quantity),14} | {FmtPrice(tick.Value),14} |");

                // Validate Trade tick fields
                Assert.Greater(tick.Value, 0, "Price should be positive");
                Assert.Greater(tick.Quantity, 0, "Quantity should be positive");
                Assert.AreEqual(TickType.Trade, tick.TickType, "TickType should be Trade");
                Assert.AreEqual(symbol, tick.Symbol, "Symbol should match");

                Thread.Sleep(100);
            }

            Log.Trace("+---------+-----------+----------------+----------------+----------------+");

            var totalElapsed = DateTime.UtcNow - startTime;
            Assert.Greater(tradeTicksReceived.Count, 0, "Should have received trade ticks from trades channel");

            LogTradeSummary(symbolValue, marketType, tradeTicksReceived, totalElapsed);

            _brokerage.Unsubscribe(tickConfig);
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

        private static void LogTestHeader(string testName, string symbolValue, int maxItems, int maxDurationSeconds)
        {
            Log.Trace("");
            Log.Trace("+========================================================================+");
            Log.Trace($"|  {testName} - {symbolValue,-25} |");
            Log.Trace("+========================================================================+");
            Log.Trace($"  Max Items: {maxItems}, Max Duration: {maxDurationSeconds}s");
            Log.Trace("");
        }

        private static SubscriptionDataConfig CreateQuoteTickConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(
                typeof(Tick),
                symbol,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false, false, false, false,
                TickType.Quote
            );
        }

        private static SubscriptionDataConfig CreateTradeTickConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(
                typeof(Tick),
                symbol,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false, false, false, false,
                TickType.Trade
            );
        }

        private static void LogTickerSummary(string symbolValue, string marketType, List<Tick> ticks, TimeSpan totalElapsed)
        {
            var firstTick = ticks.First();
            var lastTick = ticks.Last();
            var avgSpread = ticks.Average(t => t.AskPrice - t.BidPrice);
            var tickRate = ticks.Count / totalElapsed.TotalSeconds;

            Log.Trace("+========================================================================+");
            Log.Trace("|                        TICKER TEST SUMMARY                             |");
            Log.Trace("+========================================================================+");
            Log.Trace($"|  Market: {marketType}, Symbol: {symbolValue}");
            Log.Trace($"|  Ticks: {ticks.Count}, Duration: {totalElapsed.TotalSeconds:F2}s, Rate: {tickRate:F2}/s");
            Log.Trace($"|  First: {FmtPrice(firstTick.BidPrice)} / {FmtPrice(firstTick.AskPrice)}, Last: {FmtPrice(lastTick.BidPrice)} / {FmtPrice(lastTick.AskPrice)}");
            Log.Trace($"|  Avg Spread: {FmtPrice(avgSpread)}");
            Log.Trace("+========================================================================+");
        }

        private static void LogTradeSummary(string symbolValue, string marketType, List<Tick> ticks, TimeSpan totalElapsed)
        {
            var firstTick = ticks.First();
            var lastTick = ticks.Last();
            var avgQuantity = ticks.Average(t => t.Quantity);
            var totalVolume = ticks.Sum(t => t.Quantity);
            var tickRate = ticks.Count / totalElapsed.TotalSeconds;

            Log.Trace("+========================================================================+");
            Log.Trace("|                        TRADE TEST SUMMARY                              |");
            Log.Trace("+========================================================================+");
            Log.Trace($"|  Market: {marketType}, Symbol: {symbolValue}");
            Log.Trace($"|  Ticks: {ticks.Count}, Duration: {totalElapsed.TotalSeconds:F2}s, Rate: {tickRate:F2}/s");
            Log.Trace($"|  First Price: {FmtPrice(firstTick.Value)}, Last Price: {FmtPrice(lastTick.Value)}");
            Log.Trace($"|  Avg Quantity: {FmtPrice(avgQuantity)}, Total Volume: {FmtPrice(totalVolume)}");
            Log.Trace("+========================================================================+");
        }

        /// <summary>
        /// Formats a decimal with appropriate decimal places based on magnitude.
        /// Small values (like SHIB ~0.00000606) get more decimals; large values (like BTC) get fewer.
        /// </summary>
        private static string FmtPrice(decimal value)
        {
            if (value == 0m) return "0";
            var abs = Math.Abs(value);
            if (abs >= 1m) return value.ToString("F2");
            if (abs >= 0.01m) return value.ToString("F4");
            if (abs >= 0.0001m) return value.ToString("F6");
            if (abs >= 0.000001m) return value.ToString("F8");
            return value.ToString("F10");
        }

        #endregion
    }
}
