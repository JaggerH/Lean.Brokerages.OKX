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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.OKX.Messages;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Custom JSON converter for OKX Unified Account
    /// Handles GET /unified/accounts response
    /// </summary>
    public class UnifiedAccountConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can write JSON
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes the JSON representation of the object
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the JSON representation of the object
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            // Helper function for safe string extraction
            string GetString(string field) => obj[field] == null || obj[field].Type == JTokenType.Null ? null : obj[field].ToString();

            // Helper function for safe long parsing
            long ParseLong(string field)
            {
                var str = obj[field]?.ToString();
                if (string.IsNullOrEmpty(str))
                    return 0;

                if (long.TryParse(str, out var result))
                    return result;

                return 0;
            }

            // Parse balances dictionary
            var balances = new Dictionary<string, UnifiedBalance>();
            var balancesObj = obj["balances"];

            if (balancesObj != null && balancesObj.Type == JTokenType.Object)
            {
                foreach (var property in ((JObject)balancesObj).Properties())
                {
                    try
                    {
                        // Use UnifiedBalanceConverter to parse each currency balance
                        var balance = property.Value.ToObject<UnifiedBalance>();
                        if (balance != null)
                        {
                            balances[property.Name.ToUpperInvariant()] = balance;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue parsing other currencies
                        QuantConnect.Logging.Log.Error($"UnifiedAccountConverter: Error parsing balance for {property.Name}: {ex.Message}");
                    }
                }
            }

            return new UnifiedAccount
            {
                UserId = ParseLong("user_id"),
                RefreshTime = ParseLong("refresh_time"),
                Locked = OKXUtility.ParseBool(obj["locked"]),
                Mode = GetString("mode") ?? "classic",  // Default to classic mode
                Balances = balances,
                TotalBorrowed = OKXUtility.ParseDecimal(obj["borrowed"]),
                TotalInitialMargin = OKXUtility.ParseDecimal(obj["total_initial_margin"]),
                TotalMarginBalance = OKXUtility.ParseDecimal(obj["total_margin_balance"]),
                TotalMaintenanceMargin = OKXUtility.ParseDecimal(obj["total_maintenance_margin"]),
                TotalInitialMarginRate = OKXUtility.ParseDecimal(obj["total_initial_margin_rate"]),
                TotalMaintenanceMarginRate = OKXUtility.ParseDecimal(obj["total_maintenance_margin_rate"]),
                TotalAvailableMargin = OKXUtility.ParseDecimal(obj["total_available_margin"]),
                UnifiedAccountTotal = OKXUtility.ParseDecimal(obj["unified_account_total"]),
                UnifiedAccountTotalLiability = OKXUtility.ParseDecimal(obj["unified_account_total_liab"]),
                UnifiedAccountTotalEquity = OKXUtility.ParseDecimal(obj["unified_account_total_equity"]),
                Leverage = OKXUtility.ParseDecimal(obj["leverage"]),
                SpotOrderLoss = OKXUtility.ParseDecimal(obj["spot_order_loss"]),
                SpotHedge = OKXUtility.ParseBool(obj["spot_hedge"]),
                UseFunding = OKXUtility.ParseBool(obj["use_funding"]),
                IsAllCollateral = OKXUtility.ParseBool(obj["is_all_collateral"])
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(UnifiedAccount);
        }
    }
}
