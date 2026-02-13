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
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Custom JSON converter for OKX Candle data
    /// Converts array format to Candle object
    /// </summary>
    public class CandleConverter : JsonConverter
    {
        /// <summary>
        /// Gets whether this converter can write JSON
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes JSON (not implemented)
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads OKX candle array and converts to Candle object
        ///
        /// OKX format: ["1597026383085", "3.721", "3.743", "3.677", "3.708", "8422410", "22698348.048", "22698348.048", "0"]
        ///
        /// Indices:
        /// [0] = timestamp (ms)
        /// [1] = open
        /// [2] = high
        /// [3] = low
        /// [4] = close
        /// [5] = volume (base currency)
        /// [6] = volCcy (quote currency)
        /// [7] = volCcyQuote (alternative, we ignore this)
        /// [8] = confirm (0 = incomplete, 1 = complete)
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            if (array == null || array.Count < 9)
            {
                Log.Error($"CandleConverter: Invalid candle array length: {array?.Count ?? 0}, expected at least 9 elements");
                return null;
            }

            try
            {
                return new Candle
                {
                    Timestamp = ParseHelper.ParseLong(array[0]),
                    Open = ParseHelper.ParseDecimal(array[1]),
                    High = ParseHelper.ParseDecimal(array[2]),
                    Low = ParseHelper.ParseDecimal(array[3]),
                    Close = ParseHelper.ParseDecimal(array[4]),
                    Volume = ParseHelper.ParseDecimal(array[5]),
                    VolumeQuote = ParseHelper.ParseDecimal(array[6]),
                    Confirm = ParseHelper.ParseBool(array[8])
                };
            }
            catch (Exception ex)
            {
                Log.Error($"CandleConverter: Failed to parse candle data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Candle);
        }
    }

    /// <summary>
    /// Domain converter extensions for Candle
    /// Converts OKX Candle messages to LEAN TradeBar objects
    /// </summary>
    public static class CandleExtensions
    {
        /// <summary>
        /// Converts an OKX Candle to a LEAN TradeBar
        /// </summary>
        /// <param name="candle">OKX candle data</param>
        /// <param name="symbol">LEAN symbol</param>
        /// <param name="period">Bar period (resolution timespan)</param>
        /// <returns>LEAN TradeBar object</returns>
        public static TradeBar ToTradeBar(this Candle candle, Symbol symbol, TimeSpan period)
        {
            // Convert Unix milliseconds to DateTime
            var time = DateTimeOffset.FromUnixTimeMilliseconds(candle.Timestamp).UtcDateTime;

            return new TradeBar(
                time,
                symbol,
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close,
                candle.Volume,
                period
            );
        }
    }
}
