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
using QuantConnect.TradingPairs;

namespace QuantConnect.Brokerages.OKX.Converters
{
    /// <summary>
    /// Custom JSON converter for OKX Futures UserTrade messages
    /// Handles futures.usertrades WebSocket channel messages
    /// </summary>
    public class FuturesUserTradeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FuturesUserTrade);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            // Extract and validate required fields
            var orderId = obj["order_id"]?.ToString();
            var contract = obj["contract"]?.ToString();

            // Validate required fields
            if (string.IsNullOrEmpty(orderId))
            {
                var tradeId = obj["id"]?.ToString() ?? "unknown";
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"FuturesUserTradeConverter: Trade {tradeId} missing required field 'order_id'. Raw: {truncated}");
                return null;
            }

            if (string.IsNullOrEmpty(contract))
            {
                var tradeId = obj["id"]?.ToString() ?? "unknown";
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"FuturesUserTradeConverter: Trade {tradeId} missing required field 'contract'. Raw: {truncated}");
                return null;
            }

            return new FuturesUserTrade
            {
                // OKX API field name inconsistency for Trade ID:
                // - GET /futures/{settle}/my_trades → "id"
                // - WebSocket futures.usertrades → "id"
                // - GET /futures/{settle}/my_trades_timerange → "trade_id"
                // We check both fields to support all endpoints
                Id = obj["trade_id"]?.ToString() ?? obj["id"]?.ToString(),
                OrderId = orderId,
                Contract = contract,
                CreateTime = obj["create_time"]?.ToObject<long>() ?? 0,
                CreateTimeMs = obj["create_time_ms"]?.ToString(),
                Size = obj["size"]?.ToObject<long>() ?? 0,
                Price = OKXUtility.ParseDecimal(obj["price"]),
                Role = obj["role"]?.ToString(),
                Fee = OKXUtility.ParseDecimal(obj["fee"]),
                PointFee = OKXUtility.ParseDecimal(obj["point_fee"]),
                Text = obj["text"]?.ToString()
            };
        }
    }

    /// <summary>
    /// Domain converter extensions for FuturesUserTrade
    /// Converts OKX FuturesUserTrade messages to LEAN OrderEvent objects
    /// </summary>
    public static class FuturesUserTradeExtensions
    {
        /// <summary>
        /// Converts a OKX FuturesUserTrade to LEAN OrderEvent
        /// </summary>
        /// <param name="trade">OKX futures user trade (assumed valid, validated by FuturesUserTradeConverter)</param>
        /// <param name="leanOrder">The LEAN order associated with this trade</param>
        /// <param name="totalFillQuantity">Total cumulative fill quantity for this order</param>
        /// <returns>LEAN OrderEvent object</returns>
        public static OrderEvent ToOrderEvent(
            this FuturesUserTrade trade,
            Order leanOrder,
            decimal totalFillQuantity)
        {
            // OrderId and Contract are guaranteed non-null by FuturesUserTradeConverter validation
            // Converter returns null if required fields are missing
            // NormalizeResultToArray filters out null objects

            // Determine order direction from trade size (positive = buy, negative = sell)
            var direction = trade.Size >= 0 ? OrderDirection.Buy : OrderDirection.Sell;

            // Convert size to absolute quantity (size is in number of contracts)
            var quantity = Math.Abs(trade.Size);

            // Determine order status based on cumulative fills
            var status = Math.Abs(totalFillQuantity) >= Math.Abs(leanOrder.Quantity)
                ? OrderStatus.Filled
                : OrderStatus.PartiallyFilled;

            // Create order fee (Futures fees are always in settlement currency USDT)
            var orderFee = new OrderFee(new CashAmount(Math.Abs(trade.Fee), "USDT"));

            // Create event message
            var eventMessage = string.IsNullOrEmpty(leanOrder.Tag)
                ? $"Trade ID: {trade.Id}"
                : $"{leanOrder.Tag} | Trade ID: {trade.Id}";

            // Convert Unix timestamp to DateTime
            var fillTime = OKXUtility.UnixSecondsToDateTime(trade.CreateTime);

            // Create order event
            return new OrderEvent(
                leanOrder.Id,
                leanOrder.Symbol,
                fillTime,
                status,
                direction,
                trade.Price,
                direction == OrderDirection.Buy ? quantity : -quantity,
                orderFee,
                eventMessage
            )
            {
                ExecutionId = trade.Id
            };
        }

        /// <summary>
        /// Converts a OKX FuturesUserTrade to ExecutionRecord for IExecutionHistoryProvider
        /// </summary>
        /// <param name="trade">OKX futures user trade</param>
        /// <param name="symbolMapper">Symbol mapper for converting brokerage symbols to LEAN symbols</param>
        /// <returns>ExecutionRecord instance</returns>
        public static ExecutionRecord ToExecutionRecord(this FuturesUserTrade trade, ISymbolMapper symbolMapper)
        {
            // Convert brokerage symbol to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(trade.Contract, SecurityType.CryptoFuture, Market.OKX);

            // Futures Size is already signed (positive = buy, negative = sell)
            var quantity = (decimal)trade.Size;

            // Convert Unix timestamp to DateTime
            var timeUtc = DateTimeOffset.FromUnixTimeSeconds(trade.CreateTime).UtcDateTime;

            // Futures fees are always in settlement currency (USDT for USDT-settled contracts, BTC for BTC-settled)
            var feeCurrency = trade.Contract.EndsWith("_USDT") ? "USDT" : "BTC";

            return new ExecutionRecord
            {
                ExecutionId = trade.Id,
                Symbol = symbol,
                Quantity = quantity,
                Price = trade.Price,
                TimeUtc = timeUtc,
                Tag = trade.Text,
                Fee = trade.Fee,
                FeeCurrency = feeCurrency
            };
        }
    }
}
