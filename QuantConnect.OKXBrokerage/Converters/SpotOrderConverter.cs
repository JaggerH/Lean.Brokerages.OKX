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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Logging;

using LeanOrder = QuantConnect.Orders.Order;
using OKXOrder = QuantConnect.Brokerages.OKX.Messages.Order;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// A custom JSON converter for the OKX <see cref="SpotOrder"/> class
    /// </summary>
    public class SpotOrderConverter : JsonConverter
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

            // Extract and validate required fields
            var id = obj["id"]?.ToString();
            var currencyPair = obj["currency_pair"]?.ToString();
            var status = obj["status"]?.ToString();

            // Validate 'id' field (required for order identification)
            if (string.IsNullOrEmpty(id))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"SpotOrderConverter: Order missing required field 'id'. Raw: {truncated}");
                return null;
            }

            // Validate 'currency_pair' field (required for symbol mapping)
            if (string.IsNullOrEmpty(currencyPair))
            {
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"SpotOrderConverter: Order {id} missing required field 'currency_pair'. Raw: {truncated}");
                return null;
            }

            // Note: 'status' field is optional
            // - REST API (/spot/open_orders): status field exists (open/closed/cancelled)
            // - WebSocket API (spot.order_place/amend/cancel): status field does NOT exist
            // WebSocket handlers use hardcoded OrderStatus values or event/finish_as fields instead

            return new SpotOrder
            {
                Id = obj["id"]?.Type == JTokenType.Null ? null : obj["id"]?.ToString(),
                ClientOrderId = obj["text"]?.Type == JTokenType.Null ? null : obj["text"]?.ToString(),
                CurrencyPair = obj["currency_pair"]?.Type == JTokenType.Null ? null : obj["currency_pair"]?.ToString(),
                Type = obj["type"]?.Type == JTokenType.Null ? null : obj["type"]?.ToString(),
                Side = obj["side"]?.Type == JTokenType.Null ? null : obj["side"]?.ToString(),
                Amount = obj["amount"]?.Type == JTokenType.Null ? null : obj["amount"]?.ToString(),
                Price = obj["price"]?.Type == JTokenType.Null ? null : obj["price"]?.ToString(),
                Status = obj["status"]?.Type == JTokenType.Null ? null : obj["status"]?.ToString(),
                CreateTime = obj["create_time"]?.Type == JTokenType.Null ? 0 : (long)obj["create_time"],
                CreateTimeMs = obj["create_time_ms"]?.Type == JTokenType.Null ? null : obj["create_time_ms"]?.ToString(),
                UpdateTime = obj["update_time"]?.Type == JTokenType.Null ? 0 : (long)obj["update_time"],
                UpdateTimeMs = obj["update_time_ms"]?.Type == JTokenType.Null ? null : obj["update_time_ms"]?.ToString(),
                Event = obj["event"]?.Type == JTokenType.Null ? null : obj["event"]?.ToString(),
                Account = obj["account"]?.Type == JTokenType.Null ? null : obj["account"]?.ToString(),
                TimeInForce = obj["time_in_force"]?.Type == JTokenType.Null ? null : obj["time_in_force"]?.ToString(),
                FilledAmount = obj["filled_amount"]?.Type == JTokenType.Null ? null : obj["filled_amount"]?.ToString(),
                FilledTotal = obj["filled_total"]?.Type == JTokenType.Null ? null : obj["filled_total"]?.ToString(),
                Left = obj["left"]?.Type == JTokenType.Null ? null : obj["left"]?.ToString(),
                Fee = obj["fee"]?.Type == JTokenType.Null ? null : obj["fee"]?.ToString(),
                FeeCurrency = obj["fee_currency"]?.Type == JTokenType.Null ? null : obj["fee_currency"]?.ToString(),
                FinishAs = obj["finish_as"]?.Type == JTokenType.Null ? null : obj["finish_as"]?.ToString()
            };
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SpotOrder);
        }
    }

    /// <summary>
    /// Domain converter extensions for SpotOrder
    /// </summary>
    public static class SpotOrderExtensions
    {
        /// <summary>
        /// Converts a OKX Spot order to a LEAN order
        /// </summary>
        /// <param name="okxOrder">OKX spot order</param>
        /// <param name="symbolMapper">Symbol mapper for converting OKX symbols to LEAN symbols</param>
        /// <returns>LEAN Order object or null if conversion fails</returns>
        public static LeanOrder ToLeanOrder(this SpotOrder okxOrder, ISymbolMapper symbolMapper)
        {
            if (okxOrder == null || string.IsNullOrEmpty(okxOrder.CurrencyPair))
            {
                return null;
            }

            // Convert OKX symbol to LEAN symbol (BTC_USDT -> BTCUSDT)
            var symbol = symbolMapper.GetLeanSymbol(okxOrder.CurrencyPair, SecurityType.Crypto, Market.OKX);

            // Parse quantity and price
            var amount = decimal.Parse(okxOrder.Amount);
            var price = decimal.Parse(okxOrder.Price);

            // Determine order direction from side field
            // For sell orders, make quantity negative
            if (okxOrder.Side == "sell")
            {
                amount = -amount;
            }

            // Parse creation time
            var createTime = OKXUtility.UnixSecondsToDateTime(okxOrder.CreateTime);

            // Create the appropriate LEAN order based on type
            LeanOrder leanOrder;
            if (okxOrder.Type == "limit")
            {
                leanOrder = new LimitOrder(symbol, amount, price, createTime);
            }
            else if (okxOrder.Type == "market")
            {
                leanOrder = new MarketOrder(symbol, amount, createTime);
            }
            else
            {
                Log.Error($"SpotOrderExtensions.ToLeanOrder(): Unsupported order type: {okxOrder.Type}");
                return null;
            }

            // Set brokerage ID and status
            leanOrder.BrokerId.Add(okxOrder.Id);

            // Status field only exists in REST API responses, not WebSocket messages
            // WebSocket handlers set OrderStatus directly (Submitted/UpdateSubmitted/Canceled)
            leanOrder.Status = !string.IsNullOrEmpty(okxOrder.Status)
                ? OKXUtility.ConvertOrderStatus(okxOrder.Status)
                : OrderStatus.Submitted;  // Default for WebSocket responses (if ever used)

            return leanOrder;
        }

        /// <summary>
        /// Converts a OKX Spot order amend response to a LEAN OrderEvent
        /// Used for handling spot.order_amend WebSocket messages
        /// </summary>
        /// <param name="okxOrder">OKX spot order (assumed valid, validated by SpotOrderConverter)</param>
        /// <param name="leanOrder">The LEAN order being amended</param>
        /// <returns>LEAN OrderEvent object</returns>
        public static OrderEvent ToOrderAmendEvent(this SpotOrder okxOrder, LeanOrder leanOrder)
        {
            // ========================================
            // NO VALIDATION NEEDED
            // ========================================
            // Required fields (id, currency_pair, status) are guaranteed non-null by SpotOrderConverter validation
            // Converter returns null if required fields are missing
            // NormalizeResultToArray filters out null objects

            // Convert OKX status to LEAN OrderStatus
            var orderStatus = OKXUtility.ConvertOrderStatus(okxOrder.Status);

            // Build amend message with updated details
            var amendedMessage = string.IsNullOrEmpty(leanOrder.Tag)
                ? $"Amended: amount={okxOrder.Amount}, price={okxOrder.Price}"
                : $"{leanOrder.Tag} | Amended: amount={okxOrder.Amount}, price={okxOrder.Price}";

            // Create OrderEvent
            return new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero)
            {
                Status = orderStatus,
                Message = amendedMessage
            };
        }
    }
}
