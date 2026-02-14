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

using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - Price Limit Management
    /// Handles price limit synchronization (REST init + WS continuous updates)
    /// and orderbook truncation to remove phantom liquidity beyond exchange price limits.
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Price limit synchronizer: TState = TMessage = PriceLimit (each WS push is a full snapshot)
        /// </summary>
        protected BrokerageMultiStateSynchronizer<Symbol, PriceLimit, PriceLimit> _priceLimitSync;

        /// <summary>
        /// Initialize price limit synchronizer. Called from Initialize() alongside InitializeOrderBookSync()
        /// </summary>
        protected virtual void CreatePriceLimitSynchronizer()
        {
            _priceLimitSync = new BrokerageMultiStateSynchronizer<Symbol, PriceLimit, PriceLimit>(
                getKey: msg => _symbolMapper.GetLeanSymbol(msg.InstrumentId, GetSecurityType(msg.InstrumentId), Market.OKX),
                reducer: (current, msg) =>
                    current == null || ParseHelper.ParseLong(msg.Timestamp) >= ParseHelper.ParseLong(current.Timestamp)
                        ? msg : current,
                capacity: 1000,
                initializer: InitializePriceLimitAsync
            );
        }

        /// <summary>
        /// REST fetch to guarantee state exists before WS messages flow
        /// </summary>
        private async Task InitializePriceLimitAsync(
            Symbol symbol, BrokerageStateSynchronizer<PriceLimit, PriceLimit> sync)
        {
            var instId = _symbolMapper.GetBrokerageSymbol(symbol);
            var data = RestApiClient.GetPriceLimit(instId);
            if (data != null)
            {
                sync.SetState(data);
            }
        }

        /// <summary>
        /// Truncate orderbook levels beyond exchange price limits.
        /// Called by OnOrderBookStateChanged before _aggregator.Update().
        /// Asks beyond BuyLimit are phantom liquidity (can't place buy orders above BuyLimit).
        /// Bids beyond SellLimit are phantom liquidity (can't place sell orders below SellLimit).
        /// </summary>
        public void TruncateByPriceLimit(Orderbook orderbook, Symbol symbol)
        {
            var limit = _priceLimitSync?.GetState(symbol);
            if (limit == null || !limit.Enabled)
            {
                return;
            }

            // Parse at consumption site (message-first pattern, same as OrderConverter for Fill)
            var buyLmt = ParseHelper.ParseDecimal(limit.BuyLimit);
            var sellLmt = ParseHelper.ParseDecimal(limit.SellLimit);

            // Asks sorted ascending (best ask first) → drop asks above BuyLimit
            if (buyLmt > 0 && orderbook.Asks?.Count > 0)
            {
                var truncated = orderbook.Asks.TakeWhile(l => l.Price <= buyLmt).ToList();
                if (truncated.Count < orderbook.Asks.Count)
                {
                    orderbook.Asks = truncated;
                }
            }

            // Bids sorted descending (best bid first) → drop bids below SellLimit
            if (sellLmt > 0 && orderbook.Bids?.Count > 0)
            {
                var truncated = orderbook.Bids.TakeWhile(l => l.Price >= sellLmt).ToList();
                if (truncated.Count < orderbook.Bids.Count)
                {
                    orderbook.Bids = truncated;
                }
            }
        }
    }
}
