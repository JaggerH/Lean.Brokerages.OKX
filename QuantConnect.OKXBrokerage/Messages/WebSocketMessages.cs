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
using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Base WebSocket message for OKX v5 API
    /// </summary>
    public class WebSocketMessage
    {
        /// <summary>
        /// Operation type: subscribe, unsubscribe, login
        /// </summary>
        [JsonProperty("op")]
        public string Operation { get; set; }

        /// <summary>
        /// Arguments for the operation (channels to subscribe, credentials, etc.)
        /// </summary>
        [JsonProperty("args")]
        public List<object> Arguments { get; set; }
    }

    /// <summary>
    /// WebSocket response from OKX
    /// </summary>
    public class WebSocketResponse
    {
        /// <summary>
        /// Event type: subscribe, unsubscribe, error, login
        /// </summary>
        [JsonProperty("event")]
        public string Event { get; set; }

        /// <summary>
        /// Operation code: 0 = success, other = error
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        [JsonProperty("msg")]
        public string Message { get; set; }

        /// <summary>
        /// Connection ID (for login response)
        /// </summary>
        [JsonProperty("connId")]
        public string ConnectionId { get; set; }

        /// <summary>
        /// Channel argument (for subscribe/unsubscribe responses)
        /// </summary>
        [JsonProperty("arg", NullValueHandling = NullValueHandling.Ignore)]
        public WebSocketChannel Arg { get; set; }
    }

    /// <summary>
    /// Channel subscription argument
    /// </summary>
    public class WebSocketChannel
    {
        /// <summary>
        /// Channel name (e.g., "tickers", "trades", "books5", "orders")
        /// </summary>
        [JsonProperty("channel")]
        public string Channel { get; set; }

        /// <summary>
        /// Instrument ID (e.g., "BTC-USDT", "BTC-USDT-SWAP")
        /// Optional for some channels
        /// </summary>
        [JsonProperty("instId", NullValueHandling = NullValueHandling.Ignore)]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Instrument type filter (SPOT, SWAP, FUTURES, OPTION)
        /// Optional
        /// </summary>
        [JsonProperty("instType", NullValueHandling = NullValueHandling.Ignore)]
        public string InstrumentType { get; set; }
    }

    /// <summary>
    /// Login authentication arguments
    /// </summary>
    public class WebSocketLoginArgs
    {
        /// <summary>
        /// API Key
        /// </summary>
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Passphrase
        /// </summary>
        [JsonProperty("passphrase")]
        public string Passphrase { get; set; }

        /// <summary>
        /// Timestamp (Unix timestamp in seconds)
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Signature (HMAC SHA256 of timestamp + method + requestPath)
        /// </summary>
        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

    /// <summary>
    /// Data message from WebSocket (tickers, trades, orderbook, orders, etc.)
    /// </summary>
    public class WebSocketDataMessage<T>
    {
        /// <summary>
        /// Argument that identifies the channel
        /// </summary>
        [JsonProperty("arg")]
        public WebSocketChannel Arg { get; set; }

        /// <summary>
        /// Data array for this message
        /// </summary>
        [JsonProperty("data")]
        public List<T> Data { get; set; }

        /// <summary>
        /// Action type (for orderbook: snapshot, update)
        /// </summary>
        [JsonProperty("action", NullValueHandling = NullValueHandling.Ignore)]
        public string Action { get; set; }
    }

    /// <summary>
    /// Ticker data from WebSocket
    /// </summary>
    public class WebSocketTicker
    {
        /// <summary>
        /// Instrument ID
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Last traded price
        /// </summary>
        [JsonProperty("last")]
        public string Last { get; set; }

        /// <summary>
        /// Best bid price
        /// </summary>
        [JsonProperty("bidPx")]
        public string BidPrice { get; set; }

        /// <summary>
        /// Best ask price
        /// </summary>
        [JsonProperty("askPx")]
        public string AskPrice { get; set; }

        /// <summary>
        /// Best bid size
        /// </summary>
        [JsonProperty("bidSz")]
        public string BidSize { get; set; }

        /// <summary>
        /// Best ask size
        /// </summary>
        [JsonProperty("askSz")]
        public string AskSize { get; set; }

        /// <summary>
        /// 24h trading volume (in base currency)
        /// </summary>
        [JsonProperty("vol24h")]
        public string Volume24h { get; set; }

        /// <summary>
        /// Timestamp (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }
    }

    /// <summary>
    /// Trade data from WebSocket
    /// </summary>
    public class WebSocketTrade
    {
        /// <summary>
        /// Instrument ID
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Trade ID
        /// </summary>
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }

        /// <summary>
        /// Trade price
        /// </summary>
        [JsonProperty("px")]
        public string Price { get; set; }

        /// <summary>
        /// Trade size
        /// </summary>
        [JsonProperty("sz")]
        public string Size { get; set; }

        /// <summary>
        /// Trade side: buy or sell (taker side)
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Trade timestamp (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }
    }

    /// <summary>
    /// Order book data from WebSocket
    /// </summary>
    public class WebSocketOrderBook
    {
        /// <summary>
        /// Instrument ID
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Bids [[price, size, liquidation orders, orders], ...]
        /// </summary>
        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; }

        /// <summary>
        /// Asks [[price, size, liquidation orders, orders], ...]
        /// </summary>
        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; }

        /// <summary>
        /// Timestamp (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Checksum (for data integrity verification)
        /// </summary>
        [JsonProperty("checksum", NullValueHandling = NullValueHandling.Ignore)]
        public int? Checksum { get; set; }

        /// <summary>
        /// Previous sequence ID (used in books channel for detecting message loss)
        /// -1 for snapshot, equals seqId for no-update keepalive
        /// </summary>
        [JsonProperty("prevSeqId", NullValueHandling = NullValueHandling.Ignore)]
        public long? PreviousSequenceId { get; set; }

        /// <summary>
        /// Current sequence ID (used in books channel for message ordering)
        /// </summary>
        [JsonProperty("seqId", NullValueHandling = NullValueHandling.Ignore)]
        public long? SequenceId { get; set; }
    }

    /// <summary>
    /// Order update from WebSocket (private channel)
    /// </summary>
    public class WebSocketOrder
    {
        /// <summary>
        /// Instrument ID
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Client order ID
        /// </summary>
        [JsonProperty("clOrdId")]
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Order state: live, partially_filled, filled, canceled, etc.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Order side: buy or sell
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; }

        /// <summary>
        /// Order type: market, limit, post_only, fok, ioc
        /// </summary>
        [JsonProperty("ordType")]
        public string OrderType { get; set; }

        /// <summary>
        /// Order price
        /// </summary>
        [JsonProperty("px")]
        public string Price { get; set; }

        /// <summary>
        /// Order size
        /// </summary>
        [JsonProperty("sz")]
        public string Size { get; set; }

        /// <summary>
        /// Accumulated filled size (total filled amount)
        /// </summary>
        [JsonProperty("accFillSz")]
        public string FilledSize { get; set; }

        /// <summary>
        /// Last fill size (most recent trade's fill amount)
        /// </summary>
        [JsonProperty("fillSz")]
        public string LastFillSize { get; set; }

        /// <summary>
        /// Last fill price (most recent trade's fill price)
        /// </summary>
        [JsonProperty("fillPx")]
        public string LastFillPrice { get; set; }

        /// <summary>
        /// Last fill time (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("fillTime")]
        public string LastFillTime { get; set; }

        /// <summary>
        /// Last fill fee
        /// </summary>
        [JsonProperty("fillFee")]
        public string LastFillFee { get; set; }

        /// <summary>
        /// Last fill fee currency
        /// </summary>
        [JsonProperty("fillFeeCcy")]
        public string LastFillFeeCurrency { get; set; }

        /// <summary>
        /// Average filled price
        /// </summary>
        [JsonProperty("avgPx")]
        public string AveragePrice { get; set; }

        /// <summary>
        /// Fee currency
        /// </summary>
        [JsonProperty("feeCcy")]
        public string FeeCurrency { get; set; }

        /// <summary>
        /// Fee amount (negative means fee paid)
        /// </summary>
        [JsonProperty("fee")]
        public string Fee { get; set; }

        /// <summary>
        /// Update timestamp (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("uTime")]
        public string UpdateTime { get; set; }

        /// <summary>
        /// Creation timestamp (Unix timestamp in milliseconds)
        /// </summary>
        [JsonProperty("cTime")]
        public string CreateTime { get; set; }

        /// <summary>
        /// Trade ID - indicates this is a fill event when present.
        /// Per OKX docs: when tradeId has value, it represents a trade/fill.
        /// When tradeId is empty and state is filled, it represents market order close.
        /// </summary>
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }
    }

    /// <summary>
    /// Account update from WebSocket (private channel)
    /// </summary>
    public class WebSocketAccount
    {
        /// <summary>
        /// Update timestamp
        /// </summary>
        [JsonProperty("uTime")]
        public string UpdateTime { get; set; }

        /// <summary>
        /// Total equity in USD
        /// </summary>
        [JsonProperty("totalEq")]
        public string TotalEquity { get; set; }

        /// <summary>
        /// Currency details
        /// </summary>
        [JsonProperty("details")]
        public List<WebSocketAccountDetail> Details { get; set; }
    }

    /// <summary>
    /// Account detail for a specific currency
    /// </summary>
    public class WebSocketAccountDetail
    {
        /// <summary>
        /// Currency
        /// </summary>
        [JsonProperty("ccy")]
        public string Currency { get; set; }

        /// <summary>
        /// Available balance
        /// </summary>
        [JsonProperty("availBal")]
        public string AvailableBalance { get; set; }

        /// <summary>
        /// Cash balance
        /// </summary>
        [JsonProperty("cashBal")]
        public string CashBalance { get; set; }

        /// <summary>
        /// Frozen balance
        /// </summary>
        [JsonProperty("frozenBal")]
        public string FrozenBalance { get; set; }

        /// <summary>
        /// Equity of currency
        /// </summary>
        [JsonProperty("eq")]
        public string Equity { get; set; }
    }

    /// <summary>
    /// Position update from WebSocket (private channel)
    /// </summary>
    public class WebSocketPosition
    {
        /// <summary>
        /// Instrument ID
        /// </summary>
        [JsonProperty("instId")]
        public string InstrumentId { get; set; }

        /// <summary>
        /// Position side: long, short, net
        /// </summary>
        [JsonProperty("posSide")]
        public string PositionSide { get; set; }

        /// <summary>
        /// Position size
        /// </summary>
        [JsonProperty("pos")]
        public string Quantity { get; set; }

        /// <summary>
        /// Available position
        /// </summary>
        [JsonProperty("availPos")]
        public string AvailablePosition { get; set; }

        /// <summary>
        /// Average price
        /// </summary>
        [JsonProperty("avgPx")]
        public string AveragePrice { get; set; }

        /// <summary>
        /// Unrealized P&L
        /// </summary>
        [JsonProperty("upl")]
        public string UnrealizedPnL { get; set; }

        /// <summary>
        /// Update timestamp
        /// </summary>
        [JsonProperty("uTime")]
        public string UpdateTime { get; set; }
    }
}
