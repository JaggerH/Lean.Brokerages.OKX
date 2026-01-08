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

using QuantConnect.Configuration;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX environment configuration helper
    /// Manages URLs for production vs testnet environments
    /// </summary>
    public static class OKXEnvironment
    {
        /// <summary>
        /// Production REST API URL (complete URL with /api/v4 prefix)
        /// </summary>
        public const string ProductionApiUrl = "https://api.okxio.ws/api/v4";

        /// <summary>
        /// Production Spot WebSocket URL
        /// </summary>
        public const string ProductionWebSocketUrl = "wss://api.okxio.ws/ws/v4/";

        /// <summary>
        /// Production Futures WebSocket URL (USDT settled perpetual contracts)
        /// Official: wss://fx-ws.okxio.ws/v4/ws/usdt
        /// </summary>
        public const string ProductionFuturesWebSocketUrl = "wss://fx-ws.okxio.ws/v4/ws/usdt";

        /// <summary>
        /// Testnet REST API URL (complete URL with /api/v4 prefix)
        /// </summary>
        public const string TestnetApiUrl = "https://api-testnet.okxapi.io/api/v4";

        /// <summary>
        /// Testnet Spot WebSocket URL (for testing spot trading operations)
        /// Official: wss://ws-testnet.okx.com/v4/ws/spot
        /// </summary>
        public const string TestnetWebSocketUrl = "wss://ws-testnet.okx.com/v4/ws/spot";

        /// <summary>
        /// Testnet Futures WebSocket URL (USDT settled perpetual contracts)
        /// Official: wss://ws-testnet.okx.com/v4/ws/futures/usdt
        /// </summary>
        public const string TestnetFuturesWebSocketUrl = "wss://ws-testnet.okx.com/v4/ws/futures/usdt";

        /// <summary>
        /// Sandbox WebSocket URL (for unit tests with MockOKXWebSocketServer)
        /// Default: ws://127.0.0.1:19999
        /// Can be overridden via "okx-websocket-url" config key
        /// </summary>
        public const string SandboxWebSocketUrl = "ws://127.0.0.1:19999";

        /// <summary>
        /// Gets whether we're running in sandbox mode (unit tests with mock server)
        /// </summary>
        public static bool IsSandbox
        {
            get
            {
                var environment = Config.Get("okx-environment", "live").ToLowerInvariant();
                return environment == "sandbox";
            }
        }

        /// <summary>
        /// Gets whether we're running in testnet mode (official OKX testnet)
        /// </summary>
        public static bool IsTestnet
        {
            get
            {
                var environment = Config.Get("okx-environment", "live").ToLowerInvariant();
                return environment == "testnet" || environment == "test";
            }
        }

        /// <summary>
        /// Gets the appropriate REST API URL based on current environment
        /// </summary>
        public static string GetRestApiUrl()
        {
            return IsTestnet ? TestnetApiUrl : ProductionApiUrl;
        }

        /// <summary>
        /// Gets the appropriate WebSocket URL based on current environment and security type
        /// Priority: Sandbox (mock server) > Testnet (official) > Production
        /// </summary>
        /// <param name="securityType">Security type (Crypto for spot, CryptoFuture for perpetuals)</param>
        public static string GetWebSocketUrl(string channelName = "spot")
        {
            // Priority 1: Sandbox mode (unit tests with MockOKXWebSocketServer)
            // In sandbox, both spot and futures connect to the same mock server
            if (IsSandbox)
                return Config.Get("okx-websocket-url", SandboxWebSocketUrl);

            var isFutures = channelName == "futures";
            // Priority 2: Testnet mode (official OKX testnet)
            if (IsTestnet)
            {
                return isFutures ? TestnetFuturesWebSocketUrl : TestnetWebSocketUrl;
            }

            // Priority 3: Production mode (live trading)
            return isFutures ? ProductionFuturesWebSocketUrl : ProductionWebSocketUrl;
        }

        /// <summary>
        /// Gets the current environment name for logging
        /// </summary>
        public static string GetEnvironmentName()
        {
            if (IsSandbox) return "Sandbox";
            if (IsTestnet) return "Testnet";
            return "Production";
        }
    }
}