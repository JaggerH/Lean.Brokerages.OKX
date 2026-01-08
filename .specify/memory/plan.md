# Lean.Brokerages.OKX Implementation Plan

**项目：** OKX 券商插件实施计划
**版本：** 1.0
**创建日期：** 2026-01-09
**预计总工期：** 35-38 个工作日

本文档详细说明 Lean.Brokerages.OKX 项目的实施策略，包括 18 个开发阶段、时间估算、依赖关系和验证标准。

---

## 目录

1. [开发阶段概览](#1-开发阶段概览)
2. [Phase 0: 项目设置](#phase-0-项目设置)
3. [Phase 1: REST API 基础](#phase-1-rest-api-基础)
4. [Phase 2: 符号映射器](#phase-2-符号映射器)
5. [Phase 3: GetHistory 实现](#phase-3-gethistory-实现)
6. [Phase 4: 账户方法](#phase-4-账户方法)
7. [Phase 5: 订单管理-读取](#phase-5-订单管理-读取)
8. [Phase 6: 订单管理-写入](#phase-6-订单管理-写入)
9. [Phase 7: WebSocket 基础](#phase-7-websocket-基础)
10. [Phase 8: WebSocket 消息路由](#phase-8-websocket-消息路由)
11. [Phase 9: 市场数据订阅](#phase-9-市场数据订阅)
12. [Phase 10: 订单簿管理](#phase-10-订单簿管理)
13. [Phase 11: 订单更新](#phase-11-订单更新)
14. [Phase 12: 重连逻辑](#phase-12-重连逻辑)
15. [Phase 13: Brokerage Factory](#phase-13-brokerage-factory)
16. [Phase 14: 高级功能](#phase-14-高级功能)
17. [Phase 15: 文档](#phase-15-文档)
18. [Phase 16: LEAN 集成](#phase-16-lean-集成)
19. [Phase 17: LEAN CLI 集成](#phase-17-lean-cli-集成)
20. [Phase 18: 手动测试](#phase-18-手动测试)
21. [依赖图和里程碑](#依赖图和里程碑)
22. [文件创建总清单](#文件创建总清单)
23. [风险管理](#风险管理)

---

## 1. 开发阶段概览

| Phase | 名称 | 工期 | 累计 | 关键交付物 |
|-------|-----|------|------|-----------|
| 0 | 项目设置 | 1天 | 1天 | 仓库结构、.csproj 文件 |
| 1 | REST API 基础 | 1-2天 | 2-3天 | OKXRestApiClient |
| 2 | 符号映射器 | 1天 | 3-4天 | OKXSymbolMapper |
| 3 | GetHistory | 2-3天 | 5-7天 | 历史数据检索 |
| 4 | 账户方法 | 1-2天 | 6-9天 | GetCashBalance, GetAccountHoldings |
| 5 | 订单管理-读取 | 0.5-1天 | 7-10天 | GetOpenOrders |
| 6 | 订单管理-写入 | 2-3天 | 9-13天 | PlaceOrder, CancelOrder, UpdateOrder |
| 7 | WebSocket 基础 | 2-3天 | 11-16天 | WebSocket 连接、保活 |
| 8 | 消息路由 | 2天 | 13-18天 | 消息分发机制 |
| 9 | 市场数据订阅 | 3天 | 16-21天 | IDataQueueHandler |
| 10 | 订单簿管理 | 2天 | 18-23天 | DefaultOrderBook |
| 11 | 订单更新 | 2-3天 | 20-26天 | WebSocket 订单事件 |
| 12 | 重连逻辑 | 2天 | 22-28天 | 断线重连 |
| 13 | Factory | 0.5-1天 | 23-29天 | OKXBrokerageFactory |
| 14 | 高级功能 | 3-4天 | 26-33天 | PairMatcher, RiskLimit |
| 15 | 文档 | 2天 | 28-35天 | README, CLAUDE.md |
| 16 | LEAN 集成 | 3-4天 | 31-39天 | BrokerageModel, FeeModel |
| 17 | LEAN CLI | 1天 | 32-40天 | modules.json |
| 18 | 手动测试 | 3-5天 | 35-45天 | 稳定性验证 |

**MVP（最小可行产品）：** Phase 0-13（~23-29 天）
**完整功能版本：** Phase 0-18（~35-45 天）

---

## Phase 0: 项目设置

**目标：** 创建项目结构，配置构建环境

**工期：** 1 天

### 任务清单

1. **创建仓库目录结构**
   ```bash
   mkdir -p C:/Users/Jagger/Documents/Code/Lean.Brokerages.OKX
   cd C:/Users/Jagger/Documents/Code/Lean.Brokerages.OKX
   git init
   ```

2. **复制 Gate 仓库作为模板**
   ```bash
   cp -r ../Lean.Brokerages.Gate/QuantConnect.GateBrokerage ./QuantConnect.OKXBrokerage
   cp -r ../Lean.Brokerages.Gate/QuantConnect.GateBrokerage.Tests ./QuantConnect.OKXBrokerage.Tests
   ```

3. **全局重命名（查找替换）**
   - `Gate` → `OKX`
   - `gate` → `okx`
   - `GATE` → `OKX`
   - `Gate.io` → `OKX`

4. **删除不需要的 Gate 特定文件**
   - 删除 `GateSpotBrokerage.cs`, `GateFuturesBrokerage.cs` 等多个实现类
   - 删除 `GateSpotRestApiClient.cs`, `GateFuturesRestApiClient.cs` 等多个 REST 客户端
   - 保留单一基类文件，重命名为 `OKXBrokerage.cs`

5. **创建 .csproj 文件**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net6.0</TargetFramework>
       <GenerateDocumentationFile>true</GenerateDocumentationFile>
       <AssemblyName>QuantConnect.OKXBrokerage</AssemblyName>
       <RootNamespace>QuantConnect.Brokerages.OKX</RootNamespace>
     </PropertyGroup>

     <ItemGroup>
       <ProjectReference Include="..\Lean\Common\QuantConnect.csproj" />
       <ProjectReference Include="..\Lean\Brokerages\QuantConnect.Brokerages.csproj" />
     </ItemGroup>
   </Project>
   ```

6. **创建 Solution 文件**
   ```bash
   dotnet new sln -n QuantConnect.OKXBrokerage
   dotnet sln add QuantConnect.OKXBrokerage/QuantConnect.OKXBrokerage.csproj
   dotnet sln add QuantConnect.OKXBrokerage.Tests/QuantConnect.OKXBrokerage.Tests.csproj
   ```

7. **创建 .gitignore**
   ```
   bin/
   obj/
   *.user
   *.suo
   .vs/
   config.json
   appsettings.json
   ```

8. **创建 config.json.example**
   ```json
   {
     "okx-api-key": "your-api-key-here",
     "okx-api-secret": "your-api-secret-here",
     "okx-passphrase": "your-passphrase-here",
     "okx-api-url": "https://www.okx.com",
     "okx-websocket-url": "wss://ws.okx.com:8443/ws/v5",
     "okx-environment": "production"
   }
   ```

9. **验证构建**
   ```bash
   dotnet restore
   dotnet build
   ```

### 验证标准

- [ ] 项目结构完整
- [ ] 所有 Gate 引用已替换为 OKX
- [ ] 编译通过（0 错误，可以有警告）
- [ ] Solution 包含 2 个项目（主项目 + 测试项目）
- [ ] .gitignore 正确配置
- [ ] config.json.example 已创建

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage.sln`
- `QuantConnect.OKXBrokerage/QuantConnect.OKXBrokerage.csproj`
- `QuantConnect.OKXBrokerage.Tests/QuantConnect.OKXBrokerage.Tests.csproj`
- `.gitignore`
- `config.json.example`

---

## Phase 1: REST API 基础

**目标：** 实现 OKX REST API 客户端基础功能

**工期：** 1-2 天

**简化要点：** OKX API 统一 → 只需一个 REST 客户端类

### 任务清单

1. **创建 OKXRestApiClient.cs**
   - 参考：`Lean.Brokerages.Gate/RestApi/GateBaseRestApiClient.cs`
   - 简化：不需要抽象基类，不需要多个派生类

   ```csharp
   public class OKXRestApiClient
   {
       private readonly string _apiKey;
       private readonly string _apiSecret;
       private readonly string _passphrase;
       private readonly string _apiUrl;

       // 速率限制器
       private readonly RateGate _orderRateLimiter;      // 1000 请求/2秒
       private readonly RateGate _accountRateLimiter;    // 10 请求/2秒
       private readonly RateGate _instrumentRateLimiter; // 20 请求/2秒

       public OKXRestApiClient(string apiKey, string apiSecret, string passphrase, string apiUrl)
       {
           _apiKey = apiKey;
           _apiSecret = apiSecret;
           _passphrase = passphrase;
           _apiUrl = apiUrl;

           _orderRateLimiter = new RateGate(1000, TimeSpan.FromSeconds(2));
           _accountRateLimiter = new RateGate(10, TimeSpan.FromSeconds(2));
           _instrumentRateLimiter = new RateGate(20, TimeSpan.FromSeconds(2));
       }
   }
   ```

2. **实现认证与签名**
   ```csharp
   private string GenerateSignature(string timestamp, string method, string path, string body)
   {
       var message = timestamp + method.ToUpper() + path + body;
       using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
       {
           var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
           return Convert.ToBase64String(hash);
       }
   }

   private void AddAuthenticationHeaders(RestRequest request, string method, string path, string body)
   {
       var timestamp = GetTimestamp();
       var signature = GenerateSignature(timestamp, method, path, body);

       request.AddHeader("OK-ACCESS-KEY", _apiKey);
       request.AddHeader("OK-ACCESS-SIGN", signature);
       request.AddHeader("OK-ACCESS-TIMESTAMP", timestamp);
       request.AddHeader("OK-ACCESS-PASSPHRASE", _passphrase);
       request.AddHeader("Content-Type", "application/json");
   }
   ```

3. **实现公共端点**
   - `GetServerTime()` - 获取服务器时间
   - `GetInstruments(string instType)` - 获取工具列表
   - `GetTicker(string instId)` - 获取行情

4. **实现账户端点**
   - `GetAccountConfig()` - 获取账户配置（检测账户模式）
   - `GetBalance(string currency = null)` - 获取余额
   - `GetPositions(string instType = null)` - 获取持仓

5. **创建消息模型**
   - `Messages/ServerTime.cs`
   - `Messages/Instrument.cs`
   - `Messages/Ticker.cs`
   - `Messages/AccountConfig.cs`
   - `Messages/Balance.cs`
   - `Messages/Position.cs`

6. **创建基础 Converter**
   - `Converters/DecimalConverter.cs`
   - `Converters/DateTimeConverter.cs`
   - `Converters/ServerTimeConverter.cs`

7. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXRestApiClientTests
   {
       [Test]
       public void GenerateSignature_WithKnownInput_ReturnsExpectedSignature()
       {
           // 使用 OKX 文档中的示例验证签名生成
       }

       [Test]
       public void GetServerTime_WithValidCredentials_ReturnsTime()
       {
           // 需要真实 API 凭证（可选）
       }
   }
   ```

### 验证标准

- [ ] 编译通过（0 错误，0 警告）
- [ ] 成功调用 `/api/v5/public/time` 端点
- [ ] 认证签名生成正确（对比 OKX 文档示例）
- [ ] 速率限制器工作正常（快速调用时被限制）
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXRestApiClient.cs`
- `QuantConnect.OKXBrokerage/Messages/ServerTime.cs`
- `QuantConnect.OKXBrokerage/Messages/AccountConfig.cs`
- `QuantConnect.OKXBrokerage/Messages/Balance.cs`
- `QuantConnect.OKXBrokerage/Messages/Position.cs`
- `QuantConnect.OKXBrokerage/Converters/DecimalConverter.cs`
- `QuantConnect.OKXBrokerage/Converters/DateTimeConverter.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXRestApiClientTests.cs`

---

## Phase 2: 符号映射器

**目标：** 实现 LEAN Symbol ↔ OKX instId 双向转换

**工期：** 1 天

### 任务清单

1. **创建 OKXSymbolMapper.cs**
   - 参考：`Lean.Brokerages.Gate/GateSymbolMapper.cs`

   ```csharp
   public class OKXSymbolMapper
   {
       public string GetBrokerageSymbol(Symbol symbol)
       {
           if (symbol.SecurityType == SecurityType.Crypto)
           {
               // BTCUSDT → BTC-USDT
               return FormatSpotSymbol(symbol);
           }
           else if (symbol.SecurityType == SecurityType.CryptoFuture)
           {
               if (IsPerpetual(symbol))
               {
                   // BTCUSDT → BTC-USDT-SWAP
                   return FormatSwapSymbol(symbol);
               }
               else
               {
                   // BTCUSDT → BTC-USDT-250328
                   return FormatFuturesSymbol(symbol);
               }
           }

           throw new NotSupportedException($"Security type {symbol.SecurityType} not supported");
       }

       public Symbol GetLeanSymbol(string instId, SecurityType securityType)
       {
           // BTC-USDT → BTCUSDT (Crypto)
           // BTC-USDT-SWAP → BTCUSDT (CryptoFuture, perpetual)
           // BTC-USDT-250328 → BTCUSDT (CryptoFuture, expiry)
       }
   }
   ```

2. **实现格式转换方法**
   - `FormatSpotSymbol()` - 现货格式
   - `FormatSwapSymbol()` - 永续格式
   - `FormatFuturesSymbol()` - 交割格式
   - `ParseInstId()` - 解析 instId

3. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXSymbolMapperTests
   {
       [Test]
       public void GetBrokerageSymbol_Spot_ReturnsCorrectFormat()
       {
           var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
           var result = _mapper.GetBrokerageSymbol(symbol);
           Assert.AreEqual("BTC-USDT", result);
       }

       [Test]
       public void GetBrokerageSymbol_Swap_ReturnsCorrectFormat()
       {
           var symbol = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX);
           var result = _mapper.GetBrokerageSymbol(symbol);
           Assert.AreEqual("BTC-USDT-SWAP", result);
       }

       [Test]
       public void BidirectionalConversion_IsReversible()
       {
           var original = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
           var brokerageSymbol = _mapper.GetBrokerageSymbol(original);
           var restored = _mapper.GetLeanSymbol(brokerageSymbol, SecurityType.Crypto);

           Assert.AreEqual(original, restored);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 现货符号转换正确（BTCUSDT ↔ BTC-USDT）
- [ ] 永续符号转换正确（BTCUSDT ↔ BTC-USDT-SWAP）
- [ ] 交割符号转换正确（BTCUSDT ↔ BTC-USDT-YYMMDD）
- [ ] 双向转换可逆
- [ ] 所有单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXSymbolMapper.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXSymbolMapperTests.cs`

---

## Phase 3: GetHistory 实现

**目标：** 实现历史数据检索功能

**工期：** 2-3 天

### 任务清单

1. **创建 OKXBrokerage.History.cs（部分类）**
   ```csharp
   public partial class OKXBrokerage
   {
       public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
       {
           if (request.Resolution == Resolution.Second)
           {
               Log.Warning("OKXBrokerage: Second resolution not supported");
               return null;
           }

           if (request.TickType == TickType.Trade)
           {
               return GetTradeTicks(request);
           }
           else if (request.TickType == TickType.Quote)
           {
               Log.Warning("OKXBrokerage: Quote tick history not supported");
               return null;
           }
           else
           {
               return GetCandles(request);
           }
       }
   }
   ```

2. **在 OKXRestApiClient 中添加历史数据端点**
   ```csharp
   public List<Candle> GetCandles(string instId, string bar, long? after, long? before, int? limit)
   {
       _instrumentRateLimiter.WaitToProceed();

       var endpoint = "/api/v5/market/candles";
       var parameters = new Dictionary<string, object>
       {
           { "instId", instId },
           { "bar", bar }
       };

       if (after.HasValue) parameters["after"] = after.Value;
       if (before.HasValue) parameters["before"] = before.Value;
       if (limit.HasValue) parameters["limit"] = limit.Value;

       return Get<List<Candle>>(endpoint, parameters);
   }

   public List<Trade> GetTrades(string instId, int? limit)
   {
       _instrumentRateLimiter.WaitToProceed();

       var endpoint = "/api/v5/market/trades";
       var parameters = new Dictionary<string, object>
       {
           { "instId", instId }
       };

       if (limit.HasValue) parameters["limit"] = limit.Value;

       return Get<List<Trade>>(endpoint, parameters);
   }
   ```

3. **创建历史数据消息模型**
   - `Messages/Candle.cs`
   - `Messages/Trade.cs`

4. **创建 Converter**
   - `Converters/CandleConverter.cs`
   - `Converters/TradeConverter.cs`
   - `Converters/CandleToBarsConverter.cs` (扩展方法)
   - `Converters/TradeToTickConverter.cs` (扩展方法)

5. **实现分辨率转换**
   ```csharp
   private string ConvertResolutionToBar(Resolution resolution)
   {
       return resolution switch
       {
           Resolution.Minute => "1m",
           Resolution.Hour => "1H",
           Resolution.Daily => "1D",
           _ => throw new NotSupportedException($"Resolution {resolution} not supported")
       };
   }
   ```

6. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageHistoryTests
   {
       [Test]
       public void GetHistory_MinuteResolution_ReturnsCandles()
       {
           var request = new HistoryRequest(
               DateTime.UtcNow.AddDays(-1),
               DateTime.UtcNow,
               typeof(TradeBar),
               Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX),
               Resolution.Minute,
               SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
               TimeZones.Utc,
               null,
               false,
               false,
               DataNormalizationMode.Raw,
               TickType.Trade
           );

           var result = _brokerage.GetHistory(request);
           Assert.IsNotNull(result);
           Assert.IsNotEmpty(result);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 可以检索 1 周的分钟数据
- [ ] 可以检索 1 天的小时数据
- [ ] 可以检索 1 个月的日线数据
- [ ] Trade Tick 数据可以检索
- [ ] Second 分辨率返回 null（不支持）
- [ ] Quote Tick 历史返回 null（不支持）
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.History.cs`
- `QuantConnect.OKXBrokerage/Messages/Candle.cs`
- `QuantConnect.OKXBrokerage/Messages/Trade.cs`
- `QuantConnect.OKXBrokerage/Converters/CandleConverter.cs`
- `QuantConnect.OKXBrokerage/Converters/TradeConverter.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageHistoryTests.cs`

---

## Phase 4: 账户方法

**目标：** 实现 GetCashBalance 和 GetAccountHoldings

**工期：** 1-2 天

**简化要点：** OKX 统一账户端点，运行时检测账户模式

### 任务清单

1. **创建 OKXBrokerage.cs（主文件）**
   ```csharp
   public partial class OKXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
   {
       private readonly OKXRestApiClient _apiClient;
       private readonly OKXSymbolMapper _symbolMapper;
       private OKXAccountMode _accountMode;

       public OKXBrokerage(string apiKey, string apiSecret, string passphrase,
                          IAlgorithm algorithm, IDataAggregator aggregator)
           : base("OKX")
       {
           _apiClient = new OKXRestApiClient(apiKey, apiSecret, passphrase, GetApiUrl());
           _symbolMapper = new OKXSymbolMapper();
           _algorithm = algorithm;
           _aggregator = aggregator;
       }

       public override void Connect()
       {
           base.Connect();

           // 检测账户模式
           _accountMode = DetectAccountMode();
           Log.Info($"OKXBrokerage: Account mode detected: {_accountMode}");
       }
   }
   ```

2. **实现账户模式检测**
   ```csharp
   private OKXAccountMode DetectAccountMode()
   {
       try
       {
           var config = _apiClient.GetAccountConfig();
           return config.AccountLevel switch
           {
               "1" => OKXAccountMode.Spot,
               "2" => OKXAccountMode.Futures,
               "3" => OKXAccountMode.MultiCurrencyMargin,
               "4" => OKXAccountMode.PortfolioMargin,
               _ => throw new InvalidOperationException($"Unknown account level: {config.AccountLevel}")
           };
       }
       catch (Exception ex)
       {
           Log.Error($"OKXBrokerage: Failed to detect account mode: {ex.Message}");
           return OKXAccountMode.MultiCurrencyMargin;  // 默认
       }
   }
   ```

3. **实现 GetCashBalance**
   ```csharp
   public override List<CashAmount> GetCashBalance()
   {
       switch (_accountMode)
       {
           case OKXAccountMode.Spot:
               return GetSpotBalances();
           case OKXAccountMode.Futures:
               return GetFuturesBalances();
           case OKXAccountMode.MultiCurrencyMargin:
           case OKXAccountMode.PortfolioMargin:
               return GetUnifiedBalances();
           default:
               throw new NotSupportedException($"Account mode {_accountMode} not supported");
       }
   }
   ```

4. **实现 GetAccountHoldings**
   ```csharp
   public override List<Holding> GetAccountHoldings()
   {
       var holdings = new List<Holding>();

       switch (_accountMode)
       {
           case OKXAccountMode.Spot:
               var spotBalances = _apiClient.GetBalance();
               holdings.AddRange(ConvertBalancesToHoldings(spotBalances));
               break;

           case OKXAccountMode.Futures:
               var futuresPositions = _apiClient.GetPositions(instType: "SWAP");
               holdings.AddRange(ConvertPositionsToHoldings(futuresPositions));
               break;

           case OKXAccountMode.MultiCurrencyMargin:
           case OKXAccountMode.PortfolioMargin:
               var allBalances = _apiClient.GetBalance();
               var allPositions = _apiClient.GetPositions();
               holdings.AddRange(ConvertBalancesToHoldings(allBalances));
               holdings.AddRange(ConvertPositionsToHoldings(allPositions));
               break;
       }

       return holdings;
   }
   ```

5. **创建 Domain Converter**
   - `Converters/BalanceExtensions.cs` - Balance → CashAmount
   - `Converters/PositionExtensions.cs` - Position → Holding

6. **创建 OKXAccountMode 枚举**
   ```csharp
   public enum OKXAccountMode
   {
       Spot,
       Futures,
       MultiCurrencyMargin,
       PortfolioMargin
   }
   ```

7. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageAccountTests
   {
       [Test]
       public void GetCashBalance_SpotMode_ReturnsBalances()
       {
           // Mock 账户模式为 Spot
           var balances = _brokerage.GetCashBalance();
           Assert.IsNotNull(balances);
       }

       [Test]
       public void GetAccountHoldings_MultiCurrencyMode_ReturnsSpotAndFuturesPositions()
       {
           // Mock 账户模式为 MultiCurrencyMargin
           var holdings = _brokerage.GetAccountHoldings();
           Assert.IsNotNull(holdings);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 账户模式正确检测
- [ ] GetCashBalance 在测试网返回正确余额
- [ ] GetAccountHoldings 在测试网返回正确持仓
- [ ] 不同账户模式行为正确
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.cs`
- `QuantConnect.OKXBrokerage/OKXAccountMode.cs`
- `QuantConnect.OKXBrokerage/Converters/BalanceExtensions.cs`
- `QuantConnect.OKXBrokerage/Converters/PositionExtensions.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageAccountTests.cs`

---

## Phase 5: 订单管理-读取

**目标：** 实现 GetOpenOrders

**工期：** 0.5-1 天

**简化要点：** 统一订单模型，无需分现货/期货

### 任务清单

1. **创建 OKXBrokerage.Orders.cs（部分类）**
   ```csharp
   public partial class OKXBrokerage
   {
       public override List<Order> GetOpenOrders()
       {
           var okxOrders = _apiClient.GetOpenOrders();
           return ConvertToLeanOrders(okxOrders);
       }

       private List<Order> ConvertToLeanOrders(List<Messages.Order> okxOrders)
       {
           var orders = new List<Order>();

           foreach (var okxOrder in okxOrders)
           {
               var leanOrder = ConvertToLeanOrder(okxOrder);
               if (leanOrder != null)
               {
                   orders.Add(leanOrder);
               }
           }

           return orders;
       }
   }
   ```

2. **在 OKXRestApiClient 中添加订单查询端点**
   ```csharp
   public List<Order> GetOpenOrders(string instType = null, string instId = null)
   {
       _orderRateLimiter.WaitToProceed();

       var endpoint = "/api/v5/trade/orders-pending";
       var parameters = new Dictionary<string, object>();

       if (!string.IsNullOrEmpty(instType))
           parameters["instType"] = instType;
       if (!string.IsNullOrEmpty(instId))
           parameters["instId"] = instId;

       return Get<List<Order>>(endpoint, parameters);
   }
   ```

3. **创建订单消息模型**
   - `Messages/Order.cs` - 统一订单模型（现货+期货）

4. **创建 Converter**
   - `Converters/OrderConverter.cs` - JSON → Order
   - `Converters/OrderExtensions.cs` - Order → LEAN Order

5. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageOrderReadTests
   {
       [Test]
       public void GetOpenOrders_ReturnsAllOpenOrders()
       {
           var orders = _brokerage.GetOpenOrders();
           Assert.IsNotNull(orders);
       }

       [Test]
       public void ConvertToLeanOrder_WithValidOKXOrder_ReturnsLeanOrder()
       {
           var okxOrder = CreateMockOKXOrder();
           var leanOrder = _brokerage.ConvertToLeanOrder(okxOrder);

           Assert.IsNotNull(leanOrder);
           Assert.AreEqual(okxOrder.ClientOrderId, leanOrder.Id.ToString());
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] GetOpenOrders 返回正确数量的订单
- [ ] 订单状态映射正确
- [ ] 订单类型映射正确
- [ ] 通过 UI 下单后，GetOpenOrders() 可见
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Orders.cs`
- `QuantConnect.OKXBrokerage/Messages/Order.cs`
- `QuantConnect.OKXBrokerage/Converters/OrderConverter.cs`
- `QuantConnect.OKXBrokerage/Converters/OrderExtensions.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageOrderReadTests.cs`

---

## Phase 6: 订单管理-写入

**目标：** 实现 PlaceOrder, CancelOrder, UpdateOrder

**工期：** 2-3 天

**简化要点：** 统一端点，无需 if/else 判断现货/期货

### 任务清单

1. **在 OKXBrokerage.Orders.cs 中实现 PlaceOrder**
   ```csharp
   public override bool PlaceOrder(Order order)
   {
       ValidateOrder(order);

       var request = ConvertToOKXOrder(order);

       _orderRateLimiter.WaitToProceed();

       var response = _apiClient.PlaceOrder(request);

       if (response.IsSuccess)
       {
           var orderData = response.Data.FirstOrDefault();
           if (orderData != null)
           {
               OnOrderIdChanged(new BrokerageOrderIdChangedEvent
               {
                   OrderId = order.Id,
                   BrokerId = orderData.OrdId
               });
           }
           return true;
       }
       else
       {
           OnMessage(new BrokerageMessageEvent(
               BrokerageMessageType.Warning,
               response.Code,
               response.Msg
           ));
           return false;
       }
   }
   ```

2. **实现订单转换**
   ```csharp
   private PlaceOrderRequest ConvertToOKXOrder(Order order)
   {
       var symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);
       var request = new PlaceOrderRequest
       {
           InstId = symbol,
           Side = order.Direction == OrderDirection.Buy ? "buy" : "sell",
           ClOrdId = order.Id.ToString(),
           TdMode = DetermineTdMode(order.Symbol, _accountMode)
       };

       // 根据订单类型设置参数（统一处理，无需区分现货/期货）
       switch (order)
       {
           case MarketOrder marketOrder:
               request.OrdType = "market";
               request.Sz = Math.Abs(marketOrder.Quantity).ToStringInvariant();
               break;

           case LimitOrder limitOrder:
               request.OrdType = DetermineLimitOrderType(limitOrder);
               request.Px = limitOrder.LimitPrice.ToStringInvariant();
               request.Sz = Math.Abs(limitOrder.Quantity).ToStringInvariant();
               break;

           case StopLimitOrder stopLimitOrder:
               request.OrdType = "trigger";
               request.TriggerPx = stopLimitOrder.StopPrice.ToStringInvariant();
               request.OrderPx = stopLimitOrder.LimitPrice.ToStringInvariant();
               request.Sz = Math.Abs(stopLimitOrder.Quantity).ToStringInvariant();
               break;
       }

       return request;
   }
   ```

3. **实现 CancelOrder**
   ```csharp
   public override bool CancelOrder(Order order)
   {
       var brokerId = GetBrokerageOrderId(order);
       if (string.IsNullOrEmpty(brokerId))
       {
           Log.Error($"OKXBrokerage: Cannot cancel order {order.Id}, brokerage ID not found");
           return false;
       }

       var symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);

       _orderRateLimiter.WaitToProceed();

       var response = _apiClient.CancelOrder(symbol, brokerId);

       return response.IsSuccess;
   }
   ```

4. **实现 UpdateOrder**
   ```csharp
   public override bool UpdateOrder(Order order)
   {
       var brokerId = GetBrokerageOrderId(order);
       if (string.IsNullOrEmpty(brokerId))
       {
           Log.Error($"OKXBrokerage: Cannot update order {order.Id}, brokerage ID not found");
           return false;
       }

       var symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);

       var request = new AmendOrderRequest
       {
           InstId = symbol,
           OrdId = brokerId
       };

       // OKX 只能改价格和数量
       if (order is LimitOrder limitOrder)
       {
           request.NewPx = limitOrder.LimitPrice.ToStringInvariant();
       }
       request.NewSz = Math.Abs(order.Quantity).ToStringInvariant();

       _orderRateLimiter.WaitToProceed();

       var response = _apiClient.AmendOrder(request);

       return response.IsSuccess;
   }
   ```

5. **在 OKXRestApiClient 中添加订单操作端点**
   ```csharp
   public OrderResponse PlaceOrder(PlaceOrderRequest request)
   {
       _orderRateLimiter.WaitToProceed();
       return Post<OrderResponse>("/api/v5/trade/order", request);
   }

   public CancelOrderResponse CancelOrder(string instId, string ordId = null, string clOrdId = null)
   {
       _orderRateLimiter.WaitToProceed();
       var body = new { instId, ordId, clOrdId };
       return Post<CancelOrderResponse>("/api/v5/trade/cancel-order", body);
   }

   public AmendOrderResponse AmendOrder(AmendOrderRequest request)
   {
       _orderRateLimiter.WaitToProceed();
       return Post<AmendOrderResponse>("/api/v5/trade/amend-order", request);
   }
   ```

6. **创建请求/响应模型**
   - `Messages/PlaceOrderRequest.cs`
   - `Messages/AmendOrderRequest.cs`
   - `Messages/OrderResponse.cs`
   - `Messages/CancelOrderResponse.cs`
   - `Messages/AmendOrderResponse.cs`

7. **创建 OKXOrderProperties**
   ```csharp
   public class OKXOrderProperties : OrderProperties
   {
       public bool ReduceOnly { get; set; }
       public string TdMode { get; set; }
   }
   ```

8. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageOrderWriteTests
   {
       [Test]
       public void PlaceOrder_MarketOrder_Success()
       {
           var order = new MarketOrder(...);
           var result = _brokerage.PlaceOrder(order);
           Assert.IsTrue(result);
       }

       [Test]
       public void PlaceOrder_LimitOrder_Success()
       {
           var order = new LimitOrder(...);
           var result = _brokerage.PlaceOrder(order);
           Assert.IsTrue(result);
       }

       [Test]
       public void CancelOrder_ExistingOrder_Success()
       {
           // 先下单
           var order = new LimitOrder(...);
           _brokerage.PlaceOrder(order);

           // 再撤单
           var result = _brokerage.CancelOrder(order);
           Assert.IsTrue(result);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 市价单可以成功下单
- [ ] 限价单可以成功下单
- [ ] 止损限价单可以成功下单
- [ ] 订单可以成功撤销
- [ ] 订单可以成功修改（价格和数量）
- [ ] 现货和期货使用相同的代码逻辑
- [ ] 单元测试通过
- [ ] 测试网上完整订单生命周期工作正常

### 关键文件

**创建/修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Orders.cs` (扩展)
- `QuantConnect.OKXBrokerage/Messages/PlaceOrderRequest.cs`
- `QuantConnect.OKXBrokerage/Messages/OrderResponse.cs`
- `QuantConnect.OKXBrokerage/Messages/CancelOrderResponse.cs`
- `QuantConnect.OKXBrokerage/OKXOrderProperties.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageOrderWriteTests.cs`

---

## Phase 7: WebSocket 基础

**目标：** 建立 WebSocket 连接，实现保活机制

**工期：** 2-3 天

**OKX 特殊要求：** 30 秒超时，必须保活

### 任务清单

1. **创建 OKXWebSocketWrapper.cs**
   - 参考：`Lean.Brokerages.Gate/GateWebSocketWrapper.cs`

   ```csharp
   public class OKXWebSocketWrapper
   {
       private readonly WebSocketClientWrapper _webSocket;
       private readonly string _url;

       public event EventHandler<WebSocketMessage> Message;
       public event EventHandler<WebSocketCloseData> Closed;
       public event EventHandler Opened;

       public OKXWebSocketWrapper(string url)
       {
           _url = url;
           _webSocket = new WebSocketClientWrapper();

           _webSocket.Message += OnMessage;
           _webSocket.Closed += OnClosed;
           _webSocket.Open += OnOpened;
       }
   }
   ```

2. **在 OKXBrokerage.cs 中实现 Connect/Disconnect**
   ```csharp
   public override void Connect()
   {
       if (IsConnected)
           return;

       // 创建公共通道 WebSocket
       _publicWebSocket = new OKXWebSocketWrapper(GetPublicWebSocketUrl());
       _publicWebSocket.Message += OnDataMessage;
       _publicWebSocket.Open();

       // 创建私有通道 WebSocket
       _privateWebSocket = new OKXWebSocketWrapper(GetPrivateWebSocketUrl());
       _privateWebSocket.Message += OnUserMessage;
       _privateWebSocket.Open();

       // 登录私有通道
       LoginPrivateChannel();

       // 启动保活定时器
       InitializeKeepAlive();

       base.Connect();
   }

   public override void Disconnect()
   {
       _keepAliveTimer?.Stop();
       _keepAliveTimer?.Dispose();

       _publicWebSocket?.Close();
       _privateWebSocket?.Close();

       base.Disconnect();
   }

   public override bool IsConnected =>
       _publicWebSocket?.IsOpen == true &&
       _privateWebSocket?.IsOpen == true;
   ```

3. **实现保活机制**
   ```csharp
   private Timer _keepAliveTimer;
   private DateTime _lastMessageTime;
   private const int KeepAliveIntervalSeconds = 20;
   private const int PongTimeoutSeconds = 30;

   private void InitializeKeepAlive()
   {
       _lastMessageTime = DateTime.UtcNow;

       _keepAliveTimer = new Timer(KeepAliveIntervalSeconds * 1000);
       _keepAliveTimer.Elapsed += OnKeepAliveTimerElapsed;
       _keepAliveTimer.AutoReset = true;
       _keepAliveTimer.Start();

       Log.Trace("OKXBrokerage: Keep-alive timer started");
   }

   private void OnKeepAliveTimerElapsed(object sender, ElapsedEventArgs e)
   {
       var timeSinceLastMessage = DateTime.UtcNow - _lastMessageTime;

       if (timeSinceLastMessage.TotalSeconds >= KeepAliveIntervalSeconds)
       {
           SendPing();

           // 检查 pong 超时
           if (timeSinceLastMessage.TotalSeconds >= PongTimeoutSeconds)
           {
               Log.Warning("OKXBrokerage: Pong timeout, reconnecting...");
               ScheduleReconnection();
           }
       }
   }

   private void SendPing()
   {
       try
       {
           _publicWebSocket?.Send("ping");
           _privateWebSocket?.Send("ping");
           Log.Trace("OKXBrokerage: Sent ping");
       }
       catch (Exception ex)
       {
           Log.Error($"OKXBrokerage: Error sending ping: {ex.Message}");
       }
   }

   private void OnMessage(string message)
   {
       _lastMessageTime = DateTime.UtcNow;

       if (message == "pong")
       {
           Log.Trace("OKXBrokerage: Received pong");
           return;
       }

       // 处理其他消息...
   }
   ```

4. **实现私有通道登录**
   ```csharp
   private void LoginPrivateChannel()
   {
       var timestamp = GetTimestamp();
       var signature = GenerateWebSocketSignature(timestamp);

       var loginRequest = new
       {
           op = "login",
           args = new[]
           {
               new
               {
                   apiKey = _apiKey,
                   passphrase = _passphrase,
                   timestamp = timestamp,
                   sign = signature
               }
           }
       };

       _privateWebSocket.Send(JsonConvert.SerializeObject(loginRequest));
       Log.Trace("OKXBrokerage: Sent login request");
   }

   private string GenerateWebSocketSignature(string timestamp)
   {
       var message = timestamp + "GET" + "/users/self/verify";
       using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
       {
           var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
           return Convert.ToBase64String(hash);
       }
   }
   ```

5. **创建 WebSocket 消息模型**
   - `Messages/WebSocketRequest.cs`
   - `Messages/WebSocketResponse.cs`
   - `Messages/LoginRequest.cs`
   - `Messages/LoginResponse.cs`

6. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageWebSocketTests
   {
       [Test]
       public void Connect_WithValidCredentials_Success()
       {
           _brokerage.Connect();
           Assert.IsTrue(_brokerage.IsConnected);
       }

       [Test]
       public void KeepAlive_SendsPing_Every20Seconds()
       {
           _brokerage.Connect();
           Thread.Sleep(25000);  // 等待 25 秒
           // 验证 ping 已发送
       }

       [Test]
       public void Disconnect_ClosesConnections_Success()
       {
           _brokerage.Connect();
           _brokerage.Disconnect();
           Assert.IsFalse(_brokerage.IsConnected);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] WebSocket 连接成功（公共 + 私有）
- [ ] 私有通道登录成功
- [ ] 保活定时器工作（每 20 秒发送 ping）
- [ ] 收到 pong 响应
- [ ] 30 秒无消息时触发重连
- [ ] Disconnect 正确关闭所有连接
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXWebSocketWrapper.cs`
- `QuantConnect.OKXBrokerage/Messages/WebSocketRequest.cs`
- `QuantConnect.OKXBrokerage/Messages/WebSocketResponse.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageWebSocketTests.cs`

**修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.cs`

---

## Phase 8: WebSocket 消息路由

**目标：** 实现消息分发机制，将不同类型的 WebSocket 消息路由到相应的处理器

**工期：** 2 天

### 任务清单

1. **创建 OKXBrokerage.Messaging.cs（部分类）**
   - 参考：`Lean.Brokerages.Gate/Core/GateBaseBrokerage.Messaging.cs`

   ```csharp
   public partial class OKXBrokerage
   {
       private void OnDataMessage(object sender, WebSocketMessage message)
       {
           try
           {
               var json = message.Data.ToString();
               RoutePublicMessage(json);
           }
           catch (Exception ex)
           {
               Log.Error($"OKXBrokerage: Error processing public message: {ex}");
           }
       }

       private void OnUserMessage(object sender, WebSocketMessage message)
       {
           try
           {
               var json = message.Data.ToString();
               RoutePrivateMessage(json);
           }
           catch (Exception ex)
           {
               Log.Error($"OKXBrokerage: Error processing private message: {ex}");
           }
       }

       private void RoutePublicMessage(string json)
       {
           // 判断消息类型
           if (json == "pong")
           {
               HandlePong();
               return;
           }

           var baseMessage = JsonConvert.DeserializeObject<WebSocketResponse>(json);

           if (baseMessage.Arg.Channel == "tickers")
           {
               HandleTickerUpdate(json);
           }
           else if (baseMessage.Arg.Channel == "trades")
           {
               HandleTradeUpdate(json);
           }
           else if (baseMessage.Arg.Channel == "books" || baseMessage.Arg.Channel == "books5")
           {
               HandleOrderBookUpdate(json);
           }
       }

       private void RoutePrivateMessage(string json)
       {
           var baseMessage = JsonConvert.DeserializeObject<WebSocketResponse>(json);

           if (baseMessage.Event == "login")
           {
               HandleLoginResponse(json);
           }
           else if (baseMessage.Arg.Channel == "orders")
           {
               HandleOrderUpdate(json);
           }
           else if (baseMessage.Arg.Channel == "account")
           {
               HandleAccountUpdate(json);
           }
           else if (baseMessage.Arg.Channel == "positions")
           {
               HandlePositionUpdate(json);
           }
       }
   }
   ```

2. **创建消息更新模型**
   - `Messages/TickerUpdate.cs` - Ticker 更新
   - `Messages/TradeUpdate.cs` - Trade 更新
   - `Messages/OrderBookUpdate.cs` - 订单簿更新
   - `Messages/OrderUpdate.cs` - 订单更新
   - `Messages/AccountUpdate.cs` - 账户更新
   - `Messages/PositionUpdate.cs` - 持仓更新

3. **创建 Converter**
   - `Converters/TickerUpdateConverter.cs`
   - `Converters/TradeUpdateConverter.cs`
   - `Converters/OrderBookUpdateConverter.cs`
   - `Converters/OrderUpdateConverter.cs`

4. **实现消息处理占位符**
   ```csharp
   private void HandleTickerUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<TickerUpdate>(json);
       Log.Trace($"OKXBrokerage: Ticker update: {update.Arg.InstId} - {update.Data[0].Last}");
   }

   private void HandleTradeUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<TradeUpdate>(json);
       Log.Trace($"OKXBrokerage: Trade update: {update.Arg.InstId}");
   }

   private void HandleOrderUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<OrderUpdate>(json);
       Log.Trace($"OKXBrokerage: Order update: {update.Data[0].OrdId}");
       // Phase 11 中实现订单事件处理
   }
   ```

5. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageMessagingTests
   {
       [Test]
       public void RoutePublicMessage_Ticker_CallsHandler()
       {
           var json = "{\"arg\":{\"channel\":\"tickers\",\"instId\":\"BTC-USDT\"},\"data\":[{\"last\":\"50000\"}]}";
           _brokerage.RoutePublicMessage(json);
           // 验证 HandleTickerUpdate 被调用
       }

       [Test]
       public void RoutePrivateMessage_Order_CallsHandler()
       {
           var json = "{\"arg\":{\"channel\":\"orders\"},\"data\":[{\"ordId\":\"123\"}]}";
           _brokerage.RoutePrivateMessage(json);
           // 验证 HandleOrderUpdate 被调用
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 所有消息类型正确识别
- [ ] Ticker 消息路由到 HandleTickerUpdate
- [ ] Trade 消息路由到 HandleTradeUpdate
- [ ] Order 消息路由到 HandleOrderUpdate
- [ ] 日志显示所有传入消息
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Messaging.cs`
- `QuantConnect.OKXBrokerage/Messages/TickerUpdate.cs`
- `QuantConnect.OKXBrokerage/Messages/TradeUpdate.cs`
- `QuantConnect.OKXBrokerage/Messages/OrderBookUpdate.cs`
- `QuantConnect.OKXBrokerage/Messages/OrderUpdate.cs`
- `QuantConnect.OKXBrokerage/Converters/TickerUpdateConverter.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageMessagingTests.cs`

---
## Phase 9: 市场数据订阅

**目标：** 实现 IDataQueueHandler，支持市场数据订阅

**工期：** 3 天

### 任务清单

1. **创建 OKXBrokerage.DataQueueHandler.cs（部分类）**
   - 参考：`Lean.Brokerages.Gate/Core/GateBaseBrokerage.DataQueueHandler.cs`

   ```csharp
   public partial class OKXBrokerage : IDataQueueHandler
   {
       private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
       private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks;

       public void SetJob(LiveNodePacket job)
       {
           // 初始化订阅管理器
       }

       public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
       {
           if (!CanSubscribe(dataConfig.Symbol))
           {
               return Enumerable.Empty<BaseData>().GetEnumerator();
           }

           var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
           _subscriptionManager.Subscribe(dataConfig);

           // 订阅相应的 WebSocket 通道
           SubscribeToWebSocketChannel(dataConfig);

           return enumerator;
       }

       public void Unsubscribe(SubscriptionDataConfig dataConfig)
       {
           _subscriptionManager.Unsubscribe(dataConfig);
           _aggregator.Remove(dataConfig);

           // 取消订阅 WebSocket 通道
           UnsubscribeFromWebSocketChannel(dataConfig);
       }

       private void SubscribeToWebSocketChannel(SubscriptionDataConfig config)
       {
           var symbol = _symbolMapper.GetBrokerageSymbol(config.Symbol);

           if (config.TickType == TickType.Trade)
           {
               SubscribeToTrades(symbol);
           }
           else if (config.TickType == TickType.Quote)
           {
               SubscribeToOrderBook(symbol);
           }
       }
   }
   ```

2. **实现订阅请求方法**
   ```csharp
   private void SubscribeToTrades(string instId)
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new
               {
                   channel = "trades",
                   instId = instId
               }
           }
       };

       _publicWebSocket.Send(JsonConvert.SerializeObject(request));
       Log.Trace($"OKXBrokerage: Subscribed to trades: {instId}");
   }

   private void SubscribeToTickers(string instId)
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new
               {
                   channel = "tickers",
                   instId = instId
               }
           }
       };

       _publicWebSocket.Send(JsonConvert.SerializeObject(request));
       Log.Trace($"OKXBrokerage: Subscribed to tickers: {instId}");
   }

   private void SubscribeToOrderBook(string instId)
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new
               {
                   channel = "books5",  // 5 档订单簿
                   instId = instId
               }
           }
       };

       _publicWebSocket.Send(JsonConvert.SerializeObject(request));
       Log.Trace($"OKXBrokerage: Subscribed to order book: {instId}");
   }

   private void UnsubscribeFromWebSocketChannel(SubscriptionDataConfig config)
   {
       var symbol = _symbolMapper.GetBrokerageSymbol(config.Symbol);

       var request = new
       {
           op = "unsubscribe",
           args = new[]
           {
               new
               {
                   channel = GetChannelName(config.TickType),
                   instId = symbol
               }
           }
       };

       _publicWebSocket.Send(JsonConvert.SerializeObject(request));
       Log.Trace($"OKXBrokerage: Unsubscribed from {symbol}");
   }
   ```

3. **实现 Tick 数据发射**
   ```csharp
   private void HandleTradeUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<TradeUpdate>(json);

       foreach (var trade in update.Data)
       {
           var symbol = _symbolMapper.GetLeanSymbol(update.Arg.InstId, GetSecurityType(update.Arg.InstId));

           var tick = new Tick
           {
               Symbol = symbol,
               Time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(trade.Ts)).UtcDateTime,
               Value = trade.Px,
               Quantity = trade.Sz,
               TickType = TickType.Trade
           };

           _aggregator.Update(tick);
       }
   }

   private void HandleTickerUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<TickerUpdate>(json);

       foreach (var ticker in update.Data)
       {
           var symbol = _symbolMapper.GetLeanSymbol(update.Arg.InstId, GetSecurityType(update.Arg.InstId));

           var tick = new Tick
           {
               Symbol = symbol,
               Time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(ticker.Ts)).UtcDateTime,
               Value = ticker.Last,
               BidPrice = ticker.BidPx,
               AskPrice = ticker.AskPx,
               TickType = TickType.Quote
           };

           _aggregator.Update(tick);
       }
   }
   ```

4. **实现连接限制管理**
   ```csharp
   private readonly Dictionary<string, List<string>> _subscriptionsByConnection = new();
   private const int MaxSubscriptionsPerConnection = 100;

   private void ManageConnectionPool(string channel, string instId)
   {
       // 如果当前连接订阅数 < 100，使用现有连接
       // 否则创建新连接（最多 30 个/通道）

       var connectionKey = GetOrCreateConnection(channel);

       if (!_subscriptionsByConnection.ContainsKey(connectionKey))
       {
           _subscriptionsByConnection[connectionKey] = new List<string>();
       }

       _subscriptionsByConnection[connectionKey].Add($"{channel}:{instId}");
   }
   ```

5. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageDataQueueHandlerTests
   {
       [Test]
       public void Subscribe_TradeData_Success()
       {
           var config = new SubscriptionDataConfig(...);
           var enumerator = _brokerage.Subscribe(config, null);

           Assert.IsNotNull(enumerator);
           // 验证 WebSocket 订阅请求已发送
       }

       [Test]
       public void Subscribe_Multiple_ManagesConnectionPool()
       {
           // 订阅 150 个符号
           for (int i = 0; i < 150; i++)
           {
               var config = new SubscriptionDataConfig(...);
               _brokerage.Subscribe(config, null);
           }

           // 验证创建了 2 个连接（100 + 50）
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 订阅 BTCUSDT，验证 Tick 流
- [ ] 订阅 100+ 符号，验证连接池管理
- [ ] Trade tick 数据正确
- [ ] Quote tick 数据正确
- [ ] 取消订阅工作正常
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.DataQueueHandler.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageDataQueueHandlerTests.cs`

**修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Messaging.cs`

---

## Phase 10: 订单簿管理

**目标：** 实现订单簿维护，处理全量快照和增量更新

**工期：** 2 天

**架构参考：** Gate 的 OrderBookContext + Channel 模式，但使用 OKX 特定的序列管理

### 任务清单

1. **创建 OKXOrderBook.cs**
   - 参考：`Lean.Brokerages.Gate/OrderBookContext.cs` 和 `DefaultOrderBook.cs`
   - 关键差异：
     - OKX 使用 `seqId` 而非 Gate 的 `id` 和 `u`
     - OKX 使用 `action="snapshot"` 或 `action="update"` 而非 Gate 的 `event` 字段
     - OKX 支持 checksum 验证

   ```csharp
   public class OKXOrderBook : DefaultOrderBook
   {
       private long _lastSeqId;
       private readonly object _lock = new object();

       public long LastSeqId => _lastSeqId;

       public OKXOrderBook(Symbol symbol) : base(symbol)
       {
           _lastSeqId = 0;
       }

       public bool ProcessUpdate(OrderBookUpdate update)
       {
           lock (_lock)
           {
               // OKX 序列检测
               if (update.SeqId <= _lastSeqId)
               {
                   Log.Warning($"OKXOrderBook: Received old or duplicate seqId {update.SeqId}, last was {_lastSeqId}");
                   return false;
               }

               // 序列间隙检测
               if (_lastSeqId > 0 && update.SeqId != _lastSeqId + 1)
               {
                   Log.Warning($"OKXOrderBook: Sequence gap detected. Expected {_lastSeqId + 1}, got {update.SeqId}");
                   return false;
               }

               // 处理快照或增量更新
               if (update.Action == "snapshot")
               {
                   ProcessSnapshot(update);
               }
               else if (update.Action == "update")
               {
                   ProcessIncrementalUpdate(update);
               }

               _lastSeqId = update.SeqId;

               // Checksum 验证（可选）
               if (!string.IsNullOrEmpty(update.Checksum))
               {
                   ValidateChecksum(update.Checksum);
               }

               return true;
           }
       }

       private void ProcessSnapshot(OrderBookUpdate update)
       {
           Clear();

           foreach (var bid in update.Bids)
           {
               UpdateBidRow(bid.Price, bid.Size);
           }

           foreach (var ask in update.Asks)
           {
               UpdateAskRow(ask.Price, ask.Size);
           }
       }

       private void ProcessIncrementalUpdate(OrderBookUpdate update)
       {
           foreach (var bid in update.Bids)
           {
               if (bid.Size == 0)
               {
                   RemoveBidRow(bid.Price);
               }
               else
               {
                   UpdateBidRow(bid.Price, bid.Size);
               }
           }

           foreach (var ask in update.Asks)
           {
               if (ask.Size == 0)
               {
                   RemoveAskRow(ask.Price);
               }
               else
               {
                   UpdateAskRow(ask.Price, ask.Size);
               }
           }
       }

       private void ValidateChecksum(string expectedChecksum)
       {
           var calculatedChecksum = CalculateChecksum();
           if (calculatedChecksum != expectedChecksum)
           {
               Log.Error($"OKXOrderBook: Checksum mismatch! Expected {expectedChecksum}, got {calculatedChecksum}");
               // 触发重新初始化
           }
       }

       private string CalculateChecksum()
       {
           // OKX checksum: CRC32 of top 25 bids/asks
           var data = new List<string>();

           var bids = BestBidAskCalculator.BestBidPrices.Take(25);
           var asks = BestBidAskCalculator.BestAskPrices.Take(25);

           foreach (var bid in bids)
           {
               data.Add($"{bid.Key}:{bid.Value.Quantity}");
           }

           foreach (var ask in asks)
           {
               data.Add($"{ask.Key}:{ask.Value.Quantity}");
           }

           var combined = string.Join(":", data);
           return CalculateCrc32(combined).ToString();
       }
   }
   ```

2. **创建 OKXOrderBookContext.cs**
   - 参考：`Lean.Brokerages.Gate/OrderBookContext.cs`

   ```csharp
   public class OKXOrderBookContext
   {
       private readonly ConcurrentDictionary<Symbol, OKXOrderBook> _orderBooks = new();
       private readonly OKXSymbolMapper _symbolMapper;
       private readonly IDataAggregator _aggregator;

       public OKXOrderBookContext(OKXSymbolMapper symbolMapper, IDataAggregator aggregator)
       {
           _symbolMapper = symbolMapper;
           _aggregator = aggregator;
       }

       public void ProcessUpdate(OrderBookUpdate update)
       {
           var symbol = _symbolMapper.GetLeanSymbol(update.InstId, GetSecurityType(update.InstId));
           var orderBook = _orderBooks.GetOrAdd(symbol, s => new OKXOrderBook(s));

           if (!orderBook.ProcessUpdate(update))
           {
               // 序列错误，请求快照重新初始化
               Log.Warning($"OKXOrderBookContext: Reinitializing order book for {symbol}");
               RequestSnapshot(update.InstId);
               return;
           }

           // 发射 BestBidAskUpdated 事件
           EmitQuoteTick(orderBook);
       }

       private void EmitQuoteTick(OKXOrderBook orderBook)
       {
           var bestBid = orderBook.BestBidPrice;
           var bestAsk = orderBook.BestAskPrice;

           if (bestBid == null || bestAsk == null)
               return;

           var tick = new Tick
           {
               Symbol = orderBook.Symbol,
               Time = DateTime.UtcNow,
               BidPrice = bestBid.Price,
               BidSize = bestBid.Size,
               AskPrice = bestAsk.Price,
               AskSize = bestAsk.Size,
               TickType = TickType.Quote
           };

           _aggregator.Update(tick);
       }

       public OKXOrderBook GetOrderBook(Symbol symbol)
       {
           return _orderBooks.TryGetValue(symbol, out var orderBook) ? orderBook : null;
       }
   }
   ```

3. **在 OKXBrokerage.Messaging.cs 中集成订单簿处理**
   ```csharp
   private void HandleOrderBookUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<OrderBookUpdate>(json);

       foreach (var data in update.Data)
       {
           data.InstId = update.Arg.InstId;
           data.Action = update.Action;
           _orderBookContext.ProcessUpdate(data);
       }
   }
   ```

4. **修改订阅逻辑以使用深度订单簿**
   ```csharp
   private void SubscribeToOrderBook(string instId)
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new
               {
                   channel = "books",  // 400 档全量订单簿
                   instId = instId
               }
           }
       };

       _publicWebSocket.Send(JsonConvert.SerializeObject(request));
       Log.Trace($"OKXBrokerage: Subscribed to order book (400 depth): {instId}");
   }
   ```

5. **实现快照请求机制**
   ```csharp
   private void RequestSnapshot(string instId)
   {
       // OKX 需要通过 REST API 获取快照
       var snapshot = _apiClient.GetOrderBookSnapshot(instId, depth: 400);

       var update = new OrderBookUpdate
       {
           InstId = instId,
           Action = "snapshot",
           SeqId = snapshot.SeqId,
           Bids = snapshot.Bids,
           Asks = snapshot.Asks
       };

       _orderBookContext.ProcessUpdate(update);
   }
   ```

6. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXOrderBookTests
   {
       [Test]
       public void ProcessSnapshot_CreatesOrderBook()
       {
           var update = CreateSnapshotUpdate();
           var result = _orderBook.ProcessUpdate(update);

           Assert.IsTrue(result);
           Assert.AreEqual(update.SeqId, _orderBook.LastSeqId);
           Assert.IsNotNull(_orderBook.BestBidPrice);
           Assert.IsNotNull(_orderBook.BestAskPrice);
       }

       [Test]
       public void ProcessUpdate_WithSequenceGap_ReturnsFalse()
       {
           _orderBook.ProcessUpdate(CreateUpdate(seqId: 100));
           var result = _orderBook.ProcessUpdate(CreateUpdate(seqId: 102)); // Gap!

           Assert.IsFalse(result);
       }

       [Test]
       public void ProcessUpdate_WithInvalidChecksum_TriggersReinitialize()
       {
           var update = CreateUpdateWithBadChecksum();
           _orderBook.ProcessUpdate(update);

           // 验证重新初始化请求
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 订单簿正确处理快照
- [ ] 订单簿正确处理增量更新
- [ ] 序列间隙检测触发重新初始化
- [ ] Checksum 验证工作正常
- [ ] BestBidAsk 实时更新
- [ ] 单元测试通过
- [ ] 压力测试：100 updates/sec，订单簿保持同步

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXOrderBook.cs`
- `QuantConnect.OKXBrokerage/OKXOrderBookContext.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXOrderBookTests.cs`

**修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Messaging.cs`
- `QuantConnect.OKXBrokerage/OKXBrokerage.DataQueueHandler.cs`

---

## Phase 11: 订单更新

**目标：** 实现 WebSocket 订单事件处理，发送 OrderEvent 到算法

**工期：** 2-3 天

### 任务清单

1. **订阅私有通道订单流**
   ```csharp
   private void SubscribeToOrders()
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new
               {
                   channel = "orders",
                   instType = "ANY"  // 订阅所有类型
               }
           }
       };

       _privateWebSocket.Send(JsonConvert.SerializeObject(request));
       Log.Trace("OKXBrokerage: Subscribed to orders channel");
   }
   ```

2. **在 OKXBrokerage.Messaging.cs 中实现 HandleOrderUpdate**
   ```csharp
   private void HandleOrderUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<OrderUpdate>(json);

       foreach (var data in update.Data)
       {
           ProcessOrderEvent(data);
       }
   }

   private void ProcessOrderEvent(OrderData orderData)
   {
       var symbol = _symbolMapper.GetLeanSymbol(orderData.InstId, GetSecurityType(orderData.InstId));

       // 查找 LEAN Order
       var order = GetOrderById(orderData.ClOrdId);
       if (order == null)
       {
           Log.Warning($"OKXBrokerage: Order not found: {orderData.ClOrdId}");
           return;
       }

       // 转换状态
       var status = ConvertOrderStatus(orderData.State);
       var fillQuantity = decimal.Parse(orderData.AccFillSz);
       var remainingQuantity = Math.Abs(order.Quantity) - fillQuantity;

       var orderEvent = new OrderEvent(
           order.Id,
           symbol,
           DateTime.UtcNow,
           status,
           order.Direction,
           decimal.Parse(orderData.AvgPx),
           decimal.Parse(orderData.FillSz),  // 本次成交数量
           OrderFee.Zero,
           $"OKX Order Event: {orderData.State}"
       )
       {
           BrokerId = new List<string> { orderData.OrdId }
       };

       OnOrderEvent(orderEvent);

       // 如果订单完全成交或取消，记录最终状态
       if (status == OrderStatus.Filled || status == OrderStatus.Canceled)
       {
           Log.Trace($"OKXBrokerage: Order {order.Id} {status}: {fillQuantity}/{Math.Abs(order.Quantity)}");
       }
   }

   private OrderStatus ConvertOrderStatus(string okxState)
   {
       return okxState switch
       {
           "live" => OrderStatus.Submitted,
           "partially_filled" => OrderStatus.PartiallyFilled,
           "filled" => OrderStatus.Filled,
           "canceled" => OrderStatus.Canceled,
           _ => OrderStatus.None
       };
   }
   ```

3. **实现订单 ID 映射管理**
   ```csharp
   private readonly ConcurrentDictionary<int, string> _orderIdMap = new();  // LEAN ID → Brokerage ID
   private readonly ConcurrentDictionary<string, int> _reverseOrderIdMap = new();  // Brokerage ID → LEAN ID

   private void OnOrderIdChanged(BrokerageOrderIdChangedEvent e)
   {
       _orderIdMap[e.OrderId] = e.BrokerId.First();
       _reverseOrderIdMap[e.BrokerId.First()] = e.OrderId;
   }

   private string GetBrokerageOrderId(Order order)
   {
       return _orderIdMap.TryGetValue(order.Id, out var brokerId) ? brokerId : null;
   }

   private Order GetOrderById(string clOrdId)
   {
       if (!int.TryParse(clOrdId, out var orderId))
           return null;

       return _algorithm?.Transactions.GetOrderById(orderId);
   }
   ```

4. **实现账户和持仓更新**
   ```csharp
   private void HandleAccountUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<AccountUpdate>(json);

       foreach (var data in update.Data)
       {
           foreach (var detail in data.Details)
           {
               var cashAmount = new CashAmount(
                   decimal.Parse(detail.AvailBal),
                   detail.Ccy
               );

               OnAccountChanged(new AccountEvent(
                   detail.Ccy,
                   cashAmount.Amount
               ));
           }
       }
   }

   private void HandlePositionUpdate(string json)
   {
       var update = JsonConvert.DeserializeObject<PositionUpdate>(json);

       foreach (var data in update.Data)
       {
           var symbol = _symbolMapper.GetLeanSymbol(data.InstId, SecurityType.CryptoFuture);
           var quantity = decimal.Parse(data.Pos);

           OnPositionChanged(new PositionChangedEvent(
               symbol,
               quantity
           ));
       }
   }
   ```

5. **连接后自动订阅**
   ```csharp
   public override void Connect()
   {
       base.Connect();

       // ... WebSocket 连接 ...

       // 订阅私有通道
       SubscribeToOrders();
       SubscribeToAccount();
       SubscribeToPositions();
   }

   private void SubscribeToAccount()
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new { channel = "account" }
           }
       };

       _privateWebSocket.Send(JsonConvert.SerializeObject(request));
   }

   private void SubscribeToPositions()
   {
       var request = new
       {
           op = "subscribe",
           args = new[]
           {
               new
               {
                   channel = "positions",
                   instType = "ANY"
               }
           }
       };

       _privateWebSocket.Send(JsonConvert.SerializeObject(request));
   }
   ```

6. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageOrderEventsTests
   {
       [Test]
       public void HandleOrderUpdate_PartiallyFilled_EmitsCorrectEvent()
       {
           var receivedEvent = false;
           _brokerage.OrderStatusChanged += (sender, e) =>
           {
               Assert.AreEqual(OrderStatus.PartiallyFilled, e.Status);
               receivedEvent = true;
           };

           var json = CreatePartiallyFilledOrderJson();
           _brokerage.HandleOrderUpdate(json);

           Assert.IsTrue(receivedEvent);
       }

       [Test]
       public void HandleOrderUpdate_Filled_EmitsCorrectEvent()
       {
           var receivedEvent = false;
           _brokerage.OrderStatusChanged += (sender, e) =>
           {
               Assert.AreEqual(OrderStatus.Filled, e.Status);
               receivedEvent = true;
           };

           var json = CreateFilledOrderJson();
           _brokerage.HandleOrderUpdate(json);

           Assert.IsTrue(receivedEvent);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] PlaceOrder 后收到 Submitted 事件
- [ ] 部分成交时收到 PartiallyFilled 事件
- [ ] 完全成交时收到 Filled 事件
- [ ] CancelOrder 后收到 Canceled 事件
- [ ] 账户余额更新正确传播
- [ ] 持仓更新正确传播
- [ ] 单元测试通过
- [ ] 完整订单生命周期测试通过

### 关键文件

**创建/修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Messaging.cs` (扩展)
- `QuantConnect.OKXBrokerage/Messages/AccountUpdate.cs`
- `QuantConnect.OKXBrokerage/Messages/PositionUpdate.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageOrderEventsTests.cs`

---

## Phase 12: 重连逻辑

**目标：** 实现断线重连、指数退避、订阅恢复

**工期：** 2 天

### 任务清单

1. **创建 OKXBrokerage.Reconnection.cs（部分类）**
   - 参考：`Lean.Brokerages.Gate/Core/GateBaseBrokerage.Reconnection.cs`

   ```csharp
   public partial class OKXBrokerage
   {
       private int _reconnectionAttempts = 0;
       private const int MaxReconnectionAttempts = 10;
       private readonly TimeSpan[] _backoffDelays = new[]
       {
           TimeSpan.FromSeconds(1),
           TimeSpan.FromSeconds(2),
           TimeSpan.FromSeconds(5),
           TimeSpan.FromSeconds(10),
           TimeSpan.FromSeconds(30),
           TimeSpan.FromMinutes(1),
           TimeSpan.FromMinutes(2),
           TimeSpan.FromMinutes(5),
           TimeSpan.FromMinutes(10),
           TimeSpan.FromMinutes(30)
       };

       private void OnWebSocketClosed(object sender, WebSocketCloseData e)
       {
           Log.Warning($"OKXBrokerage: WebSocket closed: {e.Reason}");
           ScheduleReconnection();
       }

       private void ScheduleReconnection()
       {
           if (_reconnectionAttempts >= MaxReconnectionAttempts)
           {
               Log.Error("OKXBrokerage: Max reconnection attempts reached");
               OnMessage(new BrokerageMessageEvent(
                   BrokerageMessageType.Error,
                   -1,
                   "Max reconnection attempts reached"
               ));
               return;
           }

           var delay = _backoffDelays[Math.Min(_reconnectionAttempts, _backoffDelays.Length - 1)];
           Log.Trace($"OKXBrokerage: Reconnecting in {delay.TotalSeconds} seconds (attempt {_reconnectionAttempts + 1})");

           Task.Delay(delay).ContinueWith(_ => Reconnect());
       }

       private void Reconnect()
       {
           try
           {
               _reconnectionAttempts++;

               Log.Trace($"OKXBrokerage: Attempting reconnection ({_reconnectionAttempts}/{MaxReconnectionAttempts})");

               // 断开现有连接
               Disconnect();

               // 重新连接
               Connect();

               // 恢复订阅
               RestoreSubscriptions();

               // 重置重连计数器
               _reconnectionAttempts = 0;
               Log.Trace("OKXBrokerage: Reconnection successful");

               OnMessage(new BrokerageMessageEvent(
                   BrokerageMessageType.Reconnect,
                   -1,
                   "Reconnected successfully"
               ));
           }
           catch (Exception ex)
           {
               Log.Error($"OKXBrokerage: Reconnection failed: {ex.Message}");
               ScheduleReconnection();
           }
       }

       private void RestoreSubscriptions()
       {
           Log.Trace("OKXBrokerage: Restoring subscriptions");

           // 恢复市场数据订阅
           var subscriptions = _subscriptionManager.GetSubscribedSymbols();
           foreach (var symbol in subscriptions)
           {
               var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
               SubscribeToTrades(brokerageSymbol);
               SubscribeToOrderBook(brokerageSymbol);
           }

           // 恢复私有通道订阅
           SubscribeToOrders();
           SubscribeToAccount();
           SubscribeToPositions();

           Log.Trace($"OKXBrokerage: Restored {subscriptions.Count()} subscriptions");
       }
   }
   ```

2. **增强保活超时处理**
   ```csharp
   private void OnKeepAliveTimerElapsed(object sender, ElapsedEventArgs e)
   {
       var timeSinceLastMessage = DateTime.UtcNow - _lastMessageTime;

       if (timeSinceLastMessage.TotalSeconds >= PongTimeoutSeconds)
       {
           Log.Warning($"OKXBrokerage: Pong timeout ({timeSinceLastMessage.TotalSeconds}s), triggering reconnection");
           _keepAliveTimer.Stop();
           ScheduleReconnection();
           return;
       }

       if (timeSinceLastMessage.TotalSeconds >= KeepAliveIntervalSeconds)
       {
           SendPing();
       }
   }
   ```

3. **订阅管理器追踪**
   ```csharp
   private class SubscriptionTracker
   {
       private readonly ConcurrentDictionary<Symbol, SubscriptionInfo> _subscriptions = new();

       public void Add(Symbol symbol, TickType tickType)
       {
           _subscriptions[symbol] = new SubscriptionInfo
           {
               Symbol = symbol,
               TickType = tickType,
               SubscribedAt = DateTime.UtcNow
           };
       }

       public void Remove(Symbol symbol)
       {
           _subscriptions.TryRemove(symbol, out _);
       }

       public IEnumerable<Symbol> GetSubscribedSymbols()
       {
           return _subscriptions.Keys;
       }

       public SubscriptionInfo GetInfo(Symbol symbol)
       {
           return _subscriptions.TryGetValue(symbol, out var info) ? info : null;
       }
   }

   private class SubscriptionInfo
   {
       public Symbol Symbol { get; set; }
       public TickType TickType { get; set; }
       public DateTime SubscribedAt { get; set; }
   }
   ```

4. **实现连接健康检查**
   ```csharp
   public bool IsHealthy()
   {
       if (!IsConnected)
           return false;

       var timeSinceLastMessage = DateTime.UtcNow - _lastMessageTime;
       if (timeSinceLastMessage.TotalSeconds > 60)
       {
           Log.Warning($"OKXBrokerage: No messages received for {timeSinceLastMessage.TotalSeconds}s");
           return false;
       }

       return true;
   }
   ```

5. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageReconnectionTests
   {
       [Test]
       public void Reconnect_AfterDisconnect_Success()
       {
           _brokerage.Connect();
           Assert.IsTrue(_brokerage.IsConnected);

           // 模拟断线
           _brokerage.SimulateDisconnection();

           // 等待重连
           Thread.Sleep(3000);

           Assert.IsTrue(_brokerage.IsConnected);
       }

       [Test]
       public void Reconnect_RestoresSubscriptions()
       {
           var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
           _brokerage.Subscribe(CreateSubscriptionConfig(symbol), null);

           // 模拟断线重连
           _brokerage.SimulateDisconnection();
           Thread.Sleep(3000);

           // 验证订阅已恢复
           var subscriptions = _brokerage.GetSubscribedSymbols();
           Assert.Contains(symbol, subscriptions.ToList());
       }

       [Test]
       public void ExponentialBackoff_IncreasesDelay()
       {
           var delays = new List<TimeSpan>();

           for (int i = 0; i < 5; i++)
           {
               delays.Add(_brokerage.GetBackoffDelay(i));
           }

           // 验证延迟递增
           for (int i = 1; i < delays.Count; i++)
           {
               Assert.Greater(delays[i], delays[i - 1]);
           }
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] 断线后自动重连
- [ ] 指数退避正确实现
- [ ] 重连后订阅自动恢复
- [ ] Pong 超时触发重连
- [ ] 最大重连次数限制生效
- [ ] IsHealthy() 正确检测连接状态
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.Reconnection.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageReconnectionTests.cs`

**修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.cs`

---

## Phase 13: Brokerage Factory

**目标：** 创建 OKXBrokerageFactory，提供单一工厂类

**工期：** 0.5-1 天

**简化要点：** OKX 统一 API → 只需一个 Factory

### 任务清单

1. **创建 OKXBrokerageFactory.cs**
   ```csharp
   public class OKXBrokerageFactory : BrokerageFactory
   {
       public override Dictionary<string, string> BrokerageData => new()
       {
           { "okx-api-key", "API Key" },
           { "okx-api-secret", "API Secret" },
           { "okx-passphrase", "Passphrase" },
           { "okx-account-mode", "Account Mode (optional)" },
           { "okx-environment", "Environment (production/demo)" }
       };

       public OKXBrokerageFactory() : base(typeof(OKXBrokerage))
       {
       }

       public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
       {
           var apiKey = Read<string>(job.BrokerageData, "okx-api-key");
           var apiSecret = Read<string>(job.BrokerageData, "okx-api-secret");
           var passphrase = Read<string>(job.BrokerageData, "okx-passphrase");
           var environment = Read<string>(job.BrokerageData, "okx-environment", "production");

           var aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
               Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"),
               forceTypeNameOnExisting: false);

           return new OKXBrokerage(
               apiKey,
               apiSecret,
               passphrase,
               environment,
               algorithm,
               aggregator
           );
       }

       public override void Dispose()
       {
           // Cleanup if needed
       }
   }
   ```

2. **创建 OKXBrokerageModel.cs**
   ```csharp
   public class OKXBrokerageModel : DefaultBrokerageModel
   {
       public OKXBrokerageModel(AccountType accountType = AccountType.Cash)
           : base(accountType)
       {
       }

       public override string Name => "OKX";

       public override decimal GetLeverage(Security security)
       {
           if (security.Type == SecurityType.CryptoFuture)
           {
               return 10m;  // 默认 10x 杠杆
           }
           return 1m;
       }

       public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
       {
           message = null;

           // 检查支持的证券类型
           if (security.Type != SecurityType.Crypto && security.Type != SecurityType.CryptoFuture)
           {
               message = new BrokerageMessageEvent(
                   BrokerageMessageType.Warning,
                   "NotSupported",
                   $"Security type {security.Type} not supported by OKX"
               );
               return false;
           }

           // 检查支持的订单类型
           if (order.Type != OrderType.Market &&
               order.Type != OrderType.Limit &&
               order.Type != OrderType.StopLimit)
           {
               message = new BrokerageMessageEvent(
                   BrokerageMessageType.Warning,
                   "NotSupported",
                   $"Order type {order.Type} not supported by OKX"
               );
               return false;
           }

           return true;
       }

       public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request,
           out BrokerageMessageEvent message)
       {
           message = null;

           // OKX 只能修改价格和数量
           if (request.Quantity.HasValue || request.LimitPrice.HasValue)
           {
               return true;
           }

           message = new BrokerageMessageEvent(
               BrokerageMessageType.Warning,
               "NotSupported",
               "OKX only supports updating quantity and limit price"
           );
           return false;
       }
   }
   ```

3. **创建 OKXFeeModel.cs**
   ```csharp
   public class OKXFeeModel : FeeModel
   {
       private const decimal MakerFee = 0.0008m;  // 0.08%
       private const decimal TakerFee = 0.001m;   // 0.10%

       public override OrderFee GetOrderFee(OrderFeeParameters parameters)
       {
           var security = parameters.Security;
           var order = parameters.Order;

           decimal fee;

           // 判断 Maker/Taker
           if (order.Type == OrderType.Limit)
           {
               fee = MakerFee;
           }
           else
           {
               fee = TakerFee;
           }

           var totalFee = fee * Math.Abs(order.Quantity) * order.Price;

           return new OrderFee(new CashAmount(totalFee, security.QuoteCurrency.Symbol));
       }
   }
   ```

4. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageFactoryTests
   {
       [Test]
       public void CreateBrokerage_WithValidConfig_Success()
       {
           var job = new LiveNodePacket
           {
               BrokerageData = new Dictionary<string, string>
               {
                   { "okx-api-key", "test-key" },
                   { "okx-api-secret", "test-secret" },
                   { "okx-passphrase", "test-pass" }
               }
           };

           var factory = new OKXBrokerageFactory();
           var brokerage = factory.CreateBrokerage(job, null);

           Assert.IsNotNull(brokerage);
           Assert.IsInstanceOf<OKXBrokerage>(brokerage);
       }
   }

   [TestFixture]
   public class OKXBrokerageModelTests
   {
       [Test]
       public void CanSubmitOrder_Crypto_ReturnsTrue()
       {
           var model = new OKXBrokerageModel();
           var security = CreateCryptoSecurity();
           var order = new MarketOrder(...);

           var result = model.CanSubmitOrder(security, order, out var message);

           Assert.IsTrue(result);
           Assert.IsNull(message);
       }

       [Test]
       public void CanSubmitOrder_UnsupportedType_ReturnsFalse()
       {
           var model = new OKXBrokerageModel();
           var security = CreateEquitySecurity();
           var order = new MarketOrder(...);

           var result = model.CanSubmitOrder(security, order, out var message);

           Assert.IsFalse(result);
           Assert.IsNotNull(message);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] Factory 正确创建 Brokerage 实例
- [ ] BrokerageModel 正确验证订单
- [ ] FeeModel 正确计算手续费
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXBrokerageFactory.cs`
- `QuantConnect.OKXBrokerage/OKXBrokerageModel.cs`
- `QuantConnect.OKXBrokerage/OKXFeeModel.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXBrokerageFactoryTests.cs`

---

## Phase 14: 高级功能

**目标：** 实现套利辅助工具（OKXPairMatcher, OKXRiskLimitHelper）

**工期：** 3-4 天

### 任务清单

1. **创建 OKXPairMatcher.cs**
   - 参考：`Lean/Algorithm/Alphas/ArbitrageAlphaModel.GatePairMatcher.cs`

   ```csharp
   public class OKXPairMatcher
   {
       private readonly OKXRestApiClient _apiClient;
       private readonly Dictionary<string, InstrumentInfo> _instrumentCache = new();
       private DateTime _lastCacheUpdate = DateTime.MinValue;
       private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(1);

       public OKXPairMatcher(OKXRestApiClient apiClient)
       {
           _apiClient = apiClient;
       }

       public List<ArbitragePair> FindArbitragePairs(string baseCurrency = "BTC", string quoteCurrency = "USDT")
       {
           RefreshCacheIfNeeded();

           var pairs = new List<ArbitragePair>();

           var spotSymbol = $"{baseCurrency}-{quoteCurrency}";
           var swapSymbol = $"{baseCurrency}-{quoteCurrency}-SWAP";

           if (_instrumentCache.ContainsKey(spotSymbol) && _instrumentCache.ContainsKey(swapSymbol))
           {
               var spotInfo = _instrumentCache[spotSymbol];
               var swapInfo = _instrumentCache[swapSymbol];

               pairs.Add(new ArbitragePair
               {
                   SpotSymbol = spotSymbol,
                   FutureSymbol = swapSymbol,
                   MinTradeSize = Math.Max(spotInfo.MinSize, swapInfo.MinSize),
                   LotSize = Math.Max(spotInfo.LotSize, swapInfo.LotSize),
                   TickSize = Math.Max(spotInfo.TickSize, swapInfo.TickSize)
               });
           }

           return pairs;
       }

       public decimal GetMinimumTradeValue(string instId)
       {
           RefreshCacheIfNeeded();

           if (_instrumentCache.TryGetValue(instId, out var info))
           {
               return info.MinSize * info.TickSize;
           }

           return 0;
       }

       private void RefreshCacheIfNeeded()
       {
           if (DateTime.UtcNow - _lastCacheUpdate > _cacheExpiry)
           {
               RefreshCache();
           }
       }

       private void RefreshCache()
       {
           _instrumentCache.Clear();

           // 获取现货工具
           var spotInstruments = _apiClient.GetInstruments("SPOT");
           foreach (var inst in spotInstruments)
           {
               _instrumentCache[inst.InstId] = new InstrumentInfo
               {
                   InstId = inst.InstId,
                   MinSize = decimal.Parse(inst.MinSz),
                   LotSize = decimal.Parse(inst.LotSz),
                   TickSize = decimal.Parse(inst.TickSz)
               };
           }

           // 获取永续工具
           var swapInstruments = _apiClient.GetInstruments("SWAP");
           foreach (var inst in swapInstruments)
           {
               _instrumentCache[inst.InstId] = new InstrumentInfo
               {
                   InstId = inst.InstId,
                   MinSize = decimal.Parse(inst.MinSz),
                   LotSize = decimal.Parse(inst.LotSz),
                   TickSize = decimal.Parse(inst.TickSz)
               };
           }

           _lastCacheUpdate = DateTime.UtcNow;
       }
   }

   public class ArbitragePair
   {
       public string SpotSymbol { get; set; }
       public string FutureSymbol { get; set; }
       public decimal MinTradeSize { get; set; }
       public decimal LotSize { get; set; }
       public decimal TickSize { get; set; }
   }

   public class InstrumentInfo
   {
       public string InstId { get; set; }
       public decimal MinSize { get; set; }
       public decimal LotSize { get; set; }
       public decimal TickSize { get; set; }
   }
   ```

2. **创建 OKXRiskLimitHelper.cs**
   ```csharp
   public class OKXRiskLimitHelper
   {
       private readonly OKXRestApiClient _apiClient;
       private readonly Dictionary<string, RiskLimitInfo> _riskLimitCache = new();
       private DateTime _lastCacheUpdate = DateTime.MinValue;
       private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);

       public OKXRiskLimitHelper(OKXRestApiClient apiClient)
       {
           _apiClient = apiClient;
       }

       public decimal GetMaxPositionSize(string instId, decimal accountEquity)
       {
           RefreshCacheIfNeeded();

           if (!_riskLimitCache.TryGetValue(instId, out var riskLimit))
           {
               return 0;
           }

           // 基于账户权益和风险限额计算最大仓位
           var maxPositionValue = accountEquity * 0.5m;  // 50% of equity
           var maxPosition = maxPositionValue / riskLimit.MinimumMargin;

           return Math.Min(maxPosition, riskLimit.MaxPosition);
       }

       public bool ValidateOrderSize(string instId, decimal orderSize)
       {
           RefreshCacheIfNeeded();

           if (!_riskLimitCache.TryGetValue(instId, out var riskLimit))
           {
               return false;
           }

           return orderSize >= riskLimit.MinSize && orderSize <= riskLimit.MaxPosition;
       }

       private void RefreshCacheIfNeeded()
       {
           if (DateTime.UtcNow - _lastCacheUpdate > _cacheExpiry)
           {
               RefreshCache();
           }
       }

       private void RefreshCache()
       {
           _riskLimitCache.Clear();

           // 获取所有永续合约的风险限额
           var instruments = _apiClient.GetInstruments("SWAP");

           foreach (var inst in instruments)
           {
               _riskLimitCache[inst.InstId] = new RiskLimitInfo
               {
                   InstId = inst.InstId,
                   MinSize = decimal.Parse(inst.MinSz),
                   MaxPosition = decimal.Parse(inst.MaxLmtSz ?? "1000000"),
                   MinimumMargin = decimal.Parse(inst.MinSz) * decimal.Parse(inst.TickSz)
               };
           }

           _lastCacheUpdate = DateTime.UtcNow;
       }
   }

   public class RiskLimitInfo
   {
       public string InstId { get; set; }
       public decimal MinSize { get; set; }
       public decimal MaxPosition { get; set; }
       public decimal MinimumMargin { get; set; }
   }
   ```

3. **集成到 OKXBrokerage**
   ```csharp
   public partial class OKXBrokerage
   {
       private OKXPairMatcher _pairMatcher;
       private OKXRiskLimitHelper _riskLimitHelper;

       public OKXPairMatcher PairMatcher => _pairMatcher ?? (_pairMatcher = new OKXPairMatcher(_apiClient));
       public OKXRiskLimitHelper RiskLimitHelper => _riskLimitHelper ?? (_riskLimitHelper = new OKXRiskLimitHelper(_apiClient));

       public override bool PlaceOrder(Order order)
       {
           // 风险限额检查
           var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);
           if (!_riskLimitHelper.ValidateOrderSize(brokerageSymbol, Math.Abs(order.Quantity)))
           {
               OnMessage(new BrokerageMessageEvent(
                   BrokerageMessageType.Warning,
                   "InvalidOrderSize",
                   $"Order size {order.Quantity} exceeds risk limits for {brokerageSymbol}"
               ));
               return false;
           }

           // ... 继续下单逻辑 ...
       }
   }
   ```

4. **编写单元测试**
   ```csharp
   [TestFixture]
   public class OKXPairMatcherTests
   {
       [Test]
       public void FindArbitragePairs_BTCUSDT_ReturnsSpotSwapPair()
       {
           var pairs = _pairMatcher.FindArbitragePairs("BTC", "USDT");

           Assert.IsNotEmpty(pairs);
           Assert.AreEqual("BTC-USDT", pairs[0].SpotSymbol);
           Assert.AreEqual("BTC-USDT-SWAP", pairs[0].FutureSymbol);
       }

       [Test]
       public void GetMinimumTradeValue_ValidSymbol_ReturnsCorrectValue()
       {
           var minValue = _pairMatcher.GetMinimumTradeValue("BTC-USDT");
           Assert.Greater(minValue, 0);
       }

       [Test]
       public void CacheExpiry_After24Hours_RefreshesData()
       {
           _pairMatcher.FindArbitragePairs("BTC", "USDT");
           var firstCacheTime = _pairMatcher.GetLastCacheUpdate();

           // 模拟 25 小时后
           _pairMatcher.SetTime(DateTime.UtcNow.AddHours(25));
           _pairMatcher.FindArbitragePairs("BTC", "USDT");
           var secondCacheTime = _pairMatcher.GetLastCacheUpdate();

           Assert.Greater(secondCacheTime, firstCacheTime);
       }
   }

   [TestFixture]
   public class OKXRiskLimitHelperTests
   {
       [Test]
       public void GetMaxPositionSize_WithAccountEquity_ReturnsValidLimit()
       {
           var maxSize = _riskLimitHelper.GetMaxPositionSize("BTC-USDT-SWAP", 10000m);
           Assert.Greater(maxSize, 0);
       }

       [Test]
       public void ValidateOrderSize_WithinLimits_ReturnsTrue()
       {
           var result = _riskLimitHelper.ValidateOrderSize("BTC-USDT-SWAP", 0.1m);
           Assert.IsTrue(result);
       }

       [Test]
       public void ValidateOrderSize_ExceedsLimits_ReturnsFalse()
       {
           var result = _riskLimitHelper.ValidateOrderSize("BTC-USDT-SWAP", 1000000m);
           Assert.IsFalse(result);
       }
   }
   ```

### 验证标准

- [ ] 编译通过
- [ ] PairMatcher 正确识别套利对
- [ ] PairMatcher 缓存机制工作（1小时过期）
- [ ] RiskLimitHelper 正确计算最大仓位
- [ ] RiskLimitHelper 缓存机制工作（24小时过期）
- [ ] 订单风险检查集成到 PlaceOrder
- [ ] 单元测试通过

### 关键文件

**创建：**
- `QuantConnect.OKXBrokerage/OKXPairMatcher.cs`
- `QuantConnect.OKXBrokerage/OKXRiskLimitHelper.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXPairMatcherTests.cs`
- `QuantConnect.OKXBrokerage.Tests/OKXRiskLimitHelperTests.cs`

**修改：**
- `QuantConnect.OKXBrokerage/OKXBrokerage.cs`

---

## Phase 15: 文档

**目标：** 创建用户文档、开发者文档、变更日志

**工期：** 2 天

### 任务清单

1. **创建 README.md**
   ```markdown
   # Lean.Brokerages.OKX

   OKX brokerage plugin for QuantConnect LEAN Algorithmic Trading Engine.

   ## Features

   - REST API integration for account management and order execution
   - WebSocket streaming for real-time market data and order updates
   - Support for Spot and Perpetual Swap trading
   - Order book management with 400-level depth
   - Automatic reconnection with exponential backoff
   - Arbitrage helper tools (PairMatcher, RiskLimitHelper)

   ## Installation

   ```bash
   dotnet add package QuantConnect.OKXBrokerage
   ```

   ## Configuration

   Add the following to your `config.json`:

   ```json
   {
     "brokerage": "OKX",
     "okx-api-key": "your-api-key",
     "okx-api-secret": "your-api-secret",
     "okx-passphrase": "your-passphrase",
     "okx-environment": "production"
   }
   ```

   ## Usage

   ### Basic Algorithm

   ```csharp
   public class MyAlgorithm : QCAlgorithm
   {
       public override void Initialize()
       {
           SetBrokerage(BrokerageName.OKX);
           SetAccountCurrency("USDT");

           var btc = AddCrypto("BTCUSDT", Resolution.Minute);

           SetBenchmark(btc.Symbol);
       }

       public override void OnData(Slice data)
       {
           if (!Portfolio.Invested)
           {
               MarketOrder("BTCUSDT", 0.01m);
           }
       }
   }
   ```

   ### Spot-Future Arbitrage

   ```csharp
   var spotSymbol = AddCrypto("BTCUSDT").Symbol;
   var futureSymbol = AddCryptoFuture("BTCUSDT").Symbol;

   var pairs = ((OKXBrokerage)Broker).PairMatcher.FindArbitragePairs("BTC", "USDT");
   ```

   ## Supported Features

   | Feature | Supported |
   |---------|-----------|
   | Spot Trading | ✅ |
   | Perpetual Swaps | ✅ |
   | Futures | ✅ |
   | Options | ❌ |
   | Market Orders | ✅ |
   | Limit Orders | ✅ |
   | Stop-Limit Orders | ✅ |
   | Historical Data | ✅ (Minute, Hour, Daily) |
   | Live Data | ✅ |
   | Order Updates | ✅ |
   | Account Updates | ✅ |

   ## Rate Limits

   - Order API: 1000 requests / 2 seconds
   - Account API: 10 requests / 2 seconds
   - Market Data API: 20 requests / 2 seconds
   - WebSocket: 100 subscriptions per connection (max 30 connections)

   ## Testing

   ```bash
   dotnet test QuantConnect.OKXBrokerage.Tests
   ```

   ## License

   Apache License 2.0

   ## Support

   - Documentation: [docs/](docs/)
   - Issues: [GitHub Issues](https://github.com/QuantConnect/Lean.Brokerages.OKX/issues)
   ```

2. **创建 CLAUDE.md**
   ```markdown
   # CLAUDE.md

   Development guide for Claude Code when working with Lean.Brokerages.OKX.

   ## Project Structure

   ```
   Lean.Brokerages.OKX/
   ├── QuantConnect.OKXBrokerage/
   │   ├── OKXBrokerage.cs              # Main brokerage class
   │   ├── OKXBrokerage.*.cs            # Partial classes
   │   ├── OKXRestApiClient.cs          # REST API client
   │   ├── OKXSymbolMapper.cs           # Symbol conversion
   │   ├── OKXOrderBook.cs              # Order book management
   │   ├── Messages/                    # API message models
   │   └── Converters/                  # JSON converters
   └── QuantConnect.OKXBrokerage.Tests/
   ```

   ## Build Commands

   ```bash
   # Build
   dotnet build QuantConnect.OKXBrokerage.sln

   # Test
   dotnet test QuantConnect.OKXBrokerage.Tests

   # Run integration tests
   dotnet test --filter "Category=Integration"
   ```

   ## Key Differences from Gate.io

   | Feature | Gate.io | OKX |
   |---------|---------|-----|
   | REST API | Split (Spot/Futures) | Unified |
   | Brokerage Classes | Multiple | Single |
   | Factory Classes | Multiple | Single |
   | Order Book SeqId | id + u | seqId |
   | Order Book Action | event field | action field |
   | Account Modes | Fixed | Runtime detection |

   ## OKX-Specific Considerations

   1. **Unified Account**: OKX uses unified account model, detected at runtime
   2. **seqId Tracking**: Order book uses seqId for sequence validation
   3. **Checksum**: OKX provides CRC32 checksum for order book validation
   4. **Keep-Alive**: Must ping every 20 seconds, 30 second timeout
   5. **Rate Limits**: Stricter than Gate, use RateGate

   ## Testing Workflow

   1. Build project
   2. Run unit tests
   3. Run integration tests (requires API credentials)
   4. Test on demo environment before production
   ```

3. **创建 CHANGELOG.md**
   ```markdown
   # Changelog

   All notable changes to this project will be documented in this file.

   ## [1.0.0] - 2026-01-XX

   ### Added
   - Initial release
   - REST API integration
   - WebSocket streaming
   - Spot and Perpetual Swap support
   - Order book management with 400-level depth
   - Automatic reconnection
   - PairMatcher for arbitrage
   - RiskLimitHelper for position management
   - Comprehensive unit tests

   ### Known Issues
   - Options trading not supported
   - Futures (delivery) partially implemented
   ```

4. **创建 docs/API.md**
   ```markdown
   # API Reference

   ## OKXBrokerage

   Main brokerage class.

   ### Constructor

   ```csharp
   public OKXBrokerage(
       string apiKey,
       string apiSecret,
       string passphrase,
       string environment,
       IAlgorithm algorithm,
       IDataAggregator aggregator)
   ```

   ### Properties

   - `PairMatcher`: Access to OKXPairMatcher
   - `RiskLimitHelper`: Access to OKXRiskLimitHelper
   - `IsConnected`: Connection status

   ### Methods

   - `Connect()`: Establish WebSocket connections
   - `Disconnect()`: Close all connections
   - `PlaceOrder(Order)`: Submit order
   - `CancelOrder(Order)`: Cancel order
   - `UpdateOrder(Order)`: Modify order
   - `GetOpenOrders()`: Get all open orders
   - `GetAccountHoldings()`: Get current positions
   - `GetCashBalance()`: Get account balance

   ## OKXPairMatcher

   Helper for finding arbitrage pairs.

   ### Methods

   - `FindArbitragePairs(string baseCurrency, string quoteCurrency)`: Find spot-swap pairs
   - `GetMinimumTradeValue(string instId)`: Get minimum trade value

   ## OKXRiskLimitHelper

   Helper for risk management.

   ### Methods

   - `GetMaxPositionSize(string instId, decimal accountEquity)`: Calculate max position
   - `ValidateOrderSize(string instId, decimal orderSize)`: Validate order size
   ```

5. **创建 docs/INTEGRATION.md**
   ```markdown
   # LEAN Integration Guide

   ## Adding OKX to LEAN

   1. Clone OKX brokerage repository
   2. Add project reference to LEAN
   3. Update `Market.cs` to add OKX market
   4. Update `symbol-properties-database.csv`
   5. Update LEAN CLI `modules.json`

   ## Running Algorithms

   ```bash
   lean live "My Project" --brokerage okx \
     --okx-api-key "your-key" \
     --okx-api-secret "your-secret" \
     --okx-passphrase "your-passphrase"
   ```

   ## Configuration

   See [config.json.example](../config.json.example) for full configuration.
   ```

### 验证标准

- [ ] README.md 完整且清晰
- [ ] CLAUDE.md 提供开发者指引
- [ ] CHANGELOG.md 记录版本历史
- [ ] API.md 文档详细
- [ ] INTEGRATION.md 提供集成步骤
- [ ] 所有代码示例可运行
- [ ] 文档无拼写错误

### 关键文件

**创建：**
- `README.md`
- `CLAUDE.md`
- `CHANGELOG.md`
- `docs/API.md`
- `docs/INTEGRATION.md`

---

## Phase 16: LEAN 集成

**目标：** 集成 OKX 到 LEAN 核心，更新市场定义和符号属性

**工期：** 3-4 天

### 任务清单

1. **更新 Lean/Common/Market.cs**
   ```csharp
   public static class Market
   {
       // ... 现有代码 ...

       /// <summary>
       /// OKX Market
       /// </summary>
       public const string OKX = "okx";

       static Market()
       {
           // ... 现有代码 ...

           // Add OKX market
           Markets.Add(OKX, new MarketHoursDatabase.Entry
           {
               DataTimeZone = TimeZones.Utc,
               ExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc)
           });

           Encode(OKX);
       }
   }
   ```

2. **更新 symbol-properties-database.csv**
   ```csv
   # OKX Spot
   crypto,*,okx,USDT,1,0.00000001,100000000
   crypto,*,okx,BTC,1,0.00000001,100
   crypto,*,okx,ETH,1,0.00000001,10000

   # OKX Perpetual Swaps
   cryptofuture,*,okx,USDT,1,0.1,100000
   ```

3. **创建 OKXSymbolPropertiesHelper.cs**
   ```csharp
   public static class OKXSymbolPropertiesHelper
   {
       public static void UpdateSymbolPropertiesDatabase(string csvPath)
       {
           var apiClient = new OKXRestApiClient(...);

           var spotInstruments = apiClient.GetInstruments("SPOT");
           var swapInstruments = apiClient.GetInstruments("SWAP");

           var lines = new List<string>();

           foreach (var inst in spotInstruments)
           {
               lines.Add($"crypto,{inst.InstId.Replace("-", "")},okx,{inst.SettleCcy},{inst.LotSz},{inst.TickSz},{inst.MaxLmtSz}");
           }

           foreach (var inst in swapInstruments)
           {
               lines.Add($"cryptofuture,{inst.InstId.Replace("-", "")},okx,{inst.SettleCcy},{inst.LotSz},{inst.TickSz},{inst.MaxLmtSz}");
           }

           File.WriteAllLines(csvPath, lines);
       }
   }
   ```

4. **创建 BrokerageModel 集成测试**
   ```csharp
   [TestFixture]
   public class OKXBrokerageModelIntegrationTests
   {
       private QCAlgorithm _algorithm;
       private OKXBrokerage _brokerage;

       [SetUp]
       public void Setup()
       {
           _algorithm = new TestAlgorithm();
           _brokerage = CreateBrokerage();
           _algorithm.SetBrokerageModel(new OKXBrokerageModel());
       }

       [Test]
       public void Algorithm_WithOKXBrokerage_CanTradeCrypto()
       {
           var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.OKX);
           var security = _algorithm.AddSecurity(symbol);

           var order = new MarketOrder(symbol, 0.01m, DateTime.UtcNow);
           var result = _brokerage.PlaceOrder(order);

           Assert.IsTrue(result);
       }

       [Test]
       public void Algorithm_WithOKXBrokerage_CanTradeSwaps()
       {
           var symbol = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.OKX);
           var security = _algorithm.AddSecurity(symbol);

           var order = new MarketOrder(symbol, 1m, DateTime.UtcNow);
           var result = _brokerage.PlaceOrder(order);

           Assert.IsTrue(result);
       }
   }
   ```

5. **创建 LEAN 配置示例**
   ```json
   // Lean/Launcher/config.json (example section)
   {
     "environment": "live-paper",
     "live-mode-brokerage": "OKX",
     "data-queue-handler": "OKXBrokerage",

     "okx-api-key": "your-api-key",
     "okx-api-secret": "your-api-secret",
     "okx-passphrase": "your-passphrase",
     "okx-environment": "production",

     "algorithm-type-name": "MyOKXAlgorithm",
     "algorithm-language": "CSharp",
     "algorithm-location": "../../../Algorithm.CSharp/bin/Debug/QuantConnect.Algorithm.CSharp.dll"
   }
   ```

6. **更新 LEAN 构建脚本**
   ```bash
   # build.sh (add OKX brokerage)
   dotnet build Lean.Brokerages.OKX/QuantConnect.OKXBrokerage.sln
   cp Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/bin/Debug/net6.0/*.dll Launcher/bin/Debug/
   ```

7. **创建集成测试算法**
   ```csharp
   // Algorithm.CSharp/OKXIntegrationAlgorithm.cs
   public class OKXIntegrationAlgorithm : QCAlgorithm
   {
       private Symbol _btcSpot;
       private Symbol _btcSwap;

       public override void Initialize()
       {
           SetStartDate(2024, 1, 1);
           SetEndDate(2024, 12, 31);
           SetCash("USDT", 10000);

           SetBrokerageModel(BrokerageName.OKX);

           _btcSpot = AddCrypto("BTCUSDT", Resolution.Minute, Market.OKX).Symbol;
           _btcSwap = AddCryptoFuture("BTCUSDT", Resolution.Minute, Market.OKX).Symbol;

           SetBenchmark(_btcSpot);
       }

       public override void OnData(Slice data)
       {
           if (!Portfolio.Invested)
           {
               SetHoldings(_btcSpot, 0.5m);
               SetHoldings(_btcSwap, -0.5m);
           }
       }
   }
   ```

### 验证标准

- [ ] Market.cs 包含 OKX
- [ ] symbol-properties-database.csv 包含 OKX 符号
- [ ] OKXBrokerageModel 在 LEAN 中可用
- [ ] 集成测试算法可运行
- [ ] 现货交易工作正常
- [ ] 永续交易工作正常
- [ ] 数据订阅工作正常
- [ ] 订单事件正确传播

### 关键文件

**修改（在 Lean 仓库）：**
- `Common/Market.cs`
- `Data/symbol-properties/symbol-properties-database.csv`

**创建（在 Lean 仓库）：**
- `Algorithm.CSharp/OKXIntegrationAlgorithm.cs`
- `Tests/Brokerages/OKX/OKXBrokerageModelIntegrationTests.cs`

**创建（在 OKX 仓库）：**
- `QuantConnect.OKXBrokerage/OKXSymbolPropertiesHelper.cs`

---

## Phase 17: LEAN CLI 集成

**目标：** 将 OKX 添加到 LEAN CLI 支持的券商列表

**工期：** 1 天

### 任务清单

1. **更新 lean-cli 的 modules.json**

   文件路径：`~/.lean/modules.json` 或 CLI 安装目录

   ```json
   {
     "brokerages": {
       // ... 现有券商 ...
       "okx": {
         "id": "okx",
         "display-name": "OKX",
         "live-support": true,
         "sandbox-support": true,
         "regions": ["global"],
         "asset-types": ["Crypto"],
         "properties": [
           {
             "id": "okx-api-key",
             "display-name": "API Key",
             "description": "Your OKX API key",
             "type": "string",
             "required": true
           },
           {
             "id": "okx-api-secret",
             "display-name": "API Secret",
             "description": "Your OKX API secret",
             "type": "string",
             "required": true,
             "secret": true
           },
           {
             "id": "okx-passphrase",
             "display-name": "Passphrase",
             "description": "Your OKX API passphrase",
             "type": "string",
             "required": true,
             "secret": true
           },
           {
             "id": "okx-environment",
             "display-name": "Environment",
             "description": "Trading environment",
             "type": "select",
             "options": [
               { "value": "production", "label": "Production" },
               { "value": "demo", "label": "Demo" }
             ],
             "default": "production",
             "required": true
           }
         ]
       }
     }
   }
   ```

2. **创建 LEAN CLI 配置生成器**
   ```bash
   # scripts/generate-lean-config.sh
   #!/bin/bash

   cat > config.json << EOF
   {
     "environment": "live-paper",
     "live-mode-brokerage": "OKX",
     "data-queue-handler": "OKXBrokerage",

     "okx-api-key": "$OKX_API_KEY",
     "okx-api-secret": "$OKX_API_SECRET",
     "okx-passphrase": "$OKX_PASSPHRASE",
     "okx-environment": "${OKX_ENVIRONMENT:-production}",

     "algorithm-type-name": "$ALGORITHM_NAME",
     "algorithm-language": "CSharp",
     "algorithm-location": "../../../Algorithm.CSharp/bin/Debug/QuantConnect.Algorithm.CSharp.dll"
   }
   EOF
   ```

3. **测试 LEAN CLI 命令**
   ```bash
   # 创建新项目
   lean create-project --language csharp "My OKX Project"

   # 配置 OKX 券商
   lean live "My OKX Project" --brokerage okx

   # 运行回测
   lean backtest "My OKX Project"

   # 启动实盘交易
   lean live "My OKX Project" --brokerage okx \
     --okx-api-key "your-key" \
     --okx-api-secret "your-secret" \
     --okx-passphrase "your-passphrase"
   ```

4. **创建 CLI 集成测试脚本**
   ```bash
   # tests/cli-integration-test.sh
   #!/bin/bash

   set -e

   echo "Testing LEAN CLI OKX integration..."

   # 1. Create project
   lean create-project --language csharp "OKX-CLI-Test"
   cd "OKX-CLI-Test"

   # 2. Configure brokerage
   lean cloud push --brokerage okx

   # 3. Run backtest
   lean backtest --brokerage okx

   # 4. Clean up
   cd ..
   rm -rf "OKX-CLI-Test"

   echo "CLI integration test passed!"
   ```

5. **更新 CLI 文档**
   ```markdown
   # docs/CLI.md

   ## Using OKX with LEAN CLI

   ### Installation

   ```bash
   pip install lean
   ```

   ### Configuration

   ```bash
   lean login
   lean live init --brokerage okx
   ```

   You will be prompted for:
   - API Key
   - API Secret
   - Passphrase
   - Environment (production/demo)

   ### Running Live Trading

   ```bash
   lean live "My Project" --brokerage okx
   ```

   ### Environment Variables

   Alternatively, set environment variables:

   ```bash
   export OKX_API_KEY="your-key"
   export OKX_API_SECRET="your-secret"
   export OKX_PASSPHRASE="your-passphrase"
   export OKX_ENVIRONMENT="production"

   lean live "My Project" --brokerage okx
   ```
   ```

### 验证标准

- [ ] `lean live --brokerage okx` 命令工作
- [ ] CLI 正确提示输入凭证
- [ ] 凭证安全存储
- [ ] 回测模式工作正常
- [ ] 实盘模式工作正常
- [ ] 集成测试脚本通过
- [ ] CLI 文档完整

### 关键文件

**修改（在 lean-cli 仓库）：**
- `modules.json`

**创建（在 OKX 仓库）：**
- `scripts/generate-lean-config.sh`
- `tests/cli-integration-test.sh`
- `docs/CLI.md`

---

## Phase 18: 手动测试

**目标：** 全面手动测试，验证稳定性和边缘情况

**工期：** 3-5 天

### 测试类别

#### 1. 基础功能测试（1天）

**连接测试：**
- [ ] 成功连接到生产环境
- [ ] 成功连接到演示环境
- [ ] 无效凭证返回错误
- [ ] 网络中断后自动重连

**账户测试：**
- [ ] GetCashBalance 返回正确余额
- [ ] GetAccountHoldings 返回正确持仓
- [ ] 账户模式正确检测（Spot/Futures/Multi）
- [ ] 余额更新实时反映

**历史数据测试：**
- [ ] 获取 1 分钟数据（1 周范围）
- [ ] 获取 1 小时数据（1 月范围）
- [ ] 获取 1 天数据（1 年范围）
- [ ] Trade tick 数据正确
- [ ] 数据时间戳准确

#### 2. 订单执行测试（1-2天）

**现货订单：**
- [ ] 市价买单成功
- [ ] 市价卖单成功
- [ ] 限价买单成功
- [ ] 限价卖单成功
- [ ] 止损限价单成功
- [ ] 订单修改成功
- [ ] 订单撤销成功

**永续订单：**
- [ ] 开多仓成功
- [ ] 开空仓成功
- [ ] 平仓成功
- [ ] 止损单触发正确
- [ ] 杠杆设置生效

**订单事件：**
- [ ] Submitted 事件收到
- [ ] PartiallyFilled 事件收到
- [ ] Filled 事件收到
- [ ] Canceled 事件收到
- [ ] 事件顺序正确

**边缘情况：**
- [ ] 余额不足时订单被拒绝
- [ ] 最小订单量验证
- [ ] 价格精度验证
- [ ] 数量精度验证
- [ ] 超过风险限额时订单被拒绝

#### 3. 市场数据测试（1天）

**订阅测试：**
- [ ] 订阅 1 个符号
- [ ] 订阅 10 个符号
- [ ] 订阅 100 个符号
- [ ] 订阅 200 个符号（验证连接池）
- [ ] 取消订阅工作正常

**数据流测试：**
- [ ] Trade tick 实时流
- [ ] Quote tick 实时流
- [ ] 订单簿更新实时
- [ ] BestBidAsk 更新正确
- [ ] 数据延迟 < 100ms

**订单簿测试：**
- [ ] 快照正确加载
- [ ] 增量更新正确应用
- [ ] 序列间隙触发重新初始化
- [ ] Checksum 验证工作
- [ ] 深度 400 档正确

#### 4. 稳定性测试（1天）

**长时间运行：**
- [ ] 连续运行 24 小时无崩溃
- [ ] 连续运行 72 小时无崩溃
- [ ] 内存使用稳定（无泄漏）
- [ ] CPU 使用合理

**重连测试：**
- [ ] 主动断开后重连成功
- [ ] 网络中断后重连成功
- [ ] Pong 超时后重连成功
- [ ] 重连后订阅恢复
- [ ] 重连后订单状态同步

**压力测试：**
- [ ] 每秒 100 个订单
- [ ] 每秒 1000 个 tick 更新
- [ ] 同时 500 个符号订阅
- [ ] 高频交易策略稳定运行

**错误处理：**
- [ ] API 错误正确处理
- [ ] 网络错误正确处理
- [ ] 订单被拒绝正确处理
- [ ] 速率限制正确处理
- [ ] 所有错误记录到日志

#### 5. 高级功能测试（0.5天）

**PairMatcher：**
- [ ] 正确识别套利对
- [ ] 最小交易量正确
- [ ] 缓存机制工作（1小时）

**RiskLimitHelper：**
- [ ] 最大仓位计算正确
- [ ] 订单量验证正确
- [ ] 缓存机制工作（24小时）

**算法集成：**
- [ ] 简单算法运行正常
- [ ] 套利算法运行正常
- [ ] 多符号算法运行正常
- [ ] 算法日志正确输出

#### 6. 文档验证（0.5天）

- [ ] README 示例可运行
- [ ] API 文档与代码匹配
- [ ] CLI 命令正常工作
- [ ] 配置示例有效
- [ ] 所有链接有效

### 测试环境

1. **演示环境测试**（2天）
   - 使用 OKX 演示账户
   - 测试所有功能
   - 记录问题

2. **生产环境测试**（1-3天）
   - 使用小额真实资金
   - 验证关键功能
   - 监控稳定性

### 测试工具

1. **手动测试脚本**
   ```bash
   # tests/manual/run-all-tests.sh
   #!/bin/bash

   echo "Running manual tests..."

   # Connection tests
   ./tests/manual/test-connection.sh

   # Order tests
   ./tests/manual/test-orders.sh

   # Data tests
   ./tests/manual/test-market-data.sh

   # Stability tests
   ./tests/manual/test-stability.sh

   echo "All manual tests completed!"
   ```

2. **测试日志收集**
   ```bash
   # tests/manual/collect-logs.sh
   #!/bin/bash

   mkdir -p test-results
   cp logs/*.log test-results/
   tar -czf test-results-$(date +%Y%m%d-%H%M%S).tar.gz test-results/
   ```

### 验证标准

- [ ] 所有测试用例通过
- [ ] 无关键 bug
- [ ] 性能满足要求
- [ ] 文档准确完整
- [ ] 生产环境验证通过

### 关键文件

**创建：**
- `tests/manual/run-all-tests.sh`
- `tests/manual/test-connection.sh`
- `tests/manual/test-orders.sh`
- `tests/manual/test-market-data.sh`
- `tests/manual/test-stability.sh`
- `tests/manual/collect-logs.sh`
- `docs/TESTING.md`

---

## 依赖图和里程碑

### 依赖关系图

```
Phase 0 (项目设置)
   │
   ├─→ Phase 1 (REST API)
   │      │
   │      ├─→ Phase 2 (符号映射器)
   │      │      │
   │      │      ├─→ Phase 3 (历史数据)
   │      │      ├─→ Phase 4 (账户方法)
   │      │      └─→ Phase 5 (订单读取)
   │      │             │
   │      │             └─→ Phase 6 (订单写入)
   │      │                    │
   │      │                    └─→ Phase 14 (高级功能)
   │      │
   │      └─→ Phase 7 (WebSocket 基础)
   │             │
   │             └─→ Phase 8 (消息路由)
   │                    │
   │                    ├─→ Phase 9 (市场数据)
   │                    │      │
   │                    │      └─→ Phase 10 (订单簿)
   │                    │
   │                    └─→ Phase 11 (订单更新)
   │
   └─→ Phase 12 (重连逻辑) [依赖 Phase 7, 9, 11]
          │
          └─→ Phase 13 (Factory)
                 │
                 ├─→ Phase 15 (文档)
                 ├─→ Phase 16 (LEAN 集成)
                 └─→ Phase 17 (CLI 集成)
                        │
                        └─→ Phase 18 (手动测试)
```

### 关键里程碑

**里程碑 1: REST API 完成（第 7-10 天）**
- Phase 0-6 完成
- 可以通过 REST API 进行完整订单生命周期操作
- 验证标准：
  - 下单、改单、撤单全部工作
  - 账户余额和持仓正确读取
  - 历史数据检索正常

**里程碑 2: WebSocket 流完成（第 20-26 天）**
- Phase 7-11 完成
- 实时市场数据和订单更新工作
- 验证标准：
  - 订阅 100+ 符号无问题
  - 订单事件实时接收
  - 订单簿正确维护

**里程碑 3: MVP 完成（第 23-29 天）**
- Phase 0-13 完成
- 基本功能完整，可以运行简单算法
- 验证标准：
  - 简单买卖算法运行正常
  - 连接稳定性良好
  - Factory 正确创建 Brokerage

**里程碑 4: 生产就绪（第 35-45 天）**
- Phase 0-18 完成
- 完整功能，文档齐全，测试充分
- 验证标准：
  - 所有单元测试通过
  - 手动测试全部通过
  - 文档完整准确
  - 生产环境验证成功

---

## 文件创建总清单

### 核心文件（~30 个）

#### 主项目文件
1. `QuantConnect.OKXBrokerage.csproj`
2. `QuantConnect.OKXBrokerage.sln`
3. `config.json.example`
4. `.gitignore`

#### Brokerage 核心
5. `OKXBrokerage.cs` - 主类
6. `OKXBrokerage.History.cs` - 历史数据
7. `OKXBrokerage.Orders.cs` - 订单管理
8. `OKXBrokerage.Messaging.cs` - 消息路由
9. `OKXBrokerage.DataQueueHandler.cs` - 数据订阅
10. `OKXBrokerage.Reconnection.cs` - 重连逻辑

#### API 客户端
11. `OKXRestApiClient.cs` - REST API
12. `OKXWebSocketWrapper.cs` - WebSocket 包装

#### 辅助类
13. `OKXSymbolMapper.cs` - 符号映射
14. `OKXOrderBook.cs` - 订单簿
15. `OKXOrderBookContext.cs` - 订单簿上下文
16. `OKXAccountMode.cs` - 账户模式枚举
17. `OKXOrderProperties.cs` - 订单属性
18. `OKXPairMatcher.cs` - 套利对匹配
19. `OKXRiskLimitHelper.cs` - 风险限额助手

#### Factory 和 Model
20. `OKXBrokerageFactory.cs`
21. `OKXBrokerageModel.cs`
22. `OKXFeeModel.cs`

### 消息模型（~25 个）

#### REST 消息
23. `Messages/ServerTime.cs`
24. `Messages/Instrument.cs`
25. `Messages/Ticker.cs`
26. `Messages/AccountConfig.cs`
27. `Messages/Balance.cs`
28. `Messages/Position.cs`
29. `Messages/Candle.cs`
30. `Messages/Trade.cs`
31. `Messages/Order.cs`
32. `Messages/PlaceOrderRequest.cs`
33. `Messages/AmendOrderRequest.cs`
34. `Messages/OrderResponse.cs`
35. `Messages/CancelOrderResponse.cs`
36. `Messages/AmendOrderResponse.cs`

#### WebSocket 消息
37. `Messages/WebSocketRequest.cs`
38. `Messages/WebSocketResponse.cs`
39. `Messages/LoginRequest.cs`
40. `Messages/LoginResponse.cs`
41. `Messages/TickerUpdate.cs`
42. `Messages/TradeUpdate.cs`
43. `Messages/OrderBookUpdate.cs`
44. `Messages/OrderUpdate.cs`
45. `Messages/AccountUpdate.cs`
46. `Messages/PositionUpdate.cs`

### Converter 类（~15 个）

47. `Converters/DecimalConverter.cs`
48. `Converters/DateTimeConverter.cs`
49. `Converters/ServerTimeConverter.cs`
50. `Converters/CandleConverter.cs`
51. `Converters/TradeConverter.cs`
52. `Converters/OrderConverter.cs`
53. `Converters/TickerUpdateConverter.cs`
54. `Converters/TradeUpdateConverter.cs`
55. `Converters/OrderBookUpdateConverter.cs`
56. `Converters/OrderUpdateConverter.cs`
57. `Converters/BalanceExtensions.cs`
58. `Converters/PositionExtensions.cs`
59. `Converters/OrderExtensions.cs`
60. `Converters/CandleToBarsConverter.cs`
61. `Converters/TradeToTickConverter.cs`

### 测试文件（~20 个）

62. `QuantConnect.OKXBrokerage.Tests.csproj`
63. `OKXRestApiClientTests.cs`
64. `OKXSymbolMapperTests.cs`
65. `OKXBrokerageHistoryTests.cs`
66. `OKXBrokerageAccountTests.cs`
67. `OKXBrokerageOrderReadTests.cs`
68. `OKXBrokerageOrderWriteTests.cs`
69. `OKXBrokerageWebSocketTests.cs`
70. `OKXBrokerageMessagingTests.cs`
71. `OKXBrokerageDataQueueHandlerTests.cs`
72. `OKXOrderBookTests.cs`
73. `OKXBrokerageOrderEventsTests.cs`
74. `OKXBrokerageReconnectionTests.cs`
75. `OKXBrokerageFactoryTests.cs`
76. `OKXBrokerageModelTests.cs`
77. `OKXPairMatcherTests.cs`
78. `OKXRiskLimitHelperTests.cs`
79. `OKXBrokerageModelIntegrationTests.cs`

### 文档文件（~10 个）

80. `README.md`
81. `CLAUDE.md`
82. `CHANGELOG.md`
83. `LICENSE`
84. `docs/API.md`
85. `docs/INTEGRATION.md`
86. `docs/CLI.md`
87. `docs/TESTING.md`
88. `docs/TROUBLESHOOTING.md`

### 脚本文件（~8 个）

89. `scripts/generate-lean-config.sh`
90. `scripts/build.sh`
91. `tests/manual/run-all-tests.sh`
92. `tests/manual/test-connection.sh`
93. `tests/manual/test-orders.sh`
94. `tests/manual/test-market-data.sh`
95. `tests/manual/test-stability.sh`
96. `tests/manual/collect-logs.sh`

### LEAN 集成文件（~5 个）

97. `OKXSymbolPropertiesHelper.cs`
98. `tests/cli-integration-test.sh`

### 配置和集成（在 LEAN 仓库，~5 个文件修改）

99. 修改：`Lean/Common/Market.cs`
100. 修改：`Lean/Data/symbol-properties/symbol-properties-database.csv`
101. 创建：`Lean/Algorithm.CSharp/OKXIntegrationAlgorithm.cs`
102. 创建：`Lean/Tests/Brokerages/OKX/...`
103. 修改：`lean-cli/modules.json`

### 总计

- **核心功能文件：** ~30
- **消息模型：** ~25
- **Converter：** ~15
- **测试文件：** ~20
- **文档：** ~10
- **脚本：** ~8
- **LEAN 集成：** ~5

**总计约 110+ 个文件**

---

## 风险管理

### 技术风险

| 风险 | 概率 | 影响 | 缓解策略 |
|------|------|------|---------|
| **OKX API 变更** | 中 | 高 | 版本锁定；订阅 API 变更通知；预留适配时间 |
| **序列管理复杂性** | 中 | 高 | 详细单元测试；参考 Gate 实现；日志完善 |
| **速率限制触发** | 高 | 中 | RateGate 实现；指数退避；用户警告 |
| **WebSocket 稳定性** | 中 | 高 | 保活机制；自动重连；健康检查 |
| **订单簿同步失败** | 中 | 高 | Checksum 验证；序列间隙检测；自动重新初始化 |
| **账户模式检测失败** | 低 | 中 | 默认值处理；用户配置覆盖；详细日志 |

### 时间风险

| 风险 | 概率 | 影响 | 缓解策略 |
|------|------|------|---------|
| **Phase 3 超期** | 中 | 中 | 历史数据可降级（仅支持部分分辨率） |
| **Phase 10 超期** | 高 | 高 | 简化为 5 档订单簿（books5），降低复杂度 |
| **Phase 14 超期** | 中 | 低 | 高级功能可推迟到 v1.1 |
| **Phase 18 超期** | 高 | 中 | 演示环境测试优先；生产测试可延后 |

### 依赖风险

| 风险 | 概率 | 影响 | 缓解策略 |
|------|------|------|---------|
| **LEAN 版本不兼容** | 低 | 高 | 锁定 LEAN 版本；预留集成时间 |
| **RestSharp 版本冲突** | 低 | 中 | 使用 LEAN 相同版本 |
| **Newtonsoft.Json 问题** | 低 | 中 | 详细测试序列化 |

### 质量风险

| 风险 | 概率 | 影响 | 缓解策略 |
|------|------|------|---------|
| **单元测试覆盖率不足** | 中 | 高 | 每个 Phase 包含测试任务；代码审查 |
| **文档不完整** | 中 | 中 | Phase 15 专门用于文档；同行审查 |
| **边缘情况未处理** | 高 | 中 | Phase 18 详细手动测试；错误日志完善 |

### 运营风险

| 风险 | 概率 | 影响 | 缓解策略 |
|------|------|------|---------|
| **生产环境故障** | 低 | 高 | 演示环境充分测试；灰度发布；回滚计划 |
| **用户资金损失** | 低 | 极高 | 订单验证；风险限额；用户确认 |
| **合规问题** | 低 | 高 | 使用官方 API；遵守 OKX ToS；法律审查 |

### 应对策略

1. **渐进式交付**
   - 先完成 MVP（Phase 0-13）
   - 验证核心功能后再开发高级功能
   - 允许分阶段发布（v1.0, v1.1, v1.2）

2. **质量保证**
   - 每个 Phase 包含单元测试
   - Phase 18 专门用于手动测试
   - 代码审查和同行评审

3. **时间缓冲**
   - 预计工期范围（如 35-45 天）
   - 高风险 Phase 预留额外时间
   - 非关键功能可推迟

4. **风险监控**
   - 每周检查进度
   - 记录技术债务
   - 及时调整计划

---

**文档结束**

*此计划为 Lean.Brokerages.OKX 项目提供完整的实施路线图，包含 18 个开发阶段、详细任务清单、验证标准和风险管理策略。预计总工期 35-45 个工作日。*

