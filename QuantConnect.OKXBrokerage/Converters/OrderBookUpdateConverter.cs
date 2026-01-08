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
    /// A custom JSON converter for the OKX <see cref="OrderBookUpdate"/> class
    /// Handles parsing of incremental order book updates from WebSocket messages
    /// </summary>
    public class OrderBookUpdateConverter : JsonConverter
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

            var timestampMs = obj["t"]?.Type == JTokenType.Null ? 0 : (long)obj["t"];
            var time = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime;

            // Parse bid and ask arrays
            var bids = ParsePriceLevels(obj["b"]);
            var asks = ParsePriceLevels(obj["a"]);

            // Support both "s" (Spot: symbol/currency_pair) and "c" (Futures: contract)
            var currencyPair = obj["s"]?.Type == JTokenType.Null ? null : obj["s"]?.ToString();
            if (string.IsNullOrEmpty(currencyPair))
            {
                currencyPair = obj["c"]?.Type == JTokenType.Null ? null : obj["c"]?.ToString();
            }

            return new OrderBookUpdate
            {
                TimestampMs = timestampMs,
                Time = time,
                CurrencyPair = currencyPair,
                FirstUpdateId = obj["U"]?.Type == JTokenType.Null ? 0 : (long)obj["U"],
                LastUpdateId = obj["u"]?.Type == JTokenType.Null ? 0 : (long)obj["u"],
                Full = obj["full"]?.Type == JTokenType.Null ? false : (bool?)obj["full"] ?? false,
                Level = obj["l"]?.Type == JTokenType.Null ? null : obj["l"]?.ToString(),
                Bids = bids,
                Asks = asks
            };
        }

        /// <summary>
        /// Parses price levels from JSON, handling both Spot and Futures formats
        /// - Spot: [["price", "size"], ...]
        /// - Futures: [{"p": "price", "s": size}, ...]
        /// </summary>
        private List<List<string>> ParsePriceLevels(JToken token)
        {
            var result = new List<List<string>>();

            if (token == null || token.Type == JTokenType.Null || !token.HasValues)
            {
                return result;
            }

            foreach (var level in token)
            {
                if (level.Type == JTokenType.Array && level.HasValues)
                {
                    // Spot format: ["price", "size"]
                    var priceLevel = new List<string>();
                    foreach (var item in level)
                    {
                        priceLevel.Add(item.ToString());
                    }

                    if (priceLevel.Count >= 2)
                    {
                        result.Add(priceLevel);
                    }
                }
                else if (level.Type == JTokenType.Object)
                {
                    // Futures format: {"p": "price", "s": size}
                    var obj = (JObject)level;
                    var price = obj["p"]?.ToString();
                    var size = obj["s"]?.ToString();

                    if (!string.IsNullOrEmpty(price) && !string.IsNullOrEmpty(size))
                    {
                        // Convert to Spot format for consistency
                        result.Add(new List<string> { price, size });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OrderBookUpdate);
        }
    }
}
