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
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.OKX.RestApi
{
    /// <summary>
    /// Base REST API client for OKX
    /// Handles authentication, common endpoints, and request signing
    /// </summary>
    public abstract class OKXBaseRestApiClient
    {
        /// <summary>
        /// Unix Epoch for timestamp calculations
        /// </summary>
        protected readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        protected readonly string _apiKey;
        protected readonly string _apiSecret;
        protected readonly string _passphrase;
        protected readonly IAlgorithm _algorithm;
        protected readonly string _restApiUrl;
        protected readonly RestClient _restClient;
        protected readonly ISymbolMapper _symbolMapper;

        // Rate limiters for OKX API v5
        // Based on OKX API documentation: https://www.okx.com/docs-v5/en/#overview-rate-limit
        private readonly RateGate _orderRateLimiter;     // Trading endpoints rate limit
        private readonly RateGate _accountRateLimiter;   // Account endpoints rate limit
        private readonly RateGate _publicRateLimiter;    // Public data endpoints rate limit

        // Time synchronization
        // Set once during initialization in SyncServerTime(), no concurrent writes
        private long _timeOffsetMs = 0;

        // ========================================
        // ABSTRACT PROPERTIES (must be overridden)
        // ========================================

        /// <summary>
        /// Returns the API path prefix for this market type
        /// Examples: "/spot", "/futures/usdt"
        /// </summary>
        protected abstract string ApiPrefix { get; }

        /// <summary>
        /// Returns the symbol parameter name for REST API calls
        /// Spot uses "currency_pair", Futures uses "contract"
        /// </summary>
        protected abstract string SymbolParameterName { get; }

        // ========================================
        // CONSTRUCTORS
        // ========================================

        /// <summary>
        /// Creates a new REST API client instance
        /// </summary>
        /// <param name="apiKey">OKX API key</param>
        /// <param name="apiSecret">OKX API secret</param>
        /// <param name="passphrase">OKX API passphrase</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="restApiUrl">REST API base URL</param>
        protected OKXBaseRestApiClient(string apiKey, string apiSecret, string passphrase, IAlgorithm algorithm, string restApiUrl)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
            _algorithm = algorithm;
            _restApiUrl = restApiUrl;

            _restClient = new RestClient(restApiUrl);
            _symbolMapper = new OKXSymbolMapper(Market.OKX);

            // Initialize rate limiters for OKX API v5
            // Trading endpoints: 60 requests per 2 seconds (conservative estimate)
            _orderRateLimiter = new RateGate(60, TimeSpan.FromSeconds(2));
            // Account endpoints: 10 requests per 2 seconds
            _accountRateLimiter = new RateGate(10, TimeSpan.FromSeconds(2));
            // Public data endpoints: 20 requests per 2 seconds
            _publicRateLimiter = new RateGate(20, TimeSpan.FromSeconds(2));

            SyncServerTime();
        }

        // ========================================
        // COMMON METHODS (shared across all markets)
        // ========================================

        /// <summary>
        /// Gets the current server time from OKX
        /// https://www.okx.com/docs-v5/en/#rest-api-public-data-get-system-time
        /// No authentication required
        /// </summary>
        /// <returns>Server time in milliseconds (Unix timestamp), or null if request fails</returns>
        public virtual long? GetServerTime()
        {
            // OKX v5 API: GET /api/v5/public/time
            var request = new RestRequest("/api/v5/public/time", Method.GET);
            var response = ExecuteRestRequest(request);
            var result = DeserializeOrDefault<OKXApiResponse<ServerTime>>(response, nameof(GetServerTime));

            if (result == null || !result.IsSuccess || result.Data == null || result.Data.Count == 0)
            {
                Log.Error($"OKXBaseRestApiClient.GetServerTime(): Invalid response - code: {result?.Code}, msg: {result?.Message}");
                return null;
            }

            return result.Data[0].ServerTimeSeconds;
        }

        /// <summary>
        /// Synchronizes with OKX server time to calculate time offset
        /// This ensures all timestamp-based requests are aligned with server clock
        /// Note: GetServerTime() already has built-in retry logic and error handling
        /// </summary>
        public virtual void SyncServerTime()
        {
            // Capture client time before making request
            var clientTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Get server time (in seconds) - GetServerTime() already retries internally
            var serverTimeSec = GetServerTime();

            if (!serverTimeSec.HasValue)
            {
                throw new Exception("Failed to synchronize with server time");
            }

            // OKX API returns time in milliseconds (despite the property name "ServerTimeSeconds")
            // No conversion needed - serverTimeSec is already in milliseconds
            var serverTimeMs = serverTimeSec.Value;

            // Calculate offset: serverTime - clientTime
            // OKX API has 30-second tolerance, so we don't need additional safety margin
            // (unlike Gate.io which has stricter ~5-second tolerance)
            var rawOffset = serverTimeMs - clientTimeMs;
            _timeOffsetMs = rawOffset;
        }

        // ========================================
        // ABSTRACT METHODS (must be implemented)
        // ========================================

        /// <summary>
        /// Gets account cash balances
        /// </summary>
        public abstract List<CashAmount> GetCashBalance();

        /// <summary>
        /// Gets account holdings (positions)
        /// </summary>
        public abstract List<Holding> GetAccountHoldings();

        /// <summary>
        /// Gets all open orders
        /// </summary>
        public abstract List<QuantConnect.Orders.Order> GetOpenOrders();

        // ========================================
        // AUTHENTICATION METHODS
        // ========================================

        /// <summary>
        /// Generates a timestamp for API requests
        /// OKX v5 API requires ISO 8601 format with millisecond precision
        /// Example: "2020-12-08T09:08:57.715Z"
        /// https://www.okx.com/docs-v5/en/#rest-api-authentication
        /// </summary>
        protected string GetNonce()
        {
            // Get current time with dynamic offset applied
            // No locking needed - _timeOffsetMs is set once during initialization
            var now = DateTimeOffset.UtcNow.AddMilliseconds(_timeOffsetMs);

            // OKX API v5 requires ISO 8601 format: "2020-12-08T09:08:57.715Z"
            // Format: yyyy-MM-ddTHH:mm:ss.fffZ (millisecond precision, UTC timezone)
            return now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets Unix timestamp in seconds (integer) with offset applied
        /// Used for WebSocket messages that expect integer timestamps
        /// </summary>
        public long GetUnixTimestamp()
        {
            // No locking needed - _timeOffsetMs is set once during initialization
            return DateTimeOffset.UtcNow.AddMilliseconds(_timeOffsetMs).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Signs a REST API request according to OKX v5 authentication requirements
        /// https://www.okx.com/docs-v5/en/#rest-api-authentication
        ///
        /// OKX v5 Signature Process:
        /// 1. Generate signature string: timestamp + method + requestPath + body
        /// 2. Sign with HMAC-SHA256 using API secret
        /// 3. Convert to Base64 string
        /// </summary>
        /// <param name="request">The REST request to sign</param>
        /// <param name="endpoint">API endpoint path (e.g., "/api/v5/account/balance")</param>
        /// <param name="queryString">Query string parameters (for GET requests, include "?" prefix)</param>
        /// <param name="body">Request body JSON (for POST/DELETE requests)</param>
        protected void SignRequest(IRestRequest request, string endpoint, string queryString = "", string body = "")
        {
            var timestamp = GetNonce();

            // Build request path (endpoint + query string if present)
            var requestPath = endpoint;
            if (!string.IsNullOrEmpty(queryString))
            {
                // Ensure query string starts with "?"
                requestPath = queryString.StartsWith("?") ? endpoint + queryString : endpoint + "?" + queryString;
            }

            // Build signature string: timestamp + method + requestPath + body
            var method = request.Method.ToString().ToUpperInvariant();
            var signatureString = timestamp + method + requestPath + (body ?? string.Empty);

            // Generate signature using HMAC-SHA256
            var signature = OKXUtility.GenerateHmacSignature(signatureString, _apiSecret);

            // Add required OKX v5 headers
            request.AddHeader("OK-ACCESS-KEY", _apiKey);
            request.AddHeader("OK-ACCESS-SIGN", signature);
            request.AddHeader("OK-ACCESS-TIMESTAMP", timestamp);
            request.AddHeader("OK-ACCESS-PASSPHRASE", _passphrase);
            request.AddHeader("Content-Type", "application/json");

            if (OKXEnvironment.IsTestnet)
            {
                request.AddHeader("x-simulated-trading", "1");
            }
        }

        // ========================================
        // REST REQUEST HELPERS
        // ========================================

        /// <summary>
        /// Executes a REST request with rate limiting and exponential backoff retry logic
        /// </summary>
        protected IRestResponse ExecuteRestRequest(IRestRequest request)
        {
            const int maxAttempts = 10;
            const int baseDelayMs = 1000; // Start with 1 second
            const int maxDelayMs = 30000; // Cap at 30 seconds
            var attempts = 0;
            IRestResponse response;

            do
            {
                // Apply exponential backoff delay after first attempt
                if (attempts > 0)
                {
                    // Calculate delay: 1s, 2s, 4s, 8s, 16s, 30s (capped)
                    var delayMs = Math.Min(baseDelayMs * (int)Math.Pow(2, attempts - 1), maxDelayMs);
                    Log.Trace($"OKXBaseRestApiClient.ExecuteRestRequest(): Rate limited, waiting {delayMs}ms before retry {attempts + 1}/{maxAttempts}");
                    System.Threading.Thread.Sleep(delayMs);
                }

                response = _restClient.Execute(request);

                // 429 status code: Too Many Requests
            } while (++attempts < maxAttempts && (int)response.StatusCode == 429);

            if ((int)response.StatusCode == 429)
            {
                Log.Error($"OKXBaseRestApiClient.ExecuteRestRequest(): Rate limit exceeded after {maxAttempts} attempts");
            }

            return response;
        }

        /// <summary>
        /// Executes an authenticated REST request with automatic signing
        /// Handles endpoint construction, query string, and request body
        /// </summary>
        /// <param name="resource">Resource path relative to API prefix (e.g., "accounts", "orders/123")</param>
        /// <param name="method">HTTP method (GET, POST, DELETE, etc.)</param>
        /// <param name="queryString">Query string parameters (without leading "?")</param>
        /// <param name="body">Request body object (will be JSON serialized)</param>
        /// <returns>REST response</returns>
        protected IRestResponse ExecuteAuthenticatedRequest(
            string resource,
            Method method,
            string queryString = "",
            object body = null)
        {
            var endpoint = GetEndpoint(resource);
            var bodyJson = body != null ? JsonConvert.SerializeObject(body) : "";

            var fullUrl = string.IsNullOrEmpty(queryString)
                ? endpoint
                : $"{endpoint}?{queryString}";

            var request = new RestRequest(fullUrl, method);

            if (!string.IsNullOrEmpty(bodyJson))
            {
                request.AddParameter("application/json", bodyJson, ParameterType.RequestBody);
            }

            SignRequest(request, endpoint, queryString, bodyJson);
            return ExecuteRestRequest(request);
        }

        /// <summary>
        /// Deserializes REST response or returns default value on error
        /// Handles status code validation and JSON deserialization with error logging
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="response">REST response to deserialize</param>
        /// <param name="methodName">Calling method name for error logging</param>
        /// <param name="defaultValue">Value to return on error</param>
        /// <param name="successCodes">Valid HTTP status codes (defaults to 200 OK)</param>
        /// <returns>Deserialized object or default value</returns>
        protected T DeserializeOrDefault<T>(
            IRestResponse response,
            string methodName,
            T defaultValue = default,
            params HttpStatusCode[] successCodes)
        {
            var validCodes = successCodes.Length > 0
                ? successCodes
                : new[] { HttpStatusCode.OK };

            if (!validCodes.Contains(response.StatusCode))
            {
                Log.Error($"OKXBaseRestApiClient.{methodName}(): request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}");
                return defaultValue;
            }

            // Handle NoContent response (no body to deserialize)
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return defaultValue;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<T>(response.Content);
                return result ?? defaultValue;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBaseRestApiClient.{methodName}(): failed to deserialize response: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Executes a GET request with authentication
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="resource">Resource path (e.g., "accounts", "orders/123")</param>
        /// <param name="queryString">Query string parameters (optional)</param>
        /// <param name="defaultValue">Value to return on error</param>
        /// <param name="methodName">Calling method name for logging (auto-detected if not provided)</param>
        /// <returns>Deserialized response or default value</returns>
        protected T Get<T>(
            string resource,
            string queryString = "",
            T defaultValue = default,
            [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var response = ExecuteAuthenticatedRequest(resource, Method.GET, queryString);
            return DeserializeOrDefault(response, methodName, defaultValue);
        }

        /// <summary>
        /// Executes a POST request with authentication
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="resource">Resource path (e.g., "orders")</param>
        /// <param name="body">Request body object (will be JSON serialized)</param>
        /// <param name="defaultValue">Value to return on error</param>
        /// <param name="methodName">Calling method name for logging (auto-detected if not provided)</param>
        /// <returns>Deserialized response or default value</returns>
        protected T Post<T>(
            string resource,
            object body,
            T defaultValue = default,
            [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var response = ExecuteAuthenticatedRequest(resource, Method.POST, "", body);
            return DeserializeOrDefault(response, methodName, defaultValue);
        }

        /// <summary>
        /// Executes a DELETE request with authentication
        /// </summary>
        /// <param name="resource">Resource path (e.g., "orders/123")</param>
        /// <param name="queryString">Query string parameters (optional)</param>
        /// <param name="methodName">Calling method name for logging (auto-detected if not provided)</param>
        /// <returns>True if successful (200 OK or 204 NoContent), false otherwise</returns>
        protected bool Delete(
            string resource,
            string queryString = "",
            [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var response = ExecuteAuthenticatedRequest(resource, Method.DELETE, queryString);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }

            Log.Error($"OKXBaseRestApiClient.{methodName}(): request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}");
            return false;
        }

        // ========================================
        // UNAUTHENTICATED REST REQUEST HELPERS
        // ========================================

        /// <summary>
        /// Executes an unauthenticated REST request (no signing)
        /// Used for public endpoints that don't require credentials
        /// </summary>
        /// <param name="resource">The API resource path</param>
        /// <param name="method">HTTP method (GET, POST, DELETE)</param>
        /// <param name="queryString">Optional query string parameters</param>
        /// <param name="body">Optional request body object (will be JSON serialized)</param>
        /// <returns>The REST response</returns>
        protected IRestResponse ExecuteUnauthenticatedRequest(
            string resource,
            Method method,
            string queryString = "",
            object body = null)
        {
            var endpoint = GetEndpoint(resource);
            var bodyJson = body != null ? JsonConvert.SerializeObject(body) : "";

            var fullUrl = string.IsNullOrEmpty(queryString)
                ? endpoint
                : $"{endpoint}?{queryString}";

            var request = new RestRequest(fullUrl, method);

            if (!string.IsNullOrEmpty(bodyJson))
            {
                request.AddParameter("application/json", bodyJson, ParameterType.RequestBody);
            }

            if (OKXEnvironment.IsTestnet)
            {
                request.AddHeader("x-simulated-trading", "1");
            }

            // NO SignRequest() call - that's the key difference from ExecuteAuthenticatedRequest
            return ExecuteRestRequest(request);
        }

        /// <summary>
        /// Executes a GET request WITHOUT authentication
        /// Used for public endpoints that don't require credentials
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="resource">The API resource path</param>
        /// <param name="queryString">Optional query string parameters</param>
        /// <param name="defaultValue">Default value to return on failure</param>
        /// <param name="methodName">Calling method name (auto-populated)</param>
        /// <returns>Deserialized response object or default value</returns>
        protected T GetPublic<T>(
            string resource,
            string queryString = "",
            T defaultValue = default,
            [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var response = ExecuteUnauthenticatedRequest(resource, Method.GET, queryString);
            return DeserializeOrDefault(response, methodName, defaultValue);
        }

        /// <summary>
        /// Executes a POST request WITHOUT authentication
        /// Used for public endpoints that don't require credentials
        /// </summary>
        /// <typeparam name="T">Expected response type</typeparam>
        /// <param name="resource">The API resource path</param>
        /// <param name="body">Request body object (will be JSON serialized)</param>
        /// <param name="defaultValue">Default value to return on failure</param>
        /// <param name="methodName">Calling method name (auto-populated)</param>
        /// <returns>Deserialized response object or default value</returns>
        protected T PostPublic<T>(
            string resource,
            object body,
            T defaultValue = default,
            [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var response = ExecuteUnauthenticatedRequest(resource, Method.POST, "", body);
            return DeserializeOrDefault(response, methodName, defaultValue);
        }

        /// <summary>
        /// Executes a DELETE request WITHOUT authentication
        /// Used for public endpoints that don't require credentials
        /// </summary>
        /// <param name="resource">The API resource path</param>
        /// <param name="queryString">Optional query string parameters</param>
        /// <param name="methodName">Calling method name (auto-populated)</param>
        /// <returns>True if successful (200/204), false otherwise</returns>
        protected bool DeletePublic(
            string resource,
            string queryString = "",
            [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var response = ExecuteUnauthenticatedRequest(resource, Method.DELETE, queryString);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }

            Log.Error($"OKXBaseRestApiClient.{methodName}(): request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}");
            return false;
        }

        /// <summary>
        /// Gets the REST API endpoint (relative path for RestClient)
        /// This method performs simple path formatting
        /// </summary>
        /// <param name="path">Path relative to API prefix (e.g., "accounts", "orders")</param>
        /// <returns>Full endpoint path (e.g., "/spot/accounts")</returns>
        protected string GetEndpoint(string path)
        {
            // Ensure path starts with /
            var formattedPath = path.StartsWith("/") ? path : $"/{path}";

            // Combine with API prefix
            return $"{ApiPrefix}{formattedPath}";
        }

        // ========================================
        // COMMON REST API METHODS
        // ========================================

        /// <summary>
        /// Cancels an order via REST API
        /// Spot: https://www.okx.io/docs/developers/apiv4/en/#cancel-a-single-order
        /// Futures: https://www.okx.io/docs/developers/futures/en/#cancel-a-single-order
        /// </summary>
        /// <param name="orderId">The broker order ID</param>
        /// <param name="currencyPair">The currency pair (e.g., BTC_USDT)</param>
        /// <returns>True if successfully cancelled</returns>
        public virtual bool CancelOrder(string orderId, string currencyPair)
        {
            var queryString = $"{SymbolParameterName}={currencyPair}";
            return Delete($"orders/{orderId}", queryString);
        }

        /// <summary>
        /// Gets ticker information for a specific currency pair via REST API
        /// Spot: https://www.okx.io/docs/developers/apiv4/en/#retrieve-ticker-information
        /// Futures: https://www.okx.io/docs/developers/futures/en/#list-futures-tickers
        /// </summary>
        /// <param name="currencyPair">The currency pair (e.g., BTC_USDT)</param>
        /// <returns>List of ticker information (single element or empty)</returns>
        public virtual List<Ticker> GetTicker(string currencyPair)
        {
            var queryString = $"{SymbolParameterName}={currencyPair}";
            return Get<List<Ticker>>("tickers", queryString, defaultValue: new List<Ticker>());
        }

        /// <summary>
        /// Gets all tickers via REST API (batch endpoint)
        /// Spot: https://www.okx.io/docs/developers/apiv4/en/#list-tickers
        /// Futures: https://www.okx.io/docs/developers/futures/en/#list-futures-tickers
        /// Note: This is a PUBLIC endpoint (no authentication required)
        /// </summary>
        /// <returns>List of all ticker information</returns>
        public virtual List<Ticker> GetTicker()
        {
            return GetPublic<List<Ticker>>("tickers", defaultValue: new List<Ticker>());
        }

        /// <summary>
        /// Gets recent trades via REST API
        /// Spot: https://www.okx.io/docs/developers/apiv4/en/#retrieve-market-trades
        /// Futures: https://www.okx.io/docs/developers/futures/en/#futures-trading-history
        /// </summary>
        /// <param name="currencyPair">The currency pair (e.g., BTC_USDT)</param>
        /// <param name="limit">Maximum number of trades (default 100, max 1000)</param>
        /// <param name="lastId">Last trade ID for pagination</param>
        /// <returns>List of recent trades</returns>
        public virtual List<Trade> GetRecentTrades(string currencyPair, int limit = 100, string lastId = null)
        {
            var queryParams = new List<string>
            {
                $"{SymbolParameterName}={currencyPair}",
                $"limit={limit}"
            };

            if (!string.IsNullOrEmpty(lastId))
                queryParams.Add($"last_id={lastId}");

            var queryString = string.Join("&", queryParams);
            return Get<List<Trade>>("trades", queryString, defaultValue: new List<Trade>());
        }

        /// <summary>
        /// Gets historical candlestick data via REST API
        /// Spot: https://www.okx.io/docs/developers/apiv4/en/#market-candlesticks
        /// Futures: https://www.okx.io/docs/developers/futures/en/#get-futures-candlesticks
        /// </summary>
        /// <param name="currencyPair">The currency pair (e.g., BTC_USDT)</param>
        /// <param name="interval">Candlestick interval (e.g., 1m, 5m, 1h, 1d)</param>
        /// <param name="from">Start time (Unix timestamp in seconds)</param>
        /// <param name="to">End time (Unix timestamp in seconds)</param>
        /// <param name="limit">Maximum number of candlesticks (default 100, max 1000)</param>
        /// <returns>List of candlestick data</returns>
        public virtual List<object[]> GetCandlesticks(string currencyPair, string interval, long? from = null, long? to = null, int limit = 100)
        {
            var queryParams = new List<string>
            {
                $"{SymbolParameterName}={currencyPair}",
                $"interval={interval}",
                $"limit={limit}"
            };

            if (from.HasValue)
                queryParams.Add($"from={from.Value}");

            if (to.HasValue)
                queryParams.Add($"to={to.Value}");

            var queryString = string.Join("&", queryParams);
            // OKX returns candlesticks as array of arrays:
            // [timestamp, volume, close, high, low, open, amount]
            return Get<List<object[]>>("candlesticks", queryString, defaultValue: new List<object[]>());
        }

        // ========================================
        // SYMBOL LOOKUP
        // ========================================

        /// <summary>
        /// Lookup symbols matching specified criteria
        /// </summary>
        /// <param name="symbol">The symbol to lookup</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency</param>
        /// <returns>Matching symbols</returns>
        public abstract IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null);

        // ========================================
        // WEBSOCKET AUTHENTICATION
        // ========================================

        /// <summary>
        /// Generates WebSocket signature for "api" event type (OKX WebSocket v4 format)
        /// Signature format: api\n{channel}\n{request_param}\n{timestamp}
        /// </summary>
        /// <param name="channel">Channel name (e.g., "spot.login", "spot.order_place")</param>
        /// <param name="requestParam">Request parameters as JSON string (empty for login)</param>
        /// <param name="timestamp">Unix timestamp in seconds</param>
        /// <returns>HMAC-SHA512 signature</returns>
        public string GenerateWebSocketApiSignature(string channel, string requestParam, long timestamp)
        {
            try
            {
                // Signature string format: api\n{channel}\n{request_param}\n{timestamp}
                var signatureString = $"api\n{channel}\n{requestParam}\n{timestamp}";

                // Generate HMAC-SHA512 signature
                var signature = OKXUtility.GenerateHmacSignature(signatureString, _apiSecret);

                return signature;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBaseRestApiClient.GenerateWebSocketApiSignature(): Error generating signature: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Generates WebSocket authentication object for private channels (subscribe events)
        /// </summary>
        /// <param name="channel">Channel name (e.g., "spot.balances", "spot.orders")</param>
        /// <param name="eventType">Event type (e.g., "subscribe")</param>
        /// <param name="timestamp">Unix timestamp in seconds</param>
        /// <returns>Authentication object with method, KEY, and SIGN</returns>
        public object GenerateWebSocketAuth(string channel, string eventType, long timestamp)
        {
            try
            {
                // Signature string format: channel=<channel>&event=<event>&time=<time>
                var signatureString = $"channel={channel}&event={eventType}&time={timestamp}";

                // Generate HMAC-SHA512 signature
                var signature = OKXUtility.GenerateHmacSignature(signatureString, _apiSecret);

                return new
                {
                    method = "api_key",
                    KEY = _apiKey,
                    SIGN = signature
                };
            }
            catch (Exception ex)
            {
                Log.Error($"OKXBaseRestApiClient.GenerateWebSocketAuth(): Error generating auth: {ex}");
                return null;
            }
        }
    }
}
