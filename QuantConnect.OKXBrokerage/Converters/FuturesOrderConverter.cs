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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Custom JSON converter for OKX Futures Order messages
    /// Handles futures.orders WebSocket channel messages
    /// </summary>
    public class FuturesOrderConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FuturesOrder);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            // Helper function for safe field extraction
            long GetLong(string field) => obj[field] == null || obj[field].Type == JTokenType.Null ? 0 : (long)obj[field];
            string GetString(string field) => obj[field] == null || obj[field].Type == JTokenType.Null ? null : obj[field].ToString();
            bool GetBool(string field) => obj[field] != null && obj[field].Type != JTokenType.Null && (bool)obj[field];

            // Extract and validate required fields
            var id = GetLong("id");
            var contract = GetString("contract");
            var status = GetString("status");

            // Validate 'id' field (required for order identification)
            if (id == 0)
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"FuturesOrderConverter: Order missing required field 'id'. Raw: {truncated}");
                return null;
            }

            // Validate 'contract' field (required for symbol mapping)
            if (string.IsNullOrEmpty(contract))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"FuturesOrderConverter: Order {id} missing required field 'contract'. Raw: {truncated}");
                return null;
            }

            // Validate 'status' field (required for order state tracking)
            if (string.IsNullOrEmpty(status))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"FuturesOrderConverter: Order {id} missing required field 'status'. Raw: {truncated}");
                return null;
            }

            return new FuturesOrder
            {
                Id = GetLong("id").ToStringInvariant(),
                Contract = GetString("contract"),
                ClientOrderId = GetString("text"),
                CreateTimeMs = GetLong("create_time_ms"),
                CreateTime = GetLong("create_time"),
                FinishTimeMs = GetLong("finish_time_ms"),
                FinishTime = GetLong("finish_time"),
                UpdateTime = GetLong("update_time"),
                Status = GetString("status"),
                Size = GetLong("size"),
                Price = GetString("price"),
                FillPrice = GetString("fill_price"),
                Mkfr = GetString("mkfr"),
                Tkfr = GetString("tkfr"),
                Left = GetLong("left"),
                TimeInForce = GetString("tif"),
                Iceberg = GetLong("iceberg"),
                IsClose = GetBool("is_close"),
                IsLiq = GetBool("is_liq"),
                IsReduceOnly = GetBool("is_reduce_only"),
                FinishAs = GetString("finish_as"),
                User = GetString("user"),
                Refu = GetLong("refu"),
                Refr = GetString("refr"),
                StpId = GetString("stp_id"),
                StpAct = GetString("stp_act"),
                AmendText = GetString("amend_text"),
                UpdateId = GetLong("update_id"),
                BizInfo = GetString("biz_info"),
                StopProfitPrice = GetString("stop_profit_price"),
                StopLossPrice = GetString("stop_loss_price"),
                AutoSize = GetString("auto_size")
            };
        }
    }

    /// <summary>
    /// Domain converter extensions for FuturesOrder
    /// </summary>
    public static class FuturesOrderExtensions
    {
        /// <summary>
        /// Converts a OKX Futures order to a LEAN order
        /// </summary>
        /// <param name="okxOrder">OKX futures order</param>
        /// <param name="symbolMapper">Symbol mapper for converting OKX symbols to LEAN symbols</param>
        /// <returns>LEAN Order object or null if conversion fails</returns>
        public static Order ToLeanOrder(this FuturesOrder okxOrder, ISymbolMapper symbolMapper)
        {
            if (okxOrder == null || string.IsNullOrEmpty(okxOrder.Contract))
            {
                return null;
            }

            // Convert OKX symbol to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(okxOrder.Contract, SecurityType.CryptoFuture, Market.OKX);

            // For Futures, Size field contains the contract size
            // The size can be positive (long) or negative (short)
            var size = okxOrder.Size;

            // Parse price and time in force
            var price = decimal.Parse(okxOrder.Price);
            var tif = okxOrder.TimeInForce;

            // Parse creation time (prefer milliseconds, fallback to seconds)
            var createTime = okxOrder.CreateTimeMs > 0
                ? OKXUtility.UnixMillisecondsToDateTime(okxOrder.CreateTimeMs)
                : OKXUtility.UnixSecondsToDateTime(okxOrder.CreateTime);

            // Determine order type based on OKX official rule:
            // Market order: price == 0 AND tif == "ioc"
            // Limit order: all other cases
            Order leanOrder;
            if (price == 0 && tif == "ioc")
            {
                leanOrder = new MarketOrder(symbol, size, createTime);
            }
            else
            {
                leanOrder = new LimitOrder(symbol, size, price, createTime);
            }

            // Set brokerage ID and status
            leanOrder.BrokerId.Add(okxOrder.Id.ToString());
            leanOrder.Status = OKXUtility.ConvertOrderStatus(okxOrder.Status);

            return leanOrder;
        }

        /// <summary>
        /// Converts a OKX Futures order amend response to a LEAN OrderEvent
        /// Used for handling futures.order_amend WebSocket messages
        /// </summary>
        /// <param name="okxOrder">OKX futures order (assumed valid, validated by FuturesOrderConverter)</param>
        /// <param name="leanOrder">The LEAN order being amended</param>
        /// <returns>LEAN OrderEvent object</returns>
        public static OrderEvent ToOrderAmendEvent(this FuturesOrder okxOrder, Order leanOrder)
        {
            // ========================================
            // NO VALIDATION NEEDED
            // ========================================
            // Required fields (id, contract, status) are guaranteed non-null by FuturesOrderConverter validation
            // Converter returns null if required fields are missing
            // NormalizeResultToArray filters out null objects

            // Convert OKX status to LEAN OrderStatus
            var orderStatus = OKXUtility.ConvertOrderStatus(okxOrder.Status);

            // Build amend message with updated details
            var amendedMessage = string.IsNullOrEmpty(leanOrder.Tag)
                ? $"Amended: size={okxOrder.Size}, price={okxOrder.Price}"
                : $"{leanOrder.Tag} | Amended: size={okxOrder.Size}, price={okxOrder.Price}";

            // Create OrderEvent
            return new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero)
            {
                Status = orderStatus,
                Message = amendedMessage
            };
        }
    }
}
