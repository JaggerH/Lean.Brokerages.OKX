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

using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.Converters;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Represents OKX v5 API candlestick/K-line data
    /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-candlesticks
    ///
    /// Response format: Array of arrays
    /// [
    ///   ["1597026383085", "3.721", "3.743", "3.677", "3.708", "8422410", "22698348.04828491", "22698348.04828491", "0"]
    /// ]
    ///
    /// Fields (in order):
    /// 0: ts - Timestamp (Unix milliseconds)
    /// 1: o - Open price
    /// 2: h - High price
    /// 3: l - Low price
    /// 4: c - Close price
    /// 5: vol - Trading volume (base currency)
    /// 6: volCcy - Trading volume (quote currency)
    /// 7: volCcyQuote - Trading volume (quote currency, alternative field)
    /// 8: confirm - State of candlesticks (0: incomplete, 1: complete)
    /// </summary>
    [JsonConverter(typeof(CandleConverter))]
    public class Candle
    {
        /// <summary>
        /// Timestamp in Unix milliseconds
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Open price
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Close price
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Trading volume in base currency
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Trading volume in quote currency
        /// </summary>
        public decimal VolumeQuote { get; set; }

        /// <summary>
        /// State of candlesticks: false = incomplete, true = complete
        /// </summary>
        public bool Confirm { get; set; }
    }
}
