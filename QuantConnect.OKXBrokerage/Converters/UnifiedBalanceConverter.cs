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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.OKX.Messages;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Custom JSON converter for OKX Unified Balance
    /// Handles per-currency balance from GET /unified/accounts
    /// </summary>
    public class UnifiedBalanceConverter : JsonConverter
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

            return new UnifiedBalance
            {
                Available = OKXUtility.ParseDecimal(obj["available"]),
                Freeze = OKXUtility.ParseDecimal(obj["freeze"]),
                Borrowed = OKXUtility.ParseDecimal(obj["borrowed"]),
                NegativeLiability = OKXUtility.ParseDecimal(obj["negative_liab"]),
                Equity = OKXUtility.ParseDecimal(obj["equity"]),
                TotalLiability = OKXUtility.ParseDecimal(obj["total_liab"]),
                SpotInUse = OKXUtility.ParseDecimal(obj["spot_in_use"]),
                Funding = OKXUtility.ParseDecimal(obj["funding"]),
                CrossBalance = OKXUtility.ParseDecimal(obj["cross_balance"]),
                IsolatedBalance = OKXUtility.ParseDecimal(obj["iso_balance"]),
                InitialMargin = OKXUtility.ParseDecimal(obj["im"]),
                MaintenanceMargin = OKXUtility.ParseDecimal(obj["mm"]),
                InitialMarginRate = OKXUtility.ParseDecimal(obj["imr"]),
                MaintenanceMarginRate = OKXUtility.ParseDecimal(obj["mmr"]),
                MarginBalance = OKXUtility.ParseDecimal(obj["margin_balance"]),
                AvailableMargin = OKXUtility.ParseDecimal(obj["available_margin"]),
                EnabledCollateral = OKXUtility.ParseBool(obj["enabled_collateral"])
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(UnifiedBalance);
        }
    }
}
