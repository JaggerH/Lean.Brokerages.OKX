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
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using System;
using System.Linq;
using System.Threading;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXRestApiIntegrationTests
    {
        private OKXRestApiClient _client;
        private string _apiKey;
        private string _apiSecret;
        private string _passphrase;
        private string _restApiUrl;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load configuration from config.json
            _apiKey = Config.Get("okx-api-key");
            _apiSecret = Config.Get("okx-api-secret");
            _passphrase = Config.Get("okx-passphrase");
            var environment = Config.Get("okx-environment", "testnet");

            // Set API URL based on environment
            _restApiUrl = environment == "production"
                ? "https://www.okx.com"
                : "https://www.okx.com"; // OKX doesn't have separate testnet URL for REST

            // Skip tests if credentials not configured
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || string.IsNullOrEmpty(_passphrase))
            {
                Assert.Ignore("OKX API credentials not configured in config.json");
            }
        }

        [SetUp]
        public void SetUp()
        {
            _client = new OKXRestApiClient(_apiKey, _apiSecret, _passphrase, _restApiUrl);

            // Small delay between tests to avoid rate limiting
            Thread.Sleep(500);
        }

        #region Public Endpoint Tests (No Authentication Required)

        /// <summary>
        /// Tests GetServerTime endpoint
        /// </summary>
        [Test]
        public void GetServerTime_ReturnsValidTimestamp()
        {
            var serverTime = _client.GetServerTime();

            Assert.IsNotNull(serverTime, "Server time should not be null");
            Assert.Greater(serverTime.Value, 0, "Server time should be positive");

            // Verify it's a recent timestamp (within last hour and next hour)
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var oneHour = 60 * 60 * 1000;

            Assert.Greater(serverTime.Value, now - oneHour, "Server time should be recent");
            Assert.Less(serverTime.Value, now + oneHour, "Server time should not be in future");

            Console.WriteLine($"Server time: {serverTime.Value} ms ({DateTimeOffset.FromUnixTimeMilliseconds(serverTime.Value):yyyy-MM-dd HH:mm:ss} UTC)");
        }

        /// <summary>
        /// Tests GetInstruments endpoint for SPOT instruments
        /// </summary>
        [Test]
        public void GetInstruments_Spot_ReturnsInstruments()
        {
            var instruments = _client.GetInstruments("SPOT");

            Assert.IsNotNull(instruments, "Instruments should not be null");
            Assert.IsNotEmpty(instruments, "Should return at least one instrument");

            // Find BTC-USDT
            var btcUsdt = instruments.FirstOrDefault(i => i.InstrumentId == "BTC-USDT");
            Assert.IsNotNull(btcUsdt, "Should contain BTC-USDT");

            // Verify instrument properties
            Assert.AreEqual("SPOT", btcUsdt.InstrumentType);
            Assert.AreEqual("BTC", btcUsdt.BaseCurrency);
            Assert.AreEqual("USDT", btcUsdt.QuoteCurrency);
            Assert.IsNotEmpty(btcUsdt.MinSize, "MinSize should not be empty");
            Assert.IsNotEmpty(btcUsdt.TickSize, "TickSize should not be empty");

            Console.WriteLine($"Found {instruments.Count} SPOT instruments");
            Console.WriteLine($"BTC-USDT: MinSize={btcUsdt.MinSize}, TickSize={btcUsdt.TickSize}, State={btcUsdt.State}");
        }

        /// <summary>
        /// Tests GetInstruments endpoint for SWAP instruments
        /// </summary>
        [Test]
        public void GetInstruments_Swap_ReturnsInstruments()
        {
            var instruments = _client.GetInstruments("SWAP");

            Assert.IsNotNull(instruments, "Instruments should not be null");
            Assert.IsNotEmpty(instruments, "Should return at least one instrument");

            // Find BTC-USDT-SWAP
            var btcUsdtSwap = instruments.FirstOrDefault(i => i.InstrumentId == "BTC-USDT-SWAP");
            Assert.IsNotNull(btcUsdtSwap, "Should contain BTC-USDT-SWAP");

            // Verify instrument properties
            Assert.AreEqual("SWAP", btcUsdtSwap.InstrumentType);
            Assert.AreEqual("BTC", btcUsdtSwap.BaseCurrency);
            Assert.IsNotEmpty(btcUsdtSwap.ContractValue, "ContractValue should not be empty");

            Console.WriteLine($"Found {instruments.Count} SWAP instruments");
            Console.WriteLine($"BTC-USDT-SWAP: ContractValue={btcUsdtSwap.ContractValue}, State={btcUsdtSwap.State}");
        }

        /// <summary>
        /// Tests GetTickerInfo endpoint
        /// </summary>
        [Test]
        public void GetTickerInfo_BtcUsdt_ReturnsTicker()
        {
            var ticker = _client.GetTickerInfo("BTC-USDT");

            Assert.IsNotNull(ticker, "Ticker should not be null");
            Assert.AreEqual("BTC-USDT", ticker.CurrencyPair);

            // Verify ticker has valid data
            Assert.IsNotNull(ticker.Last, "Last price should not be null");
            Assert.Greater(ticker.HighestBid, 0, "Bid price should be positive");
            Assert.Greater(ticker.LowestAsk, 0, "Ask price should be positive");
            Assert.Less(ticker.HighestBid, ticker.LowestAsk, "Bid should be less than Ask");

            Console.WriteLine($"BTC-USDT Ticker: Last={ticker.Last}, Bid={ticker.HighestBid}, Ask={ticker.LowestAsk}");
        }

        #endregion

        #region Private Endpoint Tests (Require Authentication)

        /// <summary>
        /// Tests GetAccountConfiguration endpoint
        /// </summary>
        [Test]
        public void GetAccountConfiguration_ReturnsConfig()
        {
            var config = _client.GetAccountConfiguration();

            Assert.IsNotNull(config, "Account config should not be null");
            Assert.IsNotEmpty(config.AccountLevel, "Account level should not be empty");
            Assert.IsNotEmpty(config.PositionMode, "Position mode should not be empty");

            // Account level: 1(Simple), 2(Single-currency margin), 3(Multi-currency margin), 4(Portfolio margin)
            Assert.That(config.AccountLevel, Is.EqualTo("1").Or.EqualTo("2").Or.EqualTo("3").Or.EqualTo("4"),
                "Account level should be 1, 2, 3, or 4");

            Console.WriteLine($"Account Level: {config.AccountLevel}, Position Mode: {config.PositionMode}");
        }

        /// <summary>
        /// Tests GetAccountBalance endpoint
        /// </summary>
        [Test]
        public void GetAccountBalance_ReturnsBalance()
        {
            var balance = _client.GetAccountBalance();

            Assert.IsNotNull(balance, "Balance should not be null");

            // Verify balance has details
            if (balance.Details != null && balance.Details.Count > 0)
            {
                var usdtBalance = balance.Details.FirstOrDefault(d => d.Currency == "USDT");

                if (usdtBalance != null)
                {
                    Console.WriteLine($"USDT Balance: Equity={usdtBalance.Equity}, Available={usdtBalance.AvailableBalance}");

                    // Verify numeric fields are parseable
                    Assert.DoesNotThrow(() => decimal.Parse(usdtBalance.Equity ?? "0"));
                    Assert.DoesNotThrow(() => decimal.Parse(usdtBalance.AvailableBalance ?? "0"));
                }
                else
                {
                    Console.WriteLine($"No USDT balance found. Available currencies: {string.Join(", ", balance.Details.Select(d => d.Currency))}");
                }
            }
            else
            {
                Console.WriteLine("Account has no balance details");
            }
        }

        /// <summary>
        /// Tests GetAccountBalance with specific currency
        /// </summary>
        [Test]
        public void GetAccountBalance_WithCurrency_ReturnsFilteredBalance()
        {
            var balance = _client.GetAccountBalance("USDT");

            Assert.IsNotNull(balance, "Balance should not be null");

            if (balance.Details != null && balance.Details.Count > 0)
            {
                // Should only return USDT or be empty if no USDT balance
                Assert.That(balance.Details.All(d => d.Currency == "USDT"),
                    "All details should be for USDT currency");

                Console.WriteLine($"USDT-specific balance query returned {balance.Details.Count} detail(s)");
            }
        }

        /// <summary>
        /// Tests GetAccountPositions endpoint
        /// </summary>
        [Test]
        public void GetAccountPositions_ReturnsPositions()
        {
            var positions = _client.GetAccountPositions();

            Assert.IsNotNull(positions, "Positions should not be null");

            if (positions.Count > 0)
            {
                var firstPosition = positions[0];

                Console.WriteLine($"Found {positions.Count} position(s)");
                Console.WriteLine($"First position: {firstPosition.InstrumentId}, Qty={firstPosition.Quantity}, AvgPx={firstPosition.AveragePrice}");

                // Verify position has required fields
                Assert.IsNotEmpty(firstPosition.InstrumentType, "InstrumentType should not be empty");
                Assert.IsNotEmpty(firstPosition.InstrumentId, "InstrumentId should not be empty");
            }
            else
            {
                Console.WriteLine("Account has no open positions");
            }
        }

        /// <summary>
        /// Tests GetAccountPositions with instrument filter
        /// </summary>
        [Test]
        public void GetAccountPositions_WithInstrumentType_ReturnsFilteredPositions()
        {
            var positions = _client.GetAccountPositions("SWAP");

            Assert.IsNotNull(positions, "Positions should not be null");

            if (positions.Count > 0)
            {
                // Verify all positions are SWAP type
                Assert.That(positions.All(p => p.InstrumentType == "SWAP"),
                    "All positions should be SWAP type");

                Console.WriteLine($"Found {positions.Count} SWAP position(s)");
            }
            else
            {
                Console.WriteLine("Account has no SWAP positions");
            }
        }

        #endregion

        #region Rate Limiting Tests

        /// <summary>
        /// Tests that multiple rapid requests don't trigger rate limiting errors
        /// </summary>
        [Test]
        public void RateLimiting_MultipleRequests_NoErrors()
        {
            var successCount = 0;
            var errorCount = 0;

            // Make 10 public API requests rapidly
            for (int i = 0; i < 10; i++)
            {
                var serverTime = _client.GetServerTime();

                if (serverTime.HasValue)
                    successCount++;
                else
                    errorCount++;

                // Small delay to avoid overwhelming the rate limiter
                Thread.Sleep(100);
            }

            Assert.AreEqual(10, successCount, "All requests should succeed");
            Assert.AreEqual(0, errorCount, "No requests should fail");

            Console.WriteLine($"Successfully made 10 rapid requests with rate limiting");
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests error handling for invalid instrument ID
        /// </summary>
        [Test]
        public void GetTickerInfo_InvalidInstrument_ReturnsNull()
        {
            var ticker = _client.GetTickerInfo("INVALID-PAIR");

            Assert.IsNull(ticker, "Should return null for invalid instrument");
        }

        /// <summary>
        /// Tests error handling for invalid instrument type
        /// </summary>
        [Test]
        public void GetInstruments_InvalidType_ReturnsEmpty()
        {
            var instruments = _client.GetInstruments("INVALID_TYPE");

            Assert.IsNotNull(instruments, "Should return empty list, not null");
            // OKX API may return empty list or error
        }

        #endregion
    }
}
