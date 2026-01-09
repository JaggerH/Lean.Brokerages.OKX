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
            : base(apiKey, apiSecret, passphrase, null, OKXEnvironment.GetRestApiUrl())
        {
        }

        /// <summary>
        /// Creates a new instance with explicit REST API URL (for backward compatibility)
        /// </summary>
        public OKXRestApiClient(string apiKey, string apiSecret, string passphrase, string restApiUrl)
            : base(apiKey, apiSecret, passphrase, null, restApiUrl)
        {
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
                    if (string.IsNullOrEmpty(detail.AvailableBalance))
                        continue;

                    if (decimal.TryParse(detail.AvailableBalance, NumberStyles.Any, CultureInfo.InvariantCulture, out var availBalance) &&
                        availBalance > 0)
                    {
                        result.Add(new CashAmount(availBalance, detail.Currency));
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
        public List<OKXPosition> GetPositions(string instType = null, string instId = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(instType))
                    queryParams.Add($"instType={instType}");

                if (!string.IsNullOrEmpty(instId))
                    queryParams.Add($"instId={instId}");

                var queryString = queryParams.Count > 0 ? string.Join("&", queryParams) : "";

                var response = Get<OKXApiResponse<OKXPosition>>(
                    "/account/positions",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetPositions(): Failed to get positions - code: {response?.Code}, msg: {response?.Message}");
                    return new List<OKXPosition>();
                }

                return response.Data ?? new List<OKXPosition>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetPositions(): Exception: {ex.Message}");
                return new List<OKXPosition>();
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
        /// Gets ticker information for a specific instrument
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-ticker
        /// No authentication required
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT)</param>
        /// <returns>Ticker information, or null if request fails</returns>
        public Ticker GetTickerInfo(string instId)
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
                    Log.Error($"OKXRestApiClient.GetTickerInfo(): Failed to get ticker - code: {response?.Code}, msg: {response?.Message}");
                    return null;
                }

                return response.Data[0];
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetTickerInfo(): Exception: {ex.Message}");
                return null;
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
        /// Gets candlestick/K-line data for a specific instrument
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-candlesticks
        /// No authentication required
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)</param>
        /// <param name="bar">Bar size (e.g., "1m", "5m", "1H", "1D"). See ConvertResolutionToBar() for supported values</param>
        /// <param name="after">Pagination: return data after this timestamp (Unix milliseconds). Earlier data if before is not provided</param>
        /// <param name="before">Pagination: return data before this timestamp (Unix milliseconds). Later data if after is not provided</param>
        /// <param name="limit">Number of results per request (max 100, default 100)</param>
        /// <returns>List of candles, or empty list if request fails</returns>
        public List<Candle> GetCandles(
            string instId,
            string bar = "1m",
            long? after = null,
            long? before = null,
            int limit = 100)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"instId={instId}",
                    $"bar={bar}",
                    $"limit={Math.Min(limit, 100)}"  // OKX max limit is 100
                };

                if (after.HasValue)
                    queryParams.Add($"after={after.Value}");

                if (before.HasValue)
                    queryParams.Add($"before={before.Value}");

                var queryString = string.Join("&", queryParams);
                var response = GetPublic<OKXApiResponse<Candle>>(
                    "/market/candles",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetCandles(): Failed to get candles - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Candle>();
                }

                return response.Data ?? new List<Candle>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetCandles(): Exception: {ex.Message}");
                return new List<Candle>();
            }
        }

        /// <summary>
        /// Gets recent trades for a specific instrument
        /// https://www.okx.com/docs-v5/en/#rest-api-market-data-get-trades
        /// No authentication required
        /// </summary>
        /// <param name="instId">Instrument ID (e.g., BTC-USDT, BTC-USDT-SWAP)</param>
        /// <param name="limit">Number of results per request (max 500, default 100)</param>
        /// <returns>List of trades, or empty list if request fails</returns>
        public List<Trade> GetTrades(string instId, int limit = 100)
        {
            try
            {
                var queryString = $"instId={instId}&limit={Math.Min(limit, 500)}";  // OKX max limit is 500
                var response = GetPublic<OKXApiResponse<Trade>>(
                    "/market/trades",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetTrades(): Failed to get trades - code: {response?.Code}, msg: {response?.Message}");
                    return new List<Trade>();
                }

                return response.Data ?? new List<Trade>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetTrades(): Exception: {ex.Message}");
                return new List<Trade>();
            }
        }

        /// <summary>
        /// Gets pending orders (unfilled or partially filled) under the current account
        /// https://www.okx.com/docs-v5/en/#rest-api-trade-get-order-list
        /// Requires authentication
        /// </summary>
        /// <param name="instType">Instrument type: SPOT, MARGIN, SWAP, FUTURES, OPTION (optional)</param>
        /// <param name="instId">Instrument ID (optional, e.g., BTC-USDT)</param>
        /// <returns>List of pending orders, or empty list if request fails</returns>
        public List<OKXOrder> GetPendingOrders(string instType = null, string instId = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(instType))
                    queryParams.Add($"instType={instType}");

                if (!string.IsNullOrEmpty(instId))
                    queryParams.Add($"instId={instId}");

                var queryString = queryParams.Count > 0 ? string.Join("&", queryParams) : "";

                var response = Get<OKXApiResponse<OKXOrder>>(
                    "/trade/orders-pending",
                    queryString,
                    defaultValue: null);

                if (response == null || !response.IsSuccess)
                {
                    Log.Error($"OKXRestApiClient.GetPendingOrders(): Failed to get pending orders - code: {response?.Code}, msg: {response?.Message}");
                    return new List<OKXOrder>();
                }

                return response.Data ?? new List<OKXOrder>();
            }
            catch (Exception ex)
            {
                Log.Error($"OKXRestApiClient.GetPendingOrders(): Exception: {ex.Message}");
                return new List<OKXOrder>();
            }
        }

        /// <summary>
        /// Gets account balance information
        /// https://www.okx.com/docs-v5/en/#rest-api-account-get-balance
        /// Requires authentication
        /// </summary>
        /// <param name="ccy">Currency (optional, e.g., USDT, BTC). If null, returns all currencies</param>
        /// <returns>Account balance data, or null if request fails</returns>
        public OKXAccountBalance GetAccountBalance(string ccy = null)
        {
            try
            {
                var queryString = !string.IsNullOrEmpty(ccy) ? $"ccy={ccy}" : "";

                var response = Get<OKXApiResponse<OKXAccountBalance>>(
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
    }
}
