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
    /// Custom JSON converter for OKX Futures Balance Update messages
    /// Handles futures.balances WebSocket channel messages
    /// </summary>
    public class FuturesBalanceUpdateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FuturesBalanceUpdate);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            // Helper function for safe field extraction
            long GetLong(string field) => obj[field] == null || obj[field].Type == JTokenType.Null ? 0 : (long)obj[field];
            string GetString(string field) => obj[field] == null || obj[field].Type == JTokenType.Null ? null : obj[field].ToString();

            return new FuturesBalanceUpdate
            {
                Balance = OKXUtility.ParseDecimal(obj["balance"]),
                Change = OKXUtility.ParseDecimal(obj["change"]),
                Text = GetString("text"),
                Time = GetLong("time"),
                TimeMs = GetLong("time_ms"),
                Type = GetString("type"),
                User = GetString("user"),
                Currency = GetString("currency")
            };
        }
    }
}
