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
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Unified parsing helpers for OKX API responses.
    /// Provides safe parsing with consistent culture and default values.
    /// </summary>
    public static class ParseHelper
    {
        /// <summary>
        /// Safely parses a JToken to decimal with InvariantCulture.
        /// Returns 0 if the token is null, empty, or invalid.
        /// </summary>
        public static decimal ParseDecimal(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return 0m;

            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return token.Value<decimal>();

            var str = token.ToString();
            if (string.IsNullOrEmpty(str))
                return 0m;

            return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }

        /// <summary>
        /// Safely parses a string to decimal with InvariantCulture.
        /// Returns 0 if the string is null, empty, or invalid.
        /// </summary>
        public static decimal ParseDecimal(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0m;

            return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }

        /// <summary>
        /// Safely parses a JToken to long.
        /// Returns 0 if the token is null or invalid.
        /// </summary>
        public static long ParseLong(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return 0;

            if (token.Type == JTokenType.Integer)
                return token.Value<long>();

            var str = token.ToString();
            if (string.IsNullOrEmpty(str))
                return 0;

            return long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0;
        }

        /// <summary>
        /// Safely parses a string to long.
        /// Returns 0 if the string is null, empty, or invalid.
        /// </summary>
        public static long ParseLong(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            return long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0;
        }

        /// <summary>
        /// Safely extracts a string from a JToken.
        /// Returns null if the token is null or JTokenType.Null.
        /// </summary>
        public static string ParseString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            return token.ToString();
        }

        /// <summary>
        /// Safely parses a JToken to bool.
        /// Handles "true", "1" as true values.
        /// Returns false if the token is null or invalid.
        /// </summary>
        public static bool ParseBool(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return false;

            if (token.Type == JTokenType.Boolean)
                return token.Value<bool>();

            var str = token.ToString().ToLowerInvariant();
            return str == "true" || str == "1";
        }

        /// <summary>
        /// Converts Unix milliseconds to DateTime UTC.
        /// Returns DateTime.MinValue if parsing fails.
        /// </summary>
        public static DateTime ParseUnixMilliseconds(string str)
        {
            if (string.IsNullOrEmpty(str))
                return DateTime.MinValue;

            return long.TryParse(str, out var ms)
                ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
                : DateTime.MinValue;
        }

        /// <summary>
        /// Converts Unix milliseconds to DateTime UTC.
        /// </summary>
        public static DateTime ParseUnixMilliseconds(long ms)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }
    }
}
