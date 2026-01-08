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
        /// Generates HMAC-SHA512 signature
        /// </summary>
        /// <param name="message">Message to sign</param>
        /// <param name="secret">Secret key</param>
        /// <returns>Hex-encoded signature string</returns>
        public static string GenerateHmacSignature(string message, string secret)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return ByteArrayToHexString(hashBytes);
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
        // ORDER TYPE CONVERSIONS
        // ========================================

        /// <summary>
        /// Converts LEAN OrderType to OKX order type string
        /// </summary>
        /// <param name="orderType">LEAN order type</param>
        /// <returns>OKX order type</returns>
        public static string ConvertOrderType(OrderType orderType)
        {
            return orderType switch
            {
                OrderType.Limit => "limit",
                OrderType.Market => "market",
                _ => throw new NotSupportedException($"Unsupported order type: {orderType}")
            };
        }

        /// <summary>
        /// Converts LEAN Order direction to OKX side string
        /// </summary>
        /// <param name="direction">Order direction</param>
        /// <returns>"buy" or "sell"</returns>
        public static string ConvertOrderDirection(OrderDirection direction)
        {
            return direction switch
            {
                OrderDirection.Buy => "buy",
                OrderDirection.Sell => "sell",
                _ => throw new ArgumentException($"Invalid direction: {direction}")
            };
        }

        /// <summary>
        /// Converts OKX order status to LEAN OrderStatus
        /// </summary>
        /// <param name="okxStatus">OKX status string</param>
        /// <returns>LEAN OrderStatus</returns>
        public static OrderStatus ConvertOrderStatus(string okxStatus)
        {
            return okxStatus?.ToLowerInvariant() switch
            {
                "open" => OrderStatus.Submitted,
                "finished" => OrderStatus.Filled,
                "cancelled" => OrderStatus.Canceled,
                "closed" => OrderStatus.Filled,
                _ => OrderStatus.None
            };
        }

        /// <summary>
        /// Gets the price to submit for an order based on order type
        /// </summary>
        /// <param name="order">LEAN order</param>
        /// <returns>Price to submit</returns>
        public static decimal GetOrderPrice(QuantConnect.Orders.Order order)
        {
            return order.Type switch
            {
                OrderType.Limit => ((LimitOrder)order).LimitPrice,
                OrderType.Market => 0, // Market orders don't need price on OKX
                _ => throw new NotSupportedException($"Unsupported order type: {order.Type}")
            };
        }

        // ========================================
        // TIME CONVERSIONS
        // ========================================

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts Unix timestamp (seconds) to DateTime
        /// </summary>
        /// <param name="seconds">Unix timestamp in seconds</param>
        /// <returns>DateTime</returns>
        public static DateTime UnixSecondsToDateTime(long seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }

        /// <summary>
        /// Converts Unix timestamp (milliseconds) to DateTime
        /// </summary>
        /// <param name="milliseconds">Unix timestamp in milliseconds</param>
        /// <returns>DateTime</returns>
        public static DateTime UnixMillisecondsToDateTime(long milliseconds)
        {
            return UnixEpoch.AddMilliseconds(milliseconds);
        }

        /// <summary>
        /// Converts DateTime to Unix timestamp (seconds)
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>Unix timestamp in seconds</returns>
        public static long DateTimeToUnixSeconds(DateTime dateTime)
        {
            return (long)(dateTime - UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Converts DateTime to Unix timestamp (milliseconds)
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>Unix timestamp in milliseconds</returns>
        public static long DateTimeToUnixMilliseconds(DateTime dateTime)
        {
            return (long)(dateTime - UnixEpoch).TotalMilliseconds;
        }

        // ========================================
        // SYMBOL CONVERSIONS
        // ========================================

        /// <summary>
        /// Converts LEAN Symbol format to OKX format
        /// Example: BTCUSDT -> BTC_USDT
        /// </summary>
        /// <param name="leanSymbol">LEAN symbol string</param>
        /// <returns>OKX symbol format</returns>
        public static string ConvertToOKXSymbol(string leanSymbol)
        {
            // For most symbols, insert underscore before "USDT" or "BTC"
            // This is a simplified version - production code should use SymbolMapper
            if (leanSymbol.EndsWith("USDT"))
            {
                return leanSymbol.Replace("USDT", "_USDT");
            }
            else if (leanSymbol.EndsWith("BTC"))
            {
                return leanSymbol.Replace("BTC", "_BTC");
            }

            return leanSymbol;
        }

        /// <summary>
        /// Converts OKX symbol format to LEAN format
        /// Example: BTC_USDT -> BTCUSDT
        /// </summary>
        /// <param name="okxSymbol">OKX symbol string</param>
        /// <returns>LEAN symbol format</returns>
        public static string ConvertToLeanSymbol(string okxSymbol)
        {
            return okxSymbol.Replace("_", "");
        }

        // ========================================
        // RESOLUTION CONVERSIONS
        // ========================================

        /// <summary>
        /// Converts LEAN Resolution to OKX candlestick interval string
        /// </summary>
        /// <param name="resolution">LEAN Resolution</param>
        /// <returns>OKX interval string</returns>
        public static string ConvertResolution(Resolution resolution)
        {
            return resolution switch
            {
                Resolution.Second => "10s",
                Resolution.Minute => "1m",
                Resolution.Hour => "1h",
                Resolution.Daily => "1d",
                _ => throw new ArgumentException($"Unsupported resolution: {resolution}")
            };
        }

        // ========================================
        // VALIDATION HELPERS
        // ========================================

        /// <summary>
        /// Validates minimum order quantity for OKX
        /// </summary>
        /// <param name="quantity">Order quantity</param>
        /// <returns>True if valid</returns>
        public static bool ValidateMinimumOrderQuantity(decimal quantity)
        {
            // OKX minimum varies by symbol
            // This is a simplified version
            return quantity >= 0.0001m;
        }

        /// <summary>
        /// Validates minimum order value (10 USDT for Spot)
        /// </summary>
        /// <param name="quantity">Order quantity</param>
        /// <param name="price">Order price</param>
        /// <returns>True if valid</returns>
        public static bool ValidateMinimumOrderValue(decimal quantity, decimal price)
        {
            return quantity * price >= 10m;
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

            // Mark that we need to send Reconnect notification on next successful auth
            _reconnectNotificationPending = true;

            // Notify LEAN engine about disconnection to start reconnect timer
            OnMessage(new BrokerageMessageEvent(
                BrokerageMessageType.Disconnect,
                code,
                reason
            ));

            // Close WebSocket to trigger framework's automatic reconnection
            WebSocket?.Close();
        }
    }
}
