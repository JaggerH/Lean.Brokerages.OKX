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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Custom JSON converter for OKX BookTicker messages
    /// Handles futures.book_ticker WebSocket channel messages
    /// </summary>
    public class BookTickerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BookTicker);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            // Extract fields
            var contract = obj["s"]?.ToString();
            var bestBidStr = obj["b"]?.ToString();
            var bestAskStr = obj["a"]?.ToString();

            // Validate required field
            if (string.IsNullOrEmpty(contract))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"BookTickerConverter: BookTicker missing required field 's' (contract). Raw: {truncated}");
                return null;
            }

            // Parse price strings to decimal
            return new BookTicker
            {
                Contract = contract,
                BestBid = string.IsNullOrEmpty(bestBidStr) ? 0 : decimal.Parse(bestBidStr),
                BestAsk = string.IsNullOrEmpty(bestAskStr) ? 0 : decimal.Parse(bestAskStr),
                BestBidSize = obj["B"]?.ToString(),
                BestAskSize = obj["A"]?.ToString(),
                Timestamp = obj["t"]?.ToObject<long>() ?? 0,
                UpdateId = obj["u"]?.ToString()
            };
        }
    }

    /// <summary>
    /// Domain converter extensions for BookTicker
    /// Converts OKX BookTicker messages to LEAN Tick objects (Quote type)
    /// </summary>
    public static class BookTickerExtensions
    {
        /// <summary>
        /// Converts a OKX BookTicker to a LEAN Quote Tick
        /// </summary>
        /// <param name="bookTicker">OKX book ticker message (assumed valid, validated by BookTickerConverter)</param>
        /// <param name="symbolMapper">Symbol mapper for converting symbols</param>
        /// <param name="securityType">Security type (Crypto or CryptoFuture)</param>
        /// <returns>LEAN Quote Tick object</returns>
        public static Tick ToQuoteTick(
            this BookTicker bookTicker,
            ISymbolMapper symbolMapper,
            SecurityType securityType)
        {
            // Contract is guaranteed non-null by BookTickerConverter validation
            // Converter returns null if Contract is missing
            // NormalizeResultToArray filters out null objects

            // Convert to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(
                bookTicker.Contract,
                securityType,
                Market.OKX);

            // Create quote tick with best bid/ask prices
            return new Tick
            {
                Symbol = symbol,
                Time = DateTime.UtcNow,
                BidPrice = bookTicker.BestBid,
                AskPrice = bookTicker.BestAsk,
                Value = (bookTicker.BestBid + bookTicker.BestAsk) / 2,  // Mid-price
                TickType = TickType.Quote
            };
        }
    }
}
