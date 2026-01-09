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
    /// A custom JSON converter for the OKX <see cref="Ticker"/> class
    /// </summary>
    public class TickerConverter : JsonConverter
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

            // OKX v5 API field names:
            // instId: Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)
            // askPx: Best ask price
            // bidPx: Best bid price
            // vol24h: 24h base currency volume
            // volCcy24h: 24h quote currency volume
            // high24h: 24h high
            // low24h: 24h low

            var instId = obj["instId"]?.Type == JTokenType.Null ? null : obj["instId"]?.ToString();
            var askPxStr = obj["askPx"]?.ToString();
            var bidPxStr = obj["bidPx"]?.ToString();

            // Validate required field
            if (string.IsNullOrEmpty(instId))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"TickerConverter: Ticker missing required field 'instId'. Raw: {truncated}");
                return null;
            }

            return new Ticker
            {
                CurrencyPair = instId,  // OKX v5 uses instId
                Last = obj["last"]?.Type == JTokenType.Null ? null : obj["last"]?.ToString(),
                LowestAsk = string.IsNullOrEmpty(askPxStr) ? 0 : decimal.Parse(askPxStr),
                HighestBid = string.IsNullOrEmpty(bidPxStr) ? 0 : decimal.Parse(bidPxStr),
                ChangePercentage = obj["changePercentage"]?.Type == JTokenType.Null ? null : obj["changePercentage"]?.ToString(),
                BaseVolume = obj["vol24h"]?.Type == JTokenType.Null ? null : obj["vol24h"]?.ToString(),
                QuoteVolume = obj["volCcy24h"]?.Type == JTokenType.Null ? null : obj["volCcy24h"]?.ToString(),
                High24h = obj["high24h"]?.Type == JTokenType.Null ? null : obj["high24h"]?.ToString(),
                Low24h = obj["low24h"]?.Type == JTokenType.Null ? null : obj["low24h"]?.ToString()
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Ticker);
        }
    }

    /// <summary>
    /// Domain converter extensions for Ticker
    /// Converts OKX Ticker messages to LEAN Tick objects (Quote type)
    /// </summary>
    public static class TickerExtensions
    {
        /// <summary>
        /// Converts a OKX Ticker to a LEAN Quote Tick
        /// </summary>
        /// <param name="ticker">OKX ticker message (assumed valid, validated by TickerConverter)</param>
        /// <param name="symbolMapper">Symbol mapper for converting symbols</param>
        /// <param name="securityType">Security type (Crypto or CryptoFuture)</param>
        /// <returns>LEAN Quote Tick object</returns>
        public static Tick ToQuoteTick(
            this Ticker ticker,
            ISymbolMapper symbolMapper,
            SecurityType securityType)
        {
            // CurrencyPair is guaranteed non-null by TickerConverter validation
            // Converter returns null if CurrencyPair is missing
            // NormalizeResultToArray filters out null objects

            // Convert to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(
                ticker.CurrencyPair,
                securityType,
                Market.OKX);

            // Create quote tick with bid/ask prices
            return new Tick
            {
                Symbol = symbol,
                Time = DateTime.UtcNow,
                BidPrice = ticker.HighestBid,
                AskPrice = ticker.LowestAsk,
                Value = (ticker.HighestBid + ticker.LowestAsk) / 2,  // Mid-price
                TickType = TickType.Quote
            };
        }
    }
}
