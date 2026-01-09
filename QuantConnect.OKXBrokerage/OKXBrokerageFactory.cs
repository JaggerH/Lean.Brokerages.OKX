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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Provides OKX brokerage factory implementation
    /// Creates brokerage instances that support Spot and Derivatives trading via OKX v5 API
    /// </summary>
    public class OKXBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Constructor for OKX brokerage factory
        /// </summary>
        public OKXBrokerageFactory() : base(typeof(OKXBrokerage))
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "okx-api-key", Config.Get("okx-api-key")},
            { "okx-api-secret", Config.Get("okx-api-secret")},
            { "okx-passphrase", Config.Get("okx-passphrase")},

            // Optional: Environment (production/demo/testnet), defaults to production
            { "okx-environment", Config.Get("okx-environment", "production")},

            // Optional: Unified account mode (cash/isolated/cross), defaults to cash
            { "okx-unified-account-mode", Config.Get("okx-unified-account-mode", "cash")},

            // load holdings if available
            { "live-holdings", Config.Get("live-holdings")},
        };

        /// <summary>
        /// Gets the OKX brokerage model
        /// Supports both Spot (Crypto) and Derivatives (CryptoFuture) trading
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        /// <returns>OKX brokerage model</returns>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider)
        {
            return new OKXBrokerageModel();
        }

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job">Live node packet</param>
        /// <param name="algorithm">Algorithm instance</param>
        /// <returns>OKX brokerage instance configured for unified account trading</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] { "okx-api-key", "okx-api-secret", "okx-passphrase" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                {
                    throw new Exception($"OKXBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
                }
            }

            var brokerage = new OKXBrokerage(
                job.BrokerageData["okx-api-key"],
                job.BrokerageData["okx-api-secret"],
                job.BrokerageData["okx-passphrase"],
                algorithm);

            return brokerage;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            // Nothing to dispose for now
        }
    }
}
