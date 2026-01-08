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
using Newtonsoft.Json;
using QuantConnect.Brokerages.OKX;
using QuantConnect.Brokerages.OKX.Converters;
using System;
using System.Globalization;
using System.IO;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class ConverterTests
    {
        #region DecimalConverter Tests

        /// <summary>
        /// Tests DecimalConverter with valid decimal string
        /// </summary>
        [Test]
        public void DecimalConverter_ValidString_ReturnsDecimal()
        {
            var json = "{ \"value\": \"123.456\" }";
            var result = JsonConvert.DeserializeObject<TestDecimalClass>(json);

            Assert.AreEqual(123.456m, result.Value);
        }

        /// <summary>
        /// Tests DecimalConverter with integer value
        /// </summary>
        [Test]
        public void DecimalConverter_IntegerValue_ReturnsDecimal()
        {
            var json = "{ \"value\": 100 }";
            var result = JsonConvert.DeserializeObject<TestDecimalClass>(json);

            Assert.AreEqual(100m, result.Value);
        }

        /// <summary>
        /// Tests DecimalConverter with null value
        /// </summary>
        [Test]
        public void DecimalConverter_NullValue_ReturnsZero()
        {
            var json = "{ \"value\": null }";
            var result = JsonConvert.DeserializeObject<TestDecimalClass>(json);

            Assert.AreEqual(0m, result.Value);
        }

        /// <summary>
        /// Tests DecimalConverter with empty string
        /// </summary>
        [Test]
        public void DecimalConverter_EmptyString_ReturnsZero()
        {
            var json = "{ \"value\": \"\" }";
            var result = JsonConvert.DeserializeObject<TestDecimalClass>(json);

            Assert.AreEqual(0m, result.Value);
        }

        /// <summary>
        /// Tests DecimalConverter with large number
        /// </summary>
        [Test]
        public void DecimalConverter_LargeNumber_ReturnsCorrectValue()
        {
            var json = "{ \"value\": \"99999999.99999999\" }";
            var result = JsonConvert.DeserializeObject<TestDecimalClass>(json);

            Assert.AreEqual(99999999.99999999m, result.Value);
        }

        /// <summary>
        /// Tests DecimalConverter with scientific notation
        /// </summary>
        [Test]
        public void DecimalConverter_ScientificNotation_ReturnsDecimal()
        {
            var json = "{ \"value\": \"1.23e-5\" }";
            var result = JsonConvert.DeserializeObject<TestDecimalClass>(json);

            Assert.AreEqual(0.0000123m, result.Value);
        }

        /// <summary>
        /// Tests DecimalConverter serialization
        /// </summary>
        [Test]
        public void DecimalConverter_Serialize_ReturnsString()
        {
            var obj = new TestDecimalClass { Value = 123.456m };
            var json = JsonConvert.SerializeObject(obj);

            Assert.IsTrue(json.Contains("\"123.456\""));
        }

        #endregion

        #region DateTimeConverter Tests

        /// <summary>
        /// Tests DateTimeConverter with Unix milliseconds timestamp
        /// </summary>
        [Test]
        public void DateTimeConverter_ValidTimestamp_ReturnsDateTime()
        {
            // 2020-08-10 00:33:03 UTC = 1597026383085 ms
            var json = "{ \"timestamp\": \"1597026383085\" }";
            var result = JsonConvert.DeserializeObject<TestDateTimeClass>(json);

            var expected = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            Assert.AreEqual(expected, result.Timestamp);
        }

        /// <summary>
        /// Tests DateTimeConverter with null value
        /// </summary>
        [Test]
        public void DateTimeConverter_NullValue_ReturnsMinValue()
        {
            var json = "{ \"timestamp\": null }";
            var result = JsonConvert.DeserializeObject<TestDateTimeClass>(json);

            Assert.AreEqual(DateTime.MinValue, result.Timestamp);
        }

        /// <summary>
        /// Tests DateTimeConverter with empty string
        /// </summary>
        [Test]
        public void DateTimeConverter_EmptyString_ReturnsMinValue()
        {
            var json = "{ \"timestamp\": \"\" }";
            var result = JsonConvert.DeserializeObject<TestDateTimeClass>(json);

            Assert.AreEqual(DateTime.MinValue, result.Timestamp);
        }

        /// <summary>
        /// Tests DateTimeConverter with zero timestamp
        /// </summary>
        [Test]
        public void DateTimeConverter_ZeroTimestamp_ReturnsEpoch()
        {
            var json = "{ \"timestamp\": \"0\" }";
            var result = JsonConvert.DeserializeObject<TestDateTimeClass>(json);

            var expected = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(expected, result.Timestamp);
        }

        /// <summary>
        /// Tests DateTimeConverter with current time
        /// </summary>
        [Test]
        public void DateTimeConverter_CurrentTime_ReturnsCorrectDateTime()
        {
            var now = DateTime.UtcNow;
            var timestampMs = new DateTimeOffset(now).ToUnixTimeMilliseconds();
            var json = $"{{ \"timestamp\": \"{timestampMs}\" }}";
            var result = JsonConvert.DeserializeObject<TestDateTimeClass>(json);

            // Allow 1 second tolerance due to rounding
            var diff = Math.Abs((result.Timestamp - now).TotalSeconds);
            Assert.Less(diff, 1.0, "DateTime should be within 1 second of now");
        }

        /// <summary>
        /// Tests DateTimeConverter serialization
        /// </summary>
        [Test]
        public void DateTimeConverter_Serialize_ReturnsTimestamp()
        {
            var timestamp = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            var obj = new TestDateTimeClass { Timestamp = timestamp };
            var json = JsonConvert.SerializeObject(obj);

            Assert.IsTrue(json.Contains("\"1597026383085\""));
        }

        #endregion

        #region Time Conversion Utility Tests

        /// <summary>
        /// Tests UnixSecondsToDateTime conversion
        /// </summary>
        [Test]
        public void UnixSecondsToDateTime_ValidTimestamp_ReturnsCorrectDateTime()
        {
            var timestamp = 1597026383L; // 2020-08-10 00:33:03 UTC
            var result = OKXUtility.UnixSecondsToDateTime(timestamp);

            var expected = new DateTime(2020, 8, 10, 0, 33, 3, DateTimeKind.Utc);

            Console.WriteLine($"Timestamp: {timestamp}");
            Console.WriteLine($"Result: {result:yyyy-MM-dd HH:mm:ss.fff} (Kind: {result.Kind})");
            Console.WriteLine($"Expected: {expected:yyyy-MM-dd HH:mm:ss.fff} (Kind: {expected.Kind})");
            Console.WriteLine($"Result Ticks: {result.Ticks}");
            Console.WriteLine($"Expected Ticks: {expected.Ticks}");
            Console.WriteLine($"Difference: {(result - expected).TotalSeconds} seconds");

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests UnixMillisecondsToDateTime conversion
        /// </summary>
        [Test]
        public void UnixMillisecondsToDateTime_ValidTimestamp_ReturnsCorrectDateTime()
        {
            var timestamp = 1597026383085L; // 2020-08-10 00:33:03.085 UTC
            var result = OKXUtility.UnixMillisecondsToDateTime(timestamp);

            var expected = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests DateTimeToUnixSeconds conversion
        /// </summary>
        [Test]
        public void DateTimeToUnixSeconds_ValidDateTime_ReturnsCorrectTimestamp()
        {
            var dateTime = new DateTime(2020, 8, 10, 0, 33, 3, DateTimeKind.Utc);
            var result = OKXUtility.DateTimeToUnixSeconds(dateTime);

            Assert.AreEqual(1597026383L, result);
        }

        /// <summary>
        /// Tests DateTimeToUnixMilliseconds conversion
        /// </summary>
        [Test]
        public void DateTimeToUnixMilliseconds_ValidDateTime_ReturnsCorrectTimestamp()
        {
            var dateTime = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            var result = OKXUtility.DateTimeToUnixMilliseconds(dateTime);

            Assert.AreEqual(1597026383085L, result);
        }

        /// <summary>
        /// Tests round-trip conversion (DateTime -> Unix -> DateTime)
        /// </summary>
        [Test]
        public void TimeConversion_RoundTrip_PreservesValue()
        {
            var original = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);

            var unixMs = OKXUtility.DateTimeToUnixMilliseconds(original);
            var restored = OKXUtility.UnixMillisecondsToDateTime(unixMs);

            Assert.AreEqual(original, restored);
        }

        #endregion

        #region Test Helper Classes

        private class TestDecimalClass
        {
            [JsonConverter(typeof(DecimalConverter))]
            [JsonProperty("value")]
            public decimal Value { get; set; }
        }

        private class TestDateTimeClass
        {
            [JsonConverter(typeof(DateTimeConverter))]
            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}
