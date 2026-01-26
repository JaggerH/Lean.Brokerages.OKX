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
using QuantConnect.Configuration;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXOrderManagementTests
    {
        private OKXBrokerage _brokerage;
        private string _apiKey;
        private string _apiSecret;
        private string _passphrase;
        private List<OrderEvent> _orderEvents;
        private Symbol _btcusdtSymbol;
        private Symbol _ethusdtSymbol;
        private int _orderIdCounter = 1;

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

            // Create test symbols
            _btcusdtSymbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
            _ethusdtSymbol = Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.OKX);
        }

        [SetUp]
        public void SetUp()
        {
            // Create OKXBrokerage instance
            _brokerage = new OKXBrokerage(
                _apiKey,
                _apiSecret,
                _passphrase,
                null  // algorithm
            );

            // Subscribe to order events
            _orderEvents = new List<OrderEvent>();
            _brokerage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                foreach (var orderEvent in orderEvents)
                {
                    _orderEvents.Add(orderEvent);
                    Console.WriteLine($"Order Event: {orderEvent.OrderId} - {orderEvent.Status} - {orderEvent.Message}");
                }
            };

            // Small delay between tests to avoid rate limiting
            Thread.Sleep(1000);
        }

        [TearDown]
        public void TearDown()
        {
            // Cancel all open orders to clean up
            try
            {
                var openOrders = _brokerage.GetOpenOrders();
                foreach (var order in openOrders)
                {
                    _brokerage.CancelOrder(order);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup error: {ex.Message}");
            }

            _brokerage?.Dispose();
            Thread.Sleep(500);
        }

        /// <summary>
        /// Helper method to create an order with a unique ID using reflection
        /// </summary>
        private T CreateOrderWithId<T>(T order) where T : Order
        {
            var idProperty = typeof(Order).GetProperty("Id");
            idProperty.SetValue(order, _orderIdCounter++);
            return order;
        }

        #region PlaceOrder Tests - Limit Orders

        /// <summary>
        /// Tests placing a limit buy order
        /// </summary>
        [Test]
        public void PlaceOrder_LimitBuy_Succeeds()
        {
            // Arrange - Create a limit buy order well below market (won't fill)
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,  // Small quantity for testing
                limitPrice: 10000m,  // Well below market price
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.PlaceOrder(order);

            // Assert
            Assert.IsTrue(result, "PlaceOrder should return true");
            Assert.IsNotEmpty(order.BrokerId, "Order should have broker ID assigned");
            Assert.AreEqual(1, order.BrokerId.Count, "Should have exactly one broker ID");

            // Verify order event was fired
            Thread.Sleep(500);  // Wait for event
            Assert.IsNotEmpty(_orderEvents, "Should receive order event");
            var submitEvent = _orderEvents.FirstOrDefault(e => e.Status == OrderStatus.Submitted);
            Assert.IsNotNull(submitEvent, "Should receive Submitted event");

            Console.WriteLine($"Order placed successfully: BrokerId={order.BrokerId[0]}");
        }

        /// <summary>
        /// Tests placing a limit sell order
        /// </summary>
        [Test]
        public void PlaceOrder_LimitSell_Succeeds()
        {
            // Arrange - Create a limit sell order well above market (won't fill)
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: -0.001m,  // Negative for sell
                limitPrice: 100000m,  // Well above market price
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.PlaceOrder(order);

            // Assert
            Assert.IsTrue(result, "PlaceOrder should return true");
            Assert.IsNotEmpty(order.BrokerId, "Order should have broker ID assigned");

            Console.WriteLine($"Sell order placed successfully: BrokerId={order.BrokerId[0]}");
        }

        #endregion

        #region PlaceOrder Tests - Market Orders

        /// <summary>
        /// Tests placing a market buy order (WARNING: Will execute at market price)
        /// </summary>
        [Test]
        [Explicit("Market order will execute - only run manually")]
        public void PlaceOrder_MarketBuy_Succeeds()
        {
            // Arrange - Create a small market buy order
            var order = CreateOrderWithId(new MarketOrder(
                _btcusdtSymbol,
                quantity: 0.001m,  // Very small quantity
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.PlaceOrder(order);

            // Assert
            Assert.IsTrue(result, "PlaceOrder should return true");
            Assert.IsNotEmpty(order.BrokerId, "Order should have broker ID assigned");

            Console.WriteLine($"Market order placed successfully: BrokerId={order.BrokerId[0]}");
        }

        #endregion

        #region UpdateOrder Tests

        /// <summary>
        /// Tests updating a limit order's price
        /// </summary>
        [Test]
        public void UpdateOrder_ChangePrice_Succeeds()
        {
            // Arrange - Place initial order
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            var placeResult = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResult, "Initial order placement should succeed");
            Thread.Sleep(1000);  // Wait for order to be accepted

            // Act - Update price using reflection (LimitPrice is read-only)
            var limitPriceProp = typeof(LimitOrder).GetProperty("LimitPrice");
            limitPriceProp.SetValue(order, 11000m);
            var updateResult = _brokerage.UpdateOrder(order);

            // Assert
            Assert.IsTrue(updateResult, "UpdateOrder should return true");
            Console.WriteLine($"Order price updated: 10000 -> 11000");
        }

        /// <summary>
        /// Tests updating a limit order's quantity
        /// </summary>
        [Test]
        public void UpdateOrder_ChangeQuantity_Succeeds()
        {
            // Arrange - Place initial order
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            var placeResult = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResult, "Initial order placement should succeed");
            Thread.Sleep(1000);

            // Act - Update quantity using reflection (Quantity is settable internally)
            var quantityProp = typeof(Order).GetProperty("Quantity");
            quantityProp.SetValue(order, 0.002m);
            var updateResult = _brokerage.UpdateOrder(order);

            // Assert
            Assert.IsTrue(updateResult, "UpdateOrder should return true");
            Console.WriteLine($"Order quantity updated: 0.001 -> 0.002");
        }

        /// <summary>
        /// Tests updating a limit order's price and quantity together
        /// </summary>
        [Test]
        public void UpdateOrder_ChangePriceAndQuantity_Succeeds()
        {
            // Arrange - Place initial order
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            var placeResult = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResult, "Initial order placement should succeed");
            Thread.Sleep(1000);

            // Act - Update both price and quantity using reflection
            var limitPriceProp = typeof(LimitOrder).GetProperty("LimitPrice");
            limitPriceProp.SetValue(order, 11000m);
            var quantityProp = typeof(Order).GetProperty("Quantity");
            quantityProp.SetValue(order, 0.002m);
            var updateResult = _brokerage.UpdateOrder(order);

            // Assert
            Assert.IsTrue(updateResult, "UpdateOrder should return true");
            Console.WriteLine($"Order updated: 0.001@10000 -> 0.002@11000");
        }

        /// <summary>
        /// Tests that updating an order without broker ID fails
        /// </summary>
        [Test]
        public void UpdateOrder_NoBrokerId_Fails()
        {
            // Arrange - Create order without broker ID
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.UpdateOrder(order);

            // Assert
            Assert.IsFalse(result, "UpdateOrder should return false without broker ID");
        }

        #endregion

        #region CancelOrder Tests

        /// <summary>
        /// Tests canceling an open limit order
        /// </summary>
        [Test]
        public void CancelOrder_LimitOrder_Succeeds()
        {
            // Arrange - Place order first
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            var placeResult = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResult, "Initial order placement should succeed");
            Thread.Sleep(1000);  // Wait for order to be accepted

            // Act - Cancel order
            var cancelResult = _brokerage.CancelOrder(order);

            // Assert
            Assert.IsTrue(cancelResult, "CancelOrder should return true");

            // Verify cancel event was fired
            Thread.Sleep(500);
            var cancelEvent = _orderEvents.FirstOrDefault(e => e.Status == OrderStatus.Canceled);
            Assert.IsNotNull(cancelEvent, "Should receive Canceled event");

            Console.WriteLine($"Order canceled successfully: BrokerId={order.BrokerId[0]}");
        }

        /// <summary>
        /// Tests that canceling an order without broker ID fails
        /// </summary>
        [Test]
        public void CancelOrder_NoBrokerId_Fails()
        {
            // Arrange - Create order without broker ID
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.CancelOrder(order);

            // Assert
            Assert.IsFalse(result, "CancelOrder should return false without broker ID");
        }

        #endregion

        #region Order Lifecycle Tests

        /// <summary>
        /// Tests complete order lifecycle: Place -> Update -> Cancel
        /// </summary>
        [Test]
        public void OrderLifecycle_PlaceUpdateCancel_Succeeds()
        {
            // Arrange
            var order = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            // Act & Assert - Place
            Console.WriteLine("Step 1: Placing order...");
            var placeResult = _brokerage.PlaceOrder(order);
            Assert.IsTrue(placeResult, "PlaceOrder should succeed");
            Assert.IsNotEmpty(order.BrokerId, "Order should have broker ID");
            Thread.Sleep(1000);

            // Act & Assert - Update
            Console.WriteLine("Step 2: Updating order...");
            var limitPriceProp = typeof(LimitOrder).GetProperty("LimitPrice");
            limitPriceProp.SetValue(order, 11000m);
            var quantityProp = typeof(Order).GetProperty("Quantity");
            quantityProp.SetValue(order, 0.002m);
            var updateResult = _brokerage.UpdateOrder(order);
            Assert.IsTrue(updateResult, "UpdateOrder should succeed");
            Thread.Sleep(1000);

            // Act & Assert - Cancel
            Console.WriteLine("Step 3: Canceling order...");
            var cancelResult = _brokerage.CancelOrder(order);
            Assert.IsTrue(cancelResult, "CancelOrder should succeed");
            Thread.Sleep(500);

            // Verify final state
            Console.WriteLine($"Complete lifecycle test passed for order {order.Id}");
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests that unsupported order types are rejected
        /// </summary>
        [Test]
        public void PlaceOrder_StopMarketOrder_ReturnsError()
        {
            // Arrange - Create stop market order (not supported in Phase 5)
            var order = CreateOrderWithId(new StopMarketOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                stopPrice: 50000m,
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.PlaceOrder(order);

            // Assert
            Assert.IsFalse(result, "StopMarket orders should not be supported");

            // Note: OrderEvent may or may not be fired depending on implementation
            Thread.Sleep(500);
        }

        /// <summary>
        /// Tests that invalid symbol is handled
        /// </summary>
        [Test]
        public void PlaceOrder_InvalidSymbol_HandlesError()
        {
            // Arrange - Create order with invalid symbol
            var invalidSymbol = Symbol.Create("INVALID", SecurityType.Crypto, Market.OKX);
            var order = CreateOrderWithId(new LimitOrder(
                invalidSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            // Act
            var result = _brokerage.PlaceOrder(order);

            // Assert - Should handle gracefully (return false or throw handled exception)
            // Note: Actual behavior depends on symbol mapper
            Console.WriteLine($"Invalid symbol order result: {result}");
        }

        #endregion

        #region REST API Direct Tests

        /// <summary>
        /// Tests PlaceOrder REST API method directly
        /// </summary>
        [Test]
        public void RestApi_PlaceOrder_Works()
        {
            // Arrange
            var restClient = new RestApi.OKXRestApiClient(_apiKey, _apiSecret, _passphrase);
            var request = new Messages.PlaceOrderRequest
            {
                InstrumentId = "BTC-USDT",
                TradeMode = "cash",
                Side = "buy",
                OrderType = "limit",
                Size = "0.001",
                Price = "10000",
                ClientOrderId = $"test-{DateTime.UtcNow.Ticks}",
                Tag = "LEAN-TEST"
            };

            // Act
            var result = restClient.PlaceOrder(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, "Response should be successful");
            Assert.IsNotNull(result.Data, "Data should not be null");
            Assert.IsNotEmpty(result.Data.OrderId, "OrderId should be populated");
            Assert.AreEqual("0", result.Data.StatusCode, "Status code should be 0 (success)");

            Console.WriteLine($"RestApi PlaceOrder succeeded: OrderId={result.Data.OrderId}");

            // Cleanup - Cancel the order
            Thread.Sleep(500);
            var cancelRequest = new Messages.CancelOrderRequest
            {
                InstrumentId = "BTC-USDT",
                OrderId = result.Data.OrderId
            };
            restClient.CancelOrder(cancelRequest);
        }

        /// <summary>
        /// Tests AmendOrder REST API method directly
        /// </summary>
        [Test]
        public void RestApi_AmendOrder_Works()
        {
            // Arrange - Place order first
            var restClient = new RestApi.OKXRestApiClient(_apiKey, _apiSecret, _passphrase);
            var placeRequest = new Messages.PlaceOrderRequest
            {
                InstrumentId = "BTC-USDT",
                TradeMode = "cash",
                Side = "buy",
                OrderType = "limit",
                Size = "0.001",
                Price = "10000",
                ClientOrderId = $"test-{DateTime.UtcNow.Ticks}",
                Tag = "LEAN-TEST"
            };

            var placeResult = restClient.PlaceOrder(placeRequest);
            Assert.IsTrue(placeResult.IsSuccess, "Place order should succeed");
            Thread.Sleep(1000);

            // Act - Amend order
            var amendRequest = new Messages.AmendOrderRequest
            {
                InstrumentId = "BTC-USDT",
                OrderId = placeResult.Data.OrderId,
                NewPrice = "11000",
                NewSize = "0.002"
            };

            var amendResult = restClient.AmendOrder(amendRequest);

            // Assert
            Assert.IsTrue(amendResult.IsSuccess, "Amend should be successful");
            Assert.IsNotNull(amendResult.Data, "Amend data should not be null");
            Assert.AreEqual("0", amendResult.Data.StatusCode, "Amend should succeed");

            Console.WriteLine($"RestApi AmendOrder succeeded: OrderId={amendResult.Data.OrderId}");

            // Cleanup
            Thread.Sleep(500);
            var cancelRequest = new Messages.CancelOrderRequest
            {
                InstrumentId = "BTC-USDT",
                OrderId = placeResult.Data.OrderId
            };
            restClient.CancelOrder(cancelRequest);
        }

        /// <summary>
        /// Tests CancelOrder REST API method directly
        /// </summary>
        [Test]
        public void RestApi_CancelOrder_Works()
        {
            // Arrange - Place order first
            var restClient = new RestApi.OKXRestApiClient(_apiKey, _apiSecret, _passphrase);
            var placeRequest = new Messages.PlaceOrderRequest
            {
                InstrumentId = "BTC-USDT",
                TradeMode = "cash",
                Side = "buy",
                OrderType = "limit",
                Size = "0.001",
                Price = "10000",
                ClientOrderId = $"test-{DateTime.UtcNow.Ticks}",
                Tag = "LEAN-TEST"
            };

            var placeResult = restClient.PlaceOrder(placeRequest);
            Assert.IsTrue(placeResult.IsSuccess, "Place order should succeed");
            Thread.Sleep(1000);

            // Act - Cancel order
            var cancelRequest = new Messages.CancelOrderRequest
            {
                InstrumentId = "BTC-USDT",
                OrderId = placeResult.Data.OrderId
            };

            var cancelResult = restClient.CancelOrder(cancelRequest);

            // Assert
            Assert.IsTrue(cancelResult.IsSuccess, "Cancel should be successful");
            Assert.IsNotNull(cancelResult.Data, "Cancel data should not be null");
            Assert.AreEqual("0", cancelResult.Data.StatusCode, "Cancel should succeed");

            Console.WriteLine($"RestApi CancelOrder succeeded: OrderId={cancelResult.Data.OrderId}");
        }

        #endregion

        #region Multi-Symbol Tests

        /// <summary>
        /// Tests placing orders on multiple symbols
        /// </summary>
        [Test]
        public void PlaceOrder_MultipleSymbols_Succeeds()
        {
            // Arrange - Create orders for different symbols
            var btcOrder = CreateOrderWithId(new LimitOrder(
                _btcusdtSymbol,
                quantity: 0.001m,
                limitPrice: 10000m,
                DateTime.UtcNow
            ));

            var ethOrder = CreateOrderWithId(new LimitOrder(
                _ethusdtSymbol,
                quantity: 0.01m,
                limitPrice: 1000m,
                DateTime.UtcNow
            ));

            // Act
            var btcResult = _brokerage.PlaceOrder(btcOrder);
            Thread.Sleep(500);
            var ethResult = _brokerage.PlaceOrder(ethOrder);
            Thread.Sleep(500);

            // Assert
            Assert.IsTrue(btcResult, "BTC order should succeed");
            Assert.IsTrue(ethResult, "ETH order should succeed");
            Assert.IsNotEmpty(btcOrder.BrokerId, "BTC order should have broker ID");
            Assert.IsNotEmpty(ethOrder.BrokerId, "ETH order should have broker ID");

            Console.WriteLine($"Multi-symbol test passed: BTC={btcOrder.BrokerId[0]}, ETH={ethOrder.BrokerId[0]}");
        }

        #endregion
    }
}
