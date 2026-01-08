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
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// JSON converter for decimal values
    /// OKX API returns decimal values as strings (e.g., "0.001", "123.45")
    /// This converter handles null values and parsing errors gracefully
    /// </summary>
    public class DecimalConverter : JsonConverter<decimal>
    {
        /// <summary>
        /// Reads JSON string and converts to decimal
        /// </summary>
        public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return 0m;
            }

            if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
            {
                return Convert.ToDecimal(reader.Value);
            }

            if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return 0m;
                }

                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                Log.Trace($"DecimalConverter: Failed to parse '{value}' as decimal, returning 0");
                return 0m;
            }

            Log.Trace($"DecimalConverter: Unexpected token type {reader.TokenType}, returning 0");
            return 0m;
        }

        /// <summary>
        /// Writes decimal as JSON string
        /// </summary>
        public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
