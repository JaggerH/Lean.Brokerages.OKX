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
using System.Collections.Generic;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Securities.UnifiedMargin;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Converter for transforming OKX WebSocket account data to BrokerageDataService data
    /// </summary>
    public static class AccountConverter
    {
        /// <summary>
        /// Converts an OKX WebSocketAccount to AccountMarginData
        /// </summary>
        public static BrokerageDataService.AccountMarginData ToAccountMarginData(this WebSocketAccount account)
        {
            var lastUpdated = ParseHelper.ParseUnixMilliseconds(account.UpdateTime);
            if (lastUpdated == DateTime.MinValue)
            {
                lastUpdated = DateTime.UtcNow;
            }

            // Sum per-currency liabilities for TotalLiability
            // Individual per-currency entries are written separately via ExtractCurrencyLiabilities()
            decimal totalLiability = 0m;
            if (account.Details != null)
            {
                foreach (var detail in account.Details)
                {
                    var liab = ParseHelper.ParseDecimal(detail.Liability);
                    if (liab != 0)
                    {
                        totalLiability += Math.Abs(liab);
                    }
                }
            }

            return new BrokerageDataService.AccountMarginData
            {
                TotalEquity = ParseHelper.ParseDecimal(account.TotalEquity),
                MarginBalance = ParseHelper.ParseDecimal(account.AdjustedEquity),
                AvailableMargin = ParseHelper.ParseDecimal(account.AvailableEquity),
                InitialMarginUsed = ParseHelper.ParseDecimal(account.InitialMarginRequirement),
                MaintenanceMarginUsed = ParseHelper.ParseDecimal(account.MaintenanceMarginRequirement),
                MaintenanceMarginRate = ParseHelper.ParseDecimal(account.MarginRatio),
                InitialMarginRate = 0m,
                TotalLiability = totalLiability,
                LastUpdated = lastUpdated
            };
        }

        /// <summary>
        /// Extracts per-currency balance snapshots from OKX account details.
        /// Returns an entry for every currency in the details array.
        /// </summary>
        public static List<BrokerageDataService.CurrencyBalance> ExtractCurrencyBalances(
            this WebSocketAccount account)
        {
            var result = new List<BrokerageDataService.CurrencyBalance>();
            if (account.Details == null) return result;

            var now = DateTime.UtcNow;
            foreach (var detail in account.Details)
            {
                if (string.IsNullOrEmpty(detail.Currency)) continue;

                var liab = ParseHelper.ParseDecimal(detail.Liability);

                result.Add(new BrokerageDataService.CurrencyBalance
                {
                    Currency = detail.Currency,
                    AvailableBalance = ParseHelper.ParseDecimal(detail.AvailableBalance),
                    FrozenBalance = ParseHelper.ParseDecimal(detail.FrozenBalance),
                    Equity = ParseHelper.ParseDecimal(detail.Equity),
                    Borrowed = Math.Abs(liab),
                    AccruedInterest = ParseHelper.ParseDecimal(detail.Interest),
                    MaxLoan = ParseHelper.ParseDecimal(detail.MaxLoan),
                    UpdatedAt = now
                });
            }

            return result;
        }
    }
}
