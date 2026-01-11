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
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
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
        /// Sends authentication request to private WebSocket channel
        /// OKX WebSocket login format: {"op": "login", "args": [{apiKey, passphrase, timestamp, sign}]}
        /// Signature: timestamp + "GET" + "/users/self/verify"
        /// </summary>
        protected override void SendAuthenticationRequest()
        {
            try
            {
                // Generate timestamp (Unix seconds)
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Build signature string: timestamp + method + requestPath
                var method = "GET";
                var requestPath = "/users/self/verify";
                var signatureInput = timestamp + method + requestPath;

                // Generate HMAC-SHA256 signature
                var signature = OKXUtility.GenerateHmacSignature(signatureInput, ApiSecret);

                // Build login message
                var loginMessage = new Messages.OKXWebSocketMessage
                {
                    Operation = "login",
                    Arguments = new List<object>
                    {
                        new Messages.OKXWebSocketLoginArgs
                        {
                            ApiKey = ApiKey,
                            Passphrase = Passphrase,
                            Timestamp = timestamp,
                            Sign = signature
                        }
                    }
                };

                // Send login request
                var message = JsonConvert.SerializeObject(loginMessage);
                WebSocket.Send(message);

                Log.Trace("OKXBrokerage.SendAuthenticationRequest(): Login request sent");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.SendAuthenticationRequest(): Error: {ex}");
            }
        }

        /// <summary>
        /// Subscribes to private channels (orders, account, positions)
        /// Called automatically after successful WebSocket authentication
        /// </summary>
        protected override void SubscribePrivateChannels()
        {
            try
            {
                Log.Trace("OKXBrokerage.SubscribePrivateChannels(): Starting private channel subscription...");

                // Subscribe to orders channel for all instrument types
                // This provides real-time order updates and fill information
                var ordersChannel = new Messages.OKXWebSocketChannel
                {
                    Channel = "orders",
                    InstrumentType = "ANY"  // Subscribe to all instrument types (SPOT, SWAP, FUTURES, etc.)
                };

                var subscribeMessage = new Messages.OKXWebSocketMessage
                {
                    Operation = "subscribe",
                    Arguments = new List<object> { ordersChannel }
                };

                var message = JsonConvert.SerializeObject(subscribeMessage);
                Log.Trace($"OKXBrokerage.SubscribePrivateChannels(): Sending subscription message: {message}");

                WebSocket.Send(message);

                Log.Trace("OKXBrokerage.SubscribePrivateChannels(): Subscription message sent for orders channel (instType: ANY)");
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBrokerage.SubscribePrivateChannels(): Error subscribing to private channels: {ex}");
                OnMessage(new BrokerageMessageEvent(
                    BrokerageMessageType.Error,
                    "PrivateChannelSubscription",
                    $"Failed to subscribe to private channels: {ex.Message}"));
            }
        }

        /// <summary>
        /// Validates and configures account settings
        /// 1. Validates acctLv matches config (cannot auto-fix, requires manual change)
        /// 2. Auto-sets posMode to net_mode if needed
        /// 3. Auto-sets autoLoan to true if needed (for multi-currency/portfolio margin)
        /// 4. Auto-sets feeType to "1" (quote currency) if needed
        /// 5. Auto-sets settleCcy to "USDT" if needed
        /// </summary>
        protected override void ValidateAccountMode()
        {
            Log.Trace("OKXBrokerage.ValidateAccountMode(): Validating and configuring account settings");

            try
            {
                var config = RestApiClient.GetAccountConfiguration();

                if (config == null)
                {
                    throw new Exception("Failed to retrieve account configuration from OKX. Please verify API credentials and permissions.");
                }

                // 1. Validate account level (cannot auto-fix)
                var configuredLevel = Configuration.Config.Get("okx-unified-account-mode", "1");
                var accountLevelDescription = config.AccountLevel switch
                {
                    "1" => "Simple (Spot only)",
                    "2" => "Single-currency margin",
                    "3" => "Multi-currency margin",
                    "4" => "Portfolio margin",
                    _ => $"Unknown ({config.AccountLevel})"
                };
                Log.Trace($"OKXBrokerage.ValidateAccountMode(): Account Level: {accountLevelDescription}");

                if (config.AccountLevel != configuredLevel)
                {
                    throw new Exception($"Account level mismatch: OKX account is '{accountLevelDescription}', but config expects level '{configuredLevel}'. " +
                                       $"Please update 'okx-unified-account-mode' in config or change your OKX account level.");
                }

                // 2. Position mode: auto-set to net_mode
                if (config.PositionMode != "net_mode")
                {
                    Log.Trace($"OKXBrokerage.ValidateAccountMode(): Setting position mode from '{config.PositionMode}' to 'net_mode'");
                    if (!RestApiClient.SetPositionMode("net_mode"))
                    {
                        throw new Exception("Failed to set position mode to 'net_mode'. Please close all positions and try again, or set manually in OKX settings.");
                    }
                }
                Log.Trace("OKXBrokerage.ValidateAccountMode(): Position mode: net_mode ✓");

                // 3. Auto loan: auto-set to true (only for multi-currency/portfolio margin modes)
                if (config.AccountLevel == "3" || config.AccountLevel == "4")
                {
                    if (!config.AutoLoan)
                    {
                        Log.Trace("OKXBrokerage.ValidateAccountMode(): Setting autoLoan to true");
                        if (!RestApiClient.SetAutoLoan(true))
                        {
                            Log.Error("OKXBrokerage.ValidateAccountMode(): Failed to set autoLoan to true");
                        }
                    }
                    Log.Trace($"OKXBrokerage.ValidateAccountMode(): Auto loan: {config.AutoLoan} ✓");
                }

                // 4. Fee type: auto-set to "1" (quote currency)
                var targetFeeType = "0";
                if (config.FeeType != targetFeeType)
                {
                    Log.Trace($"OKXBrokerage.ValidateAccountMode(): Setting feeType from '{config.FeeType}' to '1' (quote currency)");
                    if (!RestApiClient.SetFeeType(targetFeeType))
                    {
                        Log.Error("OKXBrokerage.ValidateAccountMode(): Failed to set feeType to '1'. Spot fees will be charged in received currency.");
                    }
                }
                Log.Trace("OKXBrokerage.ValidateAccountMode(): Fee type: quote currency ✓");

                // 5. Settlement currency: auto-set to USDT (only if available in settleCcyList)
                const string targetSettleCcy = "USDT";
                if (config.SettleCurrencyList?.Contains(targetSettleCcy) == true)
                {
                    if (config.SettleCurrency != targetSettleCcy)
                    {
                        Log.Trace($"OKXBrokerage.ValidateAccountMode(): Setting settleCcy from '{config.SettleCurrency}' to '{targetSettleCcy}'");
                        if (!RestApiClient.SetSettleCurrency(targetSettleCcy))
                        {
                            Log.Error($"OKXBrokerage.ValidateAccountMode(): Failed to set settleCcy to '{targetSettleCcy}'");
                        }
                    }
                    Log.Trace($"OKXBrokerage.ValidateAccountMode(): Settlement currency: {targetSettleCcy} ✓");
                }
                else
                {
                    Log.Trace($"OKXBrokerage.ValidateAccountMode(): Settlement currency: {config.SettleCurrency} (USDT not available, options: {string.Join(", ", config.SettleCurrencyList ?? new List<string>())})");
                }

                Log.Trace("OKXBrokerage.ValidateAccountMode(): Account configuration validated successfully");
            }
            catch (Exception ex) when (!ex.Message.Contains("mismatch") && !ex.Message.Contains("Failed to retrieve"))
            {
                Log.Error($"OKXBrokerage.ValidateAccountMode(): Warning - {ex.Message}");
            }
        }
    }
}
