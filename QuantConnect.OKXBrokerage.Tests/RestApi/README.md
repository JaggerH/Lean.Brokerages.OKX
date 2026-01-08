# RestApi Unit Tests

This directory contains comprehensive unit tests for the Gate.io REST API client layer.

## Test Structure

The tests are organized into four separate test files, matching the structure of the RestApi implementation:

### 1. GateBaseRestApiClientTests.cs
Tests for the base REST API client functionality shared across all market types:

- **Time Synchronization Tests**
  - `GetServerTime_ReturnsValidTimestamp()` - Validates server time retrieval
  - `SyncServerTime_SucceedsWithValidCredentials()` - Tests time sync mechanism

- **Authentication Tests**
  - `GetNonce_ReturnsValidFormat()` - Validates nonce generation format
  - `GetUnixTimestamp_ReturnsReasonableValue()` - Tests Unix timestamp generation
  - `ComputeSha512Hash_ProducesCorrectHash()` - Validates SHA512 hashing
  - `GenerateHmacSignature_ProducesValidSignature()` - Tests HMAC-SHA512 signing
  - `ByteArrayToHexString_ConvertsCorrectly()` - Tests hex conversion utility

- **Time Conversion Tests**
  - `UnixSecondsToDateTime_ConvertsCorrectly()` - Tests timestamp to DateTime conversion
  - `UnixMillisecondsToDateTime_ConvertsCorrectly()` - Tests millisecond precision conversion
  - `DateTimeToUnixSeconds_ConvertsCorrectly()` - Tests DateTime to timestamp conversion
  - `DateTimeToUnixMilliseconds_ConvertsCorrectly()` - Tests millisecond precision conversion
  - `TimeConversion_RoundTrip_PreservesValue()` - Validates bidirectional conversion accuracy

- **Endpoint Construction Tests**
  - `GetEndpoint_FormatsCorrectly()` - Tests URL path construction

### 2. GateSpotRestApiClientTests.cs
Tests for Spot market-specific REST API functionality:

- **Property Tests**
  - `ApiPrefix_ReturnsSpot()` - Validates Spot API prefix
  - `SymbolParameterName_ReturnsCurrencyPair()` - Tests symbol parameter naming

- **Account Balance Tests**
  - `GetCashBalance_ReturnsValidBalances()` - Tests Spot balance retrieval
  - `GetAccountHoldings_ReturnsEmptyList()` - Validates that Spot has no holdings

- **Order Tests**
  - `GetOpenOrders_ReturnsValidList()` - Tests open order retrieval

- **Market Data Tests**
  - `GetOrderBookSnapshot_ReturnsValidData()` - Tests order book snapshot retrieval and validation
  - `GetTicker_ReturnsValidData()` - Tests ticker data retrieval
  - `GetRecentTrades_ReturnsValidData()` - Tests trade history retrieval
  - `GetCandlesticks_ReturnsValidData()` - Tests candlestick data retrieval
  - Error handling tests for invalid symbols/parameters

- **Parameter Validation Tests**
  - `GetOrderBookSnapshot_WithLargeLimit_RespectsMaximum()` - Tests limit enforcement
  - `GetRecentTrades_WithZeroLimit_ReturnsEmpty()` - Tests edge case handling

### 3. GateFuturesRestApiClientTests.cs
Tests for Futures market-specific REST API functionality:

- **Property Tests**
  - `ApiPrefix_ReturnsFuturesWithSettle()` - Validates Futures API prefix with settle currency
  - `ApiPrefix_WithBtcSettle_ReturnsCorrectPrefix()` - Tests BTC settle currency support
  - `SymbolParameterName_ReturnsContract()` - Tests contract parameter naming

- **Constructor Tests**
  - `Constructor_WithUppercaseSettle_NormalizesToLowercase()` - Tests settle normalization
  - `Constructor_DefaultSettle_IsUsdt()` - Validates default settle currency

- **Account Balance Tests**
  - `GetCashBalance_ReturnsValidBalances()` - Tests Futures balance retrieval
  - `GetAccountHoldings_ReturnsValidList()` - Tests position retrieval

- **Order Tests**
  - `GetOpenOrders_ReturnsValidList()` - Tests open order retrieval

- **Market Data Tests**
  - `GetOrderBookSnapshot_ReturnsValidData()` - Tests order book for futures contracts
  - `GetTicker_ReturnsValidData()` - Tests futures ticker data
  - `GetRecentTrades_ReturnsValidData()` - Tests futures trade history
  - `GetCandlesticks_ReturnsValidData()` - Tests futures candlestick data
  - `GetCandlesticks_WithDifferentIntervals_ReturnsData()` - Tests multiple timeframes
  - Error handling tests for invalid contracts

- **Settle Currency Tests**
  - `MultipleInstances_WithDifferentSettle_HaveCorrectPrefixes()` - Tests multi-settle support

- **Parameter Validation Tests**
  - `GetRecentTrades_WithPagination_ReturnsData()` - Tests trade pagination

### 4. GateUnifiedRestApiClientTests.cs
Tests for Unified Account routing and aggregation:

- **Property Tests**
  - `ApiPrefix_ThrowsNotSupportedException()` - Validates that unified client delegates to sub-clients
  - `SymbolParameterName_ThrowsNotSupportedException()` - Same as above
  - `SpotClient_IsNotNull()` - Tests Spot client initialization
  - `FuturesClient_IsNotNull()` - Tests Futures client initialization

- **Client Routing Tests**
  - `GetClientForSymbol_WithCryptoSymbol_ReturnsSpotClient()` - Tests Spot routing
  - `GetClientForSymbol_WithCryptoFutureSymbol_ReturnsFuturesClient()` - Tests Futures routing

- **Account Balance Tests**
  - `GetCashBalance_ReturnsSpotBalances()` - Validates that unified returns Spot balances
  - `GetAccountHoldings_ReturnsFuturesHoldings()` - Validates that unified returns Futures positions

- **Order Tests**
  - `GetOpenOrders_CombinesBothMarkets()` - Tests aggregation of orders from both markets

- **Market Data Routing Tests**
  - `GetOrderBookSnapshot_WithSpotSymbol_RoutesToSpotClient()` - Tests Spot routing
  - `GetOrderBookSnapshot_WithFuturesSymbol_RoutesToFuturesClient()` - Tests Futures routing
  - `GetOrderBookSnapshot_WithoutSymbol_DefaultsToSpot()` - Tests default routing
  - Similar routing tests for `GetOrderById()` and `CancelOrder()`

- **Time Synchronization Tests**
  - `SyncServerTime_SynchronizesBothClients()` - Tests global time sync

- **Constructor Tests**
  - `Constructor_WithDifferentSettle_PassesToFuturesClient()` - Tests settle currency propagation
  - `Constructor_DefaultSettle_UsesUsdt()` - Validates default behavior

- **Edge Case Tests**
  - `GetClientForSymbol_WithNullSymbol_ReturnsSpotClient()` - Tests null handling
  - `GetOrderBookSnapshot_SymbolRouting_WorksForBothMarkets()` - Tests cross-market data retrieval

## Test Coverage Summary

| Component | Unit Tests | Integration Tests | Coverage Focus |
|-----------|-----------|-------------------|----------------|
| GateBaseRestApiClient | 16 | 2 | Authentication, time conversion, utilities |
| GateSpotRestApiClient | 2 | 11 | Spot market data, balances, orders |
| GateFuturesRestApiClient | 5 | 12 | Futures market data, positions, settle currencies |
| GateUnifiedRestApiClient | 6 | 16 | Routing logic, aggregation, delegation |
| **Total** | **29** | **41** | **70 test cases** |

## Running the Tests

### Prerequisites

Before running the tests, ensure:

1. The main `QuantConnect.GateBrokerage` project compiles successfully
2. API credentials are configured in `QuantConnect.GateBrokerage.Tests/config.json`:

```json
{
  "gate-api-key": "your_api_key_here",
  "gate-api-secret": "your_api_secret_here",
  "gate-api-url": "https://api-testnet.gateapi.io/api/v4"
}
```

### Build the Test Project

```bash
dotnet build QuantConnect.GateBrokerage.Tests/QuantConnect.GateBrokerage.Tests.csproj -c Release
```

### Run All RestApi Tests

```bash
dotnet test QuantConnect.GateBrokerage.Tests/QuantConnect.GateBrokerage.Tests.csproj --filter "FullyQualifiedName~RestApi"
```

### Run Specific Test Files

**Base REST API Client Tests:**
```bash
dotnet test --filter "FullyQualifiedName~GateBaseRestApiClientTests"
```

**Spot REST API Client Tests:**
```bash
dotnet test --filter "FullyQualifiedName~GateSpotRestApiClientTests"
```

**Futures REST API Client Tests:**
```bash
dotnet test --filter "FullyQualifiedName~GateFuturesRestApiClientTests"
```

**Unified REST API Client Tests:**
```bash
dotnet test --filter "FullyQualifiedName~GateUnifiedRestApiClientTests"
```

### Run Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~GateBaseRestApiClientTests.GetNonce_ReturnsValidFormat"
```

## Test Categories

### Unit Tests
Tests that don't require API credentials and can run offline:
- Time conversion tests
- Endpoint construction tests
- Hashing and signature tests
- Property validation tests

### Integration Tests
Tests that require valid API credentials and network access:
- Market data retrieval tests
- Account balance tests
- Order retrieval tests
- Time synchronization tests

Integration tests will be skipped automatically if credentials are not configured.

## Known Issues

1. **Main Project Compilation**: The main `QuantConnect.GateBrokerage` project currently has compilation errors that prevent test execution. These need to be resolved first:
   - Missing interface member implementations in `GateBaseBrokerage.cs`
   - Required methods: `Subscribe()`, `Unsubscribe()`, `SetJob()`, `LookupSymbols()`, `CanPerformSelection()`

2. **Order Conversion**: Some tests return empty lists because the Gate.io Order → LEAN Order conversion is not yet implemented (see TODOs in source code).

## Test Design Principles

1. **Isolation**: Each test file focuses on a single client class
2. **Reflection**: Protected/internal members are tested via reflection to ensure proper encapsulation
3. **Graceful Degradation**: Integration tests skip gracefully when credentials are missing
4. **Error Handling**: Tests verify both success and error scenarios
5. **Data Validation**: Market data tests validate structure, constraints, and business rules
6. **Edge Cases**: Comprehensive coverage of boundary conditions and invalid inputs

## Contributing

When adding new functionality to the RestApi layer:

1. Add corresponding unit tests for new methods
2. Test both success and error paths
3. Validate input parameters and return values
4. Add integration tests for API endpoints
5. Update this README with new test descriptions

## References

- Gate.io API Documentation: https://www.gate.io/docs/developers/apiv4/en/
- Gate.io Futures API: https://www.gate.io/docs/developers/futures/en/
- LEAN Algorithm Framework: https://github.com/QuantConnect/Lean
