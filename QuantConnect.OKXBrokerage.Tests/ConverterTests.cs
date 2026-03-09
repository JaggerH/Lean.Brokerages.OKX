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
using QuantConnect.Brokerages.OKX.Messages;
using QuantConnect.Securities;
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
            // 2020-08-10 00:33:03 UTC = 1597019583085 ms
            var json = "{ \"timestamp\": \"1597019583085\" }";
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

            Assert.IsTrue(json.Contains("\"1597019583085\""));
        }

        #endregion

        #region Time Conversion Utility Tests

        /// <summary>
        /// Tests UnixSecondsToDateTime conversion
        /// </summary>
        [Test]
        public void UnixSecondsToDateTime_ValidTimestamp_ReturnsCorrectDateTime()
        {
            var timestamp = 1597019583L; // 2020-08-10 00:33:03 UTC
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
            var timestamp = 1597019583085L; // 2020-08-10 00:33:03.085 UTC
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

            Assert.AreEqual(1597019583L, result);
        }

        /// <summary>
        /// Tests DateTimeToUnixMilliseconds conversion
        /// </summary>
        [Test]
        public void DateTimeToUnixMilliseconds_ValidDateTime_ReturnsCorrectTimestamp()
        {
            var dateTime = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            var result = OKXUtility.DateTimeToUnixMilliseconds(dateTime);

            Assert.AreEqual(1597019583085L, result);
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

        #region Fill ToExecutionRecord Tests

        /// <summary>
        /// Tests ToExecutionRecord converts Fill to ExecutionRecord correctly for buy order
        /// </summary>
        [Test]
        public void ToExecutionRecord_BuyOrder_ReturnsPositiveQuantity()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "SPOT",
                Side = "buy",
                FillSize = "0.01",
                FillPrice = "50000.5",
                Fee = "-0.5",
                FeeCurrency = "USDT",
                FillTime = "1597019583085",
                Tag = "test-tag"
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            Assert.IsNotNull(result);
            Assert.AreEqual("12345", result.ExecutionId);
            Assert.AreEqual("67890", result.OrderId);
            Assert.AreEqual(0.01m, result.Quantity, "Buy order should have positive quantity");
            Assert.AreEqual(50000.5m, result.Price);
            Assert.AreEqual(0.5m, result.Fee, "Fee should be absolute value");
            Assert.AreEqual("USDT", result.FeeCurrency);
            Assert.AreEqual("test-tag", result.Tag);
            Assert.AreEqual(SecurityType.Crypto, result.Symbol.SecurityType);
        }

        /// <summary>
        /// Tests ToExecutionRecord converts Fill to ExecutionRecord correctly for sell order
        /// </summary>
        [Test]
        public void ToExecutionRecord_SellOrder_ReturnsNegativeQuantity()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "SPOT",
                Side = "sell",
                FillSize = "0.01",
                FillPrice = "50000.5",
                Fee = "-0.5",
                FeeCurrency = "USDT",
                FillTime = "1597019583085"
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            Assert.IsNotNull(result);
            Assert.AreEqual(-0.01m, result.Quantity, "Sell order should have negative quantity");
        }

        /// <summary>
        /// Tests ToExecutionRecord correctly determines SecurityType for SWAP instruments
        /// </summary>
        [Test]
        public void ToExecutionRecord_SwapInstrument_ReturnsCryptoFuture()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT-SWAP",
                InstrumentType = "SWAP",
                Side = "buy",
                FillSize = "1",
                FillPrice = "50000",
                Fee = "-0.1",
                FeeCurrency = "USDT",
                FillTime = "1597019583085"
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            Assert.IsNotNull(result);
            Assert.AreEqual(SecurityType.CryptoFuture, result.Symbol.SecurityType);
        }

        /// <summary>
        /// Tests ToExecutionRecord correctly determines SecurityType for FUTURES instruments
        /// Note: Uses MARGIN instrument type as FUTURES delivery contracts may not be in symbol database
        /// </summary>
        [Test]
        public void ToExecutionRecord_MarginInstrument_ReturnsCrypto()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "MARGIN",
                Side = "buy",
                FillSize = "1",
                FillPrice = "50000",
                Fee = "-0.1",
                FeeCurrency = "USDT",
                FillTime = "1597019583085"
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            Assert.IsNotNull(result);
            // MARGIN maps to Crypto (spot-like trading with leverage)
            Assert.AreEqual(SecurityType.Crypto, result.Symbol.SecurityType);
        }

        /// <summary>
        /// Tests ToExecutionRecord uses FillTime over Timestamp when available
        /// </summary>
        [Test]
        public void ToExecutionRecord_UsesFillTimeOverTimestamp()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "SPOT",
                Side = "buy",
                FillSize = "0.01",
                FillPrice = "50000",
                Fee = "-0.5",
                FeeCurrency = "USDT",
                FillTime = "1597019583085",  // 2020-08-10 00:33:03.085 UTC
                Timestamp = "1597019600000"  // Different time
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            var expected = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            Assert.AreEqual(expected, result.TimeUtc, "Should use FillTime, not Timestamp");
        }

        /// <summary>
        /// Tests ToExecutionRecord falls back to Timestamp when FillTime is empty
        /// </summary>
        [Test]
        public void ToExecutionRecord_FallbackToTimestamp_WhenFillTimeEmpty()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "SPOT",
                Side = "buy",
                FillSize = "0.01",
                FillPrice = "50000",
                Fee = "-0.5",
                FeeCurrency = "USDT",
                FillTime = "",  // Empty
                Timestamp = "1597019583085"  // 2020-08-10 00:33:03.085 UTC
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            var expected = new DateTime(2020, 8, 10, 0, 33, 3, 85, DateTimeKind.Utc);
            Assert.AreEqual(expected, result.TimeUtc, "Should fall back to Timestamp");
        }

        /// <summary>
        /// Tests ToExecutionRecord handles null FeeCurrency with default
        /// </summary>
        [Test]
        public void ToExecutionRecord_NullFeeCurrency_DefaultsToUSDT()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "SPOT",
                Side = "buy",
                FillSize = "0.01",
                FillPrice = "50000",
                Fee = "-0.5",
                FeeCurrency = null,
                FillTime = "1597019583085"
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            Assert.AreEqual("USDT", result.FeeCurrency, "Should default to USDT when FeeCurrency is null");
        }

        /// <summary>
        /// Tests ToExecutionRecord handles positive fee (rebate) correctly
        /// </summary>
        [Test]
        public void ToExecutionRecord_PositiveFee_ReturnsAbsoluteValue()
        {
            var fill = new Fill
            {
                TradeId = "12345",
                OrderId = "67890",
                InstrumentId = "BTC-USDT",
                InstrumentType = "SPOT",
                Side = "buy",
                FillSize = "0.01",
                FillPrice = "50000",
                Fee = "0.1",  // Positive = rebate
                FeeCurrency = "USDT",
                FillTime = "1597019583085"
            };

            var symbolMapper = new OKXSymbolMapper(Market.OKX);
            var result = fill.ToExecutionRecord(symbolMapper);

            Assert.AreEqual(0.1m, result.Fee, "Fee should be absolute value");
        }

        #endregion

        #region Bill Deserialization Tests

        /// <summary>
        /// Tests Bill deserialization with a complete funding fee record (type=8)
        /// </summary>
        [Test]
        public void Bill_FundingFee_DeserializesAllFields()
        {
            var json = @"{
                ""billId"": ""654321"",
                ""instType"": ""SWAP"",
                ""instId"": ""BTC-USDT-SWAP"",
                ""type"": ""8"",
                ""subType"": ""173"",
                ""ccy"": ""USDT"",
                ""balChg"": ""-0.12345678"",
                ""bal"": ""1000.50"",
                ""sz"": ""1.5"",
                ""ts"": ""1709251200000""
            }";

            var bill = JsonConvert.DeserializeObject<Bill>(json);

            Assert.AreEqual("654321", bill.BillId);
            Assert.AreEqual("SWAP", bill.InstType);
            Assert.AreEqual("BTC-USDT-SWAP", bill.InstId);
            Assert.AreEqual("8", bill.Type);
            Assert.AreEqual("173", bill.SubType);
            Assert.AreEqual("USDT", bill.Ccy);
            Assert.AreEqual("-0.12345678", bill.BalanceChange);
            Assert.AreEqual("1000.50", bill.Balance);
            Assert.AreEqual("1.5", bill.Size);
            Assert.AreEqual("1709251200000", bill.Ts);
        }

        /// <summary>
        /// Tests Bill deserialization with an interest deduction record (type=7)
        /// </summary>
        [Test]
        public void Bill_InterestDeduction_DeserializesCorrectly()
        {
            var json = @"{
                ""billId"": ""789012"",
                ""instType"": ""MARGIN"",
                ""instId"": ""ETH-USDT"",
                ""type"": ""7"",
                ""subType"": ""9"",
                ""ccy"": ""USDT"",
                ""balChg"": ""-0.00567"",
                ""bal"": ""500.25"",
                ""sz"": ""10"",
                ""ts"": ""1709337600000""
            }";

            var bill = JsonConvert.DeserializeObject<Bill>(json);

            Assert.AreEqual("789012", bill.BillId);
            Assert.AreEqual("MARGIN", bill.InstType);
            Assert.AreEqual("ETH-USDT", bill.InstId);
            Assert.AreEqual("7", bill.Type);
            Assert.AreEqual("-0.00567", bill.BalanceChange);
        }

        /// <summary>
        /// Tests Bill deserialization with positive balChg (income, e.g. funding received)
        /// </summary>
        [Test]
        public void Bill_PositiveBalanceChange_DeserializesCorrectly()
        {
            var json = @"{
                ""billId"": ""111111"",
                ""instType"": ""SWAP"",
                ""instId"": ""ETH-USDT-SWAP"",
                ""type"": ""8"",
                ""subType"": ""173"",
                ""ccy"": ""USDT"",
                ""balChg"": ""0.98765432"",
                ""bal"": ""2000.00"",
                ""sz"": ""5"",
                ""ts"": ""1709424000000""
            }";

            var bill = JsonConvert.DeserializeObject<Bill>(json);

            Assert.AreEqual("0.98765432", bill.BalanceChange);
            var amount = decimal.Parse(bill.BalanceChange, CultureInfo.InvariantCulture);
            Assert.Greater(amount, 0m, "Positive balChg indicates income (funding received)");
        }

        /// <summary>
        /// Tests Bill deserialization with empty/null optional fields
        /// </summary>
        [Test]
        public void Bill_MissingOptionalFields_DeserializesWithNulls()
        {
            var json = @"{
                ""billId"": ""999999"",
                ""instType"": ""SWAP"",
                ""instId"": ""BTC-USDT-SWAP"",
                ""type"": ""8"",
                ""ccy"": ""USDT"",
                ""balChg"": ""-0.01"",
                ""ts"": ""1709510400000""
            }";

            var bill = JsonConvert.DeserializeObject<Bill>(json);

            Assert.AreEqual("999999", bill.BillId);
            Assert.AreEqual("8", bill.Type);
            Assert.IsNull(bill.SubType, "Missing subType should be null");
            Assert.IsNull(bill.Balance, "Missing bal should be null");
            Assert.IsNull(bill.Size, "Missing sz should be null");
        }

        /// <summary>
        /// Tests Bill deserialization from OKXApiResponse wrapper (simulating actual API response)
        /// </summary>
        [Test]
        public void Bill_ApiResponseWrapper_DeserializesCorrectly()
        {
            var json = @"{
                ""code"": ""0"",
                ""msg"": """",
                ""data"": [
                    {
                        ""billId"": ""100001"",
                        ""instType"": ""SWAP"",
                        ""instId"": ""BTC-USDT-SWAP"",
                        ""type"": ""8"",
                        ""subType"": ""173"",
                        ""ccy"": ""USDT"",
                        ""balChg"": ""-0.5"",
                        ""bal"": ""999.50"",
                        ""sz"": ""1"",
                        ""ts"": ""1709596800000""
                    },
                    {
                        ""billId"": ""100002"",
                        ""instType"": ""SWAP"",
                        ""instId"": ""ETH-USDT-SWAP"",
                        ""type"": ""8"",
                        ""subType"": ""173"",
                        ""ccy"": ""USDT"",
                        ""balChg"": ""0.25"",
                        ""bal"": ""999.75"",
                        ""sz"": ""2"",
                        ""ts"": ""1709596800000""
                    }
                ]
            }";

            var response = JsonConvert.DeserializeObject<OKXApiResponse<Bill>>(json);

            Assert.IsTrue(response.IsSuccess, "Response should indicate success");
            Assert.AreEqual(2, response.Data.Count, "Should contain 2 bills");

            Assert.AreEqual("100001", response.Data[0].BillId);
            Assert.AreEqual("BTC-USDT-SWAP", response.Data[0].InstId);
            Assert.AreEqual("-0.5", response.Data[0].BalanceChange);

            Assert.AreEqual("100002", response.Data[1].BillId);
            Assert.AreEqual("ETH-USDT-SWAP", response.Data[1].InstId);
            Assert.AreEqual("0.25", response.Data[1].BalanceChange);
        }

        /// <summary>
        /// Tests Bill numeric string fields are parseable as decimal
        /// </summary>
        [Test]
        public void Bill_NumericFields_AreParseable()
        {
            var json = @"{
                ""billId"": ""222222"",
                ""instType"": ""SWAP"",
                ""instId"": ""BTC-USDT-SWAP"",
                ""type"": ""8"",
                ""subType"": ""173"",
                ""ccy"": ""USDT"",
                ""balChg"": ""-0.00000001"",
                ""bal"": ""99999999.99999999"",
                ""sz"": ""0.001"",
                ""ts"": ""1709683200000""
            }";

            var bill = JsonConvert.DeserializeObject<Bill>(json);

            var balChg = decimal.Parse(bill.BalanceChange, CultureInfo.InvariantCulture);
            var bal = decimal.Parse(bill.Balance, CultureInfo.InvariantCulture);
            var sz = decimal.Parse(bill.Size, CultureInfo.InvariantCulture);
            var ts = long.Parse(bill.Ts);

            Assert.AreEqual(-0.00000001m, balChg);
            Assert.AreEqual(99999999.99999999m, bal);
            Assert.AreEqual(0.001m, sz);
            Assert.AreEqual(1709683200000L, ts);
        }

        /// <summary>
        /// Tests Bill deserialization with empty data array in API response
        /// </summary>
        [Test]
        public void Bill_EmptyDataArray_DeserializesCorrectly()
        {
            var json = @"{
                ""code"": ""0"",
                ""msg"": """",
                ""data"": []
            }";

            var response = JsonConvert.DeserializeObject<OKXApiResponse<Bill>>(json);

            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(0, response.Data.Count);
        }

        /// <summary>
        /// Tests Bill deserialization preserves descending BillId order (newest first)
        /// </summary>
        [Test]
        public void Bill_DescendingBillIdOrder_IsPreserved()
        {
            var json = @"{
                ""code"": ""0"",
                ""msg"": """",
                ""data"": [
                    { ""billId"": ""300003"", ""instType"": ""SWAP"", ""instId"": ""BTC-USDT-SWAP"", ""type"": ""8"", ""ccy"": ""USDT"", ""balChg"": ""-0.1"", ""ts"": ""1709769600000"" },
                    { ""billId"": ""300002"", ""instType"": ""SWAP"", ""instId"": ""BTC-USDT-SWAP"", ""type"": ""8"", ""ccy"": ""USDT"", ""balChg"": ""-0.2"", ""ts"": ""1709766000000"" },
                    { ""billId"": ""300001"", ""instType"": ""SWAP"", ""instId"": ""BTC-USDT-SWAP"", ""type"": ""8"", ""ccy"": ""USDT"", ""balChg"": ""-0.3"", ""ts"": ""1709762400000"" }
                ]
            }";

            var response = JsonConvert.DeserializeObject<OKXApiResponse<Bill>>(json);

            Assert.AreEqual(3, response.Data.Count);

            // BillIds should be in descending order (newest first, as returned by OKX API)
            var id0 = long.Parse(response.Data[0].BillId);
            var id1 = long.Parse(response.Data[1].BillId);
            var id2 = long.Parse(response.Data[2].BillId);
            Assert.Greater(id0, id1);
            Assert.Greater(id1, id2);
        }

        #endregion
    }
}
