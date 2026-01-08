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
    /// JSON converter for SpotCurrencyPair objects
    /// Converts OKX API response to business-oriented model
    /// </summary>
    public class SpotCurrencyPairConverter : JsonConverter
    {
        /// <summary>
        /// Reads JSON and converts to SpotCurrencyPair object
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);

            return new SpotCurrencyPair
            {
                Id = obj["id"]?.ToString(),
                Base = obj["base"]?.ToString(),
                BaseName = obj["base_name"]?.ToString(),
                Quote = obj["quote"]?.ToString(),
                TradeStatus = obj["trade_status"]?.ToString(),
                MinBaseAmount = obj["min_base_amount"]?.Value<decimal>() ?? 0m,
                MinQuoteAmount = obj["min_quote_amount"]?.Value<decimal>() ?? 0m,
                AmountPrecision = obj["amount_precision"]?.Value<int>() ?? 0,
                Precision = obj["precision"]?.Value<int>() ?? 0
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SpotCurrencyPair);
        }

        /// <summary>
        /// Writes JSON (not implemented - read-only converter)
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("SpotCurrencyPairConverter is read-only");
        }
    }
}
