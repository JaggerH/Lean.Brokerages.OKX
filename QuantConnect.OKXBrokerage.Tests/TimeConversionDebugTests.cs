using NUnit.Framework;
using System;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class TimeConversionDebugTests
    {
        [Test]
        public void DebugUnixEpoch()
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Console.WriteLine($"Unix Epoch: {unixEpoch:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Unix Epoch Kind: {unixEpoch.Kind}");
            Console.WriteLine($"Unix Epoch Ticks: {unixEpoch.Ticks}");

            // Expected ticks for Unix Epoch
            long expectedTicks = 621355968000000000L;
            Console.WriteLine($"Expected Ticks: {expectedTicks}");
            Console.WriteLine($"Ticks Match: {unixEpoch.Ticks == expectedTicks}");
        }

        [Test]
        public void DebugTimeConversion()
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = 1597019583L;

            var result = unixEpoch.AddSeconds(timestamp);
            var expected = new DateTime(2020, 8, 10, 0, 33, 3, DateTimeKind.Utc);

            Console.WriteLine($"Unix Epoch: {unixEpoch:yyyy-MM-dd HH:mm:ss} (Ticks: {unixEpoch.Ticks})");
            Console.WriteLine($"Timestamp: {timestamp} seconds");
            Console.WriteLine($"Result: {result:yyyy-MM-dd HH:mm:ss.fff} (Kind: {result.Kind}, Ticks: {result.Ticks})");
            Console.WriteLine($"Expected: {expected:yyyy-MM-dd HH:mm:ss.fff} (Kind: {expected.Kind}, Ticks: {expected.Ticks})");
            Console.WriteLine($"Difference: {(result - expected).TotalSeconds} seconds");
            Console.WriteLine($"Difference: {(result.Ticks - expected.Ticks)} ticks");

            Assert.AreEqual(expected.Ticks, result.Ticks, "Ticks should match");
        }

        [Test]
        public void DebugOKXUtilityDirectCall()
        {
            var timestamp = 1597019583L;

            // Call the actual OKXUtility method
            var result = OKXUtility.UnixSecondsToDateTime(timestamp);
            var expected = new DateTime(2020, 8, 10, 0, 33, 3, DateTimeKind.Utc);

            Console.WriteLine($"OKXUtility Result: {result:yyyy-MM-dd HH:mm:ss.fff} (Kind: {result.Kind})");
            Console.WriteLine($"Expected: {expected:yyyy-MM-dd HH:mm:ss.fff} (Kind: {expected.Kind})");
            Console.WriteLine($"Result Ticks: {result.Ticks}");
            Console.WriteLine($"Expected Ticks: {expected.Ticks}");
            Console.WriteLine($"Difference: {(result - expected).TotalSeconds} seconds");

            // Also check the reverse conversion
            var backToTimestamp = OKXUtility.DateTimeToUnixSeconds(expected);
            Console.WriteLine($"Back to timestamp: {backToTimestamp}");
            Console.WriteLine($"Original timestamp: {timestamp}");
            Console.WriteLine($"Timestamps match: {backToTimestamp == timestamp}");
        }
    }
}
