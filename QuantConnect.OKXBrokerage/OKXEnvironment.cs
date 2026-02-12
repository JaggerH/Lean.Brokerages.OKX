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

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX v5 API environment configuration helper
    /// Manages URLs and settings for live vs demo trading environments
    /// Official documentation: https://www.okx.com/docs-v5/en/#overview-production-trading-services
    /// </summary>
    public static class OKXEnvironment
    {
        // ========================================
        // REST API URLs
        // ========================================

        /// <summary>
        /// REST API base URL (same for both live and demo trading)
        /// Live and demo differentiated by x-simulated-trading header
        /// </summary>
        public const string RestApiUrl = "https://www.okx.com";

        // ========================================
        // WebSocket URLs - Public Channel
        // ========================================

        /// <summary>
        /// Live trading - WebSocket public channel (market data)
        /// Official: wss://ws.okx.com:8443/ws/v5/public
        /// </summary>
        public const string LiveWebSocketPublicUrl = "wss://ws.okx.com:8443/ws/v5/public";

        /// <summary>
        /// Demo trading - WebSocket public channel (market data)
        /// Official: wss://wspap.okx.com:8443/ws/v5/public
        /// </summary>
        public const string DemoWebSocketPublicUrl = "wss://wspap.okx.com:8443/ws/v5/public";

        // ========================================
        // WebSocket URLs - Private Channel
        // ========================================

        /// <summary>
        /// Live trading - WebSocket private channel (account, orders, positions)
        /// Official: wss://ws.okx.com:8443/ws/v5/private
        /// </summary>
        public const string LiveWebSocketPrivateUrl = "wss://ws.okx.com:8443/ws/v5/private";

        /// <summary>
        /// Demo trading - WebSocket private channel (account, orders, positions)
        /// Official: wss://wspap.okx.com:8443/ws/v5/private
        /// </summary>
        public const string DemoWebSocketPrivateUrl = "wss://wspap.okx.com:8443/ws/v5/private";

        // ========================================
        // WebSocket URLs - Business Channel
        // ========================================

        /// <summary>
        /// Live trading - WebSocket business channel (grid trading, algo orders)
        /// Official: wss://ws.okx.com:8443/ws/v5/business
        /// </summary>
        public const string LiveWebSocketBusinessUrl = "wss://ws.okx.com:8443/ws/v5/business";

        /// <summary>
        /// Demo trading - WebSocket business channel (grid trading, algo orders)
        /// Official: wss://wspap.okx.com:8443/ws/v5/business
        /// </summary>
        public const string DemoWebSocketBusinessUrl = "wss://wspap.okx.com:8443/ws/v5/business";

        // ========================================
        // Sandbox (Unit Tests)
        // ========================================

        /// <summary>
        /// Sandbox WebSocket URL (for unit tests with mock server)
        /// Default: ws://127.0.0.1:19999
        /// Can be overridden via "okx-websocket-url" config key
        /// </summary>
        public const string SandboxWebSocketUrl = "ws://127.0.0.1:19999";

        // ========================================
        // Environment Detection (cached from config)
        // ========================================

        private static readonly string _environment = Config.Get("okx-environment", "live").ToLowerInvariant();

        /// <summary>
        /// Whether we're running in sandbox mode (unit tests with mock server)
        /// Config: okx-environment = "sandbox"
        /// </summary>
        public static bool IsSandbox => _environment == "sandbox";

        /// <summary>
        /// Whether we're running in demo/testnet mode (OKX demo trading)
        /// Config: okx-environment = "testnet" or "demo"
        /// </summary>
        public static bool IsTestnet => _environment == "testnet" || _environment == "demo";

        /// <summary>
        /// Whether we're running in live/production mode (real trading)
        /// </summary>
        public static bool IsLiveTrading => !IsSandbox && !IsTestnet;

        // ========================================
        // URL Getters
        // ========================================

        /// <summary>
        /// Gets the REST API base URL
        /// Note: OKX v5 uses the same URL for live and demo trading
        /// Differentiation is done via x-simulated-trading header
        /// </summary>
        public static string GetRestApiUrl()
        {
            return RestApiUrl;
        }

        /// <summary>
        /// Gets the appropriate WebSocket URL for public channel (market data)
        /// </summary>
        public static string GetWebSocketPublicUrl()
        {
            // Sandbox mode (unit tests)
            if (IsSandbox)
                return Config.Get("okx-websocket-url", SandboxWebSocketUrl);

            // Demo trading
            if (IsTestnet)
                return DemoWebSocketPublicUrl;

            // Live trading
            return LiveWebSocketPublicUrl;
        }

        /// <summary>
        /// Gets the appropriate WebSocket URL for private channel (account/orders)
        /// </summary>
        public static string GetWebSocketPrivateUrl()
        {
            // Sandbox mode (unit tests)
            if (IsSandbox)
                return Config.Get("okx-websocket-url", SandboxWebSocketUrl);

            // Demo trading
            if (IsTestnet)
                return DemoWebSocketPrivateUrl;

            // Live trading
            return LiveWebSocketPrivateUrl;
        }

        /// <summary>
        /// Gets the appropriate WebSocket URL for business channel (algo orders)
        /// </summary>
        public static string GetWebSocketBusinessUrl()
        {
            // Sandbox mode (unit tests)
            if (IsSandbox)
                return Config.Get("okx-websocket-url", SandboxWebSocketUrl);

            // Demo trading
            if (IsTestnet)
                return DemoWebSocketBusinessUrl;

            // Live trading
            return LiveWebSocketBusinessUrl;
        }

        /// <summary>
        /// Gets the current environment name for logging
        /// </summary>
        public static string GetEnvironmentName()
        {
            if (IsSandbox) return "Sandbox";
            if (IsTestnet) return "Demo";
            return "Live";
        }
    }
}