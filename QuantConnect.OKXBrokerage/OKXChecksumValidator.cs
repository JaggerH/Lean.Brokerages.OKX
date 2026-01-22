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
using System.Linq;
using System.Text;
using Force.Crc32;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// Validates OKX order book data integrity using CRC32 checksum
    /// Implements the checksum mechanism as described in OKX v5 API documentation
    /// </summary>
    public static class OKXChecksumValidator
    {
        /// <summary>
        /// Calculates CRC32 checksum for OKX order book data
        /// Uses first 25 levels of bids and asks, interleaved (bid:ask:bid:ask...)
        /// Format: "bidPrice:bidSize:askPrice:askSize:bidPrice:bidSize:askPrice:askSize..."
        /// </summary>
        /// <param name="orderBook">The order book to calculate checksum for</param>
        /// <returns>CRC32 checksum as 32-bit signed integer</returns>
        public static int CalculateChecksum(OKXOrderBook orderBook)
        {
            if (orderBook == null)
            {
                throw new ArgumentNullException(nameof(orderBook));
            }

            // Get bid and ask levels (already sorted correctly)
            var bids = orderBook.GetBids().ToList(); // Descending order (best bid first)
            var asks = orderBook.GetAsks().ToList(); // Ascending order (best ask first)

            // Build checksum string according to OKX specification
            var checksumBuilder = new StringBuilder();

            // Take up to 25 levels, interleaving bids and asks
            int maxLevels = Math.Min(25, Math.Max(bids.Count, asks.Count));

            for (int i = 0; i < maxLevels; i++)
            {
                // Add bid if available
                if (i < bids.Count)
                {
                    if (checksumBuilder.Length > 0)
                    {
                        checksumBuilder.Append(':');
                    }
                    checksumBuilder.Append(bids[i].Key.ToString("0.########"));
                    checksumBuilder.Append(':');
                    checksumBuilder.Append(bids[i].Value.ToString("0.########"));
                }

                // Add ask if available
                if (i < asks.Count)
                {
                    if (checksumBuilder.Length > 0)
                    {
                        checksumBuilder.Append(':');
                    }
                    checksumBuilder.Append(asks[i].Key.ToString("0.########"));
                    checksumBuilder.Append(':');
                    checksumBuilder.Append(asks[i].Value.ToString("0.########"));
                }
            }

            // Calculate CRC32 checksum
            var checksumString = checksumBuilder.ToString();
            var bytes = Encoding.UTF8.GetBytes(checksumString);
            var crc32Value = Crc32Algorithm.Compute(bytes);

            // Convert to signed 32-bit integer (as per OKX specification)
            return unchecked((int)crc32Value);
        }

        /// <summary>
        /// Validates order book checksum against expected value from OKX
        /// </summary>
        /// <param name="orderBook">The order book to validate</param>
        /// <param name="expectedChecksum">Expected checksum from OKX message</param>
        /// <returns>True if checksum matches, false otherwise</returns>
        public static bool ValidateChecksum(OKXOrderBook orderBook, int expectedChecksum)
        {
            var calculatedChecksum = CalculateChecksum(orderBook);
            return calculatedChecksum == expectedChecksum;
        }

        /// <summary>
        /// Validates order book checksum and returns both values for debugging
        /// </summary>
        /// <param name="orderBook">The order book to validate</param>
        /// <param name="expectedChecksum">Expected checksum from OKX message</param>
        /// <param name="calculatedChecksum">Output: calculated checksum</param>
        /// <returns>True if checksum matches, false otherwise</returns>
        public static bool ValidateChecksum(OKXOrderBook orderBook, int expectedChecksum, out int calculatedChecksum)
        {
            calculatedChecksum = CalculateChecksum(orderBook);
            return calculatedChecksum == expectedChecksum;
        }
    }
}
