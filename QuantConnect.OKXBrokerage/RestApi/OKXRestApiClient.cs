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
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX.Converters;
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Brokerages.OKX.RestApi;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.OKX.RestApi
{
    /// <summary>
    /// OKX REST API client for unified trading account
    /// </summary>
    public class OKXRestApiClient : OKXBaseRestApiClient
    {
        // Rate limiters for specific OKX endpoints
        // Based on OKX API documentation: https://www.okx.com/docs-v5/en/#overview-rate-limit
        private readonly RateGate _candlesRateLimiter;  // /market/candles: 40 requests per 2 seconds
        private readonly RateGate _tradesRateLimiter;   // /market/trades: 100 requests per 2 seconds
        private readonly RateGate _priceLimitRateLimiter;  // /public/price-limit: 20 requests per 2 seconds
        private readonly RateGate _orderBookRateLimiter;  // /market/books: 40 requests per 2 seconds

        /// <summary>
        /// API prefix for OKX unified account (/api/v5)
        /// </summary>
        protected override string ApiPrefix => "/api/v5";

        /// <summary>
        /// Symbol parameter name for OKX API (instId)
        /// </summary>
        protected override string SymbolParameterName => "instId";

        /// <summary>
        /// Creates a new instance
        /// Note: Uses OKXEnvironment to determine REST API URL based on okx-environment config
        /// </summary>
        public OKXRestApiClient(string apiKey, string apiSecret, string passphrase)
            : this(apiKey, apiSecret, passphrase, OKXEnvironment.GetRestApiUrl())
        {
        }

        /// <summary>
        /// Creates a new instance with explicit REST API URL (for backward compatibility)
        /// </summary>
        public OKXRestApiClient(string apiKey, string apiSecret, string passphrase, string restApiUrl)
            : base(apiKey, apiSecret, passphrase, null, restApiUrl)
        {
            _candlesRateLimiter = new RateGate(40, TimeSpan.FromSeconds(2));
            _tradesRateLimiter = new RateGate(100, TimeSpan.FromSeconds(2));
            _priceLimitRateLimiter = new RateGate(20, TimeSpan.FromSeconds(2));
            _orderBookRateLimiter = new RateGate(40, TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Gets the cash balance for the account
        /// </summary>
        public override List<CashAmount> GetCashBalance()
        {
            try
            {
                var balanceData = GetAccountBalance();
                if (balanceData == null || balanceData.Details == null || balanceData.Details.Count == 0)
                {
                    Log.Error("OKXRestApiClient.GetCashBalance(): Failed to get account balance");
                    return new List<CashAmount>();
                }

                var result = new List<CashAmount>();
                foreach (var detail in balanceData.Details)
                {
                    // Use cashBal (cash balance) instead of availBal (available balance).
                    // In portfolio margin mode, availBal is near-zero because holdings are
                    // frozen as margin collateral. cashBal reflects actual held quantities.
                    var balanceStr = detail.CashBalance;
                    if (string.IsNullOrEmpty(balanceStr))
                        continue;

                    if (decimal.TryParse(balanceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var cashBalance) &&
                        cashBalance > 0)
                    {
                        result.Add(new CashAmount(cashBalance, detail.Currency));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetCashBalance(): Exception: {ex.Message}");
                return new List<CashAmount>();
            }
        }

        /// <summary>
        /// Gets the account holdings
        /// </summary>
        public override List<Holding> GetAccountHoldings()
        {
            try
            {
                var positions = GetPositions();
                if (positions == null || positions.Count == 0)
                {
                    return new List<Holding>();
                }

                var result = new List<Holding>();
                foreach (var position in positions)
                {
                    var holding = position.ToHolding(_symbolMapper);
                    if (holding != null)
                    {
                        result.Add(holding);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetAccountHoldings(): Exception: {ex.Message}");
                return new List<Holding>();
            }
        }

        /// <summary>
        /// Gets the open orders
        /// </summary>
        public override List<QuantConnect.Orders.Order> GetOpenOrders()
        {
            try
            {
                var pendingOrders = GetPendingOrders();
                if (pendingOrders == null || pendingOrders.Count == 0)
                {
                    return new List<QuantConnect.Orders.Order>();
                }

                var result = new List<QuantConnect.Orders.Order>();
                foreach (var okxOrder in pendingOrders)
                {
                    var order = okxOrder.ToLeanOrder(_symbolMapper);
                    if (order != null)
                    {
                        result.Add(order);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetOpenOrders(): Exception: {ex.Message}");
                return new List<QuantConnect.Orders.Order>();
            }
        }

        /// <summary>
        /// Lookup symbols
        /// </summary>
        public override IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
        {
            // TODO: Implement LookupSymbols for OKX
            return Enumerable.Empty<Symbol>();
        }

        /// <summary>
        /// Gets positions for the current account
        /// https://www.okx.com/docs-v5/en/#rest-api-account-get-positions
        /// Requires authentication
        /// </summary>
        /// <param name="instType">Instrument type: MARGIN, SWAP, FUTURES, OPTION (optional)</param>
        /// <param name="instId">Instrument ID (optional, e.g., BTC-USDT-SWAP)</param>
        /// <returns>List of positions, or empty list if request fails</returns>
        public List<Position> GetPositions(string instType = null, string instId = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(instType))
                    queryParams.Add($"instType={instType}");

                if (!string.IsNullOrEmpty(instId))
                    queryParams.Add($"instId={instId}");

                var queryString = queryParams.Count > 0 ? string.Join("&", queryParams) : "";

                var response = Get<OKXApiResponse<Position>>(
                    "/account/positions",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetPositions(): Failed to get positions - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Position>();
                }

                return response.Data ?? new List<Position>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetPositions(): Exception: {ex.Message}");
                return new List<Position>();
            }
        }

        /// <summary>
        /// Gets risk limit tiers (placeholder for OKX implementation)
        /// </summary>
        public List<RiskLimitTier> GetRiskLimitTiers(string contract)
        {
            // TODO: Implement GetRiskLimitTiers for OKX
            return new List<RiskLimitTier>();
        }

        /// <summary>
        /// Gets available instruments (trading pairs) from OKX
        /// https://www.okx.com/docs-v5/en/#rest-api-public-data-get-instruments
        /// No authentication required
        /// </summary>
        /// <param name="instType">Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION</param>
        /// <returns>List of instruments, or empty list if request fails</returns>
        public List<Instrument> GetInstruments(string instType = "SPOT")
        {
            try
            {
                var queryString = $"instType={instType}";
                var response = GetPublic<OKXApiResponse<Instrument>>(
                    "/public/instruments",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetInstruments(): Failed to get instruments - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Instrument>();
                }

                return response.Data ?? new List<Instrument>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetInstruments(): Exception: {ex.Message}");
                return new List<Instrument>();
            }
        }

        /// <summary>
        /// Gets ticker for a specific instrument via OKX v5 API
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-ticker
        /// Overrides Gate.io base class endpoint
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)</param>
        /// <returns>List containing single ticker, or empty list</returns>
        public override List<Ticker> GetTicker(string instId)
        {
            try
            {
                var queryString = $"instId={instId}";
                var response = GetPublic<OKXApiResponse<Ticker>>(
                    "/market/ticker",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess || response.Data == null || response.Data.Count == 0)
                {
                    Log.Error($"OKXRestApiClient.GetTicker(): Failed - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Ticker>();
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetTicker(): Exception: {ex.Message}");
                return new List<Ticker>();
            }
        }

        /// <summary>
        /// Gets price limit for a specific instrument via OKX v5 API
        /// https://www.okx.com/docs-v5/en/#rest-api-public-data-get-limit-price
        /// Returns the maximum buy price and minimum sell price for new orders.
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)</param>
        /// <returns>PriceLimit data, or null if request fails</returns>
        public PriceLimit GetPriceLimit(string instId)
        {
            _priceLimitRateLimiter?.WaitToProceed();
            var queryString = $"instId={instId}";
            var response = GetPublic<OKXApiResponse<PriceLimit>>(
                "/public/price-limit",
                queryString,
                defaultValue: null);

            if (response?.IsSuccess == true && response.Data?.Count > 0)
            {
                return response.Data[0];
            }

            return null;
        }

        /// <summary>
        /// Gets all tickers via OKX v5 API (SPOT + SWAP combined)
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-tickers
        /// Overrides Gate.io base class endpoint
        /// </summary>
        /// <returns>List of all tickers (spot + swap)</returns>
        public override List<Ticker> GetTicker()
        {
            var result = new List<Ticker>();
            foreach (var instType in new[] { "SPOT", "SWAP" })
            {
                result.AddRange(GetTickers(instType));
            }
            return result;
        }

        /// <summary>
        /// Gets all tickers for a specific instrument type
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-tickers
        /// No authentication required
        /// </summary>
        /// <param name="instType">Instrument type: SPOT, SWAP, FUTURES, OPTION</param>
        /// <returns>List of tickers for the instrument type</returns>
        public List<Ticker> GetTickers(string instType)
        {
            try
            {
                var queryString = $"instType={instType}";
                var response = GetPublic<OKXApiResponse<Ticker>>(
                    "/market/tickers",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetTickers(): Failed for {instType} - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Ticker>();
                }

                return response.Data ?? new List<Ticker>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetTickers(): Exception: {ex.Message}");
                return new List<Ticker>();
            }
        }

        /// <summary>
        /// Gets account configuration
        /// https://www.okx.com/docs-v5/en/#rest-api-account-get-account-configuration
        /// Requires authentication
        /// </summary>
        /// <returns>Account configuration, or null if request fails</returns>
        public AccountConfig GetAccountConfiguration()
        {
            try
            {
                var response = Get<OKXApiResponse<AccountConfig>>(
                    "/account/config",
                    queryString: "",
                    defaultValue: null);

                if (response == null || !response.IsSuccess || response.Data == null || response.Data.Count == 0)
                {
                    Log.Error($"OKXRestApiClient.GetAccountConfiguration(): Failed to get config - code: {response?.Code}, msg: {response?.Message}");
                    return null;
                }

                return response.Data[0];
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetAccountConfiguration(): Exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the position mode for the account
        /// POST /api/v5/account/set-position-mode
        /// </summary>
        public bool SetPositionMode(string posMode)
        {
            try
            {
                var response = Post<OKXApiResponse<AccountConfig>>("/account/set-position-mode", new { posMode }, null);
                if (response?.IsSuccess == true)
                {
                    Log.Trace($"OKXRestApiClient.SetPositionMode(): Set to {posMode}");
                    return true;
                }
                Log.Error($"OKXRestApiClient.SetPositionMode(): Failed - {response?.Code}: {response?.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.SetPositionMode(): Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets auto loan for margin trading
        /// POST /api/v5/account/set-auto-loan
        /// </summary>
        public bool SetAutoLoan(bool autoLoan)
        {
            try
            {
                var response = Post<OKXApiResponse<AccountConfig>>("/account/set-auto-loan", new { autoLoan }, null);
                if (response?.IsSuccess == true)
                {
                    Log.Trace($"OKXRestApiClient.SetAutoLoan(): Set to {autoLoan}");
                    return true;
                }
                Log.Error($"OKXRestApiClient.SetAutoLoan(): Failed - {response?.Code}: {response?.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.SetAutoLoan(): Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets fee type for spot trading
        /// POST /api/v5/account/set-fee-type
        /// </summary>
        public bool SetFeeType(string feeType)
        {
            try
            {
                var response = Post<OKXApiResponse<AccountConfig>>("/account/set-fee-type", new { feeType }, null);
                if (response?.IsSuccess == true)
                {
                    Log.Trace($"OKXRestApiClient.SetFeeType(): Set to {feeType}");
                    return true;
                }
                Log.Error($"OKXRestApiClient.SetFeeType(): Failed - {response?.Code}: {response?.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.SetFeeType(): Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets settlement currency for USD-margined contracts
        /// POST /api/v5/account/set-settle-currency
        /// </summary>
        public bool SetSettleCurrency(string settleCcy)
        {
            try
            {
                var response = Post<OKXApiResponse<AccountConfig>>("/account/set-settle-currency", new { settleCcy }, null);
                if (response?.IsSuccess == true)
                {
                    Log.Trace($"OKXRestApiClient.SetSettleCurrency(): Set to {settleCcy}");
                    return true;
                }
                Log.Error($"OKXRestApiClient.SetSettleCurrency(): Failed - {response?.Code}: {response?.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.SetSettleCurrency(): Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all candlestick/K-line data for a time range, handling pagination internally.
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-candlesticks
        /// OKX always returns descending order (newest first), max 100 per page, max 1440 total.
        /// Paginates backward using 'after', then reverses for chronological order.
        ///
        /// OKX parameter semantics:
        ///   after  = records OLDER than ts (paginate backward)
        ///   before = records NEWER than ts
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT)</param>
        /// <param name="bar">Bar size (e.g., "1m", "1H", "1D")</param>
        /// <param name="startMs">Start time in Unix milliseconds</param>
        /// <param name="endMs">End time in Unix milliseconds</param>
        /// <returns>Candles in chronological order</returns>
        public List<Candle> GetCandles(string instId, string bar, long startMs, long endMs)
        {
            var allCandles = new List<Candle>();
            var currentAfter = endMs;

            while (true)
            {
                _candlesRateLimiter?.WaitToProceed();

                var queryString = $"instId={instId}&bar={bar}&after={currentAfter}&limit=100";
                var response = GetPublic<OKXApiResponse<Candle>>(
                    "/market/candles",
                    queryString,
                    defaultValue: null);

                if (response?.Data == null || response.Data.Count == 0)
                {
                    break;
                }

                var candles = response.Data;

                // OKX returns descending: candles[0]=newest, candles[^1]=oldest
                foreach (var candle in candles)
                {
                    if (candle.Timestamp < startMs)
                    {
                        goto done;
                    }
                    allCandles.Add(candle);
                }

                if (candles.Count < 100)
                {
                    break;
                }

                currentAfter = candles[^1].Timestamp;
            }

            done:
            allCandles.Reverse();
            return allCandles;
        }


        /// <summary>
        /// Gets recent trades for a specific instrument.
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-trades
        /// Returns up to 500 recent trades in descending order (newest first).
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT)</param>
        /// <param name="limit">Number of results (max 500, default 100)</param>
        /// <returns>List of trades, or empty list if request fails</returns>
        public List<Trade> GetTrades(string instId, int limit = 100)
        {
            _tradesRateLimiter?.WaitToProceed();

            var queryString = $"instId={instId}&limit={Math.Min(limit, 500)}";
            var response = GetPublic<OKXApiResponse<Trade>>(
                "/market/trades",
                queryString,
                defaultValue: null);

            if (response?.IsSuccess != true)
            {
                Log.Error($"OKXRestApiClient.GetTrades(): Failed - code: {response?.Code}, msg: {response?.Message}");
                return new List<Trade>();
            }

            return response.Data ?? new List<Trade>();
        }

        /// <summary>
        /// Gets order book snapshot for a specific instrument via OKX v5 API
        /// https://www.okx.com/docs-v5/en/#order-book-trading-market-data-get-order-book
        /// No authentication required
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT)</param>
        /// <param name="depth">Number of levels (max 400, default 400)</param>
        /// <returns>Order book snapshot, or null if request fails</returns>
        public WebSocketOrderBook GetOrderBook(string instId, int depth = 400)
        {
            _orderBookRateLimiter?.WaitToProceed();
            var queryString = $"instId={instId}&sz={Math.Min(depth, 400)}";
            var response = GetPublic<OKXApiResponse<WebSocketOrderBook>>(
                "/market/books",
                queryString,
                defaultValue: null);

            if (response?.IsSuccess != true || response.Data == null || response.Data.Count == 0)
            {
                Log.Error($"OKXRestApiClient.GetOrderBook(): Failed for {instId} - code: {response?.Code}, msg: {response?.Message}");
                return null;
            }

            return response.Data[0];
        }

        /// <summary>
        /// Gets pending orders (unfilled or partially filled) under the current account
        /// https://www.okx.com/docs-v5/en/#rest-api-trade-get-order-list
        /// Requires authentication
        /// </summary>
        /// <param name="instType">Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION (optional)</param>
        /// <param name="instId">Instrument ID (optional, e.g., BTC-USDT)</param>
        /// <returns>List of pending orders, or empty list if request fails</returns>
        public List<Order> GetPendingOrders(string instType = null, string instId = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(instType))
                    queryParams.Add($"instType={instType}");

                if (!string.IsNullOrEmpty(instId))
                    queryParams.Add($"instId={instId}");

                var queryString = queryParams.Count > 0 ? string.Join("&", queryParams) : "";

                var response = Get<OKXApiResponse<Order>>(
                    "/trade/orders-pending",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetPendingOrders(): Failed to get pending orders - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Order>();
                }

                return response.Data ?? new List<Order>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetPendingOrders(): Exception: {ex.Message}");
                return new List<Order>();
            }
        }

        /// <summary>
        /// Gets account balance information
        /// https://www.okx.com/docs-v5/en/#rest-api-account-get-balance
        /// Requires authentication
        /// </summary>
        /// <param name="ccy">Currency (optional, e.g., USDT, BTC). If null, returns all currencies</param>
        /// <returns>Account balance data, or null if request fails</returns>
        public AccountBalance GetAccountBalance(string ccy = null)
        {
            try
            {
                var queryString = !string.IsNullOrEmpty(ccy) ? $"ccy={ccy}" : "";

                var response = Get<OKXApiResponse<AccountBalance>>(
                    "/account/balance",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess || response.Data == null || response.Data.Count == 0)
                {
                    Log.Error($"OKXRestApiClient.GetAccountBalance(): Failed to get balance - code: {response?.Code}, msg: {response?.Message}");
                    return null;
                }

                return response.Data[0];
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetAccountBalance(): Exception: {ex.Message}");
                return null;
            }
        }

        // ========================================
        // ORDER MANAGEMENT
        // ========================================

        /// <summary>
        /// Places a new order
        /// https://www.okx.com/docs-v5/en/#rest-api-trade-place-order
        /// Requires authentication
        /// </summary>
        /// <param name="request">Order request</param>
        /// <returns>Place order response on success</returns>
        /// <exception cref="InvalidOperationException">Thrown on API error or order rejection</exception>
        public PlaceOrderResponse PlaceOrder(PlaceOrderRequest request)
        {
            var response = Post<OKXApiResponse<PlaceOrderResponse>>(
                "/trade/order",
                request,
                defaultValue: null);

            // Always log full response JSON for diagnostics
            Log.Trace($"OKXRestApiClient.PlaceOrder(): Response: {JsonConvert.SerializeObject(response)}");

            if (response?.Data == null || response.Data.Count == 0)
            {
                throw new InvalidOperationException(
                    $"PlaceOrder API error - code: {response?.Code}, msg: {response?.Message}");
            }

            var orderResponse = response.Data[0];

            if (orderResponse.StatusCode != "0")
            {
                throw new InvalidOperationException(
                    $"PlaceOrder rejected - sCode: {orderResponse.StatusCode}, sMsg: {orderResponse.StatusMessage}");
            }

            return orderResponse;
        }

        /// <summary>
        /// Amends an existing order (modify quantity or price)
        /// https://www.okx.com/docs-v5/en/#rest-api-trade-amend-order
        /// Requires authentication
        /// </summary>
        /// <param name="request">Amend order request</param>
        /// <returns>Amend order response on success</returns>
        /// <exception cref="InvalidOperationException">Thrown on API error or order rejection</exception>
        public AmendOrderResponse AmendOrder(AmendOrderRequest request)
        {
            var response = Post<OKXApiResponse<AmendOrderResponse>>(
                "/trade/amend-order",
                request,
                defaultValue: null);

            if (response?.Data == null || response.Data.Count == 0)
            {
                throw new InvalidOperationException(
                    $"AmendOrder API error - code: {response?.Code}, msg: {response?.Message}");
            }

            var amendResponse = response.Data[0];

            if (amendResponse.StatusCode != "0")
            {
                throw new InvalidOperationException(
                    $"AmendOrder rejected - sCode: {amendResponse.StatusCode}, sMsg: {amendResponse.StatusMessage}");
            }

            return amendResponse;
        }

        /// <summary>
        /// Cancels an existing order
        /// https://www.okx.com/docs-v5/en/#rest-api-trade-cancel-order
        /// Requires authentication
        /// </summary>
        /// <param name="request">Cancel order request</param>
        /// <returns>Cancel order response on success</returns>
        /// <exception cref="InvalidOperationException">Thrown on API error or order rejection</exception>
        public CancelOrderResponse CancelOrder(CancelOrderRequest request)
        {
            var response = Post<OKXApiResponse<CancelOrderResponse>>(
                "/trade/cancel-order",
                request,
                defaultValue: null);

            if (response?.Data == null || response.Data.Count == 0)
            {
                throw new InvalidOperationException(
                    $"CancelOrder API error - code: {response?.Code}, msg: {response?.Message}");
            }

            var cancelResponse = response.Data[0];

            if (cancelResponse.StatusCode != "0")
            {
                throw new InvalidOperationException(
                    $"CancelOrder rejected - sCode: {cancelResponse.StatusCode}, sMsg: {cancelResponse.StatusMessage}");
            }

            return cancelResponse;
        }

        /// <summary>
        /// Gets user execution/fill history within a time range
        /// https://www.okx.com/docs-v5/en/#rest-api-trade-get-transaction-details-last-3-days
        /// GET /api/v5/trade/fills
        /// Rate limit: 60 requests per 2 seconds
        /// Requires authentication
        /// </summary>
        /// <param name="instType">Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION (optional)</param>
        /// <param name="instId">Instrument ID (optional)</param>
        /// <param name="beginMs">Start timestamp in milliseconds (optional)</param>
        /// <param name="endMs">End timestamp in milliseconds (optional)</param>
        /// <param name="limit">Max results per request, default 100, max 100 (optional)</param>
        /// <returns>List of fills, or empty list if request fails</returns>
        public List<Fill> GetExecutionHistory(
            string instType = null,
            string instId = null,
            long? beginMs = null,
            long? endMs = null,
            int limit = 100)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(instType))
                    queryParams.Add($"instType={instType}");

                if (!string.IsNullOrEmpty(instId))
                    queryParams.Add($"instId={instId}");

                if (beginMs.HasValue)
                    queryParams.Add($"begin={beginMs.Value}");

                if (endMs.HasValue)
                    queryParams.Add($"end={endMs.Value}");

                queryParams.Add($"limit={Math.Min(limit, 100)}");

                var queryString = string.Join("&", queryParams);
                var response = Get<OKXApiResponse<Fill>>(
                    "/trade/fills",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetExecutionHistory(): Failed - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Fill>();
                }

                return response.Data ?? new List<Fill>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetExecutionHistory(): Exception: {ex.Message}");
                return new List<Fill>();
            }
        }
    }
}
