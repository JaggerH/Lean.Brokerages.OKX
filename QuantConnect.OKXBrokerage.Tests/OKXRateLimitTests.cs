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

using NUnit.Framework;
using QuantConnect.Util;
using System;
using System.Diagnostics;
using System.Threading;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXRateLimitTests
    {
        /// <summary>
        /// Tests that RateGate allows specified number of requests per time period
        /// </summary>
        [Test]
        public void TestRateGateAllowsCorrectNumberOfRequests()
        {
            // Allow 5 requests per second
            using (var rateGate = new RateGate(5, TimeSpan.FromSeconds(1)))
            {
                var count = 0;
                var stopwatch = Stopwatch.StartNew();

                // Make 5 requests - should all pass quickly
                for (int i = 0; i < 5; i++)
                {
                    rateGate.WaitToProceed();
                    count++;
                }

                stopwatch.Stop();

                Assert.AreEqual(5, count, "Should allow 5 requests");
                Assert.Less(stopwatch.ElapsedMilliseconds, 200, "First 5 requests should be fast (< 200ms)");
            }
        }

        /// <summary>
        /// Tests that RateGate blocks requests exceeding the limit
        /// </summary>
        [Test]
        public void TestRateGateBlocksExcessRequests()
        {
            // Allow 3 requests per second
            using (var rateGate = new RateGate(3, TimeSpan.FromSeconds(1)))
            {
                var stopwatch = Stopwatch.StartNew();

                // Make 3 requests - should be fast
                for (int i = 0; i < 3; i++)
                {
                    rateGate.WaitToProceed();
                }

                var elapsed1 = stopwatch.ElapsedMilliseconds;
                Assert.Less(elapsed1, 200, "First 3 requests should be fast");

                // 4th request should be delayed
                rateGate.WaitToProceed();
                var elapsed2 = stopwatch.ElapsedMilliseconds;

                stopwatch.Stop();

                Assert.GreaterOrEqual(elapsed2, 900, "4th request should be delayed by ~1 second");
            }
        }

        /// <summary>
        /// Tests OKX order rate limiter configuration (60 req / 2s)
        /// </summary>
        [Test]
        public void TestOrderRateLimiterConfiguration()
        {
            // OKX order rate limiter: 60 requests per 2 seconds
            using (var rateGate = new RateGate(60, TimeSpan.FromSeconds(2)))
            {
                var stopwatch = Stopwatch.StartNew();

                // Make 60 requests - should all pass quickly
                for (int i = 0; i < 60; i++)
                {
                    rateGate.WaitToProceed();
                }

                stopwatch.Stop();

                Assert.Less(stopwatch.ElapsedMilliseconds, 500, "60 requests should complete quickly");
            }
        }

        /// <summary>
        /// Tests OKX account rate limiter configuration (10 req / 2s)
        /// </summary>
        [Test]
        public void TestAccountRateLimiterConfiguration()
        {
            // OKX account rate limiter: 10 requests per 2 seconds
            using (var rateGate = new RateGate(10, TimeSpan.FromSeconds(2)))
            {
                var stopwatch = Stopwatch.StartNew();

                // Make 10 requests - should all pass quickly
                for (int i = 0; i < 10; i++)
                {
                    rateGate.WaitToProceed();
                }

                stopwatch.Stop();

                Assert.Less(stopwatch.ElapsedMilliseconds, 500, "10 requests should complete quickly");
            }
        }

        /// <summary>
        /// Tests OKX public data rate limiter configuration (20 req / 2s)
        /// </summary>
        [Test]
        public void TestPublicRateLimiterConfiguration()
        {
            // OKX public rate limiter: 20 requests per 2 seconds
            using (var rateGate = new RateGate(20, TimeSpan.FromSeconds(2)))
            {
                var stopwatch = Stopwatch.StartNew();

                // Make 20 requests - should all pass quickly
                for (int i = 0; i < 20; i++)
                {
                    rateGate.WaitToProceed();
                }

                stopwatch.Stop();

                Assert.Less(stopwatch.ElapsedMilliseconds, 500, "20 requests should complete quickly");
            }
        }

        /// <summary>
        /// Tests that RateGate resets after time period
        /// </summary>
        [Test]
        public void TestRateGateReset()
        {
            // Allow 2 requests per second
            using (var rateGate = new RateGate(2, TimeSpan.FromSeconds(1)))
            {
                // Make 2 requests
                rateGate.WaitToProceed();
                rateGate.WaitToProceed();

                // Wait for rate limit to reset
                Thread.Sleep(1100);

                var stopwatch = Stopwatch.StartNew();

                // Should be able to make 2 more requests quickly
                rateGate.WaitToProceed();
                rateGate.WaitToProceed();

                stopwatch.Stop();

                Assert.Less(stopwatch.ElapsedMilliseconds, 200, "Requests after reset should be fast");
            }
        }

        /// <summary>
        /// Tests RateGate thread safety (concurrent requests)
        /// </summary>
        [Test]
        public void TestRateGateThreadSafety()
        {
            // Allow 10 requests per second
            using (var rateGate = new RateGate(10, TimeSpan.FromSeconds(1)))
            {
                var successCount = 0;
                var threads = new Thread[20];

                // Create 20 threads trying to make requests
                for (int i = 0; i < 20; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        rateGate.WaitToProceed();
                        Interlocked.Increment(ref successCount);
                    });
                }

                var stopwatch = Stopwatch.StartNew();

                // Start all threads
                foreach (var thread in threads)
                {
                    thread.Start();
                }

                // Wait for all threads
                foreach (var thread in threads)
                {
                    thread.Join();
                }

                stopwatch.Stop();

                Assert.AreEqual(20, successCount, "All requests should eventually succeed");
                // First 10 should be fast, remaining 10 should wait ~1 second
                Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 900, "Should take at least 1 second for 20 requests");
            }
        }

        /// <summary>
        /// Tests multiple rate limiters operating independently
        /// </summary>
        [Test]
        public void TestMultipleRateLimitersIndependent()
        {
            using (var orderLimiter = new RateGate(60, TimeSpan.FromSeconds(2)))
            using (var accountLimiter = new RateGate(10, TimeSpan.FromSeconds(2)))
            {
                var stopwatch = Stopwatch.StartNew();

                // Use order limiter 60 times - should be fast
                for (int i = 0; i < 60; i++)
                {
                    orderLimiter.WaitToProceed();
                }

                var orderTime = stopwatch.ElapsedMilliseconds;

                // Use account limiter 10 times - should also be fast (independent of order limiter)
                for (int i = 0; i < 10; i++)
                {
                    accountLimiter.WaitToProceed();
                }

                stopwatch.Stop();

                Assert.Less(orderTime, 500, "Order limiter requests should be fast");
                Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Account limiter should be independent");
            }
        }
    }
}
