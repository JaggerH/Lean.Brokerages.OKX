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
    /// Custom JSON converter for OKX <see cref="SpotUserTrade"/> class
    /// Handles spot.usertrades WebSocket channel messages
    /// </summary>
    public class SpotUserTradeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SpotUserTrade);
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
            var currencyPair = obj["currency_pair"]?.ToString();

            // Validate required fields
            if (string.IsNullOrEmpty(orderId))
            {
                var tradeId = obj["id"]?.ToString() ?? "unknown";
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"SpotUserTradeConverter: Trade {tradeId} missing required field 'order_id'. Raw: {truncated}");
                return null;
            }

            if (string.IsNullOrEmpty(currencyPair))
            {
                var tradeId = obj["id"]?.ToString() ?? "unknown";
                var rawJson = obj.ToString(Formatting.None);
                var truncated = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                Log.Error($"SpotUserTradeConverter: Trade {tradeId} missing required field 'currency_pair'. Raw: {truncated}");
                return null;
            }

            return new SpotUserTrade
            {
                Id = obj["id"]?.ToObject<long>() ?? 0,
                UserId = obj["user_id"]?.ToObject<int>() ?? 0,
                OrderId = orderId,
                CurrencyPair = currencyPair,
                CreateTime = obj["create_time"]?.ToObject<long>() ?? 0,
                CreateTimeMs = obj["create_time_ms"]?.ToString(),
                Side = obj["side"]?.ToString(),
                Amount = OKXUtility.ParseDecimal(obj["amount"]),
                Role = obj["role"]?.ToString(),
                Price = OKXUtility.ParseDecimal(obj["price"]),
                Fee = OKXUtility.ParseDecimal(obj["fee"]),
                FeeCurrency = obj["fee_currency"]?.ToString(),
                PointFee = OKXUtility.ParseDecimal(obj["point_fee"]),
                GtFee = OKXUtility.ParseDecimal(obj["gt_fee"]),
                Text = obj["text"]?.ToString(),
                IdMarket = obj["id_market"]?.ToObject<int>() ?? 0
            };
        }
    }

    /// <summary>
    /// Domain converter extensions for SpotUserTrade
    /// Converts OKX SpotUserTrade messages to LEAN OrderEvent objects
    /// </summary>
    public static class SpotUserTradeExtensions
    {
        /// <summary>
        /// Converts a OKX SpotUserTrade to LEAN OrderEvent
        /// </summary>
        /// <param name="trade">OKX spot user trade (assumed valid, validated by SpotUserTradeConverter)</param>
        /// <param name="leanOrder">The LEAN order associated with this trade</param>
        /// <param name="totalFillQuantity">Total cumulative fill quantity for this order</param>
        /// <returns>LEAN OrderEvent object</returns>
        public static OrderEvent ToOrderEvent(
            this SpotUserTrade trade,
            Order leanOrder,
            decimal totalFillQuantity)
        {
            // OrderId and CurrencyPair are guaranteed non-null by SpotUserTradeConverter validation
            // Converter returns null if required fields are missing
            // NormalizeResultToArray filters out null objects

            // Determine order direction from trade side
            var direction = trade.Side == "buy" ? OrderDirection.Buy : OrderDirection.Sell;

            // Determine order status based on cumulative fills
            var status = Math.Abs(totalFillQuantity) >= Math.Abs(leanOrder.Quantity)
                ? OrderStatus.Filled
                : OrderStatus.PartiallyFilled;

            // Create order fee
            var feeCurrency = string.IsNullOrEmpty(trade.FeeCurrency)
                ? "USDT"
                : trade.FeeCurrency.ToUpperInvariant();
            var orderFee = new OrderFee(new CashAmount(Math.Abs(trade.Fee), feeCurrency));

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
                direction == OrderDirection.Buy ? trade.Amount : -trade.Amount,
                orderFee,
                eventMessage
            )
            {
                ExecutionId = trade.Id.ToString()
            };
        }

        /// <summary>
        /// Converts a OKX SpotUserTrade to ExecutionRecord for IExecutionHistoryProvider
        /// </summary>
        /// <param name="trade">OKX spot user trade</param>
        /// <param name="symbolMapper">Symbol mapper for converting brokerage symbols to LEAN symbols</param>
        /// <returns>ExecutionRecord instance</returns>
        public static ExecutionRecord ToExecutionRecord(this SpotUserTrade trade, ISymbolMapper symbolMapper)
        {
            // Convert brokerage symbol to LEAN symbol
            var symbol = symbolMapper.GetLeanSymbol(trade.CurrencyPair, SecurityType.Crypto, Market.OKX);

            // Convert side to signed quantity (buy = positive, sell = negative)
            var quantity = trade.Side.Equals("buy", StringComparison.OrdinalIgnoreCase)
                ? trade.Amount
                : -trade.Amount;

            // Convert Unix timestamp to DateTime
            var timeUtc = DateTimeOffset.FromUnixTimeSeconds(trade.CreateTime).UtcDateTime;

            // Use FeeCurrency if available, otherwise default to USDT
            var feeCurrency = string.IsNullOrEmpty(trade.FeeCurrency)
                ? "USDT"
                : trade.FeeCurrency.ToUpperInvariant();

            return new ExecutionRecord
            {
                ExecutionId = trade.Id.ToString(),
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
