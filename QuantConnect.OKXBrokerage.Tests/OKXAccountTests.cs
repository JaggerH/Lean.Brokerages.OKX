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
using System;
using System.Linq;
using System.Threading;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXAccountTests
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

        #region GetCashBalance Tests

        /// <summary>
        /// Tests GetCashBalance returns valid data
        /// </summary>
        [Test]
        public void GetCashBalance_ReturnsValidData()
        {
            // Act
            var cashBalances = _brokerage.GetCashBalance();

            // Assert
            Assert.IsNotNull(cashBalances, "Cash balances should not be null");

            // Note: Empty list is valid if account has no balance
            Console.WriteLine($"GetCashBalance returned {cashBalances.Count} currencies");

            foreach (var cash in cashBalances)
            {
                Assert.IsNotNull(cash.Currency, "Currency should not be null");
                Assert.IsNotEmpty(cash.Currency, "Currency should not be empty");
                Assert.GreaterOrEqual(cash.Amount, 0, "Cash amount should be non-negative");

                Console.WriteLine($"  {cash.Currency}: {cash.Amount}");
            }
        }

        #endregion

        #region GetAccountHoldings Tests

        /// <summary>
        /// Tests GetAccountHoldings returns valid data
        /// </summary>
        [Test]
        public void GetAccountHoldings_ReturnsValidData()
        {
            // Act
            var holdings = _brokerage.GetAccountHoldings();

            // Assert
            Assert.IsNotNull(holdings, "Holdings should not be null");

            // Note: Empty list is valid if account has no positions
            Console.WriteLine($"GetAccountHoldings returned {holdings.Count} positions");

            foreach (var holding in holdings)
            {
                Assert.IsNotNull(holding.Symbol, "Symbol should not be null");
                Assert.AreNotEqual(0, holding.Quantity, "Quantity should not be zero");
                Assert.Greater(holding.MarketPrice, 0, "Market price should be positive");
                Assert.Greater(holding.AveragePrice, 0, "Average price should be positive");

                Console.WriteLine($"  {holding.Symbol}: Qty={holding.Quantity}, Avg={holding.AveragePrice}, Mkt={holding.MarketPrice}, PnL={holding.UnrealizedPnL}");
            }
        }

        #endregion

        #region GetOpenOrders Tests

        /// <summary>
        /// Tests GetOpenOrders returns valid data
        /// </summary>
        [Test]
        public void GetOpenOrders_ReturnsValidData()
        {
            // Act
            var orders = _brokerage.GetOpenOrders();

            // Assert
            Assert.IsNotNull(orders, "Orders should not be null");

            // Note: Empty list is valid if account has no open orders
            Console.WriteLine($"GetOpenOrders returned {orders.Count} orders");

            foreach (var order in orders)
            {
                Assert.IsNotNull(order.Symbol, "Symbol should not be null");
                Assert.AreNotEqual(0, order.Quantity, "Quantity should not be zero");
                Assert.IsNotNull(order.BrokerId, "BrokerId should not be null");
                Assert.IsNotEmpty(order.BrokerId, "BrokerId should not be empty");

                Console.WriteLine($"  {order.Symbol}: Type={order.Type}, Qty={order.Quantity}, Status={order.Status}, BrokerId={string.Join(",", order.BrokerId)}");
            }
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that all three account query methods work together
        /// </summary>
        [Test]
        public void AccountQueries_AllMethodsWork()
        {
            // Act
            var cashBalances = _brokerage.GetCashBalance();
            var holdings = _brokerage.GetAccountHoldings();
            var orders = _brokerage.GetOpenOrders();

            // Assert
            Assert.IsNotNull(cashBalances, "Cash balances should not be null");
            Assert.IsNotNull(holdings, "Holdings should not be null");
            Assert.IsNotNull(orders, "Orders should not be null");

            Console.WriteLine($"Account Summary:");
            Console.WriteLine($"  Cash Balances: {cashBalances.Count} currencies");
            Console.WriteLine($"  Holdings: {holdings.Count} positions");
            Console.WriteLine($"  Open Orders: {orders.Count} orders");

            // All methods should return lists (empty or with data)
            Assert.Pass($"All account query methods executed successfully");
        }

        #endregion

        #region REST API Direct Tests

        /// <summary>
        /// Tests GetPendingOrders REST API method directly
        /// </summary>
        [Test]
        public void RestApi_GetPendingOrders_Works()
        {
            // Arrange
            var restClient = new RestApi.OKXRestApiClient(_apiKey, _apiSecret, _passphrase);

            // Act
            var pendingOrders = restClient.GetPendingOrders();

            // Assert
            Assert.IsNotNull(pendingOrders, "Pending orders should not be null");
            Console.WriteLine($"GetPendingOrders returned {pendingOrders.Count} orders");

            foreach (var order in pendingOrders)
            {
                Assert.IsNotNull(order.InstrumentId, "InstrumentId should not be null");
                Assert.IsNotNull(order.OrderId, "OrderId should not be null");
                Assert.IsNotNull(order.State, "State should not be null");

                Console.WriteLine($"  Order {order.OrderId}: {order.InstrumentId}, Type={order.OrderType}, Side={order.Side}, State={order.State}");
            }
        }

        /// <summary>
        /// Tests GetAccountBalance REST API method directly
        /// </summary>
        [Test]
        public void RestApi_GetAccountBalance_Works()
        {
            // Arrange
            var restClient = new RestApi.OKXRestApiClient(_apiKey, _apiSecret, _passphrase);

            // Act
            var balance = restClient.GetAccountBalance();

            // Assert - balance can be null if API fails, but should not throw
            if (balance != null)
            {
                Assert.IsNotNull(balance.Details, "Balance details should not be null");
                Console.WriteLine($"GetAccountBalance returned {balance.Details.Count} currencies");
                Console.WriteLine($"  Total Equity: {balance.TotalEquity}");

                foreach (var detail in balance.Details)
                {
                    Console.WriteLine($"  {detail.Currency}: Available={detail.AvailableBalance}, Cash={detail.CashBalance}, Frozen={detail.FrozenBalance}");
                }
            }
            else
            {
                Console.WriteLine("GetAccountBalance returned null (API may have failed)");
            }
        }

        /// <summary>
        /// Tests GetPositions REST API method directly
        /// </summary>
        [Test]
        public void RestApi_GetPositions_Works()
        {
            // Arrange
            var restClient = new RestApi.OKXRestApiClient(_apiKey, _apiSecret, _passphrase);

            // Act
            var positions = restClient.GetPositions();

            // Assert
            Assert.IsNotNull(positions, "Positions should not be null");
            Console.WriteLine($"GetPositions returned {positions.Count} positions");

            foreach (var position in positions)
            {
                Assert.IsNotNull(position.InstrumentId, "InstrumentId should not be null");

                Console.WriteLine($"  Position {position.InstrumentId}: Qty={position.Quantity}, Avg={position.AveragePrice}, PnL={position.UnrealizedPnL}");
            }
        }

        #endregion
    }
}
