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

            // Parse price strings to decimal
            var lowestAskStr = obj["lowest_ask"]?.ToString();
            var highestBidStr = obj["highest_bid"]?.ToString();

            // Support both "currency_pair" (Spot) and "contract" (Futures)
            var currencyPair = obj["currency_pair"]?.Type == JTokenType.Null ? null : obj["currency_pair"]?.ToString();
            if (string.IsNullOrEmpty(currencyPair))
            {
                currencyPair = obj["contract"]?.Type == JTokenType.Null ? null : obj["contract"]?.ToString();
            }

            // Smart mapping for volume: Try Spot field first, fallback to Futures field
            // Spot API returns "quote_volume" (USDT volume)
            // Futures API returns "volume_24h_settle" (USDT settlement volume)
            // Both represent the same concept: 24h USDT-denominated volume
            var quoteVolume = obj["quote_volume"]?.Type == JTokenType.Null ? null : obj["quote_volume"]?.ToString();
            if (string.IsNullOrEmpty(quoteVolume))
            {
                quoteVolume = obj["volume_24h_settle"]?.Type == JTokenType.Null ? null : obj["volume_24h_settle"]?.ToString();
            }

            // Validate required field
            if (string.IsNullOrEmpty(currencyPair))
            {
                // Ticker has no ID field, use "unknown" as identifier
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"TickerConverter: Ticker missing required field 'currency_pair' or 'contract'. Raw: {truncated}");
                return null;
            }

            return new Ticker
            {
                CurrencyPair = currencyPair,
                Last = obj["last"]?.Type == JTokenType.Null ? null : obj["last"]?.ToString(),
                LowestAsk = string.IsNullOrEmpty(lowestAskStr) ? 0 : decimal.Parse(lowestAskStr),
                HighestBid = string.IsNullOrEmpty(highestBidStr) ? 0 : decimal.Parse(highestBidStr),
                ChangePercentage = obj["change_percentage"]?.Type == JTokenType.Null ? null : obj["change_percentage"]?.ToString(),
                BaseVolume = obj["base_volume"]?.Type == JTokenType.Null ? null : obj["base_volume"]?.ToString(),
                QuoteVolume = quoteVolume,  // Unified field for both Spot and Futures
                High24h = obj["high_24h"]?.Type == JTokenType.Null ? null : obj["high_24h"]?.ToString(),
                Low24h = obj["low_24h"]?.Type == JTokenType.Null ? null : obj["low_24h"]?.ToString()
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
