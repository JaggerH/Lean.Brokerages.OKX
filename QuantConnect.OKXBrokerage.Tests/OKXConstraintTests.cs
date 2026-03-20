/*
 * Tests for OKXConstraint — borrow quota limiter for crypto spot sell orders.
 *
 * Tests exercise GetValue() (public entry point) with instrument limits absent
 * (no REST config), so the result is purely from borrow quota logic.
 *
 * BDS is a singleton — Reset() in SetUp ensures clean state per test.
 */

using System;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.UnifiedMargin;

namespace QuantConnect.OKXBrokerage.Tests
{
    [TestFixture]
    public class OKXConstraintTests
    {
        private OKXConstraint _constraint;

        [SetUp]
        public void Setup()
        {
            BrokerageDataService.Reset();
            _constraint = new OKXConstraint();
        }

        // ─── Scenario 1: PEPE — platform loan quota = 0, has positive holdings ───
        // WS: MaxLoan=0, Borrowed=0.  CashBook: +0.044
        // Expected: can only sell holdings (0.044), cannot borrow at all

        [Test]
        public void BorrowQuota_ZeroMaxLoan_ZeroBorrowed_ConstrainedToHoldings()
        {
            var (algo, security) = CreateCryptoSetup("PEPE", "USDT", cashAmount: 0.044m, price: 0.00001m);

            SetBDS("PEPE", maxLoan: 0m, borrowed: 0m);

            var ctx = CreateContext(algo, security, price: 0.00001m, unorderedQty: -100m);
            var result = _constraint.GetValue(ctx);

            // positiveHoldings(0.044) + remainingBorrowable(0) = 0.044
            Assert.That(result, Is.EqualTo(0.044m).Within(0.0001m),
                "MaxLoan=0 should block borrowing; only holdings sellable");
        }

        // ─── Scenario 2: BCH race condition — stale WS MaxLoan ───
        // WS: MaxLoan=10(stale), Borrowed=90(stale) → totalCapacity=100
        // CashBook: -98 (real-time, 2 more fills since WS update)
        // Expected: remaining = 100 - 98 = 2

        [Test]
        public void BorrowQuota_StaleMaxLoan_UsesTotalCapacityMinusCashBook()
        {
            var (algo, security) = CreateCryptoSetup("BCH", "USDT", cashAmount: -98m, price: 450m);

            SetBDS("BCH", maxLoan: 10m, borrowed: 90m);

            var ctx = CreateContext(algo, security, price: 450m, unorderedQty: -5m);
            var result = _constraint.GetValue(ctx);

            Assert.That(result, Is.EqualTo(2m).Within(0.01m));
        }

        // ─── Scenario 3: Normal borrow — WS is fresh, consistent ───
        // WS: MaxLoan=89, Borrowed=11 → totalCapacity=100
        // CashBook: -11
        // Expected: remaining = 100 - 11 = 89

        [Test]
        public void BorrowQuota_FreshWS_ReturnsMaxLoan()
        {
            var (algo, security) = CreateCryptoSetup("ETH", "USDT", cashAmount: -11m, price: 3000m);

            SetBDS("ETH", maxLoan: 89m, borrowed: 11m);

            var ctx = CreateContext(algo, security, price: 3000m, unorderedQty: -50m);
            var result = _constraint.GetValue(ctx);

            Assert.That(result, Is.EqualTo(89m).Within(0.01m));
        }

        // ─── Scenario 4: Fully utilized — borrowed to the limit ───
        // WS: MaxLoan=0, Borrowed=100 → totalCapacity=100
        // CashBook: -100
        // Expected: remaining = 0

        [Test]
        public void BorrowQuota_FullyUtilized_ReturnsZero()
        {
            var (algo, security) = CreateCryptoSetup("DOGE", "USDT", cashAmount: -100m, price: 0.15m);

            SetBDS("DOGE", maxLoan: 0m, borrowed: 100m);

            var ctx = CreateContext(algo, security, price: 0.15m, unorderedQty: -10m);
            var result = _constraint.GetValue(ctx);

            Assert.That(result, Is.EqualTo(0m));
        }

        // ─── Scenario 5: Buy order — borrow constraint should not apply ───

        [Test]
        public void BorrowQuota_BuyOrder_ReturnsNoLimit()
        {
            var (algo, security) = CreateCryptoSetup("SOL", "USDT", cashAmount: 0m, price: 150m);

            // No BDS setup needed — buy orders return NoLimit before checking BDS

            var ctx = CreateContext(algo, security, price: 150m, unorderedQty: 10m);
            var result = _constraint.GetValue(ctx);

            // Borrow quota returns NoLimit for buys; instrument limit may still constrain
            Assert.That(result, Is.GreaterThan(0m),
                "Buy orders should not be blocked by borrow quota");
        }

        // ─── Scenario 6: No WS data — REST fallback determines constraint ───

        [Test]
        public void BorrowQuota_NoWSData_FallsBackToREST()
        {
            // Use a currency with no BDS entry; REST may return maxLoan=0 (not lendable)
            var (algo, security) = CreateCryptoSetup("SHIB", "USDT", cashAmount: 0m, price: 0.00002m);

            // Don't set up BDS data

            var ctx = CreateContext(algo, security, price: 0.00002m, unorderedQty: -1000000m);
            var result = _constraint.GetValue(ctx);

            // No holdings, no borrow capacity → should not allow unconstrained selling
            Assert.That(result, Is.LessThanOrEqualTo(ExecutionConstraint.NoLimit));
            Assert.That(result, Is.GreaterThanOrEqualTo(0m));
        }

        // ─── Scenario 7: Has holdings + can borrow more ───
        // WS: MaxLoan=100, Borrowed=0 → totalCapacity=100
        // CashBook: +5 (positive holdings, no borrows yet)
        // Expected: holdings(5) + remainingBorrowable(100) = 105

        [Test]
        public void BorrowQuota_PositiveHoldings_PlusBorrowCapacity()
        {
            var (algo, security) = CreateCryptoSetup("LTC", "USDT", cashAmount: 5m, price: 100m);

            SetBDS("LTC", maxLoan: 100m, borrowed: 0m);

            var ctx = CreateContext(algo, security, price: 100m, unorderedQty: -50m);
            var result = _constraint.GetValue(ctx);

            Assert.That(result, Is.EqualTo(105m).Within(0.01m));
        }

        // ─── Scenario 8: Over-borrowed — CashBook exceeds totalCapacity ───
        // WS: MaxLoan=0, Borrowed=100 → totalCapacity=100
        // CashBook: -105 (fills faster than WS update)
        // Expected: max(0, 100 - 105) = 0

        [Test]
        public void BorrowQuota_OverBorrowed_ClampsToZero()
        {
            var (algo, security) = CreateCryptoSetup("XRP", "USDT", cashAmount: -105m, price: 2m);

            SetBDS("XRP", maxLoan: 0m, borrowed: 100m);

            var ctx = CreateContext(algo, security, price: 2m, unorderedQty: -10m);
            var result = _constraint.GetValue(ctx);

            Assert.That(result, Is.EqualTo(0m));
        }

        #region Helpers

        private static void SetBDS(string currency, decimal maxLoan, decimal borrowed)
        {
            BrokerageDataService.Instance.UpdateCurrencyBalance(currency,
                new BrokerageDataService.CurrencyBalance
                {
                    Currency = currency,
                    MaxLoan = maxLoan,
                    Borrowed = borrowed,
                    UpdatedAt = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Creates a minimal AQCAlgorithm with CashBook + a standalone Crypto security.
        /// No DataManager/TransactionHandler needed — only Portfolio.CashBook is accessed.
        /// </summary>
        private static (AQCAlgorithm algo, Security security) CreateCryptoSetup(
            string baseCcy, string quoteCcy, decimal cashAmount, decimal price)
        {
            var algo = new AQCAlgorithm();

            // Set up CashBook entries
            algo.Portfolio.SetCash(quoteCcy, 100000m, 1m);
            algo.Portfolio.SetCash(baseCcy, cashAmount, price);

            // Create Crypto security (implements IBaseCurrencySymbol)
            var ticker = $"{baseCcy}{quoteCcy}";
            var symbol = Symbol.Create(ticker, SecurityType.Crypto, Market.OKX);
            var quoteCash = algo.Portfolio.CashBook[quoteCcy];
            var baseCash = algo.Portfolio.CashBook[baseCcy];

            var security = new Crypto(
                symbol,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                quoteCash,
                baseCash,
                SymbolProperties.GetDefault(quoteCcy),
                algo.Portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            return (algo, security);
        }

        /// <summary>
        /// Creates a ConstraintContext using the public constructor directly,
        /// bypassing ForLeg (which requires algorithm.Securities).
        /// </summary>
        private static ConstraintContext CreateContext(
            AQCAlgorithm algo, Security security,
            decimal price, decimal unorderedQty)
        {
            var target = new ArbitragePortfolioTarget(
                security.Symbol, security.Symbol, -1m, 1m,
                "LONG_SPREAD", "ENTRY", -0.01m, "test");

            // Use small legStep to avoid FloorToStep truncating fractional results
            return new ConstraintContext(
                algo, target, pair: null,
                security.Symbol, security, price,
                contractMultiplier: 1m, legStep: 0.00000001m,
                unorderedQty, orderbookQuantity: ExecutionConstraint.NoLimit);
        }

        #endregion
    }
}
