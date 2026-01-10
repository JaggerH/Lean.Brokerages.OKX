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
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Logging;
using QuantConnect.OKXBrokerage.ToolBox;

namespace QuantConnect.Brokerages.OKX.Tests
{
    [TestFixture]
    public class OKXExchangeInfoDownloaderTests
    {
        /// <summary>
        /// Basic Functionality Test: Verifies data is downloaded
        /// </summary>
        [Test]
        public void DownloadsExchangeInfo()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            Assert.IsTrue(entries.Any(), "No entries were downloaded");
            Assert.IsTrue(entries.All(e => e.StartsWith("okx,", StringComparison.OrdinalIgnoreCase)),
                "Not all entries start with 'okx,'");

            Log.Trace($"OKXExchangeInfoDownloaderTests.DownloadsExchangeInfo(): Retrieved {entries.Count} entries");
        }

        /// <summary>
        /// Basic Functionality Test: Verifies non-empty results
        /// </summary>
        [Test]
        public void ReturnsNonEmptyResults()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            Assert.Greater(entries.Count, 0, "Should return at least one entry");

            var spotEntries = entries.Where(e => e.Contains(",crypto,")).ToList();
            var futuresEntries = entries.Where(e => e.Contains(",cryptofuture,")).ToList();

            Assert.Greater(spotEntries.Count, 0, "Should have at least one spot entry");
            Assert.Greater(futuresEntries.Count, 0, "Should have at least one futures entry");

            Log.Trace($"OKXExchangeInfoDownloaderTests.ReturnsNonEmptyResults(): Spot: {spotEntries.Count}, Futures: {futuresEntries.Count}");
        }

        /// <summary>
        /// CSV Format Test: Verifies spot entries have correct format
        /// </summary>
        [Test]
        public void SpotEntries_HaveCorrectFormat()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();
            var spotEntries = entries.Where(e => e.Contains(",crypto,")).ToList();

            // Assert
            Assert.IsTrue(spotEntries.Any(), "Should have spot entries");

            foreach (var entry in spotEntries.Take(10)) // Check first 10 for performance
            {
                var fields = entry.Split(',');
                Assert.AreEqual(12, fields.Length, $"Spot entry should have 12 fields: {entry}");
                Assert.AreEqual("okx", fields[0], $"Market should be 'okx': {entry}");
                Assert.AreEqual("crypto", fields[2], $"Type should be 'crypto': {entry}");
                Assert.AreEqual("1", fields[5], $"Contract multiplier should be '1' for spot: {entry}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.SpotEntries_HaveCorrectFormat(): Verified {Math.Min(10, spotEntries.Count)} spot entries");
        }

        /// <summary>
        /// CSV Format Test: Verifies futures entries have correct format
        /// </summary>
        [Test]
        public void FuturesEntries_HaveCorrectFormat()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();
            var futuresEntries = entries.Where(e => e.Contains(",cryptofuture,")).ToList();

            // Assert
            Assert.IsTrue(futuresEntries.Any(), "Should have futures entries");

            foreach (var entry in futuresEntries.Take(10)) // Check first 10 for performance
            {
                var fields = entry.Split(',');
                Assert.AreEqual(12, fields.Length, $"Futures entry should have 12 fields: {entry}");
                Assert.AreEqual("okx", fields[0], $"Market should be 'okx': {entry}");
                Assert.AreEqual("cryptofuture", fields[2], $"Type should be 'cryptofuture': {entry}");

                // Description should contain "Perpetual"
                Assert.IsTrue(fields[3].Contains("Perpetual"), $"Futures description should contain 'Perpetual': {entry}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.FuturesEntries_HaveCorrectFormat(): Verified {Math.Min(10, futuresEntries.Count)} futures entries");
        }

        /// <summary>
        /// CSV Format Test: Verifies symbols don't contain underscores
        /// </summary>
        [Test]
        public void AllSymbols_AreValidFormat()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            foreach (var entry in entries)
            {
                var fields = entry.Split(',');
                var symbol = fields[1]; // Symbol is field index 1

                Assert.IsFalse(symbol.Contains("_"), $"Symbol should not contain underscore: {symbol}");
                Assert.IsTrue(symbol.Length > 0, "Symbol should not be empty");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.AllSymbols_AreValidFormat(): Verified {entries.Count} symbol formats");
        }

        /// <summary>
        /// CSV Format Test: Verifies market tickers contain hyphens (OKX format)
        /// </summary>
        [Test]
        public void MarketTickers_ContainHyphens()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            foreach (var entry in entries.Take(50)) // Check sample for performance
            {
                var fields = entry.Split(',');
                var marketTicker = fields[8]; // market_ticker is field index 8

                Assert.IsTrue(marketTicker.Contains("-"), $"Market ticker should contain hyphen: {marketTicker}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.MarketTickers_ContainHyphens(): Verified {Math.Min(50, entries.Count)} market tickers");
        }

        /// <summary>
        /// Data Quality Test: Verifies minimum price variation is valid decimal
        /// </summary>
        [Test]
        public void MinimumPriceVariation_IsValidDecimal()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            foreach (var entry in entries.Take(50)) // Check sample for performance
            {
                var fields = entry.Split(',');
                var minPriceVar = fields[6]; // minimum_price_variation is field index 6

                Assert.IsTrue(decimal.TryParse(minPriceVar, NumberStyles.Any, CultureInfo.InvariantCulture, out var value),
                    $"Minimum price variation should be valid decimal: {minPriceVar} in entry: {entry}");
                Assert.Greater(value, 0, $"Minimum price variation should be positive: {minPriceVar}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.MinimumPriceVariation_IsValidDecimal(): Verified {Math.Min(50, entries.Count)} entries");
        }

        /// <summary>
        /// Data Quality Test: Verifies lot size is positive
        /// </summary>
        [Test]
        public void LotSize_IsPositive()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            foreach (var entry in entries.Take(50)) // Check sample for performance
            {
                var fields = entry.Split(',');
                var lotSize = fields[7]; // lot_size is field index 7

                Assert.IsTrue(decimal.TryParse(lotSize, NumberStyles.Any, CultureInfo.InvariantCulture, out var value),
                    $"Lot size should be valid decimal: {lotSize} in entry: {entry}");
                Assert.Greater(value, 0, $"Lot size should be positive: {lotSize}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.LotSize_IsPositive(): Verified {Math.Min(50, entries.Count)} entries");
        }

        /// <summary>
        /// Data Quality Test: Verifies contract multiplier is valid
        /// </summary>
        [Test]
        public void ContractMultiplier_IsValid()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();
            var spotEntries = entries.Where(e => e.Contains(",crypto,")).Take(10).ToList();
            var futuresEntries = entries.Where(e => e.Contains(",cryptofuture,")).Take(10).ToList();

            // Assert spot: contract_multiplier should be 1
            foreach (var entry in spotEntries)
            {
                var fields = entry.Split(',');
                var multiplier = fields[5]; // contract_multiplier is field index 5

                Assert.AreEqual("1", multiplier, $"Spot contract multiplier should be '1': {entry}");
            }

            // Assert futures: contract_multiplier should be positive
            foreach (var entry in futuresEntries)
            {
                var fields = entry.Split(',');
                var multiplier = fields[5]; // contract_multiplier is field index 5

                Assert.IsTrue(decimal.TryParse(multiplier, NumberStyles.Any, CultureInfo.InvariantCulture, out var value),
                    $"Futures contract multiplier should be valid decimal: {multiplier} in entry: {entry}");
                Assert.Greater(value, 0, $"Futures contract multiplier should be positive: {multiplier}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.ContractMultiplier_IsValid(): Verified spot and futures multipliers");
        }

        /// <summary>
        /// Data Quality Test: Verifies quote currency is not empty
        /// </summary>
        [Test]
        public void QuoteCurrency_IsNotEmpty()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();

            // Assert
            foreach (var entry in entries.Take(50)) // Check sample for performance
            {
                var fields = entry.Split(',');
                var quoteCurrency = fields[4]; // quote_currency is field index 4

                Assert.IsNotEmpty(quoteCurrency, $"Quote currency should not be empty: {entry}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.QuoteCurrency_IsNotEmpty(): Verified {Math.Min(50, entries.Count)} entries");
        }

        /// <summary>
        /// Tokenized Stock Test: Verifies tokenized stocks have correct descriptions
        /// </summary>
        [Test]
        public void TokenizedStocks_HaveCorrectDescriptions()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();
            var tokenizedStockEntries = entries
                .Where(e => e.Contains(",crypto,") && e.Contains("xStock"))
                .ToList();

            // Assert
            if (tokenizedStockEntries.Any())
            {
                Log.Trace($"OKXExchangeInfoDownloaderTests.TokenizedStocks_HaveCorrectDescriptions(): Found {tokenizedStockEntries.Count} tokenized stocks");

                foreach (var entry in tokenizedStockEntries.Take(5)) // Check first 5
                {
                    var fields = entry.Split(',');
                    var description = fields[3]; // description is field index 3

                    Assert.IsTrue(description.Contains("xStock") || description.Contains("Ondo Tokenized"),
                        $"Tokenized stock description should contain 'xStock' or 'Ondo Tokenized': {description}");
                }
            }
            else
            {
                Log.Trace("OKXExchangeInfoDownloaderTests.TokenizedStocks_HaveCorrectDescriptions(): No tokenized stocks found (this is OK)");
            }
        }

        /// <summary>
        /// Tokenized Stock Test: Verifies tokenized stock futures inherit description
        /// </summary>
        [Test]
        public void TokenizedStockFutures_InheritDescription()
        {
            // Arrange
            var downloader = new OKXExchangeInfoDownloader();

            // Act
            var entries = downloader.Get().ToList();
            var tokenizedStockFutures = entries
                .Where(e => e.Contains(",cryptofuture,") && e.Contains("xStock"))
                .ToList();

            // Assert
            if (tokenizedStockFutures.Any())
            {
                Log.Trace($"OKXExchangeInfoDownloaderTests.TokenizedStockFutures_InheritDescription(): Found {tokenizedStockFutures.Count} tokenized stock futures");

                foreach (var entry in tokenizedStockFutures.Take(5)) // Check first 5
                {
                    var fields = entry.Split(',');
                    var description = fields[3]; // description is field index 3

                    Assert.IsTrue(description.Contains("xStock") && description.Contains("Perpetual"),
                        $"Tokenized stock futures should have 'xStock' and 'Perpetual' in description: {description}");
                }
            }
            else
            {
                Log.Trace("OKXExchangeInfoDownloaderTests.TokenizedStockFutures_InheritDescription(): No tokenized stock futures found (this is OK)");
            }
        }

        /// <summary>
        /// Symbol Filter Test: Verifies symbol filter works for spot pairs
        /// </summary>
        [Test]
        public void SymbolFilter_FiltersSpotPairs()
        {
            // Arrange
            var filter = new System.Collections.Generic.HashSet<string> { "BTC-USDT", "ETH-USDT" };
            var downloader = new OKXExchangeInfoDownloader(filter);

            // Act
            var entries = downloader.Get().ToList();
            var spotEntries = entries.Where(e => e.Contains(",crypto,")).ToList();

            // Assert
            Assert.IsTrue(spotEntries.Any(), "Should have filtered spot entries");

            foreach (var entry in spotEntries)
            {
                var fields = entry.Split(',');
                var marketTicker = fields[8]; // market_ticker is field index 8

                Assert.IsTrue(filter.Contains(marketTicker),
                    $"Market ticker should be in filter: {marketTicker}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.SymbolFilter_FiltersSpotPairs(): Filtered to {spotEntries.Count} spot entries");
        }

        /// <summary>
        /// Symbol Filter Test: Verifies symbol filter works for futures
        /// </summary>
        [Test]
        public void SymbolFilter_FiltersFutures()
        {
            // Arrange
            var filter = new System.Collections.Generic.HashSet<string> { "BTC-USDT-SWAP", "ETH-USDT-SWAP" };
            var downloader = new OKXExchangeInfoDownloader(filter);

            // Act
            var entries = downloader.Get().ToList();
            var futuresEntries = entries.Where(e => e.Contains(",cryptofuture,")).ToList();

            // Assert
            Assert.IsTrue(futuresEntries.Any(), "Should have filtered futures entries");

            foreach (var entry in futuresEntries)
            {
                var fields = entry.Split(',');
                var marketTicker = fields[8]; // market_ticker is field index 8

                Assert.IsTrue(filter.Contains(marketTicker),
                    $"Market ticker should be in filter: {marketTicker}");
            }

            Log.Trace($"OKXExchangeInfoDownloaderTests.SymbolFilter_FiltersFutures(): Filtered to {futuresEntries.Count} futures entries");
        }

        /// <summary>
        /// Symbol Filter Test: Verifies null filter returns all symbols
        /// </summary>
        [Test]
        public void NoFilter_ReturnsAll()
        {
            // Arrange
            var downloaderWithFilter = new OKXExchangeInfoDownloader(null);
            var downloaderWithoutFilter = new OKXExchangeInfoDownloader();

            // Act
            var entriesWithFilter = downloaderWithFilter.Get().ToList();
            var entriesWithoutFilter = downloaderWithoutFilter.Get().ToList();

            // Assert
            Assert.AreEqual(entriesWithoutFilter.Count, entriesWithFilter.Count,
                "Null filter should return same count as no filter");

            Log.Trace($"OKXExchangeInfoDownloaderTests.NoFilter_ReturnsAll(): Both returned {entriesWithFilter.Count} entries");
        }
    }
}
