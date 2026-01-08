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
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Brokerage - Order Management
    /// Implements PlaceOrder, UpdateOrder, CancelOrder, and Subscribe
    /// </summary>
    public abstract partial class OKXBaseBrokerage
    {
        // ========================================
        // ORDER MANAGEMENT ABSTRACT METHODS
        // ========================================

        /// <summary>
        /// Places a new order
        /// Must be implemented by derived classes (OKXSpotBrokerage, OKXFuturesBrokerage)
        /// </summary>
        public abstract override bool PlaceOrder(Order order);

        /// <summary>
        /// Updates an existing order
        /// OKX does NOT support order modification - cancel and place new order instead
        /// </summary>
        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException(
                "OKX does not support order updates. Cancel the existing order and place a new one instead.");
        }

        /// <summary>
        /// Cancels an existing order
        /// Must be implemented by derived classes (OKXSpotBrokerage, OKXFuturesBrokerage)
        /// </summary>
        public abstract override bool CancelOrder(Order order);
    }
}
