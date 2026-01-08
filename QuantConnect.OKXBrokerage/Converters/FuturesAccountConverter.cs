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
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Domain converter for Futures account (REST API)
    /// Converts OKX Futures account models to LEAN models
    /// </summary>
    public static class FuturesAccountConverter
    {
        /// <summary>
        /// Converts Futures account to LEAN CashAmount
        /// Returns available margin as cash balance
        /// Note: The "available" field represents margin available for trading or withdrawal
        /// </summary>
        /// <param name="account">OKX futures account from REST API</param>
        /// <returns>LEAN CashAmount or null if conversion fails</returns>
        public static CashAmount? ToCashAmount(this FuturesAccount account)
        {
            if (account == null || string.IsNullOrEmpty(account.Currency))
            {
                return null;
            }

            // Parse available margin
            // Key: Return "available" field as margin balance
            if (!decimal.TryParse(account.Available, out var amount))
            {
                return null;
            }

            // Return available margin as CashAmount
            return new CashAmount(amount, account.Currency.ToUpperInvariant());
        }

        /// <summary>
        /// Gets total margin balance including unrealized PnL
        /// Formula: total + unrealised_pnl = total margin balance
        /// </summary>
        /// <param name="account">OKX futures account</param>
        /// <returns>Total margin balance including unrealized PnL</returns>
        public static decimal GetTotalMarginBalance(this FuturesAccount account)
        {
            if (account == null)
            {
                return 0m;
            }

            decimal.TryParse(account.Total, out var total);
            decimal.TryParse(account.UnrealisedPnl, out var unrealisedPnl);

            return total + unrealisedPnl;
        }

        /// <summary>
        /// Gets the margin utilization ratio
        /// Formula: (position_margin + order_margin) / (total + unrealised_pnl)
        /// </summary>
        /// <param name="account">OKX futures account</param>
        /// <returns>Margin utilization ratio (0.0 to 1.0), or 0 if no positions</returns>
        public static decimal GetMarginUtilizationRatio(this FuturesAccount account)
        {
            if (account == null)
            {
                return 0m;
            }

            var totalMargin = account.GetTotalMarginBalance();
            if (totalMargin == 0)
            {
                return 0m;
            }

            decimal.TryParse(account.PositionMargin, out var positionMargin);
            decimal.TryParse(account.OrderMargin, out var orderMargin);

            var usedMargin = positionMargin + orderMargin;
            return usedMargin / totalMargin;
        }
    }
}
