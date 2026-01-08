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
    /// Spot market currency pair information from OKX
    /// Represents a trading pair available on the Spot exchange
    /// </summary>
    [JsonConverter(typeof(SpotCurrencyPairConverter))]
    public class SpotCurrencyPair
    {
        /// <summary>
        /// Currency pair ID (e.g., "BTC_USDT")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Base currency (e.g., "BTC")
        /// </summary>
        public string Base { get; set; }

        /// <summary>
        /// Base currency full name (e.g., "Bitcoin" or "Apple xStock")
        /// Used for tokenized stock detection and descriptions
        /// </summary>
        public string BaseName { get; set; }

        /// <summary>
        /// Quote currency (e.g., "USDT")
        /// </summary>
        public string Quote { get; set; }

        /// <summary>
        /// Trading status ("tradable", "untradable", "buyable", "sellable")
        /// </summary>
        public string TradeStatus { get; set; }

        /// <summary>
        /// Minimum base currency amount for orders
        /// </summary>
        public decimal MinBaseAmount { get; set; }

        /// <summary>
        /// Minimum quote currency amount for orders
        /// </summary>
        public decimal MinQuoteAmount { get; set; }

        /// <summary>
        /// Amount precision (number of decimal places for quantity)
        /// </summary>
        public int AmountPrecision { get; set; }

        /// <summary>
        /// Price precision (number of decimal places for price)
        /// </summary>
        public int Precision { get; set; }
    }
}
