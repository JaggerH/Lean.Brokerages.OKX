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
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage implementation for OKX v5 API
    /// Supports spot and derivatives trading on OKX exchange
    /// </summary>
    public partial class OKXBrokerage : Brokerage
    {
        private readonly OKXRestApiClient _restApiClient;
        private readonly ISymbolMapper _symbolMapper;
        private readonly IAlgorithm _algorithm;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _passphrase;

        /// <summary>
        /// Creates a new instance of OKXBrokerage
        /// </summary>
        /// <param name="apiKey">OKX API key</param>
        /// <param name="apiSecret">OKX API secret</param>
        /// <param name="passphrase">OKX API passphrase</param>
        /// <param name="algorithm">The algorithm instance</param>
        public OKXBrokerage(
            string apiKey,
            string apiSecret,
            string passphrase,
            IAlgorithm algorithm)
            : base("OKX")
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
            _algorithm = algorithm;

            _symbolMapper = new OKXSymbolMapper(Market.OKX);
            _restApiClient = new OKXRestApiClient(apiKey, apiSecret, passphrase);

            Log.Trace($"OKXBrokerage(): Initialized for {OKXEnvironment.GetEnvironmentName()} environment");
        }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _restApiClient != null;

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders</returns>
        public override List<Order> GetOpenOrders()
        {
            // TODO: Implement in Phase 4
            return new List<Order>();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            // TODO: Implement in Phase 4
            return new List<Holding>();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            // TODO: Implement in Phase 4
            return new List<CashAmount>();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            // TODO: Implement in Phase 5
            throw new NotImplementedException("OKXBrokerage.PlaceOrder() - Phase 5");
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            // TODO: Implement in Phase 5
            throw new NotImplementedException("OKXBrokerage.UpdateOrder() - Phase 5");
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            // TODO: Implement in Phase 5
            throw new NotImplementedException("OKXBrokerage.CancelOrder() - Phase 5");
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            // Connection is established through REST API client initialization
            Log.Trace("OKXBrokerage.Connect(): Connected to OKX REST API");
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            Log.Trace("OKXBrokerage.Disconnect(): Disconnected from OKX REST API");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            Log.Trace("OKXBrokerage.Dispose(): Disposing OKXBrokerage");
        }
    }
}
