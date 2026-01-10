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

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Generic result wrapper for OKX order operations
    /// Contains both success data and detailed error information
    /// Follows Binance brokerage pattern for comprehensive error reporting
    /// </summary>
    /// <typeparam name="T">The type of order response (OKXPlaceOrderResponse, OKXAmendOrderResponse, OKXCancelOrderResponse)</typeparam>
    public class OKXOrderResult<T>
    {
        /// <summary>
        /// Indicates whether the order operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The response data (populated on success, may contain error codes on failure)
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// HTTP-level error code from OKXApiResponse.Code
        /// "0" = success, non-zero = API gateway error (auth, rate limit, malformed request)
        /// </summary>
        public string HttpCode { get; set; }

        /// <summary>
        /// HTTP-level error message from OKXApiResponse.Message
        /// Human-readable description of API gateway error
        /// </summary>
        public string HttpMessage { get; set; }

        /// <summary>
        /// Order-level status code from OKXPlaceOrderResponse.StatusCode (sCode)
        /// "0" = success, non-zero = order rejected by trading engine
        /// Common codes:
        /// - "51000": Order parameter error
        /// - "51001": Insufficient balance
        /// - "51004": Order not found
        /// - "51008": Order is being cancelled
        /// </summary>
        public string OrderStatusCode { get; set; }

        /// <summary>
        /// Order-level status message from OKXPlaceOrderResponse.StatusMessage (sMsg)
        /// Specific reason why order was rejected by trading engine
        /// </summary>
        public string OrderStatusMessage { get; set; }

        /// <summary>
        /// Gets a complete error message combining all error details
        /// Returns null if operation was successful
        /// </summary>
        /// <returns>Formatted error message or null</returns>
        public string GetErrorMessage()
        {
            if (IsSuccess)
                return null;

            // Priority 1: Order-level error (most specific)
            if (!string.IsNullOrEmpty(OrderStatusMessage))
            {
                return $"Order rejected - sCode: {OrderStatusCode}, sMsg: {OrderStatusMessage}";
            }

            // Priority 2: HTTP-level error
            if (!string.IsNullOrEmpty(HttpMessage))
            {
                return $"API error - code: {HttpCode}, msg: {HttpMessage}";
            }

            // Fallback: Generic error
            return "Order operation failed - no error details available";
        }
    }
}
