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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX futures risk limit calculator
    /// Calculates the maximum order value allowed for a given futures contract
    /// </summary>
    /// <remarks>
    /// OKX uses tiered risk limits to manage position sizes. This calculator:
    /// 1. Queries risk limit tiers from OKX API (with 24-hour caching)
    /// 2. Calculates effective position value (position + pending orders)
    /// 3. Determines current tier based on position value
    /// 4. Returns available capacity = tier limit - used limit
    ///
    /// Risk Limit Formula (from OKX):
    /// Effective Position Value = max(Long Direction Value, Short Direction Value)
    /// where:
    ///   Long Direction Value = (Long Position + Long Pending Orders) × Mark Price × Multiplier
    ///   Short Direction Value = (Short Position + Short Pending Orders) × Mark Price × Multiplier
    ///
    /// Available Limit = Current Tier Risk Limit - Effective Position Value
    /// </remarks>
    public class OKXRiskLimitCalculator
    {
        private readonly OKXRestApiClient _restClient;
        private readonly ISymbolMapper _symbolMapper;
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Tier cache: contract name -> (tiers, cached timestamp)
        /// </summary>
        private readonly ConcurrentDictionary<string, (List<RiskLimitTier> Tiers, DateTime CachedAt)> _tierCache;

        /// <summary>
        /// Cache expiry duration (24 hours)
        /// </summary>
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

        /// <summary>
        /// Creates a new risk limit calculator
        /// </summary>
        /// <param name="restClient">OKX Futures REST API client</param>
        /// <param name="symbolMapper">Symbol mapper for converting between LEAN and OKX symbols</param>
        /// <param name="algorithm">Algorithm instance for accessing OrderTickets</param>
        public OKXRiskLimitCalculator(OKXRestApiClient restClient, ISymbolMapper symbolMapper, IAlgorithm algorithm)
        {
            _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
            _symbolMapper = symbolMapper ?? throw new ArgumentNullException(nameof(symbolMapper));
            _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            _tierCache = new ConcurrentDictionary<string, (List<RiskLimitTier>, DateTime)>();
        }

        /// <summary>
        /// Gets the available risk limit for a futures contract
        /// </summary>
        /// <param name="symbol">LEAN Symbol (must be SecurityType.CryptoFuture)</param>
        /// <returns>Available order value in USD. Returns decimal.MaxValue for non-futures symbols.</returns>
        /// <exception cref="Exception">Thrown when API calls fail</exception>
        /// <remarks>
        /// For spot symbols, returns decimal.MaxValue (no risk limit).
        /// For futures symbols, calculates available capacity based on current position and pending orders.
        /// </remarks>
        public decimal GetAvailableRiskLimit(Symbol symbol)
        {
            // Spot trading has no risk limit
            if (symbol.SecurityType != SecurityType.CryptoFuture)
            {
                return decimal.MaxValue;
            }

            // Convert LEAN symbol to OKX contract name
            var contract = _symbolMapper.GetBrokerageSymbol(symbol);

            // Get current positions from REST API
            var positions = _restClient.GetPositions();
            var position = positions.FirstOrDefault(p =>
                p.Contract.Equals(contract, StringComparison.OrdinalIgnoreCase));

            // Get open order tickets from Algorithm (more accurate than REST API)
            var openOrderTickets = _algorithm.Transactions
                .GetOpenOrderTickets(ticket => ticket.Symbol == symbol)
                .ToList();

            // Calculate effective position value
            var effectivePositionValue = CalculateEffectivePositionValue(position, openOrderTickets, contract);

            // Get risk limit tiers (with caching)
            var tiers = GetCachedRiskLimitTiers(contract);

            // Determine current tier
            var currentTier = DetermineCurrentTier(effectivePositionValue, tiers);

            // Calculate available limit
            var availableLimit = currentTier.RiskLimit - effectivePositionValue;

            Log.Trace($"OKXRiskLimitCalculator.GetAvailableRiskLimit({symbol.Value}): " +
                     $"Tier={currentTier.Tier}, TierLimit={currentTier.RiskLimit:N0}, " +
                     $"EffectiveValue={effectivePositionValue:N0}, Available={availableLimit:N0}");

            return Math.Max(0m, availableLimit);
        }

        /// <summary>
        /// Calculates the effective position value for risk limit calculation
        /// </summary>
        /// <param name="position">Current position (can be null if no position)</param>
        /// <param name="openOrderTickets">List of open order tickets for this contract</param>
        /// <param name="contract">Contract name for logging</param>
        /// <returns>Effective position value in USD</returns>
        private decimal CalculateEffectivePositionValue(
            FuturesPosition position,
            List<OrderTicket> openOrderTickets,
            string contract)
        {
            // Position value
            decimal positionValue = 0m;
            decimal markPrice = 0m;

            if (position != null && position.Size != 0)
            {
                // Use the Value field from position (already calculated by OKX)
                if (!string.IsNullOrEmpty(position.Value) &&
                    decimal.TryParse(position.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pv))
                {
                    positionValue = Math.Abs(pv);
                }

                // Get mark price for pending order value calculation
                if (!string.IsNullOrEmpty(position.MarkPrice) &&
                    decimal.TryParse(position.MarkPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var mp))
                {
                    markPrice = mp;
                }

                Log.Trace($"OKXRiskLimitCalculator: {contract} Position: " +
                         $"Size={position.Size}, Value={positionValue:N2}, MarkPrice={markPrice:N2}");
            }

            // Calculate pending order value using OrderTicket.QuantityRemaining
            // This is more accurate than using Order.AbsoluteQuantity
            decimal longPendingValue = 0m;
            decimal shortPendingValue = 0m;

            foreach (var ticket in openOrderTickets)
            {
                // Use QuantityRemaining for accurate unfilled quantity
                var remainingQty = Math.Abs(ticket.QuantityRemaining);
                if (remainingQty <= 0)
                    continue;

                // Estimate order value using mark price (if available) or order price
                decimal orderPrice = markPrice > 0 ? markPrice : GetOrderPriceFromTicket(ticket);
                decimal orderValue = remainingQty * orderPrice;

                // Determine direction based on original quantity sign
                if (ticket.Quantity > 0)
                {
                    longPendingValue += orderValue;
                }
                else
                {
                    shortPendingValue += orderValue;
                }
            }

            // Calculate total pending value
            var totalPendingValue = longPendingValue + shortPendingValue;

            if (totalPendingValue > 0)
            {
                Log.Trace($"OKXRiskLimitCalculator: {contract} Pending Orders: " +
                         $"Long={longPendingValue:N2}, Short={shortPendingValue:N2}, Total={totalPendingValue:N2}");
            }

            // Effective position value = position value + pending order value
            // Note: OKX formula uses max(long, short) but for simplicity we sum all
            return positionValue + totalPendingValue;
        }

        /// <summary>
        /// Gets the price from an order ticket
        /// </summary>
        private decimal GetOrderPriceFromTicket(OrderTicket ticket)
        {
            // Try to get limit price from the submit request
            var submitRequest = ticket.SubmitRequest;
            if (submitRequest != null && submitRequest.LimitPrice > 0)
            {
                return submitRequest.LimitPrice;
            }

            // Fallback to average fill price if partially filled
            if (ticket.AverageFillPrice > 0)
            {
                return ticket.AverageFillPrice;
            }

            // For market orders without fills, return 0 (will use mark price instead)
            return 0m;
        }

        /// <summary>
        /// Gets risk limit tiers with caching
        /// </summary>
        /// <param name="contract">Contract name (e.g., BTC_USDT)</param>
        /// <returns>List of risk limit tiers</returns>
        private List<RiskLimitTier> GetCachedRiskLimitTiers(string contract)
        {
            var now = DateTime.UtcNow;

            // Check cache
            if (_tierCache.TryGetValue(contract, out var cached))
            {
                if (now - cached.CachedAt < CacheExpiry)
                {
                    return cached.Tiers;
                }
            }

            // Fetch from API
            var tiers = _restClient.GetRiskLimitTiers(contract);

            // Update cache
            _tierCache[contract] = (tiers, now);

            Log.Trace($"OKXRiskLimitCalculator: Cached {tiers.Count} risk limit tiers for {contract}");

            return tiers;
        }

        /// <summary>
        /// Determines the current tier based on position value
        /// </summary>
        /// <param name="positionValue">Current effective position value</param>
        /// <param name="tiers">Available risk limit tiers (must be sorted by tier ascending)</param>
        /// <returns>The current applicable tier</returns>
        private RiskLimitTier DetermineCurrentTier(decimal positionValue, List<RiskLimitTier> tiers)
        {
            if (tiers == null || tiers.Count == 0)
            {
                throw new InvalidOperationException("No risk limit tiers available");
            }

            // Find the appropriate tier for current position value
            foreach (var tier in tiers)
            {
                if (positionValue <= tier.RiskLimit)
                {
                    return tier;
                }
            }

            // If position exceeds all tiers, return the highest tier
            return tiers.Last();
        }

        /// <summary>
        /// Clears the tier cache (useful for testing or forced refresh)
        /// </summary>
        public void ClearCache()
        {
            _tierCache.Clear();
            Log.Trace("OKXRiskLimitCalculator: Cache cleared");
        }
    }
}
