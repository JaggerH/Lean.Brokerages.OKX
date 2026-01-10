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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage implementation for OKX v5 API
    /// Supports spot and derivatives trading on OKX exchange using unified account API
    /// </summary>
    public class OKXBrokerage : OKXBaseBrokerage
    {

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
            : this(apiKey, apiSecret, passphrase, algorithm, Composer.Instance.GetPart<IDataAggregator>())
        {
        }

        /// <summary>
        /// Creates a new instance of OKXBrokerage with data aggregator
        /// </summary>
        /// <param name="apiKey">OKX API key</param>
        /// <param name="apiSecret">OKX API secret</param>
        /// <param name="passphrase">OKX API passphrase</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="aggregator">Data aggregator for tick consolidation</param>
        public OKXBrokerage(
            string apiKey,
            string apiSecret,
            string passphrase,
            IAlgorithm algorithm,
            IDataAggregator aggregator)
            : base(apiKey, apiSecret, passphrase, algorithm, aggregator, null)
        {
            Log.Trace($"OKXBrokerage(): Initialized for {OKXEnvironment.GetEnvironmentName()} environment");
        }


        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders</returns>
        public override List<Order> GetOpenOrders()
        {
            return RestApiClient.GetOpenOrders();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            return RestApiClient.GetAccountHoldings();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            return RestApiClient.GetCashBalance();
        }

        // ========================================
        // ABSTRACT METHOD IMPLEMENTATIONS
        // ========================================

        /// <summary>
        /// Initializes the REST API client
        /// </summary>
        protected override void InitializeRestClient(string apiKey, string apiSecret)
        {
            // OKX requires passphrase in addition to key/secret
            // This is stored during Initialize() call in base
            // RestApiClient property is set in base class
        }

        /// <summary>
        /// Sends authentication request to private WebSocket channel
        /// </summary>
        protected override void SendAuthenticationRequest()
        {
            // Authentication is handled automatically by BaseWebsocketsBrokerage
            // using apiKey and apiSecret stored during Initialize()
            Log.Trace("OKXBrokerage.SendAuthenticationRequest(): WebSocket authentication initiated");
        }

        /// <summary>
        /// Subscribes to private channels (orders, account, positions)
        /// </summary>
        protected override void SubscribePrivateChannels()
        {
            // Private channel subscriptions are handled by base class WebSocket infrastructure
            // Subscription messages are sent automatically after authentication
            Log.Trace("OKXBrokerage.SubscribePrivateChannels(): Private channels will be subscribed after authentication");
        }

        /// <summary>
        /// Unsubscribes from private channels
        /// </summary>
        protected override void UnsubscribePrivateChannels()
        {
            // Unsubscribe from private channels
            Log.Trace("OKXBrokerage.UnsubscribePrivateChannels(): Unsubscribed from private channels");
        }

        /// <summary>
        /// Validates account configuration
        /// Ensures position mode is net_mode (one-way) and logs account level
        /// Throws exception if validation fails
        /// </summary>
        protected override void ValidateAccountMode()
        {
            Log.Trace("OKXBrokerage.ValidateAccountMode(): Validating account configuration");

            try
            {
                var config = RestApiClient.GetAccountConfiguration();

                if (config == null)
                {
                    var errorMessage = "Failed to retrieve account configuration from OKX. Please verify API credentials and permissions.";
                    Log.Error($"OKXBrokerage.ValidateAccountMode(): {errorMessage}");
                    throw new Exception(errorMessage);
                }

                // Map account level to human-readable description
                var accountLevelDescription = config.AccountLevel switch
                {
                    "1" => "Simple (Spot trading only)",
                    "2" => "Single-currency margin",
                    "3" => "Multi-currency margin",
                    "4" => "Portfolio margin",
                    _ => $"Unknown (Level: {config.AccountLevel})"
                };

                Log.Trace($"OKXBrokerage.ValidateAccountMode(): Account Level: {accountLevelDescription}");

                // Validate position mode - must be net_mode (one-way)
                if (config.PositionMode != "net_mode")
                {
                    var errorMessage = $"Position mode mismatch: current='{config.PositionMode}', required='net_mode'. " +
                                     $"Please change your OKX account position mode to 'One-way Mode' (net_mode) in your account settings. " +
                                     $"Go to OKX -> Settings -> Trading Preferences -> Position Mode -> Select 'One-way Mode'.";
                    Log.Error($"OKXBrokerage.ValidateAccountMode(): {errorMessage}");
                    throw new Exception(errorMessage);
                }

                Log.Trace($"OKXBrokerage.ValidateAccountMode(): Position mode validated successfully (net_mode)");
            }
            catch (Exception ex) when (!(ex.Message.Contains("Position mode mismatch") || ex.Message.Contains("Failed to retrieve account configuration")))
            {
                // Log other errors but don't block connection (API might be temporarily unavailable)
                Log.Error($"OKXBrokerage.ValidateAccountMode(): Warning - failed to validate account configuration: {ex.Message}");
            }
        }
    }
}
