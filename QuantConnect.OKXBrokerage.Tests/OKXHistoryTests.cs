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
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Linq;
using System.Threading;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXHistoryTests
    {
        private OKXBrokerage _brokerage;
        private string _apiKey;
        private string _apiSecret;
        private string _passphrase;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load configuration from config.json
            _apiKey = Config.Get("okx-api-key");
            _apiSecret = Config.Get("okx-api-secret");
            _passphrase = Config.Get("okx-passphrase");

            // Skip tests if credentials not configured
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || string.IsNullOrEmpty(_passphrase))
            {
                Assert.Ignore("OKX API credentials not configured in config.json");
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Create OKXBrokerage instance
            _brokerage = new OKXBrokerage(
                _apiKey,
                _apiSecret,
                _passphrase,
                null  // algorithm
            );

            // Small delay between tests to avoid rate limiting
            Thread.Sleep(500);
        }

        [TearDown]
        public void TearDown()
        {
            _brokerage?.Dispose();
        }

        #region TradeBar Tests

        /// <summary>
        /// Tests GetHistory for 1-minute bars over 1 week
        /// </summary>
        [Test]
        public void GetHistory_Minute_BTCUSDT_ReturnsValidData()
        {
            // Arrange
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-7);  // 1 week of data

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(TradeBar),
                symbol,
                Resolution.Minute,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade
            );

            // Act
            var history = _brokerage.GetHistory(request).ToList();

            // Assert
            Assert.IsNotNull(history, "History should not be null");
            Assert.IsNotEmpty(history, "History should contain data");
            Assert.IsTrue(history.Count > 100, $"Should have more than 100 bars for 1 week, got {history.Count}");

            // Verify all items are TradeBars
            Assert.IsTrue(history.All(x => x is TradeBar), "All history items should be TradeBars");

            // Verify data quality
            var tradeBars = history.Cast<TradeBar>().ToList();
            foreach (var bar in tradeBars)
            {
                Assert.AreEqual(symbol, bar.Symbol, "Symbol should match");
                Assert.Greater(bar.Open, 0, "Open price should be positive");
                Assert.Greater(bar.High, 0, "High price should be positive");
                Assert.Greater(bar.Low, 0, "Low price should be positive");
                Assert.Greater(bar.Close, 0, "Close price should be positive");
                Assert.GreaterOrEqual(bar.Volume, 0, "Volume should be non-negative");
                Assert.GreaterOrEqual(bar.High, bar.Low, "High should be >= Low");
                Assert.GreaterOrEqual(bar.High, bar.Open, "High should be >= Open");
                Assert.GreaterOrEqual(bar.High, bar.Close, "High should be >= Close");
                Assert.LessOrEqual(bar.Low, bar.Open, "Low should be <= Open");
                Assert.LessOrEqual(bar.Low, bar.Close, "Low should be <= Close");
            }

            // Verify chronological order
            for (int i = 1; i < tradeBars.Count; i++)
            {
                Assert.GreaterOrEqual(tradeBars[i].Time, tradeBars[i - 1].Time,
                    "Bars should be in chronological order");
            }

            Console.WriteLine($"Fetched {history.Count} minute bars for {symbol}");
            Console.WriteLine($"First bar: {tradeBars[0].Time:yyyy-MM-dd HH:mm:ss} - Close: {tradeBars[0].Close}");
            Console.WriteLine($"Last bar: {tradeBars[tradeBars.Count - 1].Time:yyyy-MM-dd HH:mm:ss} - Close: {tradeBars[tradeBars.Count - 1].Close}");
        }

        /// <summary>
        /// Tests GetHistory for 1-hour bars over 1 month
        /// </summary>
        [Test]
        public void GetHistory_Hour_ETHUSDT_ReturnsValidData()
        {
            // Arrange
            var symbol = Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-30);  // 1 month of data

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(TradeBar),
                symbol,
                Resolution.Hour,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade
            );

            // Act
            var history = _brokerage.GetHistory(request).ToList();

            // Assert
            Assert.IsNotNull(history, "History should not be null");
            Assert.IsNotEmpty(history, "History should contain data");
            Assert.IsTrue(history.Count > 100, $"Should have more than 100 bars for 1 month, got {history.Count}");

            // Verify all items are TradeBars
            var tradeBars = history.Cast<TradeBar>().ToList();
            foreach (var bar in tradeBars)
            {
                Assert.AreEqual(symbol, bar.Symbol, "Symbol should match");
                Assert.Greater(bar.Close, 0, "Close price should be positive");
            }

            Console.WriteLine($"Fetched {history.Count} hour bars for {symbol}");
        }

        /// <summary>
        /// Tests GetHistory for daily bars over 1 year
        /// </summary>
        [Test]
        public void GetHistory_Daily_BTCUSDT_ReturnsValidData()
        {
            // Arrange
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-365);  // 1 year of data

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(TradeBar),
                symbol,
                Resolution.Daily,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade
            );

            // Act
            var history = _brokerage.GetHistory(request).ToList();

            // Assert
            Assert.IsNotNull(history, "History should not be null");
            Assert.IsNotEmpty(history, "History should contain data");
            Assert.IsTrue(history.Count >= 300, $"Should have ~365 bars for 1 year, got {history.Count}");

            // Verify all items are TradeBars
            var tradeBars = history.Cast<TradeBar>().ToList();
            foreach (var bar in tradeBars)
            {
                Assert.AreEqual(symbol, bar.Symbol, "Symbol should match");
                Assert.Greater(bar.Close, 0, "Close price should be positive");
            }

            Console.WriteLine($"Fetched {history.Count} daily bars for {symbol}");
        }

        #endregion

        #region Tick Tests

        /// <summary>
        /// Tests GetHistory for trade ticks (recent data only)
        /// </summary>
        [Test]
        public void GetHistory_TradeTick_BTCUSDT_ReturnsValidData()
        {
            // Arrange
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-10);  // Recent 10 minutes

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(Tick),
                symbol,
                Resolution.Tick,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade
            );

            // Act
            var history = _brokerage.GetHistory(request).ToList();

            // Assert
            Assert.IsNotNull(history, "History should not be null");
            Assert.IsNotEmpty(history, "History should contain data");

            // Verify all items are Ticks
            Assert.IsTrue(history.All(x => x is Tick), "All history items should be Ticks");

            var ticks = history.Cast<Tick>().ToList();
            foreach (var tick in ticks)
            {
                Assert.AreEqual(symbol, tick.Symbol, "Symbol should match");
                Assert.AreEqual(TickType.Trade, tick.TickType, "Should be trade tick");
                Assert.Greater(tick.Value, 0, "Trade price should be positive");
                Assert.Greater(tick.Quantity, 0, "Trade quantity should be positive");
            }

            Console.WriteLine($"Fetched {history.Count} trade ticks for {symbol}");
            if (ticks.Count > 0)
            {
                Console.WriteLine($"First tick: {ticks[0].Time:yyyy-MM-dd HH:mm:ss} - Price: {ticks[0].Value}, Qty: {ticks[0].Quantity}");
                Console.WriteLine($"Last tick: {ticks[ticks.Count - 1].Time:yyyy-MM-dd HH:mm:ss} - Price: {ticks[ticks.Count - 1].Value}, Qty: {ticks[ticks.Count - 1].Quantity}");
            }
        }

        /// <summary>
        /// Tests GetHistory for quote ticks (should return empty as not supported)
        /// </summary>
        [Test]
        public void GetHistory_QuoteTick_ReturnsNull()
        {
            // Arrange
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-10);

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(Tick),
                symbol,
                Resolution.Tick,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Quote
            );

            // Act & Assert
            Assert.IsNull(_brokerage.GetHistory(request), "Quote ticks not supported, should return null");
        }

        #endregion

        #region Pagination Tests

        /// <summary>
        /// Tests GetHistory pagination by requesting more than 100 candles
        /// </summary>
        [Test]
        public void GetHistory_Pagination_Works()
        {
            // Arrange
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-10);  // Request 10 days of hourly data (~240 bars)

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(TradeBar),
                symbol,
                Resolution.Hour,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade
            );

            // Act
            var history = _brokerage.GetHistory(request).ToList();

            // Assert
            Assert.IsNotNull(history, "History should not be null");
            Assert.IsNotEmpty(history, "History should contain data");
            Assert.IsTrue(history.Count > 100, $"Should have more than 100 bars (testing pagination), got {history.Count}");

            // Verify no duplicates by timestamp
            var tradeBars = history.Cast<TradeBar>().ToList();
            var timestamps = tradeBars.Select(x => x.Time).ToList();
            var uniqueTimestamps = timestamps.Distinct().ToList();

            Assert.AreEqual(timestamps.Count, uniqueTimestamps.Count,
                "Should not have duplicate timestamps (pagination issue)");

            Console.WriteLine($"Fetched {history.Count} bars across multiple pages (pagination test passed)");
        }

        #endregion

        #region Error Tests

        /// <summary>
        /// Tests GetHistory with second resolution (should throw NotSupportedException)
        /// </summary>
        [Test]
        public void GetHistory_SecondResolution_ReturnsNull()
        {
            // Arrange
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-10);

            var request = new HistoryRequest(
                startTime,
                endTime,
                typeof(TradeBar),
                symbol,
                Resolution.Second,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                TimeZones.Utc,
                null,
                true,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade
            );

            // Act & Assert
            Assert.IsNull(_brokerage.GetHistory(request), "Second resolution not supported, should return null");
        }

        #endregion

    }
}
