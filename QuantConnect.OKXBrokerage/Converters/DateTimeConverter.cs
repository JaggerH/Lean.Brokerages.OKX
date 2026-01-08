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
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// JSON converter for DateTime from Unix milliseconds timestamp
    /// OKX API returns timestamps as strings in milliseconds (e.g., "1597026383085")
    /// </summary>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Reads JSON string (Unix milliseconds) and converts to DateTime
        /// </summary>
        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return DateTime.MinValue;
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                var timestampMs = Convert.ToInt64(reader.Value);
                return UnixEpoch.AddMilliseconds(timestampMs);
            }

            if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value.ToString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return DateTime.MinValue;
                }

                if (long.TryParse(value, out var timestampMs))
                {
                    return UnixEpoch.AddMilliseconds(timestampMs);
                }

                Log.Trace($"DateTimeConverter: Failed to parse '{value}' as Unix milliseconds timestamp, returning DateTime.MinValue");
                return DateTime.MinValue;
            }

            Log.Trace($"DateTimeConverter: Unexpected token type {reader.TokenType}, returning DateTime.MinValue");
            return DateTime.MinValue;
        }

        /// <summary>
        /// Writes DateTime as Unix milliseconds timestamp string
        /// </summary>
        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            var timestampMs = (long)(value - UnixEpoch).TotalMilliseconds;
            writer.WriteValue(timestampMs.ToString());
        }
    }
}
