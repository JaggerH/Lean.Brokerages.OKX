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

using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage Model
    /// Provides OKX-specific implementations for order handling, fees, and margin
    /// </summary>
    public class OKXBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Market name for OKX
        /// </summary>
        private const string MarketName = Market.OKX;

        /// <summary>
        /// Creates a new instance of OKXBrokerageModel
        /// </summary>
        public OKXBrokerageModel() : base(AccountType.Cash)
        {
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new OKXFeeModel();
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order
        /// </summary>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            // Validate market is OKX
            if (security.Symbol.ID.Market != MarketName)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    $"The {GetType().Name} does not support {security.Symbol.ID.Market} market.");
                return false;
            }

            // Validate order type support
            if (order.Type == OrderType.StopMarket)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderTypeNotSupported",
                    $"{order.Type} orders are not supported by OKX v5 API. Use StopLimit instead.");
                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            // OKX supports amending orders (price and quantity modification)
            return true;
        }

        /// <summary>
        /// Gets the default markets for OKX
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(MarketName);

        /// <summary>
        /// Helper method to get default markets
        /// </summary>
        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string market)
        {
            var map = new Dictionary<SecurityType, string>
            {
                { SecurityType.Base, Market.USA },
                { SecurityType.Equity, Market.USA },
                { SecurityType.Option, Market.USA },
                { SecurityType.Forex, Market.Oanda },
                { SecurityType.Future, Market.CME },
                { SecurityType.Cfd, Market.Oanda },
                { SecurityType.Crypto, market },
                { SecurityType.CryptoFuture, market }
            };
            return map.ToReadOnlyDictionary();
        }
    }
}
