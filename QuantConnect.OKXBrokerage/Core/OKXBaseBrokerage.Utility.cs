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
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Pure static utility methods for OKX Brokerage
    /// NO BUSINESS LOGIC - only type conversions and cryptographic helpers
    /// </summary>
    public static class OKXUtility
    {
        // ========================================
        // CRYPTOGRAPHIC HELPERS
        // ========================================

        /// <summary>
        /// Computes SHA512 hash of payload
        /// </summary>
        /// <param name="payload">String to hash</param>
        /// <returns>Hex-encoded hash string</returns>
        public static string ComputeSha512Hash(string payload)
        {
            using (var sha512 = SHA512.Create())
            {
                var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return ByteArrayToHexString(hashBytes);
            }
        }

        /// <summary>
        /// Generates HMAC-SHA256 signature for OKX REST API
        /// OKX uses Base64 encoding (not Hex)
        /// </summary>
        /// <param name="message">Message to sign</param>
        /// <param name="secret">Secret key</param>
        /// <returns>Base64-encoded signature string</returns>
        public static string GenerateHmacSignature(string message, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Converts byte array to hex string
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <returns>Hex string</returns>
        public static string ByteArrayToHexString(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            }
            return hex.ToString();
        }

        // ========================================
        // ORDER STATUS CONVERSION
        // ========================================

        /// <summary>
        /// Converts OKX order status to LEAN OrderStatus
        /// OKX v5 API order states:
        /// - canceled: Order canceled successfully
        /// - live: Waiting to be filled
        /// - partially_filled: Partially filled
        /// - filled: Completely filled
        /// - mmp_canceled: Auto-canceled by Market Maker Protection
        /// </summary>
        /// <param name="okxStatus">OKX status string</param>
        /// <returns>LEAN OrderStatus</returns>
        public static OrderStatus ConvertOrderStatus(string okxStatus)
        {
            return okxStatus?.ToLowerInvariant() switch
            {
                "live" => OrderStatus.Submitted,
                "partially_filled" => OrderStatus.PartiallyFilled,
                "filled" => OrderStatus.Filled,
                "canceled" => OrderStatus.Canceled,
                "mmp_canceled" => OrderStatus.Canceled,
                _ => OrderStatus.None
            };
        }

        // ========================================
        // TIME CONVERSIONS
        // ========================================

        /// <summary>
        /// Converts Unix timestamp (seconds) to DateTime
        /// Uses DateTimeOffset for reliable UTC conversion
        /// </summary>
        /// <param name="seconds">Unix timestamp in seconds</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime UnixSecondsToDateTime(long seconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        /// <summary>
        /// Converts Unix timestamp (milliseconds) to DateTime
        /// Uses DateTimeOffset for reliable UTC conversion
        /// </summary>
        /// <param name="milliseconds">Unix timestamp in milliseconds</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime UnixMillisecondsToDateTime(long milliseconds)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }

        /// <summary>
        /// Converts DateTime to Unix timestamp (seconds)
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>Unix timestamp in seconds</returns>
        public static long DateTimeToUnixSeconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Converts DateTime to Unix timestamp (milliseconds)
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>Unix timestamp in milliseconds</returns>
        public static long DateTimeToUnixMilliseconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        // ========================================
        // JSON PARSING HELPERS
        // ========================================

        /// <summary>
        /// Safely parses a JToken to decimal with InvariantCulture
        /// Returns 0 if the token is null, empty, or invalid
        /// </summary>
        /// <param name="token">JToken to parse (from obj[field])</param>
        /// <returns>Parsed decimal value or 0 if invalid</returns>
        public static decimal ParseDecimal(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return 0m;

            var str = token.ToString();
            if (string.IsNullOrEmpty(str))
                return 0m;

            if (decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0m;
        }

        /// <summary>
        /// Safely parses a JToken to boolean
        /// Handles both boolean types and string representations ("true", "false", "1", "0")
        /// Returns false if the token is null or invalid
        /// </summary>
        /// <param name="token">JToken to parse (from obj[field])</param>
        /// <returns>Parsed boolean value or false if invalid</returns>
        public static bool ParseBool(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return false;

            if (token.Type == JTokenType.Boolean)
                return token.Value<bool>();

            // Handle string representations
            var str = token.ToString().ToLowerInvariant();
            return str == "true" || str == "1";
        }

        // ========================================
        // RESPONSE FLATTENING HELPERS
        // ========================================

        /// <summary>
        /// Flattens a nested API response by extracting items from wrapper objects.
        /// Used when OKX API returns grouped data structures (e.g., Spot open_orders groups by currency_pair).
        /// </summary>
        /// <typeparam name="TWrapper">The wrapper type containing grouped data</typeparam>
        /// <typeparam name="TItem">The item type to extract</typeparam>
        /// <param name="wrappers">Collection of wrapper objects (can be null)</param>
        /// <param name="selector">Function to extract items from each wrapper</param>
        /// <returns>Flattened list of all items, or empty list if input is null/empty</returns>
        /// <example>
        /// // Example: Flatten Spot open orders grouped by currency pair
        /// var responseGroups = Deserialize&lt;List&lt;SpotOpenOrdersResponse&gt;&gt;(json);
        /// var allOrders = OKXUtility.FlattenResponse(responseGroups, group => group.Orders);
        /// </example>
        public static List<TItem> FlattenResponse<TWrapper, TItem>(
            IEnumerable<TWrapper> wrappers,
            Func<TWrapper, IEnumerable<TItem>> selector)
        {
            if (wrappers == null)
            {
                return new List<TItem>();
            }

            return wrappers
                .Where(wrapper => selector(wrapper) != null)
                .SelectMany(selector)
                .ToList();
        }

        /// <summary>
        /// Normalizes OKX WebSocket message result field to always return an array
        /// Handles both single object and array formats (Spot vs Futures API differences)
        /// Filters out null objects (invalid data rejected by converters)
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="result">The JToken result field from WebSocket message</param>
        /// <returns>List of deserialized objects (null objects filtered out), empty list if null or error</returns>
        public static List<T> NormalizeResultToArray<T>(JToken result) where T : class
        {
            if (result == null || !result.HasValues)
            {
                return new List<T>();
            }

            try
            {
                if (result.Type == JTokenType.Array)
                {
                    // 直接从 JArray 转换,避免 ToString() 的序列化开销
                    // 过滤 null 对象 (Converter 验证失败返回的 null)
                    return result.ToObject<List<T>>()
                        ?.Where(item => item != null)
                        .ToList()
                        ?? new List<T>();
                }
                else
                {
                    // 单个对象包装为列表
                    var singleItem = result.ToObject<T>();
                    return singleItem != null ? new List<T> { singleItem } : new List<T>();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"OKXUtility.NormalizeResultToArray<{typeof(T).Name}>(): Deserialization error: {ex.Message}");
                return new List<T>();
            }
        }
    }

    // ========================================
    // BROKERAGE TAG ENCODING
    // ========================================

    /// <summary>
    /// Encodes LEAN order Tag to OKX compatible text field format using hash.
    ///
    /// OKX text field requirements:
    /// - Must start with "t-"
    /// - Max 28 bytes (excluding "t-" prefix)
    /// - Only alphanumeric, underscore (_), hyphen (-), and dot (.)
    ///
    /// Format: t-TPGPH-{22 char hash}
    /// - TPGPH = TradingPairGridPositionHash (marker for decode guidance)
    /// - Total: 6 (TPGPH-) + 22 (hash) = 28 bytes
    ///
    /// Decode is handled by TradingPairManager.Reconciliation.ConvertToOrderEvent()
    /// which matches hash against known GridPosition tags.
    /// </summary>
    public static class BrokerageTagEncoder
    {
        private const int HashLength = 22;
        private const string Prefix = "t-";
        private const string Marker = "TPGPH-";

        /// <summary>
        /// Encodes a LEAN Tag to OKX compatible format using hash.
        /// Output format: t-TPGPH-{22 char hash}
        /// </summary>
        /// <param name="leanTag">Original LEAN order tag</param>
        /// <param name="orderId">Order ID as fallback when tag is empty</param>
        /// <returns>OKX compatible text field value</returns>
        public static string Encode(string leanTag, int orderId)
        {
            // If no tag, use order ID directly
            if (string.IsNullOrEmpty(leanTag))
            {
                return $"{Prefix}{orderId}";
            }

            // Compute hash: t-TPGPH-{22 char hash} = t- + 28 bytes
            var hash = ComputeHash(leanTag);
            return $"{Prefix}{Marker}{hash}";
        }

        /// <summary>
        /// Computes a 22-character SHA256 hash (88 bits)
        /// Collision probability: ~2^44 operations for 50% collision (birthday paradox)
        /// </summary>
        private static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var fullHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return fullHash.Substring(0, HashLength);
            }
        }
    }

    /// <summary>
    /// OKXBaseBrokerage partial class - Utility methods
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        /// <summary>
        /// Triggers WebSocket reconnection by sending Disconnect event and closing the connection.
        /// LEAN engine will wait 15 minutes for Reconnect before stopping the algorithm.
        /// </summary>
        /// <param name="code">Error code for the disconnect event</param>
        /// <param name="reason">Reason for triggering reconnection</param>
        protected void TriggerReconnect(string code, string reason)
        {
            Log.Error($"{GetType().Name}.TriggerReconnect(): [{code}] {reason}");

            // Notify LEAN engine about disconnection to start reconnect timer
            OnMessage(new BrokerageMessageEvent(
                BrokerageMessageType.Disconnect,
                code,
                reason
            ));

            // Close WebSocket to trigger framework's automatic reconnection
            WebSocket?.Close();
        }

        /// <summary>
        /// Determines the security type from OKX instrument ID
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., "BTC-USDT", "BTC-USDT-SWAP", "BTC-USD-230630")</param>
        /// <returns>SecurityType.Crypto for spot, SecurityType.CryptoFuture for futures/swaps</returns>
        protected SecurityType GetSecurityType(string instId)
        {
            if (string.IsNullOrEmpty(instId))
            {
                return SecurityType.Crypto;
            }

            // OKX Instrument ID formats:
            // Spot: BTC-USDT
            // Perpetual Swap: BTC-USDT-SWAP
            // Futures: BTC-USD-230630
            if (instId.Contains("-SWAP") || instId.Contains("-FUTURES") ||
                (instId.Split('-').Length == 3 && !instId.EndsWith("-SWAP")))
            {
                return SecurityType.CryptoFuture;
            }

            return SecurityType.Crypto;
        }
    }
}
