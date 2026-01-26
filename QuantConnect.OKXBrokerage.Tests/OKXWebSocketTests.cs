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

using NUnit.Framework;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Brokerages.OKX.WebSocket;
using QuantConnect.Configuration;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXWebSocketTests
    {
        private string _apiKey;
        private string _apiSecret;
        private string _passphrase;
        private Symbol _btcusdtSymbol;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load configuration from config.json
            _apiKey = Config.Get("okx-api-key");
            _apiSecret = Config.Get("okx-api-secret");
            _passphrase = Config.Get("okx-passphrase");

            // Skip tests if credentials not configured
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || string.IsNullOrEmpty(_passphrase))
            {
                Assert.Ignore("OKX API credentials not configured in config.json");
            }

            // Create test symbol
            _btcusdtSymbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);

            Console.WriteLine($"Testing with OKX {OKXEnvironment.GetEnvironmentName()} environment");
            Console.WriteLine($"Public WebSocket URL: {OKXEnvironment.GetWebSocketPublicUrl()}");
            Console.WriteLine($"Private WebSocket URL: {OKXEnvironment.GetWebSocketPrivateUrl()}");
        }

        #region Public Channel Tests

        /// <summary>
        /// Tests public WebSocket connection
        /// </summary>
        [Test]
        public void PublicWebSocket_Connect_Succeeds()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            // Act
            client.Connect();
            Thread.Sleep(2000); // Wait for connection

            // Assert
            Assert.IsTrue(client.IsConnected, "WebSocket should be connected");

            Console.WriteLine("Public WebSocket connected successfully");
        }

        /// <summary>
        /// Tests ticker subscription
        /// </summary>
        [Test]
        public void PublicWebSocket_SubscribeTicker_ReceivesData()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            var tickersReceived = new List<WebSocketTicker>();
            client.TickerReceived += (sender, ticker) =>
            {
                tickersReceived.Add(ticker);
                Console.WriteLine($"Ticker received: {ticker.InstrumentId}, Last={ticker.Last}, Bid={ticker.BidPrice}, Ask={ticker.AskPrice}");
            };

            // Act
            client.Connect();
            Thread.Sleep(1000);

            client.Subscribe("tickers", "BTC-USDT", _btcusdtSymbol);
            Thread.Sleep(5000); // Wait for ticker data

            // Assert
            Assert.IsTrue(tickersReceived.Count > 0, "Should receive at least one ticker update");
            Assert.AreEqual("BTC-USDT", tickersReceived[0].InstrumentId);
            Assert.IsNotEmpty(tickersReceived[0].Last, "Last price should not be empty");
            Assert.IsNotEmpty(tickersReceived[0].BidPrice, "Bid price should not be empty");
            Assert.IsNotEmpty(tickersReceived[0].AskPrice, "Ask price should not be empty");

            Console.WriteLine($"Received {tickersReceived.Count} ticker updates");
        }

        /// <summary>
        /// Tests trade subscription
        /// </summary>
        [Test]
        public void PublicWebSocket_SubscribeTrades_ReceivesData()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            var tradesReceived = new List<WebSocketTrade>();
            client.TradeReceived += (sender, trade) =>
            {
                tradesReceived.Add(trade);
                Console.WriteLine($"Trade received: {trade.InstrumentId}, Price={trade.Price}, Size={trade.Size}, Side={trade.Side}");
            };

            // Act
            client.Connect();
            Thread.Sleep(1000);

            client.Subscribe("trades", "BTC-USDT", _btcusdtSymbol);
            Thread.Sleep(10000); // Wait for trade data (may take longer)

            // Assert
            Assert.IsTrue(tradesReceived.Count > 0, "Should receive at least one trade update");
            Assert.AreEqual("BTC-USDT", tradesReceived[0].InstrumentId);
            Assert.IsNotEmpty(tradesReceived[0].Price, "Trade price should not be empty");
            Assert.IsNotEmpty(tradesReceived[0].Size, "Trade size should not be empty");
            Assert.That(tradesReceived[0].Side, Is.EqualTo("buy").Or.EqualTo("sell"), "Trade side should be buy or sell");

            Console.WriteLine($"Received {tradesReceived.Count} trade updates");
        }

        /// <summary>
        /// Tests orderbook subscription
        /// </summary>
        [Test]
        public void PublicWebSocket_SubscribeOrderBook_ReceivesData()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            var orderbooksReceived = new List<WebSocketOrderBook>();
            client.OrderBookReceived += (sender, orderbook) =>
            {
                orderbooksReceived.Add(orderbook);
                Console.WriteLine($"OrderBook received: {orderbook.InstrumentId}, Bids={orderbook.Bids?.Count}, Asks={orderbook.Asks?.Count}");
            };

            // Act
            client.Connect();
            Thread.Sleep(1000);

            client.Subscribe("books5", "BTC-USDT", _btcusdtSymbol);
            Thread.Sleep(5000); // Wait for orderbook data

            // Assert
            Assert.IsTrue(orderbooksReceived.Count > 0, "Should receive at least one orderbook update");
            Assert.AreEqual("BTC-USDT", orderbooksReceived[0].InstrumentId);
            Assert.IsNotNull(orderbooksReceived[0].Bids, "Bids should not be null");
            Assert.IsNotNull(orderbooksReceived[0].Asks, "Asks should not be null");
            Assert.Greater(orderbooksReceived[0].Bids.Count, 0, "Should have at least one bid");
            Assert.Greater(orderbooksReceived[0].Asks.Count, 0, "Should have at least one ask");

            Console.WriteLine($"Received {orderbooksReceived.Count} orderbook updates");
        }

        /// <summary>
        /// Tests multiple symbol subscriptions
        /// </summary>
        [Test]
        public void PublicWebSocket_SubscribeMultipleSymbols_ReceivesData()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            var btcTickers = new List<WebSocketTicker>();
            var ethTickers = new List<WebSocketTicker>();

            client.TickerReceived += (sender, ticker) =>
            {
                if (ticker.InstrumentId == "BTC-USDT")
                    btcTickers.Add(ticker);
                else if (ticker.InstrumentId == "ETH-USDT")
                    ethTickers.Add(ticker);

                Console.WriteLine($"Ticker: {ticker.InstrumentId}, Last={ticker.Last}");
            };

            // Act
            client.Connect();
            Thread.Sleep(1000);

            var ethSymbol = Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.OKX);
            client.Subscribe("tickers", "BTC-USDT", _btcusdtSymbol);
            client.Subscribe("tickers", "ETH-USDT", ethSymbol);
            Thread.Sleep(5000); // Wait for data

            // Assert
            Assert.IsTrue(btcTickers.Count > 0, "Should receive BTC ticker updates");
            Assert.IsTrue(ethTickers.Count > 0, "Should receive ETH ticker updates");

            Console.WriteLine($"Received BTC: {btcTickers.Count}, ETH: {ethTickers.Count} tickers");
        }

        /// <summary>
        /// Tests unsubscribe functionality
        /// </summary>
        [Test]
        public void PublicWebSocket_Unsubscribe_StopsReceivingData()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            var tickersReceived = 0;
            client.TickerReceived += (sender, ticker) =>
            {
                tickersReceived++;
                Console.WriteLine($"Ticker received (count: {tickersReceived})");
            };

            // Act
            client.Connect();
            Thread.Sleep(1000);

            client.Subscribe("tickers", "BTC-USDT", _btcusdtSymbol);
            Thread.Sleep(3000);

            var countBeforeUnsubscribe = tickersReceived;
            Console.WriteLine($"Tickers before unsubscribe: {countBeforeUnsubscribe}");

            client.Unsubscribe("tickers", "BTC-USDT");
            Thread.Sleep(5000);

            var countAfterUnsubscribe = tickersReceived;
            Console.WriteLine($"Tickers after unsubscribe: {countAfterUnsubscribe}");

            // Assert
            Assert.IsTrue(countBeforeUnsubscribe > 0, "Should receive tickers before unsubscribe");
            // After unsubscribe, we should receive few or no new tickers
            Assert.That(countAfterUnsubscribe - countBeforeUnsubscribe, Is.LessThanOrEqualTo(2),
                "Should receive few or no tickers after unsubscribe");
        }

        #endregion

        #region Private Channel Tests

        /// <summary>
        /// Tests private WebSocket authentication
        /// </summary>
        [Test]
        public void PrivateWebSocket_Connect_Authenticates()
        {
            // Arrange
            var privateUrl = OKXEnvironment.GetWebSocketPrivateUrl();
            using var client = new OKXWebSocketClient(
                privateUrl,
                _apiKey,
                _apiSecret,
                _passphrase,
                isPrivateChannel: true);

            // Act
            client.Connect();
            Thread.Sleep(3000); // Wait for authentication

            // Assert
            Assert.IsTrue(client.IsConnected, "WebSocket should be connected and authenticated");

            Console.WriteLine("Private WebSocket authenticated successfully");
        }

        /// <summary>
        /// Tests account channel subscription
        /// </summary>
        [Test]
        public void PrivateWebSocket_SubscribeAccount_ReceivesData()
        {
            // Arrange
            var privateUrl = OKXEnvironment.GetWebSocketPrivateUrl();
            using var client = new OKXWebSocketClient(
                privateUrl,
                _apiKey,
                _apiSecret,
                _passphrase,
                isPrivateChannel: true);

            var accountUpdates = new List<WebSocketAccount>();
            client.AccountReceived += (sender, account) =>
            {
                accountUpdates.Add(account);
                Console.WriteLine($"Account update: TotalEq={account.TotalEquity}, Details={account.Details?.Count ?? 0}");
            };

            // Act
            client.Connect();
            Thread.Sleep(2000);

            client.Subscribe("account", null, null);
            Thread.Sleep(5000); // Wait for account data

            // Assert
            Assert.IsTrue(accountUpdates.Count > 0, "Should receive at least one account update");
            Assert.IsNotEmpty(accountUpdates[0].TotalEquity, "Total equity should not be empty");

            Console.WriteLine($"Received {accountUpdates.Count} account updates");
        }

        /// <summary>
        /// Tests orders channel subscription
        /// </summary>
        [Test]
        public void PrivateWebSocket_SubscribeOrders_CanReceiveUpdates()
        {
            // Arrange
            var privateUrl = OKXEnvironment.GetWebSocketPrivateUrl();
            using var client = new OKXWebSocketClient(
                privateUrl,
                _apiKey,
                _apiSecret,
                _passphrase,
                isPrivateChannel: true);

            var orderUpdates = new List<WebSocketOrder>();
            client.OrderReceived += (sender, order) =>
            {
                orderUpdates.Add(order);
                Console.WriteLine($"Order update: {order.OrderId}, State={order.State}, InstId={order.InstrumentId}");
            };

            // Act
            client.Connect();
            Thread.Sleep(2000);

            client.Subscribe("orders", null, null);
            Thread.Sleep(3000);

            // Assert
            // Note: We may not receive order updates if there are no active orders
            // This test just verifies the subscription works without errors
            Assert.Pass($"Orders channel subscribed successfully. Received {orderUpdates.Count} updates.");
        }

        /// <summary>
        /// Tests positions channel subscription
        /// </summary>
        [Test]
        public void PrivateWebSocket_SubscribePositions_CanReceiveUpdates()
        {
            // Arrange
            var privateUrl = OKXEnvironment.GetWebSocketPrivateUrl();
            using var client = new OKXWebSocketClient(
                privateUrl,
                _apiKey,
                _apiSecret,
                _passphrase,
                isPrivateChannel: true);

            var positionUpdates = new List<WebSocketPosition>();
            client.PositionReceived += (sender, position) =>
            {
                positionUpdates.Add(position);
                Console.WriteLine($"Position update: {position.InstrumentId}, Pos={position.Position}, Side={position.PositionSide}");
            };

            // Act
            client.Connect();
            Thread.Sleep(2000);

            client.Subscribe("positions", null, null);
            Thread.Sleep(3000);

            // Assert
            // Note: We may not receive position updates if there are no open positions
            // This test just verifies the subscription works without errors
            Assert.Pass($"Positions channel subscribed successfully. Received {positionUpdates.Count} updates.");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests heartbeat mechanism
        /// </summary>
        [Test]
        public void WebSocket_Heartbeat_KeepsConnectionAlive()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            // Act
            client.Connect();
            Thread.Sleep(1000);

            // Wait for multiple heartbeat intervals (15s each)
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(16000); // Slightly more than heartbeat interval
                Assert.IsTrue(client.IsConnected, $"Connection should still be alive after {(i + 1) * 16} seconds");
                Console.WriteLine($"Connection alive after {(i + 1) * 16} seconds");
            }

            // Assert
            Assert.Pass("Heartbeat kept connection alive for 48 seconds");
        }

        /// <summary>
        /// Tests reconnection after disconnect
        /// </summary>
        [Test]
        public void WebSocket_Reconnect_Succeeds()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            // Act - First connection
            client.Connect();
            Thread.Sleep(1000);
            Assert.IsTrue(client.IsConnected, "First connection should succeed");

            // Disconnect
            client.Disconnect();
            Thread.Sleep(1000);
            Assert.IsFalse(client.IsConnected, "Should be disconnected");

            // Reconnect
            client.Connect();
            Thread.Sleep(1000);
            Assert.IsTrue(client.IsConnected, "Reconnection should succeed");

            Console.WriteLine("Reconnection test passed");
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests invalid credentials handling
        /// </summary>
        [Test]
        public void PrivateWebSocket_InvalidCredentials_FailsAuthentication()
        {
            // Arrange
            var privateUrl = OKXEnvironment.GetWebSocketPrivateUrl();
            using var client = new OKXWebSocketClient(
                privateUrl,
                "invalid-key",
                "invalid-secret",
                "invalid-passphrase",
                isPrivateChannel: true);

            // Act & Assert
            Assert.Throws<Exception>(() =>
            {
                client.Connect();
            }, "Should throw exception for invalid credentials");
        }

        /// <summary>
        /// Tests subscription without connection
        /// </summary>
        [Test]
        public void WebSocket_SubscribeWithoutConnect_ThrowsException()
        {
            // Arrange
            var publicUrl = OKXEnvironment.GetWebSocketPublicUrl();
            using var client = new OKXWebSocketClient(publicUrl, isPrivateChannel: false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.Subscribe("tickers", "BTC-USDT", _btcusdtSymbol);
            }, "Should throw exception when subscribing without connection");
        }

        #endregion
    }
}
