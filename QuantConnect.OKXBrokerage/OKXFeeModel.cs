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

using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Fee Model
    /// Implements OKX's fee structure for spot and derivatives trading
    /// </summary>
    /// <remarks>
    /// OKX fee structure (as of 2024):
    /// - Spot/Margin: Maker 0.08%, Taker 0.10%
    /// - Futures/Perpetual: Maker 0.02%, Taker 0.05%
    /// Fees vary based on trading tier and 30-day volume
    /// Using standard tier (Lv 1) fees as baseline
    /// </remarks>
    public class OKXFeeModel : FeeModel
    {
        /// <summary>
        /// Spot market maker fee rate (0.08%)
        /// </summary>
        private const decimal SpotMakerFeeRate = 0.0008m;

        /// <summary>
        /// Spot market taker fee rate (0.10%)
        /// </summary>
        private const decimal SpotTakerFeeRate = 0.0010m;

        /// <summary>
        /// Derivatives market maker fee rate (0.02%)
        /// </summary>
        private const decimal DerivativesMakerFeeRate = 0.0002m;

        /// <summary>
        /// Derivatives market taker fee rate (0.05%)
        /// </summary>
        private const decimal DerivativesTakerFeeRate = 0.0005m;

        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">The fee parameters</param>
        /// <returns>The cost of the order in the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            // Determine if this is a maker or taker order
            // Limit orders that add liquidity are makers, market orders and IOC limit orders are takers
            var isMaker = order.Type == OrderType.Limit && order.TimeInForce != TimeInForce.Day;

            // Determine fee rate based on security type
            decimal feeRate;
            if (security.Type == SecurityType.CryptoFuture)
            {
                // Derivatives (Futures/Perpetual)
                feeRate = isMaker ? DerivativesMakerFeeRate : DerivativesTakerFeeRate;
            }
            else
            {
                // Spot/Margin
                feeRate = isMaker ? SpotMakerFeeRate : SpotTakerFeeRate;
            }

            // Calculate fee: notional value * fee rate
            var fee = order.AbsoluteQuantity * order.Price * feeRate;

            // Determine fee currency
            // For crypto pairs like BTC-USDT, fee is typically in quote currency (USDT)
            var feeCurrency = security.QuoteCurrency.Symbol;

            return new OrderFee(new CashAmount(fee, feeCurrency));
        }
    }
}
