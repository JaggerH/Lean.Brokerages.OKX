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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.OKX.Messages;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// JSON converter for RiskLimitTier objects
    /// Converts OKX API response to business-oriented model
    /// </summary>
    /// <remarks>
    /// OKX returns numeric fields as strings in the API response.
    /// This converter handles the string-to-decimal conversion.
    ///
    /// Example API response:
    /// {
    ///   "tier": 1,
    ///   "risk_limit": "20000",
    ///   "initial_rate": "0.02",
    ///   "maintenance_rate": "0.01",
    ///   "leverage_max": "50",
    ///   "contract": "BTC_USDT"
    /// }
    /// </remarks>
    public class RiskLimitTierConverter : JsonConverter
    {
        /// <summary>
        /// Reads JSON and converts to RiskLimitTier object
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);

            return new RiskLimitTier
            {
                Tier = obj["tier"]?.Value<int>() ?? 1,
                RiskLimit = ParseDecimal(obj["risk_limit"]),
                InitialRate = ParseDecimal(obj["initial_rate"]),
                MaintenanceRate = ParseDecimal(obj["maintenance_rate"]),
                LeverageMax = ParseDecimal(obj["leverage_max"]),
                Contract = obj["contract"]?.ToString()
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RiskLimitTier);
        }

        /// <summary>
        /// Writes JSON (not implemented - read-only converter)
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("RiskLimitTierConverter is read-only");
        }

        /// <summary>
        /// Parses a JToken to decimal, handling both string and numeric formats
        /// </summary>
        private static decimal ParseDecimal(JToken token)
        {
            if (token == null)
                return 0m;

            var str = token.ToString();
            if (string.IsNullOrEmpty(str))
                return 0m;

            return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }
    }
}
