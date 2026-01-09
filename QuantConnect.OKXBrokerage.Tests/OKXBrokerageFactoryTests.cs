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
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXBrokerageFactoryTests
    {
        private string _apiKey;
        private string _apiSecret;
        private string _passphrase;

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
        }

        [Test]
        public void BrokerageFactory_CanInstantiate()
        {
            // Arrange & Act
            var factory = new OKXBrokerageFactory();

            // Assert
            Assert.IsNotNull(factory);
            Assert.IsInstanceOf<BrokerageFactory>(factory);
        }

        [Test]
        public void BrokerageData_ReturnsRequiredFields()
        {
            // Arrange
            var factory = new OKXBrokerageFactory();

            // Act
            var brokerageData = factory.BrokerageData;

            // Assert
            Assert.IsNotNull(brokerageData);
            Assert.IsTrue(brokerageData.ContainsKey("okx-api-key"));
            Assert.IsTrue(brokerageData.ContainsKey("okx-api-secret"));
            Assert.IsTrue(brokerageData.ContainsKey("okx-passphrase"));
            Assert.IsTrue(brokerageData.ContainsKey("okx-environment"));
            Assert.IsTrue(brokerageData.ContainsKey("okx-unified-account-mode"));
        }

        [Test]
        public void GetBrokerageModel_ReturnsOKXBrokerageModel()
        {
            // Arrange
            var factory = new OKXBrokerageFactory();

            // Act
            var model = factory.GetBrokerageModel(null);

            // Assert
            Assert.IsNotNull(model);
            Assert.IsInstanceOf<OKXBrokerageModel>(model);
        }

        [Test]
        public void CreateBrokerage_WithValidCredentials_CreatesInstance()
        {
            // Arrange
            var factory = new OKXBrokerageFactory();
            var job = new LiveNodePacket
            {
                BrokerageData = new Dictionary<string, string>
                {
                    { "okx-api-key", _apiKey },
                    { "okx-api-secret", _apiSecret },
                    { "okx-passphrase", _passphrase }
                }
            };

            // Act
            var brokerage = factory.CreateBrokerage(job, null);

            // Assert
            Assert.IsNotNull(brokerage);
            Assert.IsInstanceOf<OKXBrokerage>(brokerage);

            // Cleanup
            brokerage.Dispose();
        }

        [Test]
        public void CreateBrokerage_WithMissingApiKey_ThrowsException()
        {
            // Arrange
            var factory = new OKXBrokerageFactory();
            var job = new LiveNodePacket
            {
                BrokerageData = new Dictionary<string, string>
                {
                    { "okx-api-key", "" },
                    { "okx-api-secret", _apiSecret },
                    { "okx-passphrase", _passphrase }
                }
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => factory.CreateBrokerage(job, null));
            Assert.IsTrue(exception.Message.Contains("okx-api-key"));
        }

        [Test]
        public void CreateBrokerage_WithMissingApiSecret_ThrowsException()
        {
            // Arrange
            var factory = new OKXBrokerageFactory();
            var job = new LiveNodePacket
            {
                BrokerageData = new Dictionary<string, string>
                {
                    { "okx-api-key", _apiKey },
                    { "okx-api-secret", "" },
                    { "okx-passphrase", _passphrase }
                }
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => factory.CreateBrokerage(job, null));
            Assert.IsTrue(exception.Message.Contains("okx-api-secret"));
        }

        [Test]
        public void CreateBrokerage_WithMissingPassphrase_ThrowsException()
        {
            // Arrange
            var factory = new OKXBrokerageFactory();
            var job = new LiveNodePacket
            {
                BrokerageData = new Dictionary<string, string>
                {
                    { "okx-api-key", _apiKey },
                    { "okx-api-secret", _apiSecret },
                    { "okx-passphrase", "" }
                }
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => factory.CreateBrokerage(job, null));
            Assert.IsTrue(exception.Message.Contains("okx-passphrase"));
        }

        [Test]
        public void BrokerageModel_DefaultMarkets_ContainsOKX()
        {
            // Arrange & Act
            var model = new OKXBrokerageModel();

            // Assert
            Assert.IsNotNull(model.DefaultMarkets);
            Assert.IsTrue(model.DefaultMarkets.ContainsKey(SecurityType.Crypto));
            Assert.AreEqual(Market.OKX, model.DefaultMarkets[SecurityType.Crypto]);
            Assert.IsTrue(model.DefaultMarkets.ContainsKey(SecurityType.CryptoFuture));
            Assert.AreEqual(Market.OKX, model.DefaultMarkets[SecurityType.CryptoFuture]);
        }

        [Test]
        public void FeeModel_CanInstantiate()
        {
            // Arrange & Act
            var feeModel = new OKXFeeModel();

            // Assert
            Assert.IsNotNull(feeModel);
            Assert.IsInstanceOf<Orders.Fees.FeeModel>(feeModel);
        }
    }
}
