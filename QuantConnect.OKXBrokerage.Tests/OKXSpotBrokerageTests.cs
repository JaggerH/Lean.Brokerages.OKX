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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Configuration;
using Moq;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Orders;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX.Tests
{
    /// <summary>
    /// Integration tests for OKX Spot market using the BrokerageTests framework
    /// Tests inherit standard order flow scenarios with Mock Algorithm setup
    /// </summary>
    [TestFixture]
    [Explicit("Requires valid OKX credentials and Spot market balance")]
    public class OKXSpotBrokerageTests : BrokerageTests
    {
        /// <summary>
        /// Creates the brokerage under test and connects it
        /// </summary>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var security = securityProvider.GetSecurity(Symbol);
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                {Symbol, security}
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());
            var algorithmSettings = new AlgorithmSettings();

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new OKXBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions, algorithmSettings));
            algorithm.Setup(a => a.Securities).Returns(securities);

            return new OKXBrokerage(
                Config.Get("okx-api-key"),
                Config.Get("okx-api-secret"),
                Config.Get("okx-passphrase"),
                algorithm.Object,
                new AggregationManager()
            );
        }

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => StaticSymbol;
        private static Symbol StaticSymbol => Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);

        /// <summary>
        /// Gets the security type associated with the Symbol
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Crypto;

        /// <summary>
        /// Gets the default order quantity
        /// </summary>
        protected override decimal GetDefaultQuantity() => 0.01m;

        /// <summary>
        /// Returns whether or not the broker's order methods implementation are async
        /// </summary>
        protected override bool IsAsync() => true;

        /// <summary>
        /// Returns whether or not the broker's order cancel method implementation is async
        /// </summary>
        protected override bool IsCancelAsync() => true;

        /// <summary>
        /// Gets the current market price of the specified security
        /// Returns the high price from DynamicPrices (fetched once via REST API)
        /// Tests don't require real-time precision, so cached price is sufficient
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            return DynamicPrices.Value.high;
        }

        /// <summary>
        /// Lazy-initialized dynamic limit prices based on current market data
        /// Fetched once on first access via REST API, cached for subsequent use
        /// </summary>
        private static readonly Lazy<(decimal high, decimal low)> DynamicPrices = new Lazy<(decimal, decimal)>(() =>
        {
            const decimal fallbackHigh = 110000m;
            const decimal fallbackLow = 95000m;

            try
            {
                var apiKey = Config.Get("okx-api-key");
                var apiSecret = Config.Get("okx-api-secret");
                var passphrase = Config.Get("okx-passphrase");
                var client = new OKXRestApiClient(apiKey, apiSecret, passphrase);

                // Get ticker data for BTC-USDT
                var ticker = client.GetTicker("BTC-USDT")?.FirstOrDefault();

                if (ticker != null && ticker.LowestAsk > 0 && ticker.HighestBid > 0)
                {
                    var high = ticker.LowestAsk * 1.05m;  // 5% above ask
                    var low = ticker.HighestBid * 0.95m;  // 5% below bid
                    Log.Trace($"[Spot] Dynamic prices: bid={ticker.HighestBid}, ask={ticker.LowestAsk} → high={high:F2}, low={low:F2}");
                    return (high, low);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Spot] Failed to fetch market prices: {ex.Message}");
            }

            Log.Trace($"[Spot] Using fallback prices: high={fallbackHigh}, low={fallbackLow}");
            return (fallbackHigh, fallbackLow);
        });

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// Note: OKX supports Market and Limit orders for Spot trading
        /// Prices are dynamically fetched from market on first access
        /// </summary>
        private static TestCaseData[] OrderParameters =>
        [
            new TestCaseData(new MarketOrderTestParameters(StaticSymbol)),
            new TestCaseData(new LimitOrderTestParameters(StaticSymbol, DynamicPrices.Value.high, DynamicPrices.Value.low))
        ];

        [Test]
        [TestCaseSource(nameof(OrderParameters))]
        [Category("Spot")]
        public void SpotCancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Test]
        [TestCaseSource(nameof(OrderParameters))]
        [Category("Spot")]
        public void SpotLongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);

            // For non-market orders, verify the order is in the open orders list
            if (parameters is not MarketOrderTestParameters)
            {
                var openOrders = Brokerage.GetOpenOrders();
                Assert.AreEqual(1, openOrders.Count);
                Assert.IsInstanceOf<LimitOrder>(openOrders[0]);
            }
        }

        [Test]
        [TestCaseSource(nameof(OrderParameters))]
        [Category("Spot")]
        public void SpotCloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, Ignore("Spot holdings are managed through cash balances. GetAccountHoldings returns empty for spot trading.")]
        [Category("Spot")]
        public override void GetAccountHoldings()
        {
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS - SPOT");
            Log.Trace("");

            var holdings = Brokerage.GetAccountHoldings();

            // For spot trading, holdings should be empty as positions are tracked via cash balances
            Assert.AreEqual(0, holdings.Count, "Spot trading should return empty holdings");

            // Log for verification
            Log.Trace($"Holdings count: {holdings.Count} (expected: 0)");
        }
    }
}
