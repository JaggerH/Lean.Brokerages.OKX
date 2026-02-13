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
using QuantConnect.Securities.UnifiedMargin;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Converter for transforming OKX WebSocket account data to BrokerageMarginCache data
    /// </summary>
    public static class AccountConverter
    {
        /// <summary>
        /// Converts an OKX WebSocketAccount to AccountMarginData
        /// </summary>
        public static BrokerageMarginCache.AccountMarginData ToAccountMarginData(this WebSocketAccount account)
        {
            var lastUpdated = ParseHelper.ParseUnixMilliseconds(account.UpdateTime);
            if (lastUpdated == DateTime.MinValue)
            {
                lastUpdated = DateTime.UtcNow;
            }

            return new BrokerageMarginCache.AccountMarginData
            {
                TotalEquity = ParseHelper.ParseDecimal(account.TotalEquity),
                MarginBalance = ParseHelper.ParseDecimal(account.AdjustedEquity),
                AvailableMargin = ParseHelper.ParseDecimal(account.AvailableEquity),
                InitialMarginUsed = ParseHelper.ParseDecimal(account.InitialMarginRequirement),
                MaintenanceMarginUsed = ParseHelper.ParseDecimal(account.MaintenanceMarginRequirement),
                MaintenanceMarginRate = ParseHelper.ParseDecimal(account.MarginRatio),
                InitialMarginRate = 0m,
                TotalLiability = 0m,
                LastUpdated = lastUpdated
            };
        }
    }
}
