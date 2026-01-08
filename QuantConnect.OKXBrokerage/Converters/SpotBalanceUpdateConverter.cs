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
    /// Custom JSON converter for OKX Spot Balance Update messages
    /// Handles spot.balances WebSocket channel messages
    /// </summary>
    public class SpotBalanceUpdateConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            // Helper function for safe field extraction
            string GetString(string field) => obj[field] == null || obj[field].Type == JTokenType.Null ? null : obj[field].ToString();

            return new SpotBalanceUpdate
            {
                Timestamp = GetString("timestamp"),
                TimestampMs = GetString("timestamp_ms"),
                User = GetString("user"),
                Currency = GetString("currency"),
                Change = OKXUtility.ParseDecimal(obj["change"]),
                Total = OKXUtility.ParseDecimal(obj["total"]),
                Available = OKXUtility.ParseDecimal(obj["available"]),
                Freeze = OKXUtility.ParseDecimal(obj["freeze"]),
                FreezeChange = OKXUtility.ParseDecimal(obj["freeze_change"]),
                ChangeType = GetString("change_type")
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SpotBalanceUpdate);
        }
    }
}
