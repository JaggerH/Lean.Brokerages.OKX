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
using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.UnifiedMargin;

namespace QuantConnect.Brokerages.OKX
{
    // Future optimization: if _maxLoanCache needs to be updated from outside this instance
    // (e.g., leverage change callbacks), consider creating a global OKX-specific data manager
    // class within the OKX project (similar to BDS but for exchange-specific variables).

    /// <summary>
    /// OKX-specific execution constraint that enforces:
    /// 1. Per-instrument order size limits (from /api/v5/public/instruments)
    /// 2. Per-currency borrow limits: min(loanQuota, maxLoan)
    ///    - loanQuota: platform-wide limit (BDS, hourly refresh)
    ///    - maxLoan: leverage-tier limit (WS CurrencyBalance → REST fallback)
    /// The minimum of all limiters is returned (min-limit model).
    /// </summary>
    public class OKXConstraint : ExecutionConstraint
    {
        private readonly Lazy<OKXRestApiClient> _client;
        private readonly OKXSymbolMapper _symbolMapper = new(Market.OKX);
        private readonly ConcurrentDictionary<Symbol, InstrumentLimit> _instrumentLimits = new();
        private readonly ConcurrentDictionary<Symbol, decimal> _maxLoanCache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private readonly struct InstrumentLimit
        {
            public readonly decimal MaxMarketSize;
            public readonly decimal MaxLimitSize;
            public readonly decimal MaxMarketAmount;
            public readonly decimal MaxLimitAmount;
            public readonly DateTime FetchedAt;

            public InstrumentLimit(decimal maxMktSz, decimal maxLmtSz, decimal maxMktAmt, decimal maxLmtAmt, DateTime fetchedAt)
            {
                MaxMarketSize = maxMktSz;
                MaxLimitSize = maxLmtSz;
                MaxMarketAmount = maxMktAmt;
                MaxLimitAmount = maxLmtAmt;
                FetchedAt = fetchedAt;
            }

            public bool IsExpired => DateTime.UtcNow - FetchedAt >= CacheTtl;
        }

        /// <summary>
        /// Creates a new OKX constraint. Lazily creates a REST client from config
        /// (okx-api-key, okx-api-secret, okx-passphrase).
        /// </summary>
        public OKXConstraint() : base("OKX")
        {
            _client = new Lazy<OKXRestApiClient>(() => new OKXRestApiClient(
                Config.Get("okx-api-key", ""),
                Config.Get("okx-api-secret", ""),
                Config.Get("okx-passphrase", "")));
        }

        /// <summary>
        /// Returns the maximum allowed order quantity based on OKX per-instrument limits
        /// and per-currency borrow quota. Takes the minimum across all limiters.
        /// </summary>
        public override decimal GetValue(ConstraintContext ctx)
        {
            var qtyFromInstrument = GetInstrumentLimit(ctx);
            var qtyFromBorrow = GetBorrowQuotaLimit(ctx);

            return ctx.FloorToStep(Math.Min(qtyFromInstrument, qtyFromBorrow));
        }

        // ─── Instrument Limits (maxMktSz / maxLmtSz / maxLmtAmt / maxMktAmt) ───

        /// <summary>
        /// Returns the maximum allowed quantity based on per-instrument size/amount limits.
        /// </summary>
        private decimal GetInstrumentLimit(ConstraintContext ctx)
        {
            if (!TryGetLimit(ctx.Symbol, out var limit))
                return NoLimit;

            // OKX brokerage converts spot market buy to FOK limit order
            var effectiveOrderType = ctx.OrderType;
            if (ctx.Security.Type == SecurityType.Crypto
                && ctx.OrderType == OrderType.Market
                && ctx.UnorderedQuantity > 0)
            {
                effectiveOrderType = OrderType.Limit;
            }

            var qtyFromSize = GetSizeLimit(ctx, limit, effectiveOrderType);
            var qtyFromAmount = GetAmountLimit(ctx, limit, effectiveOrderType);

            return Math.Min(qtyFromSize, qtyFromAmount);
        }

        /// <summary>
        /// Size limiter: maxLmtSz / maxMktSz
        /// All types except SPOT market: raw value is native quantity (base ccy or contracts).
        /// SPOT market: maxMktSz is in USDT, needs price conversion.
        /// </summary>
        private decimal GetSizeLimit(ConstraintContext ctx, InstrumentLimit limit, OrderType orderType)
        {
            var raw = orderType == OrderType.Limit ? limit.MaxLimitSize : limit.MaxMarketSize;
            if (raw <= 0) return NoLimit;

            // Only SPOT market has maxMktSz in USDT → convert to quantity
            if (orderType == OrderType.Market && ctx.Security.Type == SecurityType.Crypto)
                return ctx.QtyFromMoney(raw);

            // Native quantity: base ccy (SPOT limit) or contracts (SWAP)
            return raw;
        }

        /// <summary>
        /// Amount limiter: maxLmtAmt / maxMktAmt (USD value → convert to quantity)
        /// maxLmtAmt: applies to all types.
        /// maxMktAmt: only applies to crypto spot/margin.
        /// </summary>
        private decimal GetAmountLimit(ConstraintContext ctx, InstrumentLimit limit, OrderType orderType)
        {
            if (orderType == OrderType.Limit)
                return limit.MaxLimitAmount > 0 ? ctx.QtyFromMoney(limit.MaxLimitAmount) : NoLimit;

            // maxMktAmt only for crypto spot/margin
            if (ctx.Security.Type == SecurityType.Crypto && limit.MaxMarketAmount > 0)
                return ctx.QtyFromMoney(limit.MaxMarketAmount);

            return NoLimit;
        }

        // ─── Borrow Limit (maxLoan, sell-side only) ───

        /// <summary>
        /// Returns the maximum sellable quantity for crypto spot, based on maxLoan.
        /// Two-layer hit:
        ///   Hit 1 — WS: BDS CurrencyBalance (Borrowed + MaxLoan = total tier capacity)
        ///   Hit 2 — REST: _maxLoanCache[Symbol], fetched on-demand via GetMaxLoan(side=sell)
        /// Only constrains crypto spot sell orders (selling beyond holdings triggers base ccy borrowing).
        /// Buy-side borrowing (quote ccy) is already constrained by Margin + SpotExposure.
        ///
        /// Race condition fix: WS MaxLoan may lag behind fills. Instead of using MaxLoan directly
        /// (which is "remaining borrowable" and stale until next WS push), we compute:
        ///   totalCapacity = Borrowed + MaxLoan   (stable tier limit, changes only on leverage/tier change)
        ///   remaining = positiveHoldings + max(0, totalCapacity - currentBorrowed)
        /// where positiveHoldings = sellable without borrowing, currentBorrowed from CashBook (real-time).
        /// </summary>
        private decimal GetBorrowQuotaLimit(ConstraintContext ctx)
        {
            if (ctx.Security.Type != SecurityType.Crypto || ctx.UnorderedQuantity >= 0)
                return NoLimit;

            if (!(ctx.Security is IBaseCurrencySymbol baseCurrency))
                return NoLimit;

            var ccy = baseCurrency.BaseCurrency.Symbol;

            // Current position from CashBook (real-time, updated on fill without WS delay)
            decimal positiveHoldings = 0;
            decimal currentBorrowed = 0;
            if (ctx.Algorithm.Portfolio.CashBook.TryGetValue(ccy, out var cash))
            {
                if (cash.Amount > 0)
                    positiveHoldings = cash.Amount;
                else if (cash.Amount < 0)
                    currentBorrowed = Math.Abs(cash.Amount);
            }

            // Hit 1: WS via BDS CurrencyBalance
            // Borrowed + MaxLoan = total tier capacity (stable across WS update delays)
            if (BrokerageDataService.Instance.TryGetCurrencyBalance(ccy, out var balance))
            {
                var totalCapacity = balance.Borrowed + balance.MaxLoan;
                return positiveHoldings + Math.Max(0, totalCapacity - currentBorrowed);
            }

            // Hit 2: REST cache (new currencies, not yet in WS)
            if (_maxLoanCache.TryGetValue(ctx.Symbol, out var cached))
                return positiveHoldings + cached;

            // Miss: REST fetch (side=sell) → cache
            try
            {
                var instId = _symbolMapper.GetBrokerageSymbol(ctx.Symbol);
                var records = _client.Value.GetMaxLoan(instId);
                var sellRecord = records.FirstOrDefault(r => r.Side == "sell");
                if (sellRecord != null)
                {
                    var maxLoan = sellRecord.GetMaxLoan();
                    if (maxLoan >= 0)
                    {
                        _maxLoanCache[ctx.Symbol] = maxLoan;
                        return positiveHoldings + maxLoan;
                    }
                }
            }
            catch { /* best-effort */ }

            return NoLimit;
        }

        // ─── Instrument Cache ───

        private readonly object _refreshLock = new();

        private bool TryGetLimit(Symbol symbol, out InstrumentLimit limit)
        {
            if (_instrumentLimits.TryGetValue(symbol, out limit) && !limit.IsExpired)
                return true;

            lock (_refreshLock)
            {
                // Double-check after acquiring lock
                if (_instrumentLimits.TryGetValue(symbol, out limit) && !limit.IsExpired)
                    return true;

                return RefreshCache(symbol, out limit);
            }
        }

        /// <summary>
        /// Fetches all instruments of the given type and bulk-updates the cache.
        /// Uses OKXSymbolMapper to convert OKX instId → LEAN Symbol for cache keys.
        /// </summary>
        private bool RefreshCache(Symbol symbol, out InstrumentLimit limit)
        {
            limit = default;
            var instType = symbol.SecurityType == SecurityType.CryptoFuture ? "SWAP" : "SPOT";
            try
            {
                var instruments = _client.Value.GetInstruments(instType);
                if (instruments == null || instruments.Count == 0)
                {
                    Log.Trace($"OKXConstraint: No instruments returned for {instType}");
                    return false;
                }

                var now = DateTime.UtcNow;
                foreach (var inst in instruments)
                {
                    Symbol leanSymbol;
                    try { leanSymbol = _symbolMapper.GetLeanSymbol(inst.InstrumentId); }
                    catch { continue; }

                    _instrumentLimits[leanSymbol] = new InstrumentLimit(
                        ParseHelper.ParseDecimal(inst.MaxMarketSize),
                        ParseHelper.ParseDecimal(inst.MaxLimitSize),
                        ParseHelper.ParseDecimal(inst.MaxMarketAmount),
                        ParseHelper.ParseDecimal(inst.MaxLimitAmount),
                        now);
                }

                Log.Trace($"OKXConstraint: Refreshed {instruments.Count} {instType} instruments");
                return _instrumentLimits.TryGetValue(symbol, out limit);
            }
            catch (Exception ex)
            {
                Log.Error($"OKXConstraint: Failed to refresh {instType} instruments: {ex.Message}");
                return false;
            }
        }
    }
}
