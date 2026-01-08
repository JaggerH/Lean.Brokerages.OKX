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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// A custom JSON converter for the OKX <see cref="Trade"/> class
    /// </summary>
    public class TradeConverter : JsonConverter
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

            // Parse amount and price strings to decimal
            var amountStr = obj["amount"]?.ToString();
            var priceStr = obj["price"]?.ToString();

            // Support both "currency_pair" (Spot) and "contract" (Futures)
            var currencyPair = obj["currency_pair"]?.Type == JTokenType.Null ? null : obj["currency_pair"]?.ToString();
            if (string.IsNullOrEmpty(currencyPair))
            {
                currencyPair = obj["contract"]?.Type == JTokenType.Null ? null : obj["contract"]?.ToString();
            }

            // Validate required field
            if (string.IsNullOrEmpty(currencyPair))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"TradeConverter: Trade Tick missing required field 'currency_pair' or 'contract'. Raw: {truncated}");
                return null;
            }

            return new Trade
            {
                Id = obj["id"]?.Type == JTokenType.Null ? 0 : (long)obj["id"],
                CurrencyPair = currencyPair,
                CreateTime = obj["create_time"]?.Type == JTokenType.Null ? 0 : (long)obj["create_time"],
                CreateTimeMs = obj["create_time_ms"]?.Type == JTokenType.Null ? null : obj["create_time_ms"]?.ToString(),
                Side = obj["side"]?.Type == JTokenType.Null ? null : obj["side"]?.ToString(),
                Amount = string.IsNullOrEmpty(amountStr) ? 0 : decimal.Parse(amountStr),
                Price = string.IsNullOrEmpty(priceStr) ? 0 : decimal.Parse(priceStr)
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Trade);
        }
    }

    /// <summary>
    /// Domain converter extensions for Trade
    /// Converts OKX Trade messages to LEAN Tick objects
    /// </summary>
    public static class TradeExtensions
    {
        /// <summary>
        /// Converts a OKX Trade to a LEAN Tick
        /// </summary>
        /// <param name="trade">OKX trade message (assumed valid, validated by TradeConverter)</param>
        /// <param name="symbolMapper">Symbol mapper for converting symbols</param>
        /// <param name="securityType">Security type (Crypto or CryptoFuture)</param>
        /// <returns>LEAN Tick object</returns>
        public static Tick ToTick(
            this Trade trade,
            ISymbolMapper symbolMapper,
            SecurityType securityType)
        {

            // Convert to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(
                trade.CurrencyPair,
                securityType,
                Market.OKX);

            // Create tick
            return new Tick
            {
                Symbol = symbol,
                Time = OKXUtility.UnixSecondsToDateTime(trade.CreateTime),
                Value = trade.Price,
                Quantity = Math.Abs(trade.Amount),
                TickType = TickType.Trade
            };
        }
    }
}
