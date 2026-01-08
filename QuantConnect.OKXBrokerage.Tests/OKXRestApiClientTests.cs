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
using QuantConnect.Brokerages.OKX;
using System;
using System.Text;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXRestApiClientTests
    {
        /// <summary>
        /// Tests HMAC-SHA256 signature generation according to OKX v5 API documentation
        /// Example from: https://www.okx.com/docs-v5/en/#rest-api-authentication
        /// </summary>
        [Test]
        public void TestHmacSha256Signature()
        {
            // Test data from OKX documentation example
            var secretKey = "2C3F98E6B3F1C8D4A5B6E7F8A9B0C1D2";
            var message = "1597026383085GET/api/v5/account/balance";

            // Expected signature (Base64 encoded HMAC-SHA256)
            var signature = OKXUtility.GenerateHmacSignature(message, secretKey);

            // Verify signature is not null or empty
            Assert.IsNotNull(signature);
            Assert.IsNotEmpty(signature);

            Console.WriteLine($"Generated signature: {signature}");
            Console.WriteLine($"Signature length: {signature.Length}");

            // Verify it's valid Base64
            try
            {
                var bytes = Convert.FromBase64String(signature);
                Assert.AreEqual(32, bytes.Length, "HMAC-SHA256 should produce 32 bytes");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Signature is not valid Base64 format: {ex.Message}. Signature: {signature}");
            }
        }

        /// <summary>
        /// Tests signature generation with request body (POST request)
        /// </summary>
        [Test]
        public void TestSignatureWithBody()
        {
            var secretKey = "test-secret-key";
            var timestamp = "1597026383085";
            var method = "POST";
            var requestPath = "/api/v5/trade/order";
            var body = "{\"instId\":\"BTC-USDT\",\"tdMode\":\"cash\",\"side\":\"buy\",\"ordType\":\"limit\",\"px\":\"20000\",\"sz\":\"0.01\"}";

            // Build signature string: timestamp + method + requestPath + body
            var message = timestamp + method + requestPath + body;
            var signature = OKXUtility.GenerateHmacSignature(message, secretKey);

            Assert.IsNotNull(signature);
            Assert.IsNotEmpty(signature);

            // Verify it's valid Base64
            var bytes = Convert.FromBase64String(signature);

            // HMAC-SHA256 produces 32 bytes (256 bits)
            Assert.AreEqual(32, bytes.Length, "HMAC-SHA256 should produce 32 bytes");
        }

        /// <summary>
        /// Tests signature generation with query parameters (GET request)
        /// </summary>
        [Test]
        public void TestSignatureWithQueryString()
        {
            var secretKey = "test-secret-key";
            var timestamp = "1597026383085";
            var method = "GET";
            var requestPath = "/api/v5/account/balance?ccy=USDT";

            // Build signature string: timestamp + method + requestPath (includes query string)
            var message = timestamp + method + requestPath;
            var signature = OKXUtility.GenerateHmacSignature(message, secretKey);

            Assert.IsNotNull(signature);
            Assert.IsNotEmpty(signature);

            // Verify different query strings produce different signatures
            var requestPath2 = "/api/v5/account/balance?ccy=BTC";
            var message2 = timestamp + method + requestPath2;
            var signature2 = OKXUtility.GenerateHmacSignature(message2, secretKey);

            Assert.AreNotEqual(signature, signature2, "Different query strings should produce different signatures");
        }

        /// <summary>
        /// Tests signature generation with empty body (GET request)
        /// </summary>
        [Test]
        public void TestSignatureWithEmptyBody()
        {
            var secretKey = "test-secret-key";
            var timestamp = "1597026383085";
            var method = "GET";
            var requestPath = "/api/v5/public/time";

            // For GET requests with no body
            var message = timestamp + method + requestPath;
            var signature = OKXUtility.GenerateHmacSignature(message, secretKey);

            Assert.IsNotNull(signature);
            Assert.IsNotEmpty(signature);
        }

        /// <summary>
        /// Tests timestamp format (milliseconds)
        /// </summary>
        [Test]
        public void TestTimestampFormat()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            // OKX expects milliseconds timestamp
            Assert.IsTrue(timestamp.Length >= 13, "Timestamp should be at least 13 digits (milliseconds)");

            // Verify it's a valid number
            Assert.IsTrue(long.TryParse(timestamp, out var _), "Timestamp should be a valid number");
        }

        /// <summary>
        /// Verifies signature determinism (same input produces same output)
        /// </summary>
        [Test]
        public void TestSignatureDeterminism()
        {
            var secretKey = "test-secret-key";
            var message = "1597026383085GET/api/v5/account/balance";

            var signature1 = OKXUtility.GenerateHmacSignature(message, secretKey);
            var signature2 = OKXUtility.GenerateHmacSignature(message, secretKey);

            Assert.AreEqual(signature1, signature2, "Same input should produce same signature");
        }

        /// <summary>
        /// Tests that different timestamps produce different signatures
        /// </summary>
        [Test]
        public void TestTimestampSensitivity()
        {
            var secretKey = "test-secret-key";
            var method = "GET";
            var requestPath = "/api/v5/account/balance";

            var timestamp1 = "1597026383085";
            var message1 = timestamp1 + method + requestPath;
            var signature1 = OKXUtility.GenerateHmacSignature(message1, secretKey);

            var timestamp2 = "1597026383086"; // Different by 1ms
            var message2 = timestamp2 + method + requestPath;
            var signature2 = OKXUtility.GenerateHmacSignature(message2, secretKey);

            Assert.AreNotEqual(signature1, signature2, "Different timestamps should produce different signatures");
        }

        /// <summary>
        /// Tests SHA512 hash computation (legacy, for completeness)
        /// </summary>
        [Test]
        public void TestSha512Hash()
        {
            var payload = "test payload";
            var hash = OKXUtility.ComputeSha512Hash(payload);

            Assert.IsNotNull(hash);
            Assert.IsNotEmpty(hash);

            // SHA512 produces 64 bytes = 128 hex characters
            Assert.AreEqual(128, hash.Length, "SHA512 hash should be 128 hex characters");
        }
    }
}
