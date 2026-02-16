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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX-specific execution constraint that enforces per-instrument order size limits
    /// fetched from the OKX /api/v5/public/instruments endpoint (no auth required).
    /// Caches instrument data in a ConcurrentDictionary with 24h TTL.
    /// </summary>
    public class OKXConstraint : ExecutionConstraint
    {
        private readonly Lazy<OKXRestApiClient> _client;
        private readonly OKXSymbolMapper _symbolMapper = new(Market.OKX);
        private readonly ConcurrentDictionary<Symbol, InstrumentLimit> _cache = new();
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
        /// Returns the maximum allowed order quantity based on OKX per-instrument limits.
        /// Two independent limiters (Size + Amount), take min.
        /// </summary>
        public override decimal GetValue(ConstraintContext ctx)
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

            return ctx.FloorToStep(Math.Min(qtyFromSize, qtyFromAmount));
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

        private readonly object _refreshLock = new();

        private bool TryGetLimit(Symbol symbol, out InstrumentLimit limit)
        {
            if (_cache.TryGetValue(symbol, out limit) && !limit.IsExpired)
                return true;

            lock (_refreshLock)
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(symbol, out limit) && !limit.IsExpired)
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

                    _cache[leanSymbol] = new InstrumentLimit(
                        ParseHelper.ParseDecimal(inst.MaxMarketSize),
                        ParseHelper.ParseDecimal(inst.MaxLimitSize),
                        ParseHelper.ParseDecimal(inst.MaxMarketAmount),
                        ParseHelper.ParseDecimal(inst.MaxLimitAmount),
                        now);
                }

                Log.Trace($"OKXConstraint: Refreshed {instruments.Count} {instType} instruments");
                return _cache.TryGetValue(symbol, out limit);
            }
            catch (Exception ex)
            {
                Log.Error($"OKXConstraint: Failed to refresh {instType} instruments: {ex.Message}");
                return false;
            }
        }
    }
}
