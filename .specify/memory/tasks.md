# Lean.Brokerages.OKX 任务分解清单

**项目：** OKX 券商插件任务分解
**版本：** 1.0
**创建日期：** 2026-01-09
**总任务数：** 183 个
**总工时：** ~380 小时

本文档将 18 个开发阶段分解为 2-4 小时的可执行任务，每个任务包含具体的检查清单和验证标准。

---

## 任务状态说明

- [ ] **待完成** - 任务尚未开始
- [>] **进行中** - 任务正在执行
- [x] **已完成** - 任务已完成并验证

---

## 目录

- [Phase 0: 项目设置](#phase-0-项目设置) (8 个任务)
- [Phase 1: REST API 基础](#phase-1-rest-api-基础) (12 个任务)
- [Phase 2: 符号映射器](#phase-2-符号映射器) (5 个任务)
- [Phase 3: GetHistory 实现](#phase-3-gethistory-实现) (8 个任务)
- [Phase 4: 账户方法](#phase-4-账户方法) (9 个任务)
- [Phase 5: 订单管理-读取](#phase-5-订单管理-读取) (6 个任务)
- [Phase 6: 订单管理-写入](#phase-6-订单管理-写入) (13 个任务)
- [Phase 7: WebSocket 基础](#phase-7-websocket-基础) (11 个任务)
- [Phase 8: WebSocket 消息路由](#phase-8-websocket-消息路由) (8 个任务)
- [Phase 9: 市场数据订阅](#phase-9-市场数据订阅) (12 个任务)
- [Phase 10: 订单簿管理](#phase-10-订单簿管理) (15 个任务)
- [Phase 11: 订单更新](#phase-11-订单更新) (9 个任务)
- [Phase 12: 重连逻辑](#phase-12-重连逻辑) (10 个任务)
- [Phase 13: Brokerage Factory](#phase-13-brokerage-factory) (8 个任务)
- [Phase 14: 高级功能](#phase-14-高级功能) (12 个任务)
- [Phase 15: 文档](#phase-15-文档) (10 个任务)
- [Phase 16: LEAN 集成](#phase-16-lean-集成) (14 个任务)
- [Phase 17: LEAN CLI 集成](#phase-17-lean-cli-集成) (6 个任务)
- [Phase 18: 手动测试](#phase-18-手动测试) (17 个任务)

---

## Phase 0: 项目设置

**总任务数：** 8 个
**预计工时：** 8 小时
**依赖：** 无

### Task 0.1: 创建仓库目录结构

- [x] **状态：** 已完成
- **预计时间：** 1 小时
- **依赖：** 无

**检查清单：**
- [x] 创建主目录 `C:/Users/Jagger/Documents/Code/Lean.Brokerages.OKX`
- [x] 初始化 Git 仓库
- [x] 创建 `.specify/memory/` 目录
- [x] 验证目录存在

**验证标准：**
- 目录存在且 Git 初始化成功
- `.git` 文件夹已创建

**涉及文件：**
- 创建：项目根目录

---

### Task 0.2: 复制 Gate 仓库作为模板

- [x] **状态：** 已完成
- **预计时间：** 1 小时
- **依赖：** Task 0.1

**检查清单：**
- [x] 复制 `Lean.Brokerages.Gate/QuantConnect.GateBrokerage` → `QuantConnect.OKXBrokerage`
- [x] 复制 `Lean.Brokerages.Gate/QuantConnect.GateBrokerage.Tests` → `QuantConnect.OKXBrokerage.Tests`
- [x] 验证文件复制完整

**验证标准：**
- 两个目录存在
- 文件数量与 Gate 模板一致

**涉及文件：**
- 复制：`QuantConnect.OKXBrokerage/` (所有文件)
- 复制：`QuantConnect.OKXBrokerage.Tests/` (所有文件)

---

### Task 0.3: 全局重命名 Gate → OKX

- [x] **状态：** 已完成
- **预计时间：** 1 小时
- **依赖：** Task 0.2

**检查清单：**
- [x] 使用 VS Code 查找替换：`Gate` → `OKX`
- [x] 使用 VS Code 查找替换：`gate` → `okx`
- [x] 使用 VS Code 查找替换：`GATE` → `OKX`
- [x] 使用 VS Code 查找替换：`Gate.io` → `OKX`
- [x] 验证命名空间：`QuantConnect.Brokerages.OKX`
- [x] 验证类名：`OKXBrokerage`, `OKXRestApiClient` 等

**验证标准：**
- 无 "Gate" 残留字符串（除注释中的参考说明）
- 所有类名和命名空间正确

**涉及文件：**
- 修改：所有 `.cs` 文件

---

### Task 0.4: 删除 Gate 特定文件

- [x] **状态：** 已完成
- **预计时间：** 0.5 小时
- **依赖：** Task 0.3

**检查清单：**
- [x] 删除 `GateSpotBrokerage.cs`
- [x] 删除 `GateFuturesBrokerage.cs`
- [x] 删除 `GateCrossMarginBrokerage.cs`
- [x] 删除 `GateUnifiedBrokerage.cs`
- [x] 删除 `GateSpotRestApiClient.cs`
- [x] 删除 `GateFuturesRestApiClient.cs`
- [x] 删除 `GateUnifiedRestApiClient.cs`
- [x] 保留 `OKXBrokerage.cs` (重命名后的基类)
- [x] 保留 `OKXRestApiClient.cs` (统一客户端)

**验证标准：**
- 只保留单一 `OKXBrokerage.cs` 和 `OKXRestApiClient.cs`
- 项目编译失败（预期，因为缺少实现）

**涉及文件：**
- 删除：多个 Gate 特定实现文件

---

### Task 0.5: 创建 .csproj 文件

- [x] **状态：** 已完成
- **预计时间：** 1 小时
- **依赖：** Task 0.4

**检查清单：**
- [x] 修改 `QuantConnect.OKXBrokerage.csproj`（Gate → OKX）
- [x] 设置 TargetFramework: net10.0
- [x] 验证 LEAN 依赖引用
- [x] 修改 `QuantConnect.OKXBrokerage.Tests.csproj`（Gate → OKX）
- [x] 验证测试框架引用 (NUnit, Moq)

**验证标准：**
- `dotnet restore` 成功
- 依赖正确解析

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/QuantConnect.OKXBrokerage.csproj`
- 创建：`QuantConnect.OKXBrokerage.Tests/QuantConnect.OKXBrokerage.Tests.csproj`

---

### Task 0.6: 创建 Solution 文件

- [x] **状态：** 已完成
- **预计时间：** 0.5 小时
- **依赖：** Task 0.5

**检查清单：**
- [x] 复制 Gate Solution 文件
- [x] 修改 Solution 文件中的项目引用（Gate → OKX）
- [x] 验证 Solution 包含所有 OKX 项目（主项目、测试项目、ToolBox）
- [x] 验证 `dotnet sln list` 显示正确的项目列表

**验证标准：**
- Solution 文件存在
- `dotnet sln list` 显示 2 个项目

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage.sln`

---

### Task 0.7: 创建配置文件

- [x] **状态：** 已完成
- **预计时间：** 1 小时
- **依赖：** Task 0.6

**检查清单：**
- [x] 复制并修改 `.gitignore` (包含 bin/, obj/, config.json)
- [x] 创建 `config.json.example` (API 凭证模板)
- [x] 添加 OKX 特定配置：
  - okx-api-key
  - okx-api-secret
  - okx-passphrase (OKX 特有)
  - okx-environment (production/testnet)
- [x] 修改 Tests/config.json 为 OKX 配置
- [x] 验证 `.gitignore` 正确排除敏感文件

**验证标准：**
- `.gitignore` 存在
- `config.json.example` 包含所有必需字段
- `config.json` 不会被 Git 追踪

**涉及文件：**
- 创建：`.gitignore`
- 创建：`config.json.example`

---

### Task 0.8: 验证初始构建

- [x] **状态：** 已完成
- **预计时间：** 2 小时
- **依赖：** Task 0.7

**检查清单：**
- [x] 运行 `dotnet restore`
- [x] 运行 `dotnet build`
- [x] 修复编译错误（删除 Gate 引用导致）
- [x] 验证 0 错误（可以有警告）
- [x] 提交初始代码：`git add . && git commit -m "Initial project setup"`

**验证标准：**
- `dotnet build` 成功（0 错误）
- Git 提交成功
- 项目结构完整

**涉及文件：**
- 修改：所有 `.cs` 文件（修复编译错误）
- 创建：`RestApi/OKXRestApiClient.cs`（具体实现类）
- 修改：`../Lean/Common/Market.cs`（添加 OKX 市场定义）
- 删除：测试文件中引用已删除实现类的文件

---

## Phase 1: REST API 基础

**总任务数：** 12 个
**预计工时：** 18 小时
**依赖：** Phase 0

### Task 1.1: 创建 OKXRestApiClient 骨架

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 0.8

**检查清单：**
- [ ] 创建 `RestApi/OKXRestApiClient.cs`
- [ ] 添加字段：`_apiKey`, `_apiSecret`, `_passphrase`, `_apiUrl`
- [ ] 添加构造函数
- [ ] 创建方法存根：`SignRequest()`
- [ ] 创建方法存根：`ExecuteRequest<T>()`
- [ ] 构建项目，验证无错误

**验证标准：**
- 文件存在且编译通过
- 类结构完整

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/RestApi/OKXRestApiClient.cs`

---

### Task 1.2: 实现 HMAC-SHA256 签名

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.1

**检查清单：**
- [ ] 实现 `GenerateSignature(timestamp, method, path, body)`
- [ ] 使用 OKX 签名格式：`timestamp + method + path + body`
- [ ] 使用 HMACSHA256 + Base64 编码
- [ ] 创建 `GetTimestamp()` 方法（Unix 毫秒）
- [ ] 编写单元测试验证签名

**验证标准：**
- 签名生成正确（对比 OKX 文档示例）
- 单元测试通过

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Tests/OKXRestApiClientTests.cs`

---

### Task 1.3: 实现认证头添加

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 1.2

**检查清单：**
- [ ] 实现 `AddAuthenticationHeaders(request, method, path, body)`
- [ ] 添加 `OK-ACCESS-KEY` 头
- [ ] 添加 `OK-ACCESS-SIGN` 头
- [ ] 添加 `OK-ACCESS-TIMESTAMP` 头
- [ ] 添加 `OK-ACCESS-PASSPHRASE` 头
- [ ] 添加 `Content-Type: application/json` 头

**验证标准：**
- 所有必需头正确添加
- 头格式符合 OKX 要求

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`

---

### Task 1.4: 实现速率限制器

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.3

**检查清单：**
- [ ] 添加 `_orderRateLimiter` - RateGate(1000, TimeSpan.FromSeconds(2))
- [ ] 添加 `_accountRateLimiter` - RateGate(10, TimeSpan.FromSeconds(2))
- [ ] 添加 `_instrumentRateLimiter` - RateGate(20, TimeSpan.FromSeconds(2))
- [ ] 在所有 API 调用前使用 `WaitToProceed()`
- [ ] 编写测试验证速率限制

**验证标准：**
- RateGate 正确初始化
- 快速调用时被限制
- 测试通过

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Tests/OKXRateLimitTests.cs`

---

### Task 1.5: 实现 GetServerTime 端点

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 1.4

**检查清单：**
- [ ] 实现 `GetServerTime()` 方法
- [ ] 调用 `GET /api/v5/public/time`
- [ ] 创建 `Messages/ServerTime.cs` 消息模型
- [ ] 创建 `Converters/ServerTimeConverter.cs`
- [ ] 测试端点调用成功

**验证标准：**
- 成功获取服务器时间
- 返回 Unix 时间戳
- 无认证错误

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/ServerTime.cs`
- 创建：`Converters/ServerTimeConverter.cs`

---

### Task 1.6: 创建基础 Converter

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.5

**检查清单：**
- [ ] 创建 `Converters/DecimalConverter.cs`
- [ ] 创建 `Converters/DateTimeConverter.cs` (Unix 毫秒 → DateTime)
- [ ] 创建 `Converters/LongConverter.cs`
- [ ] 编写单元测试验证转换

**验证标准：**
- 所有 Converter 正确处理 null 值
- 数值转换精确
- 测试通过

**涉及文件：**
- 创建：`Converters/DecimalConverter.cs`
- 创建：`Converters/DateTimeConverter.cs`
- 创建：`Converters/LongConverter.cs`
- 创建：`Tests/ConverterTests.cs`

---

### Task 1.7: 实现 GetInstruments 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.6

**检查清单：**
- [ ] 实现 `GetInstruments(instType)` 方法
- [ ] 调用 `GET /api/v5/public/instruments?instType={type}`
- [ ] 创建 `Messages/Instrument.cs`
- [ ] 创建 `Converters/InstrumentConverter.cs`
- [ ] 测试获取 SPOT/SWAP/FUTURES 工具

**验证标准：**
- 成功获取工具列表
- 返回 instId, baseCcy, quoteCcy 等字段
- 所有工具类型都能查询

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Instrument.cs`
- 创建：`Converters/InstrumentConverter.cs`

---

### Task 1.8: 实现 GetTicker 端点

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 1.7

**检查清单：**
- [ ] 实现 `GetTicker(instId)` 方法
- [ ] 调用 `GET /api/v5/market/ticker?instId={id}`
- [ ] 创建 `Messages/Ticker.cs`
- [ ] 创建 `Converters/TickerConverter.cs`
- [ ] 测试获取 BTC-USDT ticker

**验证标准：**
- 成功获取 ticker 数据
- 包含 last, bidPx, askPx 等字段
- 价格为 decimal 类型

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Ticker.cs`
- 创建：`Converters/TickerConverter.cs`

---

### Task 1.9: 实现 GetAccountConfig 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.8

**检查清单：**
- [ ] 实现 `GetAccountConfig()` 方法（需要认证）
- [ ] 调用 `GET /api/v5/account/config`
- [ ] 创建 `Messages/AccountConfig.cs`
- [ ] 创建 `Converters/AccountConfigConverter.cs`
- [ ] 解析 `acctLv` 字段（账户模式）
- [ ] 测试获取账户配置

**验证标准：**
- 成功获取账户配置
- 正确解析账户模式（1/2/3/4）
- 认证头工作正常

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/AccountConfig.cs`
- 创建：`Converters/AccountConfigConverter.cs`

---

### Task 1.10: 实现 GetBalance 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.9

**检查清单：**
- [ ] 实现 `GetBalance(currency)` 方法
- [ ] 调用 `GET /api/v5/account/balance`
- [ ] 创建 `Messages/Balance.cs`
- [ ] 创建 `Converters/BalanceConverter.cs`
- [ ] 测试获取 USDT 余额

**验证标准：**
- 成功获取余额数据
- 包含 availBal, frozenBal 等字段
- 余额为 decimal 类型

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Balance.cs`
- 创建：`Converters/BalanceConverter.cs`

---

### Task 1.11: 实现 GetPositions 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.10

**检查清单：**
- [ ] 实现 `GetPositions(instType, instId)` 方法
- [ ] 调用 `GET /api/v5/account/positions`
- [ ] 创建 `Messages/Position.cs`
- [ ] 创建 `Converters/PositionConverter.cs`
- [ ] 测试获取期货持仓

**验证标准：**
- 成功获取持仓数据
- 包含 pos, avgPx, upl 等字段
- 区分多头/空头

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Position.cs`
- 创建：`Converters/PositionConverter.cs`

---

### Task 1.12: REST API 集成测试

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 1.11

**检查清单：**
- [ ] 创建集成测试类 `OKXRestApiIntegrationTests`
- [ ] 测试所有公共端点（GetServerTime, GetInstruments, GetTicker）
- [ ] 测试所有私有端点（GetAccountConfig, GetBalance, GetPositions）
- [ ] 验证速率限制不被触发
- [ ] 验证错误处理

**验证标准：**
- 所有端点调用成功
- 认证头正确
- 速率限制工作正常
- 集成测试通过

**涉及文件：**
- 创建：`Tests/OKXRestApiIntegrationTests.cs`

---

## Phase 2: 符号映射器

**总任务数：** 5 个
**预计工时：** 6 小时
**依赖：** Phase 1

### Task 2.1: 创建 OKXSymbolMapper 骨架

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 1.12

**检查清单：**
- [ ] 创建 `OKXSymbolMapper.cs`
- [ ] 添加字段：`_market`
- [ ] 创建构造函数
- [ ] 创建方法存根：`GetBrokerageSymbol(Symbol)`
- [ ] 创建方法存根：`GetLeanSymbol(string, SecurityType)`

**验证标准：**
- 文件存在且编译通过
- 类结构完整

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXSymbolMapper.cs`

---

### Task 2.2: 实现 LEAN → OKX 符号转换（现货）

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 2.1

**检查清单：**
- [ ] 实现 `FormatSpotSymbol(Symbol)` 方法
- [ ] 转换：BTCUSDT → BTC-USDT
- [ ] 处理不同报价货币（USDT, USDC, USD）
- [ ] 编写单元测试

**验证标准：**
- BTCUSDT → BTC-USDT
- ETHUSDT → ETH-USDT
- 测试通过

**涉及文件：**
- 修改：`OKXSymbolMapper.cs`
- 创建：`Tests/OKXSymbolMapperTests.cs`

---

### Task 2.3: 实现 LEAN → OKX 符号转换（永续）

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 2.2

**检查清单：**
- [ ] 实现 `FormatSwapSymbol(Symbol)` 方法
- [ ] 转换：BTCUSDT (CryptoFuture, perpetual) → BTC-USDT-SWAP
- [ ] 判断是否永续：`symbol.ID.Date == SecurityIdentifier.DefaultDate`
- [ ] 编写单元测试

**验证标准：**
- BTCUSDT (perpetual) → BTC-USDT-SWAP
- 测试通过

**涉及文件：**
- 修改：`OKXSymbolMapper.cs`
- 修改：`Tests/OKXSymbolMapperTests.cs`

---

### Task 2.4: 实现 LEAN → OKX 符号转换（交割）

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 2.3

**检查清单：**
- [ ] 实现 `FormatFuturesSymbol(Symbol)` 方法
- [ ] 转换：BTCUSDT (expiry: 2025-03-28) → BTC-USDT-250328
- [ ] 格式化到期日：YYMMDD
- [ ] 编写单元测试

**验证标准：**
- 交割合约正确格式化
- 日期转换正确
- 测试通过

**涉及文件：**
- 修改：`OKXSymbolMapper.cs`
- 修改：`Tests/OKXSymbolMapperTests.cs`

---

### Task 2.5: 实现 OKX → LEAN 符号转换

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 2.4

**检查清单：**
- [ ] 实现 `GetLeanSymbol(instId, securityType)` 方法
- [ ] 解析 instId 格式：BTC-USDT, BTC-USDT-SWAP, BTC-USDT-250328
- [ ] 创建对应的 LEAN Symbol
- [ ] 编写双向转换测试（可逆性）

**验证标准：**
- BTC-USDT → BTCUSDT (Crypto)
- BTC-USDT-SWAP → BTCUSDT (CryptoFuture, perpetual)
- BTC-USDT-250328 → BTCUSDT (CryptoFuture, expiry: 2025-03-28)
- 双向转换可逆
- 测试通过

**涉及文件：**
- 修改：`OKXSymbolMapper.cs`
- 修改：`Tests/OKXSymbolMapperTests.cs`

---

## Phase 3: GetHistory 实现

**总任务数：** 8 个
**预计工时：** 16 小时
**依赖：** Phase 2

### Task 3.1: 创建 OKXBrokerage.History.cs 部分类

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 2.5

**检查清单：**
- [ ] 创建 `OKXBrokerage.History.cs`
- [ ] 声明为 `public partial class OKXBrokerage`
- [ ] 添加 `GetHistory(HistoryRequest)` 方法存根
- [ ] 验证编译通过

**验证标准：**
- 文件存在
- 部分类声明正确
- 编译通过

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXBrokerage.History.cs`

---

### Task 3.2: 实现 GetCandles REST 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 3.1

**检查清单：**
- [ ] 在 `OKXRestApiClient` 中添加 `GetCandles(instId, bar, after, before, limit)` 方法
- [ ] 调用 `GET /api/v5/market/candles`
- [ ] 创建 `Messages/Candle.cs`
- [ ] 创建 `Converters/CandleConverter.cs`
- [ ] 测试获取 1 天的 1 分钟 K 线

**验证标准：**
- 成功获取 K 线数据
- 包含 ts, o, h, l, c, vol 字段
- 价格为 decimal 类型

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Candle.cs`
- 创建：`Converters/CandleConverter.cs`

---

### Task 3.3: 实现 GetTrades REST 端点

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 3.2

**检查清单：**
- [ ] 在 `OKXRestApiClient` 中添加 `GetTrades(instId, limit)` 方法
- [ ] 调用 `GET /api/v5/market/trades`
- [ ] 创建 `Messages/Trade.cs`
- [ ] 创建 `Converters/TradeConverter.cs`
- [ ] 测试获取最近 100 笔交易

**验证标准：**
- 成功获取交易数据
- 包含 ts, px, sz, side 字段
- 交易方向正确

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Trade.cs`
- 创建：`Converters/TradeConverter.cs`

---

### Task 3.4: 实现分辨率转换

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 3.3

**检查清单：**
- [ ] 实现 `ConvertResolutionToBar(Resolution)` 方法
- [ ] Minute → "1m"
- [ ] Hour → "1H"
- [ ] Daily → "1D"
- [ ] Second → 抛出异常（不支持）
- [ ] 编写单元测试

**验证标准：**
- 所有支持的分辨率正确转换
- Second 抛出 NotSupportedException
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.History.cs`
- 创建：`Tests/OKXResolutionTests.cs`

---

### Task 3.5: 实现 Candle → TradeBar 转换

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 3.4

**检查清单：**
- [ ] 创建 `Converters/CandleExtensions.cs`
- [ ] 实现 `ToTradeBar(Candle, Symbol)` 扩展方法
- [ ] 转换 OHLCV 数据
- [ ] 转换时间戳（Unix 毫秒 → DateTime）
- [ ] 编写单元测试

**验证标准：**
- Candle 正确转换为 TradeBar
- 时间戳正确
- OHLCV 数值准确
- 测试通过

**涉及文件：**
- 创建：`Converters/CandleExtensions.cs`
- 创建：`Tests/CandleConversionTests.cs`

---

### Task 3.6: 实现 Trade → Tick 转换

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 3.5

**检查清单：**
- [ ] 创建 `Converters/TradeExtensions.cs`
- [ ] 实现 `ToTick(Trade, Symbol)` 扩展方法
- [ ] 设置 TickType.Trade
- [ ] 转换价格和数量
- [ ] 编写单元测试

**验证标准：**
- Trade 正确转换为 Tick
- TickType 为 Trade
- 价格和数量准确
- 测试通过

**涉及文件：**
- 创建：`Converters/TradeExtensions.cs`
- 创建：`Tests/TradeConversionTests.cs`

---

### Task 3.7: 实现 GetHistory 主逻辑

- [ ] **状态：** 待完成
- **预计时间：** 3 小时
- **依赖：** Task 3.6

**检查清单：**
- [ ] 在 `GetHistory()` 中判断 TickType
- [ ] TradeBar/QuoteBar → 调用 `GetCandles()`
- [ ] Trade Tick → 调用 `GetTrades()`
- [ ] Quote Tick → 返回 null（不支持历史）
- [ ] 实现分页（处理 100 条限制）
- [ ] 处理时间范围（startDate, endDate）
- [ ] 编写单元测试

**验证标准：**
- 可以获取 1 周的分钟数据
- 可以获取 Trade Tick 数据
- Quote Tick 返回 null
- 分页工作正常
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.History.cs`
- 创建：`Tests/OKXBrokerageHistoryTests.cs`

---

### Task 3.8: GetHistory 集成测试

- [ ] **状态：** 待完成
- **预计时间：** 4 小时
- **依赖：** Task 3.7

**检查清单：**
- [ ] 测试 Minute 分辨率（1 周数据）
- [ ] 测试 Hour 分辨率（1 个月数据）
- [ ] 测试 Daily 分辨率（1 年数据）
- [ ] 测试 Trade Tick
- [ ] 验证数据完整性（无缺失）
- [ ] 验证数据准确性（对比 OKX 网站）
- [ ] 性能测试（1000+ 条数据）

**验证标准：**
- 所有分辨率都能获取数据
- 数据完整且准确
- 性能可接受
- 集成测试通过

**涉及文件：**
- 修改：`Tests/OKXBrokerageHistoryTests.cs`

---

## Phase 4: 账户方法

**总任务数：** 9 个
**预计工时：** 14 小时
**依赖：** Phase 3

### Task 4.1: 创建 OKXBrokerage.cs 主文件

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 3.8

**检查清单：**
- [ ] 创建 `OKXBrokerage.cs`
- [ ] 继承 `BaseWebsocketsBrokerage`
- [ ] 实现 `IDataQueueHandler` 接口
- [ ] 添加字段：`_apiClient`, `_symbolMapper`, `_algorithm`, `_aggregator`
- [ ] 实现构造函数
- [ ] 创建 `Connect()` 方法存根
- [ ] 创建 `Disconnect()` 方法存根

**验证标准：**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXBrokerage.cs`

---

### Task 4.2: 创建 OKXAccountMode 枚举

- [ ] **状态：** 待完成
- **预计时间：** 0.5 小时
- **依赖：** Task 4.1

**检查清单：**
- [ ] 创建 `OKXAccountMode.cs`
- [ ] 定义枚举值：Spot, Futures, MultiCurrencyMargin, PortfolioMargin
- [ ] 添加 XML 文档注释

**验证标准：**
- 枚举存在
- 4 个值都定义
- 编译通过

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXAccountMode.cs`

---

### Task 4.3: 实现账户模式检测

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 4.2

**检查清单：**
- [ ] 在 `OKXBrokerage.cs` 中添加 `_accountMode` 字段
- [ ] 实现 `DetectAccountMode()` 方法
- [ ] 调用 `_apiClient.GetAccountConfig()`
- [ ] 解析 `acctLv` 字段：1=Spot, 2=Futures, 3=Multi, 4=Portfolio
- [ ] 在 `Connect()` 中调用检测
- [ ] 编写单元测试

**验证标准：**
- 账户模式正确检测
- 默认值为 MultiCurrencyMargin（如果失败）
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 创建：`Tests/OKXAccountModeTests.cs`

---

### Task 4.4: 实现 GetCashBalance（Spot 模式）

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 4.3

**检查清单：**
- [ ] 实现 `GetCashBalance()` 方法
- [ ] 根据 `_accountMode` 分支处理
- [ ] 实现 `GetSpotBalances()` 私有方法
- [ ] 调用 `_apiClient.GetBalance()`
- [ ] 转换为 `List<CashAmount>`
- [ ] 编写单元测试

**验证标准：**
- Spot 模式返回现货余额
- 余额正确转换
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 创建：`Tests/OKXCashBalanceTests.cs`

---

### Task 4.5: 实现 GetCashBalance（Futures 模式）

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 4.4

**检查清单：**
- [ ] 实现 `GetFuturesBalances()` 私有方法
- [ ] 返回期货保证金余额
- [ ] 编写单元测试

**验证标准：**
- Futures 模式返回期货余额
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 修改：`Tests/OKXCashBalanceTests.cs`

---

### Task 4.6: 实现 GetCashBalance（统一账户模式）

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 4.5

**检查清单：**
- [ ] 实现 `GetUnifiedBalances()` 私有方法
- [ ] 返回所有币种余额（现货+期货）
- [ ] 编写单元测试

**验证标准：**
- Multi/Portfolio 模式返回所有余额
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 修改：`Tests/OKXCashBalanceTests.cs`

---

### Task 4.7: 创建 Balance → CashAmount 转换器

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 4.6

**检查清单：**
- [ ] 创建 `Converters/BalanceExtensions.cs`
- [ ] 实现 `ToCashAmount(Balance)` 扩展方法
- [ ] 转换币种和金额
- [ ] 编写单元测试

**验证标准：**
- Balance 正确转换为 CashAmount
- 币种符号正确
- 测试通过

**涉及文件：**
- 创建：`Converters/BalanceExtensions.cs`
- 创建：`Tests/BalanceConversionTests.cs`

---

### Task 4.8: 实现 GetAccountHoldings

- [ ] **状态：** 待完成
- **预计时间：** 3 小时
- **依赖：** Task 4.7

**检查清单：**
- [ ] 实现 `GetAccountHoldings()` 方法
- [ ] 根据账户模式分支处理
- [ ] Spot 模式：只返回现货持仓
- [ ] Futures 模式：只返回期货持仓
- [ ] Multi/Portfolio 模式：返回所有持仓
- [ ] 调用 `_apiClient.GetPositions()`
- [ ] 转换为 `List<Holding>`
- [ ] 编写单元测试

**验证标准：**
- 所有账户模式都能获取持仓
- 持仓数据正确
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 创建：`Tests/OKXAccountHoldingsTests.cs`

---

### Task 4.9: 创建 Position → Holding 转换器

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 4.8

**检查清单：**
- [ ] 创建 `Converters/PositionExtensions.cs`
- [ ] 实现 `ToHolding(Position, SymbolMapper)` 扩展方法
- [ ] 转换持仓数量、平均价、未实现盈亏
- [ ] 编写单元测试

**验证标准：**
- Position 正确转换为 Holding
- 数量和价格准确
- 测试通过

**涉及文件：**
- 创建：`Converters/PositionExtensions.cs`
- 创建：`Tests/PositionConversionTests.cs`

---

## Phase 5: 订单管理-读取

**总任务数：** 6 个
**预计工时：** 8 小时
**依赖：** Phase 4

### Task 5.1: 创建 OKXBrokerage.Orders.cs 部分类

- [ ] **状态：** 待完成
- **预计时间：** 0.5 小时
- **依赖：** Task 4.9

**检查清单：**
- [ ] 创建 `OKXBrokerage.Orders.cs`
- [ ] 声明为 `public partial class OKXBrokerage`
- [ ] 添加 `GetOpenOrders()` 方法存根

**验证标准：**
- 文件存在
- 部分类声明正确
- 编译通过

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXBrokerage.Orders.cs`

---

### Task 5.2: 实现 GetOpenOrders REST 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 5.1

**检查清单：**
- [ ] 在 `OKXRestApiClient` 中添加 `GetOpenOrders(instType, instId)` 方法
- [ ] 调用 `GET /api/v5/trade/orders-pending`
- [ ] 创建 `Messages/Order.cs`（统一订单模型）
- [ ] 创建 `Converters/OrderConverter.cs`
- [ ] 测试获取当前挂单

**验证标准：**
- 成功获取挂单列表
- 包含 ordId, clOrdId, state, px, sz 等字段
- 统一模型支持现货和期货

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`
- 创建：`Messages/Order.cs`
- 创建：`Converters/OrderConverter.cs`

---

### Task 5.3: 实现订单状态转换

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 5.2

**检查清单：**
- [ ] 创建 `ConvertOrderStatus(string okxStatus)` 方法
- [ ] 映射：live → Submitted
- [ ] 映射：partially_filled → PartiallyFilled
- [ ] 映射：filled → Filled
- [ ] 映射：canceled → Canceled
- [ ] 编写单元测试

**验证标准：**
- 所有 OKX 状态正确映射到 LEAN OrderStatus
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/OrderStatusTests.cs`

---

### Task 5.4: 创建 OKX Order → LEAN Order 转换器

- [ ] **状态：** 待完成
- **预计时间：** 2.5 小时
- **依赖：** Task 5.3

**检查清单：**
- [ ] 创建 `Converters/OrderExtensions.cs`
- [ ] 实现 `ToLeanOrder(Order, SymbolMapper)` 扩展方法
- [ ] 转换订单类型（Market, Limit, StopLimit）
- [ ] 转换订单方向（Buy/Sell）
- [ ] 转换价格和数量
- [ ] 编写单元测试

**验证标准：**
- Order 正确转换为 LEAN Order
- 所有订单类型都支持
- 测试通过

**涉及文件：**
- 创建：`Converters/OrderExtensions.cs`
- 创建：`Tests/OrderConversionTests.cs`

---

### Task 5.5: 实现 GetOpenOrders 主逻辑

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 5.4

**检查清单：**
- [ ] 在 `GetOpenOrders()` 中调用 `_apiClient.GetOpenOrders()`
- [ ] 转换所有订单为 LEAN Order
- [ ] 过滤无效订单
- [ ] 编写单元测试

**验证标准：**
- 返回所有挂单
- 订单数据正确
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/OKXGetOpenOrdersTests.cs`

---

### Task 5.6: GetOpenOrders 集成测试

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 5.5

**检查清单：**
- [ ] 通过 OKX 网站手动下单
- [ ] 调用 `GetOpenOrders()`
- [ ] 验证订单可见
- [ ] 验证订单信息正确
- [ ] 测试现货和期货订单

**验证标准：**
- 手动下的单可以通过 API 查询到
- 订单信息准确
- 集成测试通过

**涉及文件：**
- 修改：`Tests/OKXGetOpenOrdersTests.cs`

---

## Phase 6: 订单管理-写入

**总任务数：** 13 个
**预计工时：** 22 小时
**依赖：** Phase 5

### Task 6.1: 创建订单请求消息模型

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 5.6

**检查清单：**
- [ ] 创建 `Messages/PlaceOrderRequest.cs`
- [ ] 添加字段：instId, tdMode, side, ordType, px, sz, clOrdId
- [ ] 创建 `Messages/AmendOrderRequest.cs`
- [ ] 创建 `Messages/OrderResponse.cs`
- [ ] 创建 `Messages/CancelOrderResponse.cs`

**验证标准：**
- 所有消息模型存在
- 字段正确
- 编译通过

**涉及文件：**
- 创建：`Messages/PlaceOrderRequest.cs`
- 创建：`Messages/AmendOrderRequest.cs`
- 创建：`Messages/OrderResponse.cs`
- 创建：`Messages/CancelOrderResponse.cs`

---

### Task 6.2: 实现 PlaceOrder REST 端点

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 6.1

**检查清单：**
- [ ] 在 `OKXRestApiClient` 中添加 `PlaceOrder(PlaceOrderRequest)` 方法
- [ ] 调用 `POST /api/v5/trade/order`
- [ ] 应用订单速率限制
- [ ] 解析响应（ordId）
- [ ] 测试下市价单和限价单

**验证标准：**
- 成功下单
- 返回 ordId
- 速率限制工作

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`

---

### Task 6.3: 实现 CancelOrder REST 端点

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 6.2

**检查清单：**
- [ ] 在 `OKXRestApiClient` 中添加 `CancelOrder(instId, ordId, clOrdId)` 方法
- [ ] 调用 `POST /api/v5/trade/cancel-order`
- [ ] 应用订单速率限制
- [ ] 测试撤单

**验证标准：**
- 成功撤单
- 返回撤单确认
- 速率限制工作

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`

---

### Task 6.4: 实现 AmendOrder REST 端点

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 6.3

**检查清单：**
- [ ] 在 `OKXRestApiClient` 中添加 `AmendOrder(AmendOrderRequest)` 方法
- [ ] 调用 `POST /api/v5/trade/amend-order`
- [ ] 应用订单速率限制
- [ ] 测试改单

**验证标准：**
- 成功改单
- 价格和数量更新
- 速率限制工作

**涉及文件：**
- 修改：`RestApi/OKXRestApiClient.cs`

---

### Task 6.5: 实现订单验证

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 6.4

**检查清单：**
- [ ] 创建 `ValidateOrder(Order)` 方法
- [ ] 验证订单类型是否支持
- [ ] 验证数量大于最小值
- [ ] 验证价格非负
- [ ] 验证符号类型支持
- [ ] 编写单元测试

**验证标准：**
- 无效订单被拒绝
- 有效订单通过验证
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/OrderValidationTests.cs`

---

### Task 6.6: 实现 LEAN Order → OKX Request 转换（Market Order）

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 6.5

**检查清单：**
- [ ] 创建 `ConvertToOKXOrder(Order)` 方法
- [ ] 实现 MarketOrder 转换
- [ ] 设置 ordType = "market"
- [ ] 设置 sz（数量）
- [ ] 设置 side（buy/sell）
- [ ] 编写单元测试

**验证标准：**
- MarketOrder 正确转换为 PlaceOrderRequest
- 所有字段正确
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/OrderRequestConversionTests.cs`

---

### Task 6.7: 实现 LEAN Order → OKX Request 转换（Limit Order）

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 6.6

**检查清单：**
- [ ] 实现 LimitOrder 转换
- [ ] 设置 ordType = "limit" 或 "post_only"
- [ ] 设置 px（限价）
- [ ] 设置 sz（数量）
- [ ] 判断是否 PostOnly
- [ ] 编写单元测试

**验证标准：**
- LimitOrder 正确转换
- PostOnly 正确处理
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 修改：`Tests/OrderRequestConversionTests.cs`

---

### Task 6.8: 实现 LEAN Order → OKX Request 转换（StopLimit Order）

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 6.7

**检查清单：**
- [ ] 实现 StopLimitOrder 转换
- [ ] 设置 ordType = "trigger"
- [ ] 设置 triggerPx（触发价）
- [ ] 设置 orderPx（限价）
- [ ] 设置 sz（数量）
- [ ] 编写单元测试

**验证标准：**
- StopLimitOrder 正确转换
- 触发价和限价都设置
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 修改：`Tests/OrderRequestConversionTests.cs`

---

### Task 6.9: 实现 tdMode 判断逻辑

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 6.8

**检查清单：**
- [ ] 创建 `DetermineTdMode(Symbol, AccountMode)` 方法
- [ ] 现货：返回 "cash"
- [ ] 期货（cross）：返回 "cross"
- [ ] 期货（isolated）：返回 "isolated"
- [ ] 根据账户模式调整
- [ ] 编写单元测试

**验证标准：**
- 所有场景的 tdMode 正确
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/TdModeTests.cs`

---

### Task 6.10: 实现 PlaceOrder 主逻辑

- [ ] **状态：** 待完成
- **预计时间：** 2.5 小时
- **依赖：** Task 6.9

**检查清单：**
- [ ] 实现 `PlaceOrder(Order)` 方法
- [ ] 验证订单
- [ ] 转换为 OKX 请求
- [ ] 调用 `_apiClient.PlaceOrder()`
- [ ] 触发 `OnOrderIdChanged` 事件
- [ ] 处理错误响应
- [ ] 编写单元测试

**验证标准：**
- 下单成功
- OrderIdChanged 事件触发
- 错误正确处理
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/PlaceOrderTests.cs`

---

### Task 6.11: 实现 CancelOrder 主逻辑

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 6.10

**检查清单：**
- [ ] 实现 `CancelOrder(Order)` 方法
- [ ] 获取 Brokerage Order ID
- [ ] 获取 instId
- [ ] 调用 `_apiClient.CancelOrder()`
- [ ] 返回成功/失败
- [ ] 编写单元测试

**验证标准：**
- 撤单成功
- 无 Brokerage ID 时正确处理
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/CancelOrderTests.cs`

---

### Task 6.12: 实现 UpdateOrder 主逻辑

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 6.11

**检查清单：**
- [ ] 实现 `UpdateOrder(Order)` 方法
- [ ] 获取 Brokerage Order ID
- [ ] 构建 AmendOrderRequest
- [ ] 只能改价格和数量（OKX 限制）
- [ ] 调用 `_apiClient.AmendOrder()`
- [ ] 编写单元测试

**验证标准：**
- 改单成功
- 只能改价格和数量
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Orders.cs`
- 创建：`Tests/UpdateOrderTests.cs`

---

### Task 6.13: 订单操作集成测试

- [ ] **状态：** 待完成
- **预计时间：** 3 小时
- **依赖：** Task 6.12

**检查清单：**
- [ ] 测试完整订单生命周期：下单 → 查询 → 撤单
- [ ] 测试改单生命周期：下单 → 改单 → 成交
- [ ] 测试市价单立即成交
- [ ] 测试现货和期货订单
- [ ] 验证速率限制不被触发
- [ ] 验证所有错误情况

**验证标准：**
- 所有订单操作工作正常
- 现货和期货都测试通过
- 集成测试通过

**涉及文件：**
- 创建：`Tests/OrderOperationsIntegrationTests.cs`

---

## Phase 7: WebSocket 基础

**总任务数：** 11 个
**预计工时：** 20 小时
**依赖：** Phase 6

### Task 7.1: 创建 OKXWebSocketWrapper 骨架

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 6.13

**检查清单：**
- [ ] 创建 `OKXWebSocketWrapper.cs`
- [ ] 继承或包装 `WebSocketClientWrapper`
- [ ] 添加字段：`_url`, `_webSocket`
- [ ] 定义事件：Message, Closed, Opened
- [ ] 创建构造函数
- [ ] 创建方法存根：Open(), Close(), Send()

**验证标准：**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXWebSocketWrapper.cs`

---

### Task 7.2: 实现 WebSocket 连接

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 7.1

**检查清单：**
- [ ] 实现 `Open()` 方法
- [ ] 连接到 OKX WebSocket URL
- [ ] 绑定事件处理器
- [ ] 实现 `OnOpened()` 事件处理
- [ ] 测试连接成功

**验证标准：**
- WebSocket 连接成功
- Opened 事件触发
- 测试通过

**涉及文件：**
- 修改：`OKXWebSocketWrapper.cs`
- 创建：`Tests/WebSocketConnectionTests.cs`

---

### Task 7.3: 实现 WebSocket 断开

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 7.2

**检查清单：**
- [ ] 实现 `Close()` 方法
- [ ] 正常关闭 WebSocket
- [ ] 实现 `OnClosed()` 事件处理
- [ ] 测试断开成功

**验证标准：**
- WebSocket 正常断开
- Closed 事件触发
- 测试通过

**涉及文件：**
- 修改：`OKXWebSocketWrapper.cs`
- 修改：`Tests/WebSocketConnectionTests.cs`

---

### Task 7.4: 实现消息发送

- [ ] **状态：** 待完成
- **预计时间：** 1 小时
- **依赖：** Task 7.3

**检查清单：**
- [ ] 实现 `Send(string message)` 方法
- [ ] 序列化 JSON
- [ ] 发送到 WebSocket
- [ ] 测试发送订阅请求

**验证标准：**
- 消息成功发送
- JSON 格式正确
- 测试通过

**涉及文件：**
- 修改：`OKXWebSocketWrapper.cs`
- 创建：`Tests/WebSocketMessageTests.cs`

---

### Task 7.5: 实现消息接收

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 7.4

**检查清单：**
- [ ] 实现 `OnMessage()` 事件处理
- [ ] 解析 JSON 消息
- [ ] 触发 Message 事件
- [ ] 测试接收订阅确认

**验证标准：**
- 消息成功接收
- Message 事件触发
- 测试通过

**涉及文件：**
- 修改：`OKXWebSocketWrapper.cs`
- 修改：`Tests/WebSocketMessageTests.cs`

---

### Task 7.6: 在 OKXBrokerage 中集成 WebSocket

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 7.5

**检查清单：**
- [ ] 在 `OKXBrokerage.cs` 中添加 `_publicWebSocket` 字段
- [ ] 在 `OKXBrokerage.cs` 中添加 `_privateWebSocket` 字段
- [ ] 实现 `Connect()` 方法
- [ ] 创建两个 WebSocket 实例（公共 + 私有）
- [ ] 绑定消息处理器
- [ ] 测试连接

**验证标准：**
- 两个 WebSocket 都连接成功
- IsConnected 返回 true
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 创建：`Tests/OKXBrokerageConnectionTests.cs`

---

### Task 7.7: 实现私有通道登录

- [ ] **状态：** 待完成
- **预计时间：** 2.5 小时
- **依赖：** Task 7.6

**检查清单：**
- [ ] 实现 `LoginPrivateChannel()` 方法
- [ ] 生成 WebSocket 签名（timestamp + "GET" + "/users/self/verify"）
- [ ] 构建登录请求
- [ ] 发送登录请求到私有 WebSocket
- [ ] 等待登录确认
- [ ] 测试登录成功

**验证标准：**
- 签名生成正确
- 登录请求发送成功
- 收到登录确认
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 创建：`Messages/LoginRequest.cs`
- 创建：`Messages/LoginResponse.cs`

---

### Task 7.8: 实现保活定时器

- [ ] **状态：** 待完成
- **预计时间：** 2.5 小时
- **依赖：** Task 7.7

**检查清单：**
- [ ] 添加 `_keepAliveTimer` 字段
- [ ] 添加 `_lastMessageTime` 字段
- [ ] 实现 `InitializeKeepAlive()` 方法
- [ ] 创建定时器（20 秒间隔）
- [ ] 实现 `OnKeepAliveTimerElapsed()` 事件处理
- [ ] 检查是否超过 30 秒无消息
- [ ] 测试保活机制

**验证标准：**
- 定时器每 20 秒触发
- 无消息时发送 ping
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`

---

### Task 7.9: 实现 ping/pong 机制

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 7.8

**检查清单：**
- [ ] 实现 `SendPing()` 方法
- [ ] 发送 "ping" 字符串到两个 WebSocket
- [ ] 实现 pong 响应处理
- [ ] 更新 `_lastMessageTime`
- [ ] 测试 ping/pong

**验证标准：**
- ping 成功发送
- 收到 pong 响应
- _lastMessageTime 更新
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 创建：`Tests/KeepAliveTests.cs`

---

### Task 7.10: 实现 pong 超时处理

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 7.9

**检查清单：**
- [ ] 在保活定时器中检测 30 秒超时
- [ ] 超时时记录警告日志
- [ ] 停止定时器
- [ ] 触发重连（Phase 12 实现）
- [ ] 测试超时检测

**验证标准：**
- 30 秒无消息时检测到超时
- 日志记录正确
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.cs`
- 修改：`Tests/KeepAliveTests.cs`

---

### Task 7.11: WebSocket 基础集成测试

- [ ] **状态：** 待完成
- **预计时间：** 3 小时
- **依赖：** Task 7.10

**检查清单：**
- [ ] 测试完整连接流程：Connect → Login → KeepAlive → Disconnect
- [ ] 测试保活机制（等待 25 秒）
- [ ] 测试 pong 超时（模拟 35 秒无消息）
- [ ] 验证两个 WebSocket 都正常工作
- [ ] 验证私有通道认证成功

**验证标准：**
- 所有 WebSocket 操作正常
- 保活机制工作
- 集成测试通过

**涉及文件：**
- 创建：`Tests/WebSocketIntegrationTests.cs`

---

## Phase 8: WebSocket 消息路由

**总任务数：** 8 个
**预计工时：** 12 小时
**依赖：** Phase 7

### Task 8.1: 创建 OKXBrokerage.Messaging.cs 部分类

- [ ] **状态：** 待完成
- **预计时间：** 0.5 小时
- **依赖：** Task 7.11

**检查清单：**
- [ ] 创建 `OKXBrokerage.Messaging.cs`
- [ ] 声明为 `public partial class OKXBrokerage`
- [ ] 添加消息处理方法存根

**验证标准：**
- 文件存在
- 部分类声明正确
- 编译通过

**涉及文件：**
- 创建：`QuantConnect.OKXBrokerage/OKXBrokerage.Messaging.cs`

---

### Task 8.2: 实现公共消息路由

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 8.1

**检查清单：**
- [ ] 实现 `OnDataMessage(sender, message)` 事件处理
- [ ] 实现 `RoutePublicMessage(json)` 方法
- [ ] 处理 "pong" 消息
- [ ] 解析 `arg.channel` 字段
- [ ] 路由到对应 handler：tickers, trades, books
- [ ] 编写单元测试

**验证标准：**
- 消息正确路由
- pong 消息单独处理
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Messaging.cs`
- 创建：`Tests/MessageRoutingTests.cs`

---

### Task 8.3: 实现私有消息路由

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 8.2

**检查清单：**
- [ ] 实现 `OnUserMessage(sender, message)` 事件处理
- [ ] 实现 `RoutePrivateMessage(json)` 方法
- [ ] 处理 login 事件
- [ ] 路由到对应 handler：orders, account, positions
- [ ] 编写单元测试

**验证标准：**
- 消息正确路由
- login 响应单独处理
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Messaging.cs`
- 修改：`Tests/MessageRoutingTests.cs`

---

### Task 8.4: 创建 Ticker 更新消息模型

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 8.3

**检查清单：**
- [ ] 创建 `Messages/TickerUpdate.cs`
- [ ] 添加字段：arg (channel, instId), data (last, bidPx, askPx, ts)
- [ ] 创建 `Converters/TickerUpdateConverter.cs`
- [ ] 编写单元测试

**验证标准：**
- 消息模型正确
- Converter 工作正常
- 测试通过

**涉及文件：**
- 创建：`Messages/TickerUpdate.cs`
- 创建：`Converters/TickerUpdateConverter.cs`

---

### Task 8.5: 创建 Trade 更新消息模型

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 8.4

**检查清单：**
- [ ] 创建 `Messages/TradeUpdate.cs`
- [ ] 添加字段：arg, data (px, sz, side, ts)
- [ ] 创建 `Converters/TradeUpdateConverter.cs`
- [ ] 编写单元测试

**验证标准：**
- 消息模型正确
- Converter 工作正常
- 测试通过

**涉及文件：**
- 创建：`Messages/TradeUpdate.cs`
- 创建：`Converters/TradeUpdateConverter.cs`

---

### Task 8.6: 创建 OrderBook 更新消息模型

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 8.5

**检查清单：**
- [ ] 创建 `Messages/OrderBookUpdate.cs`
- [ ] 添加字段：action, arg, data (bids, asks, seqId, checksum, ts)
- [ ] 创建 `Converters/OrderBookUpdateConverter.cs`
- [ ] 编写单元测试

**验证标准：**
- 消息模型正确
- action 字段支持 snapshot/update
- Converter 工作正常
- 测试通过

**涉及文件：**
- 创建：`Messages/OrderBookUpdate.cs`
- 创建：`Converters/OrderBookUpdateConverter.cs`

---

### Task 8.7: 实现消息处理占位符

- [ ] **状态：** 待完成
- **预计时间：** 1.5 小时
- **依赖：** Task 8.6

**检查清单：**
- [ ] 实现 `HandleTickerUpdate(json)` 方法（日志记录）
- [ ] 实现 `HandleTradeUpdate(json)` 方法（日志记录）
- [ ] 实现 `HandleOrderBookUpdate(json)` 方法（日志记录）
- [ ] 实现 `HandleOrderUpdate(json)` 方法（日志记录）
- [ ] 实现 `HandleLoginResponse(json)` 方法
- [ ] 测试消息处理

**验证标准：**
- 所有 handler 被正确调用
- 日志输出正确
- 测试通过

**涉及文件：**
- 修改：`OKXBrokerage.Messaging.cs`

---

### Task 8.8: 消息路由集成测试

- [ ] **状态：** 待完成
- **预计时间：** 2 小时
- **依赖：** Task 8.7

**检查清单：**
- [ ] 测试所有公共消息路由（ticker, trade, books）
- [ ] 测试所有私有消息路由（orders, account, positions）
- [ ] 测试 pong 消息
- [ ] 测试 login 响应
- [ ] 验证日志输出

**验证标准：**
- 所有消息类型正确路由
- handler 被正确调用
- 集成测试通过

**涉及文件：**
- 修改：`Tests/MessageRoutingTests.cs`

---

## Phase 9: 市场数据订阅

**总任务数:** 12 个
**预计工时:** 18 小时
**依赖:** Phase 8

### Task 9.1: 创建订阅管理器骨架

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 8.8

**检查清单:**
- [ ] 创建 `OKXEventBasedDataQueueHandlerSubscriptionManager.cs`
- [ ] 继承 `EventBasedDataQueueHandlerSubscriptionManager`
- [ ] 添加字段: `_brokerage`, `_channelProvider`
- [ ] 添加字段: `_lastCacheUpdate`
- [ ] 实现构造函数
- [ ] 创建方法存根: `Subscribe()`, `Unsubscribe()`

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXEventBasedDataQueueHandlerSubscriptionManager.cs`

---

### Task 9.2: 实现连接池管理器

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 9.1

**检查清单:**
- [ ] 创建 `WebSocketConnectionPool.cs`
- [ ] 添加字段: `_connections` (List<OKXWebSocketWrapper>)
- [ ] 添加字段: `_subscriptionsPerConnection` (Dictionary<int, int>)
- [ ] 实现 `GetOrCreateConnection()` 方法
- [ ] 限制: 每个连接最多 100 个订阅
- [ ] 限制: 最多 30 个连接
- [ ] 编写单元测试

**验证标准:**
- 连接池正确管理连接
- 超过 100 订阅时创建新连接
- 最多 30 个连接
- 测试通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/WebSocketConnectionPool.cs`
- 创建: `Tests/ConnectionPoolTests.cs`

---

### Task 9.3: 实现订阅请求构建

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 9.2

**检查清单:**
- [ ] 创建 `Messages/SubscribeRequest.cs`
- [ ] 添加字段: op ("subscribe"), args (channel, instId)
- [ ] 实现 `BuildSubscribeRequest(channel, symbols)` 方法
- [ ] 支持频道: tickers, trades, books, books5
- [ ] 编写单元测试

**验证标准:**
- 订阅请求格式正确
- 所有频道都支持
- 测试通过

**涉及文件:**
- 创建: `Messages/SubscribeRequest.cs`
- 修改: `OKXBrokerage.Messaging.cs`

---

### Task 9.4: 实现取消订阅请求构建

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 9.3

**检查清单:**
- [ ] 创建 `Messages/UnsubscribeRequest.cs`
- [ ] 添加字段: op ("unsubscribe"), args
- [ ] 实现 `BuildUnsubscribeRequest(channel, symbols)` 方法
- [ ] 编写单元测试

**验证标准:**
- 取消订阅请求格式正确
- 测试通过

**涉及文件:**
- 创建: `Messages/UnsubscribeRequest.cs`
- 修改: `OKXBrokerage.Messaging.cs`

---

### Task 9.5: 实现 IDataQueueHandler.Subscribe (Quote)

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 9.4

**检查清单:**
- [ ] 在 `OKXBrokerage.cs` 中实现 `Subscribe(SubscriptionDataConfig, EventHandler)`
- [ ] 判断数据类型: Quote → books5 频道
- [ ] 获取可用连接
- [ ] 发送订阅请求
- [ ] 存储订阅映射
- [ ] 编写单元测试

**验证标准:**
- Quote 订阅成功
- books5 频道被订阅
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/SubscribeTests.cs`

---

### Task 9.6: 实现 IDataQueueHandler.Subscribe (Trade)

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 9.5

**检查清单:**
- [ ] 判断数据类型: Trade → trades 频道
- [ ] 发送订阅请求
- [ ] 存储订阅映射
- [ ] 编写单元测试

**验证标准:**
- Trade 订阅成功
- trades 频道被订阅
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/SubscribeTests.cs`

---

### Task 9.7: 实现 IDataQueueHandler.Subscribe (QuoteBar/TradeBar)

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 9.6

**检查清单:**
- [ ] 判断数据类型: QuoteBar/TradeBar → tickers 频道
- [ ] 发送订阅请求
- [ ] 存储订阅映射
- [ ] 编写单元测试

**验证标准:**
- QuoteBar/TradeBar 订阅成功
- tickers 频道被订阅
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/SubscribeTests.cs`

---

### Task 9.8: 实现 IDataQueueHandler.Unsubscribe

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 9.7

**检查清单:**
- [ ] 实现 `Unsubscribe(SubscriptionDataConfig)` 方法
- [ ] 查找订阅映射
- [ ] 发送取消订阅请求
- [ ] 移除订阅映射
- [ ] 如果连接无订阅，关闭连接
- [ ] 编写单元测试

**验证标准:**
- 取消订阅成功
- 映射正确移除
- 空连接被关闭
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/UnsubscribeTests.cs`

---

### Task 9.9: 实现 Ticker → Quote 转换

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 9.8

**检查清单:**
- [ ] 在 `Converters/TickerExtensions.cs` 中实现 `ToQuote(Ticker, Symbol)` 方法
- [ ] 转换 bidPx, askPx, bidSz, askSz
- [ ] 转换时间戳
- [ ] 编写单元测试

**验证标准:**
- Ticker 正确转换为 Quote
- 价格和数量准确
- 测试通过

**涉及文件:**
- 创建: `Converters/TickerExtensions.cs`
- 创建: `Tests/TickerConversionTests.cs`

---

### Task 9.10: 实现 Tick 发射

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 9.9

**检查清单:**
- [ ] 在 `HandleTickerUpdate()` 中解析消息
- [ ] 转换为 Quote Tick
- [ ] 调用 `_aggregator.Update(tick)`
- [ ] 在 `HandleTradeUpdate()` 中解析消息
- [ ] 转换为 Trade Tick
- [ ] 调用 `_aggregator.Update(tick)`
- [ ] 编写单元测试

**验证标准:**
- Tick 正确发射
- Aggregator 收到 Tick
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.Messaging.cs`
- 创建: `Tests/TickEmissionTests.cs`

---

### Task 9.11: 实现订阅确认处理

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 9.10

**检查清单:**
- [ ] 创建 `Messages/SubscribeResponse.cs`
- [ ] 在 `RoutePublicMessage()` 中处理订阅响应
- [ ] 判断 event 字段: "subscribe", "error"
- [ ] 记录日志
- [ ] 编写单元测试

**验证标准:**
- 订阅响应正确处理
- 错误订阅被记录
- 测试通过

**涉及文件:**
- 创建: `Messages/SubscribeResponse.cs`
- 修改: `OKXBrokerage.Messaging.cs`

---

### Task 9.12: 市场数据订阅集成测试

- [ ] **状态:** 待完成
- **预计时间:** 3 小时
- **依赖:** Task 9.11

**检查清单:**
- [ ] 测试订阅 Quote 数据（books5）
- [ ] 测试订阅 Trade Tick 数据
- [ ] 测试订阅 Ticker 数据
- [ ] 测试取消订阅
- [ ] 测试连接池自动扩展（订阅 150+ 符号）
- [ ] 验证 Tick 正确发射到 Aggregator
- [ ] 性能测试（100+ 订阅）

**验证标准:**
- 所有数据类型订阅成功
- Tick 实时接收
- 连接池工作正常
- 集成测试通过

**涉及文件:**
- 创建: `Tests/MarketDataSubscriptionIntegrationTests.cs`

---

## Phase 10: 订单簿管理

**总任务数:** 15 个
**预计工时:** 20 小时
**依赖:** Phase 9

### Task 10.1: 创建 OKXOrderBook 类

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 9.12

**检查清单:**
- [ ] 创建 `OKXOrderBook.cs`
- [ ] 参考 Gate 架构: `DefaultOrderBook`
- [ ] 添加字段: `_bids` (SortedDictionary<decimal, decimal>)
- [ ] 添加字段: `_asks` (SortedDictionary<decimal, decimal>)
- [ ] 添加字段: `_symbol`
- [ ] 实现构造函数
- [ ] 创建方法存根: `UpdateBid()`, `UpdateAsk()`, `Clear()`

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXOrderBook.cs`

---

### Task 10.2: 创建 OrderBookContext 类

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.1

**检查清单:**
- [ ] 创建 `OrderBookContext.cs`
- [ ] 添加字段: `Symbol`, `OrderBook`, `LastSeqId`
- [ ] 添加字段: `PendingUpdates` (Queue<OrderBookUpdate>)
- [ ] 添加字段: `IsInitialized` (bool)
- [ ] 实现构造函数
- [ ] 编写单元测试

**验证标准:**
- Context 正确存储状态
- seqId 跟踪正常
- 测试通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OrderBookContext.cs`
- 创建: `Tests/OrderBookContextTests.cs`

---

### Task 10.3: 实现 Channel buffering

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 10.2

**检查清单:**
- [ ] 添加字段: `_orderBookChannel` (Channel<OrderBookUpdate>)
- [ ] 创建 unbounded channel
- [ ] 在 `HandleOrderBookUpdate()` 中写入 channel
- [ ] 实现 `StartOrderBookProcessor()` 方法
- [ ] 启动后台任务消费 channel
- [ ] 编写单元测试

**验证标准:**
- Channel 正确创建
- 消息写入 channel
- 后台任务启动
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/OrderBookChannelTests.cs`

---

### Task 10.4: 实现 ProcessOrderBookUpdatesAsync 消费者

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 10.3

**检查清单:**
- [ ] 实现 `ProcessOrderBookUpdatesAsync()` 方法
- [ ] 使用 `await foreach` 读取 channel
- [ ] 调用 `ProcessOrderBookUpdate(update)`
- [ ] 处理异常
- [ ] 支持 CancellationToken
- [ ] 编写单元测试

**验证标准:**
- 消费者正确读取 channel
- 异常被正确处理
- 取消令牌工作
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/OrderBookChannelTests.cs`

---

### Task 10.5: 实现 Full Snapshot 处理

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 10.4

**检查清单:**
- [ ] 实现 `ProcessSnapshot(OrderBookUpdate, OrderBookContext)` 方法
- [ ] 判断 action == "snapshot"
- [ ] 清空现有订单簿
- [ ] 应用所有 bids 和 asks
- [ ] 更新 LastSeqId
- [ ] 设置 IsInitialized = true
- [ ] 编写单元测试

**验证标准:**
- Snapshot 正确处理
- 订单簿完整重建
- seqId 更新
- 测试通过

**涉及文件:**
- 创建: `OKXBrokerage.OrderBooks.cs` (部分类)
- 创建: `Tests/OrderBookSnapshotTests.cs`

---

### Task 10.6: 实现 Incremental Update 处理

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 10.5

**检查清单:**
- [ ] 实现 `ProcessIncrementalUpdate(OrderBookUpdate, OrderBookContext)` 方法
- [ ] 判断 action == "update"
- [ ] 遍历 bids: 数量为 0 则删除，否则更新
- [ ] 遍历 asks: 数量为 0 则删除，否则更新
- [ ] 更新 LastSeqId
- [ ] 编写单元测试

**验证标准:**
- Incremental update 正确处理
- 价格档位正确更新/删除
- seqId 更新
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.OrderBooks.cs`
- 创建: `Tests/OrderBookIncrementalTests.cs`

---

### Task 10.7: 实现 Sequence Gap 检测

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.6

**检查清单:**
- [ ] 在 `ProcessOrderBookUpdate()` 中检查 seqId
- [ ] 如果 newSeqId != lastSeqId + 1，检测到 gap
- [ ] 记录警告日志
- [ ] 触发重新订阅（获取新 snapshot）
- [ ] 编写单元测试

**验证标准:**
- Gap 正确检测
- 日志记录
- 重新订阅触发
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.OrderBooks.cs`
- 创建: `Tests/SequenceGapTests.cs`

---

### Task 10.8: 实现 Checksum 验证

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 10.7

**检查清单:**
- [ ] 实现 `CalculateChecksum(OrderBook)` 方法
- [ ] 取前 25 个 bid 和 ask
- [ ] 拼接字符串: bid0:bidQty0:ask0:askQty0:...
- [ ] 计算 CRC32
- [ ] 对比更新消息中的 checksum
- [ ] 不匹配时记录错误并重新订阅
- [ ] 编写单元测试

**验证标准:**
- Checksum 计算正确
- 对比 OKX 文档示例
- 不匹配时触发重新订阅
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.OrderBooks.cs`
- 创建: `Tests/ChecksumTests.cs`

---

### Task 10.9: 实现 UpdateBid/UpdateAsk 方法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.8

**检查清单:**
- [ ] 在 `OKXOrderBook.cs` 中实现 `UpdateBid(price, quantity)`
- [ ] 如果 quantity == 0，移除价格档位
- [ ] 否则添加或更新价格档位
- [ ] 实现 `UpdateAsk(price, quantity)` 方法（同样逻辑）
- [ ] 编写单元测试

**验证标准:**
- Bid/Ask 正确更新
- 0 数量档位被删除
- 测试通过

**涉及文件:**
- 修改: `OKXOrderBook.cs`
- 创建: `Tests/OrderBookUpdateTests.cs`

---

### Task 10.10: 实现 GetBestBid/GetBestAsk 方法

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 10.9

**检查清单:**
- [ ] 实现 `GetBestBid()` 方法（返回最高 bid）
- [ ] 实现 `GetBestAsk()` 方法（返回最低 ask）
- [ ] 实现 `GetBidAskSpread()` 方法
- [ ] 编写单元测试

**验证标准:**
- 最优价格正确返回
- 价差计算准确
- 测试通过

**涉及文件:**
- 修改: `OKXOrderBook.cs`
- 修改: `Tests/OrderBookUpdateTests.cs`

---

### Task 10.11: 实现订单簿深度限制

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 10.10

**检查清单:**
- [ ] 添加配置: `MaxOrderBookDepth` (默认 400)
- [ ] 在更新后检查深度
- [ ] 如果超过限制，移除最远价格档位
- [ ] 编写单元测试

**验证标准:**
- 订单簿深度被限制
- 最远档位被删除
- 测试通过

**涉及文件:**
- 修改: `OKXOrderBook.cs`
- 修改: `Tests/OrderBookUpdateTests.cs`

---

### Task 10.12: 实现订单簿订阅

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.11

**检查清单:**
- [ ] 添加 `_orderBookContexts` (Dictionary<Symbol, OrderBookContext>)
- [ ] 在订阅时创建 OrderBookContext
- [ ] 订阅 "books" 频道
- [ ] 在取消订阅时移除 context
- [ ] 编写单元测试

**验证标准:**
- 订单簿订阅成功
- Context 正确创建
- 取消订阅时清理
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/OrderBookSubscriptionTests.cs`

---

### Task 10.13: 实现 PendingUpdates 缓冲队列

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.12

**检查清单:**
- [ ] 如果 IsInitialized == false，将 update 加入 PendingUpdates
- [ ] Snapshot 处理后，应用所有 PendingUpdates
- [ ] 按 seqId 排序
- [ ] 清空 PendingUpdates
- [ ] 编写单元测试

**验证标准:**
- Snapshot 前的 update 被缓冲
- Snapshot 后正确应用
- 顺序正确
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.OrderBooks.cs`
- 创建: `Tests/PendingUpdatesTests.cs`

---

### Task 10.14: 实现订单簿到 Quote Tick 转换

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.13

**检查清单:**
- [ ] 在订单簿更新后，生成 Quote Tick
- [ ] 使用 BestBid, BestAsk
- [ ] 发射到 Aggregator
- [ ] 编写单元测试

**验证标准:**
- 订单簿更新触发 Tick 发射
- Quote 数据正确
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.OrderBooks.cs`
- 创建: `Tests/OrderBookToTickTests.cs`

---

### Task 10.15: 订单簿管理集成测试

- [ ] **状态:** 待完成
- **预计时间:** 3 小时
- **依赖:** Task 10.14

**检查清单:**
- [ ] 测试完整订单簿生命周期: 订阅 → Snapshot → Update → Checksum
- [ ] 测试 Sequence Gap 检测和恢复
- [ ] 测试 Checksum 验证失败和恢复
- [ ] 测试 PendingUpdates 缓冲
- [ ] 测试多个订单簿并发管理
- [ ] 性能测试（100+ 订单簿同时更新）

**验证标准:**
- 所有订单簿功能正常
- Gap 和 Checksum 错误正确处理
- 并发管理无问题
- 集成测试通过

**涉及文件:**
- 创建: `Tests/OrderBookIntegrationTests.cs`

---

## Phase 11: 订单更新

**总任务数:** 9 个
**预计工时:** 16 小时
**依赖:** Phase 10

### Task 11.1: 创建 OrderUpdate 消息模型

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 10.15

**检查清单:**
- [ ] 创建 `Messages/OrderUpdate.cs`
- [ ] 添加字段: arg (channel, instId), data (ordId, clOrdId, state, px, sz, fillPx, fillSz, ts)
- [ ] 创建 `Converters/OrderUpdateConverter.cs`
- [ ] 编写单元测试

**验证标准:**
- 消息模型正确
- Converter 工作正常
- 测试通过

**涉及文件:**
- 创建: `Messages/OrderUpdate.cs`
- 创建: `Converters/OrderUpdateConverter.cs`

---

### Task 11.2: 订阅订单频道

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 11.1

**检查清单:**
- [ ] 在 `Connect()` 中登录后订阅 "orders" 频道
- [ ] 订阅到私有 WebSocket
- [ ] 订阅所有工具类型: SPOT, SWAP, FUTURES
- [ ] 编写单元测试

**验证标准:**
- orders 频道订阅成功
- 订阅确认收到
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/OrderChannelSubscriptionTests.cs`

---

### Task 11.3: 实现 BrokerageConcurrentMessageHandler

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 11.2

**检查清单:**
- [ ] 创建 `BrokerageConcurrentMessageHandler<T>.cs`
- [ ] 参考 Gate 架构
- [ ] 添加字段: `_messageQueue` (ConcurrentQueue<T>)
- [ ] 添加字段: `_messageEvent` (ManualResetEventSlim)
- [ ] 实现 `HandleNewMessage(message)` 方法
- [ ] 实现 `MessageProcessingThread()` 方法
- [ ] 编写单元测试

**验证标准:**
- 消息正确入队
- 处理线程正常工作
- 测试通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/BrokerageConcurrentMessageHandler.cs`
- 创建: `Tests/ConcurrentMessageHandlerTests.cs`

---

### Task 11.4: 创建 OrderUpdate handler

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 11.3

**检查清单:**
- [ ] 添加 `_orderUpdateHandler` 字段 (BrokerageConcurrentMessageHandler<OrderUpdate>)
- [ ] 在构造函数中初始化
- [ ] 实现 `HandleOrderUpdateMessage(OrderUpdate)` 回调
- [ ] 启动处理线程
- [ ] 编写单元测试

**验证标准:**
- Handler 正确初始化
- 回调函数被调用
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/OrderUpdateHandlerTests.cs`

---

### Task 11.5: 实现 OrderUpdate 解析和路由

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 11.4

**检查清单:**
- [ ] 在 `HandleOrderUpdate(json)` 中解析 OrderUpdate
- [ ] 传递给 `_orderUpdateHandler.HandleNewMessage(update)`
- [ ] 在 `HandleOrderUpdateMessage()` 中处理
- [ ] 编写单元测试

**验证标准:**
- OrderUpdate 正确解析
- 路由到 handler
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.Messaging.cs`
- 修改: `Tests/OrderUpdateHandlerTests.cs`

---

### Task 11.6: 实现订单状态转换和 OrderEvent 创建

- [ ] **状态:** 待完成
- **预计时间:** 2.5 小时
- **依赖:** Task 11.5

**检查清单:**
- [ ] 实现 `ConvertToOrderEvent(OrderUpdate)` 方法
- [ ] 映射订单状态: live, partially_filled, filled, canceled
- [ ] 创建 OrderEvent
- [ ] 设置 FillPrice, FillQuantity
- [ ] 计算 OrderFee
- [ ] 编写单元测试

**验证标准:**
- OrderEvent 正确创建
- 所有字段准确
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/OrderEventConversionTests.cs`

---

### Task 11.7: 实现 OnOrderEvent 发射

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 11.6

**检查清单:**
- [ ] 在 `HandleOrderUpdateMessage()` 中调用 `OnOrderEvent(orderEvent)`
- [ ] 处理重复事件（使用 ordId 去重）
- [ ] 记录日志
- [ ] 编写单元测试

**验证标准:**
- OrderEvent 正确触发
- 重复事件被过滤
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/OrderEventConversionTests.cs`

---

### Task 11.8: 实现订单费用计算

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 11.7

**检查清单:**
- [ ] 实现 `CalculateOrderFee(OrderUpdate)` 方法
- [ ] 解析 fee 和 feeCcy 字段
- [ ] 创建 OrderFee 对象
- [ ] 区分 maker/taker 费率
- [ ] 编写单元测试

**验证标准:**
- 费用计算正确
- 币种正确
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/OrderFeeTests.cs`

---

### Task 11.9: 订单更新集成测试

- [ ] **状态:** 待完成
- **预计时间:** 3 小时
- **依赖:** Task 11.8

**检查清单:**
- [ ] 测试完整订单生命周期: 下单 → Submitted → PartiallyFilled → Filled
- [ ] 测试撤单: Submitted → Canceled
- [ ] 测试 OrderEvent 触发
- [ ] 测试费用计算
- [ ] 测试现货和期货订单
- [ ] 验证事件顺序正确

**验证标准:**
- 所有订单状态正确触发
- OrderEvent 准确
- 集成测试通过

**涉及文件:**
- 创建: `Tests/OrderUpdateIntegrationTests.cs`

---

## Phase 12: 重连逻辑

**总任务数:** 10 个
**预计工时:** 12 小时
**依赖:** Phase 11

### Task 12.1: 创建重连管理器骨架

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 11.9

**检查清单:**
- [ ] 创建 `OKXReconnectionHandler.cs`
- [ ] 添加字段: `_brokerage`, `_reconnectDelay`, `_maxReconnectDelay`
- [ ] 实现构造函数
- [ ] 创建方法存根: `ScheduleReconnection()`, `CancelReconnection()`

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXReconnectionHandler.cs`

---

### Task 12.2: 实现指数退避算法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 12.1

**检查清单:**
- [ ] 实现 `CalculateNextDelay(currentDelay)` 方法
- [ ] 初始延迟: 1 秒
- [ ] 每次失败: delay *= 2
- [ ] 最大延迟: 60 秒
- [ ] 添加随机抖动（±20%）
- [ ] 编写单元测试

**验证标准:**
- 延迟正确计算
- 指数增长
- 最大值限制
- 测试通过

**涉及文件:**
- 修改: `OKXReconnectionHandler.cs`
- 创建: `Tests/ReconnectionTests.cs`

---

### Task 12.3: 实现 ScheduleReconnection 方法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 12.2

**检查清单:**
- [ ] 实现 `ScheduleReconnection(reason)` 方法
- [ ] 记录警告日志
- [ ] 计算延迟时间
- [ ] 使用 Task.Delay 等待
- [ ] 调用 `Reconnect()`
- [ ] 编写单元测试

**验证标准:**
- 重连被正确调度
- 延迟时间准确
- 测试通过

**涉及文件:**
- 修改: `OKXReconnectionHandler.cs`
- 修改: `Tests/ReconnectionTests.cs`

---

### Task 12.4: 实现 Reconnect 主逻辑

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 12.3

**检查清单:**
- [ ] 实现 `Reconnect()` 方法
- [ ] 关闭现有 WebSocket
- [ ] 创建新 WebSocket
- [ ] 调用 `Connect()`
- [ ] 如果失败，重新调度重连
- [ ] 如果成功，重置延迟
- [ ] 编写单元测试

**验证标准:**
- 重连流程正确
- 失败时重试
- 成功时重置
- 测试通过

**涉及文件:**
- 修改: `OKXReconnectionHandler.cs`
- 修改: `Tests/ReconnectionTests.cs`

---

### Task 12.5: 实现订阅恢复

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 12.4

**检查清单:**
- [ ] 实现 `RestoreSubscriptions()` 方法
- [ ] 从订阅管理器获取所有订阅
- [ ] 重新订阅所有频道
- [ ] 记录日志
- [ ] 编写单元测试

**验证标准:**
- 所有订阅恢复
- 无遗漏
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/SubscriptionRestoreTests.cs`

---

### Task 12.6: 实现私有通道重新登录

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 12.5

**检查清单:**
- [ ] 在 `Reconnect()` 后调用 `LoginPrivateChannel()`
- [ ] 等待登录确认
- [ ] 重新订阅私有频道 (orders)
- [ ] 编写单元测试

**验证标准:**
- 私有通道重新认证
- orders 频道重新订阅
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/ReconnectionTests.cs`

---

### Task 12.7: 实现 OrderBookContext 重新初始化

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 12.6

**检查清单:**
- [ ] 在重连后，重置所有 OrderBookContext
- [ ] 清空 OrderBook
- [ ] 重置 IsInitialized = false
- [ ] 清空 PendingUpdates
- [ ] 重新订阅 books 频道（获取新 snapshot）
- [ ] 编写单元测试

**验证标准:**
- OrderBook 正确重置
- 新 snapshot 获取
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/SubscriptionRestoreTests.cs`

---

### Task 12.8: 实现断线检测触发

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 12.7

**检查清单:**
- [ ] 在 `OnClosed()` 事件中触发重连
- [ ] 在 pong 超时中触发重连
- [ ] 在连接错误中触发重连
- [ ] 编写单元测试

**验证标准:**
- 所有断线场景都触发重连
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 修改: `Tests/ReconnectionTests.cs`

---

### Task 12.9: 实现重连取消

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 12.8

**检查清单:**
- [ ] 实现 `CancelReconnection()` 方法
- [ ] 使用 CancellationTokenSource
- [ ] 在 `Disconnect()` 中调用
- [ ] 编写单元测试

**验证标准:**
- 重连可以被取消
- Disconnect 时停止重连
- 测试通过

**涉及文件:**
- 修改: `OKXReconnectionHandler.cs`
- 修改: `Tests/ReconnectionTests.cs`

---

### Task 12.10: 重连逻辑集成测试

- [ ] **状态:** 待完成
- **预计时间:** 3 小时
- **依赖:** Task 12.9

**检查清单:**
- [ ] 测试网络断开后自动重连
- [ ] 测试 pong 超时后重连
- [ ] 测试指数退避（模拟多次失败）
- [ ] 测试订阅恢复
- [ ] 测试 OrderBook 重新初始化
- [ ] 验证重连后所有功能正常

**验证标准:**
- 所有重连场景正常
- 订阅完整恢复
- 数据流恢复正常
- 集成测试通过

**涉及文件:**
- 创建: `Tests/ReconnectionIntegrationTests.cs`

---

## Phase 13: Brokerage Factory

**总任务数:** 8 个
**预计工时:** 8 小时
**依赖:** Phase 12

### Task 13.1: 创建 OKXBrokerageFactory 类

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 12.10

**检查清单:**
- [ ] 创建 `OKXBrokerageFactory.cs`
- [ ] 继承 `BrokerageFactory`
- [ ] 实现 `BrokerageType` 属性（返回 typeof(OKXBrokerage)）
- [ ] 创建构造函数

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXBrokerageFactory.cs`

---

### Task 13.2: 实现 CreateBrokerage 方法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 13.1

**检查清单:**
- [ ] 实现 `CreateBrokerage(IOrderProvider, ISecurityProvider)` 方法
- [ ] 从配置读取 API 凭证
- [ ] 创建 OKXBrokerage 实例
- [ ] 返回 brokerage
- [ ] 编写单元测试

**验证标准:**
- Brokerage 正确创建
- 凭证正确传递
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerageFactory.cs`
- 创建: `Tests/OKXBrokerageFactoryTests.cs`

---

### Task 13.3: 创建 OKXBrokerageModel 类

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 13.2

**检查清单:**
- [ ] 创建 `OKXBrokerageModel.cs`
- [ ] 继承 `DefaultBrokerageModel`
- [ ] 实现构造函数
- [ ] 设置 AccountType = Margin（支持现货和期货）

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXBrokerageModel.cs`

---

### Task 13.4: 实现 GetBrokerageModel 方法

- [ ] **状态:** 待完成
- **预计时间:** 0.5 小时
- **依赖:** Task 13.3

**检查清单:**
- [ ] 在 `OKXBrokerageFactory` 中实现 `GetBrokerageModel()`
- [ ] 返回新 OKXBrokerageModel 实例
- [ ] 编写单元测试

**验证标准:**
- BrokerageModel 正确返回
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerageFactory.cs`
- 修改: `Tests/OKXBrokerageFactoryTests.cs`

---

### Task 13.5: 创建 OKXFeeModel 类

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 13.4

**检查清单:**
- [ ] 创建 `OKXFeeModel.cs`
- [ ] 继承 `FeeModel`
- [ ] 实现 `GetOrderFee(OrderFeeParameters)` 方法
- [ ] 现货: Maker 0.08%, Taker 0.1%
- [ ] 期货: Maker 0.02%, Taker 0.05%
- [ ] 编写单元测试

**验证标准:**
- 费用计算正确
- 现货和期货费率准确
- 测试通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXFeeModel.cs`
- 创建: `Tests/OKXFeeModelTests.cs`

---

### Task 13.6: 在 BrokerageModel 中使用 FeeModel

- [ ] **状态:** 待完成
- **预计时间:** 0.5 小时
- **依赖:** Task 13.5

**检查清单:**
- [ ] 在 `OKXBrokerageModel` 中覆盖 `GetFeeModel()` 方法
- [ ] 返回新 OKXFeeModel 实例
- [ ] 编写单元测试

**验证标准:**
- FeeModel 正确返回
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerageModel.cs`
- 修改: `Tests/OKXBrokerageFactoryTests.cs`

---

### Task 13.7: 添加 Market.OKX 支持

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 13.6

**检查清单:**
- [ ] 在 LEAN 仓库中修改 `Common/Markets.cs`
- [ ] 添加 `public const string OKX = "okx";`
- [ ] 在 `OKXSymbolMapper` 中使用 Market.OKX
- [ ] 编写单元测试

**验证标准:**
- Market.OKX 常量存在
- SymbolMapper 使用正确 market
- 测试通过

**涉及文件:**
- 修改: `Lean/Common/Markets.cs` (LEAN 仓库)
- 修改: `OKXSymbolMapper.cs`

---

### Task 13.8: 更新 BrokerageName.cs

- [ ] **状态:** 待完成
- **预计时间:** 0.5 小时
- **依赖:** Task 13.7

**检查清单:**
- [ ] 在 LEAN 仓库中修改 `Common/Brokerages/BrokerageName.cs`
- [ ] 添加 `OKX = "OKX"`
- [ ] 验证编译通过

**验证标准:**
- BrokerageName.OKX 存在
- 编译通过

**涉及文件:**
- 修改: `Lean/Common/Brokerages/BrokerageName.cs` (LEAN 仓库)

---

## Phase 14: 高级功能

**总任务数:** 12 个
**预计工时:** 16 小时
**依赖:** Phase 13

### Task 14.1: 创建 OKXPairMatcher 类

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 13.8

**检查清单:**
- [ ] 创建 `OKXPairMatcher.cs`
- [ ] 参考 Gate 架构: `GatePairMatcher`
- [ ] 添加字段: `_spotInstruments`, `_swapInstruments`, `_futuresInstruments`
- [ ] 添加字段: `_lastCacheUpdate`
- [ ] 实现构造函数

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXPairMatcher.cs`

---

### Task 14.2: 实现 Instrument 缓存

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 14.1

**检查清单:**
- [ ] 实现 `RefreshInstrumentCache()` 方法
- [ ] 调用 `_apiClient.GetInstruments()` 分别获取 SPOT/SWAP/FUTURES
- [ ] 存储到对应缓存字段
- [ ] 设置 24 小时过期时间
- [ ] 编写单元测试

**验证标准:**
- 缓存正确刷新
- 24 小时后自动刷新
- 测试通过

**涉及文件:**
- 修改: `OKXPairMatcher.cs`
- 创建: `Tests/PairMatcherTests.cs`

---

### Task 14.3: 实现 IsPairAvailable 方法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 14.2

**检查清单:**
- [ ] 实现 `IsPairAvailable(Symbol)` 方法
- [ ] 转换 Symbol 到 OKX instId
- [ ] 根据 SecurityType 查询对应缓存
- [ ] 检查 instId 是否存在
- [ ] 编写单元测试

**验证标准:**
- 可用交易对返回 true
- 不可用交易对返回 false
- 测试通过

**涉及文件:**
- 修改: `OKXPairMatcher.cs`
- 修改: `Tests/PairMatcherTests.cs`

---

### Task 14.4: 实现 GetMinimumOrderSize 方法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 14.3

**检查清单:**
- [ ] 实现 `GetMinimumOrderSize(Symbol)` 方法
- [ ] 从缓存中获取 Instrument
- [ ] 返回 minSz 字段
- [ ] 编写单元测试

**验证标准:**
- 最小订单量正确返回
- 测试通过

**涉及文件:**
- 修改: `OKXPairMatcher.cs`
- 修改: `Tests/PairMatcherTests.cs`

---

### Task 14.5: 实现 GetTickSize 方法

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 14.4

**检查清单:**
- [ ] 实现 `GetTickSize(Symbol)` 方法
- [ ] 从缓存中获取 Instrument
- [ ] 返回 tickSz 字段
- [ ] 编写单元测试

**验证标准:**
- Tick size 正确返回
- 测试通过

**涉及文件:**
- 修改: `OKXPairMatcher.cs`
- 修改: `Tests/PairMatcherTests.cs`

---

### Task 14.6: 集成 PairMatcher 到 Brokerage

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 14.5

**检查清单:**
- [ ] 在 `OKXBrokerage.cs` 中添加 `_pairMatcher` 字段
- [ ] 在构造函数中初始化
- [ ] 在 `PlaceOrder()` 前验证交易对可用
- [ ] 编写单元测试

**验证标准:**
- PairMatcher 集成成功
- 不可用交易对被拒绝
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/PairMatcherIntegrationTests.cs`

---

### Task 14.7: 创建 OKXRiskLimitHelper 类

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 14.6

**检查清单:**
- [ ] 创建 `OKXRiskLimitHelper.cs`
- [ ] 添加字段: `_riskLimitCache` (Dictionary<string, RiskLimit>)
- [ ] 添加字段: `_lastCacheUpdate`
- [ ] 实现构造函数
- [ ] 创建方法存根: `GetRiskLimit()`, `RefreshCache()`

**验证标准:**
- 文件存在
- 类结构完整
- 编译通过

**涉及文件:**
- 创建: `QuantConnect.OKXBrokerage/OKXRiskLimitHelper.cs`

---

### Task 14.8: 实现 GetRiskLimitTiers REST 端点

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 14.7

**检查清单:**
- [ ] 在 `OKXRestApiClient` 中添加 `GetRiskLimitTiers(instType, instId)` 方法
- [ ] 调用 `GET /api/v5/public/position-tiers`
- [ ] 创建 `Messages/RiskLimit.cs`
- [ ] 创建 `Converters/RiskLimitConverter.cs`
- [ ] 测试获取风险限额

**验证标准:**
- 成功获取风险限额
- 包含 tier, maxSz, maxAmt 等字段
- 测试通过

**涉及文件:**
- 修改: `RestApi/OKXRestApiClient.cs`
- 创建: `Messages/RiskLimit.cs`
- 创建: `Converters/RiskLimitConverter.cs`

---

### Task 14.9: 实现 RiskLimit 缓存

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 14.8

**检查清单:**
- [ ] 实现 `RefreshCache(instType)` 方法
- [ ] 调用 `_apiClient.GetRiskLimitTiers()`
- [ ] 存储到缓存
- [ ] 设置 24 小时过期时间
- [ ] 编写单元测试

**验证标准:**
- 缓存正确刷新
- 24 小时后自动刷新
- 测试通过

**涉及文件:**
- 修改: `OKXRiskLimitHelper.cs`
- 创建: `Tests/RiskLimitTests.cs`

---

### Task 14.10: 实现 GetMaxPositionSize 方法

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 14.9

**检查清单:**
- [ ] 实现 `GetMaxPositionSize(Symbol, accountTier)` 方法
- [ ] 从缓存中获取 RiskLimit
- [ ] 根据账户等级返回对应 maxSz
- [ ] 编写单元测试

**验证标准:**
- 最大持仓量正确返回
- 不同账户等级正确
- 测试通过

**涉及文件:**
- 修改: `OKXRiskLimitHelper.cs`
- 修改: `Tests/RiskLimitTests.cs`

---

### Task 14.11: 集成 RiskLimitHelper 到 Brokerage

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 14.10

**检查清单:**
- [ ] 在 `OKXBrokerage.cs` 中添加 `_riskLimitHelper` 字段
- [ ] 在构造函数中初始化
- [ ] 在 `PlaceOrder()` 前验证持仓限制
- [ ] 编写单元测试

**验证标准:**
- RiskLimitHelper 集成成功
- 超限订单被拒绝
- 测试通过

**涉及文件:**
- 修改: `OKXBrokerage.cs`
- 创建: `Tests/RiskLimitIntegrationTests.cs`

---

### Task 14.12: 高级功能集成测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 14.11

**检查清单:**
- [ ] 测试 PairMatcher 完整功能
- [ ] 测试 RiskLimitHelper 完整功能
- [ ] 测试缓存刷新机制
- [ ] 测试订单验证集成
- [ ] 验证性能（缓存命中率）

**验证标准:**
- 所有高级功能正常
- 缓存工作正常
- 集成测试通过

**涉及文件:**
- 创建: `Tests/AdvancedFeaturesIntegrationTests.cs`

---

## Phase 15: 文档

**总任务数:** 10 个
**预计工时:** 12 小时
**依赖:** Phase 14

### Task 15.1: 创建 README.md

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 14.12

**检查清单:**
- [ ] 创建 `README.md`
- [ ] 添加项目简介
- [ ] 添加功能列表
- [ ] 添加安装说明
- [ ] 添加快速开始示例
- [ ] 添加配置说明
- [ ] 添加支持的订单类型
- [ ] 添加支持的账户模式

**验证标准:**
- README 完整且清晰
- 示例代码可运行
- 格式正确

**涉及文件:**
- 创建: `README.md`

---

### Task 15.2: 创建 CLAUDE.md

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 15.1

**检查清单:**
- [ ] 创建 `CLAUDE.md`
- [ ] 添加项目架构说明
- [ ] 添加关键类说明
- [ ] 添加构建命令
- [ ] 添加测试命令
- [ ] 添加开发指南
- [ ] 添加常见问题解决

**验证标准:**
- 文档完整
- 构建命令准确
- 格式正确

**涉及文件:**
- 创建: `CLAUDE.md`

---

### Task 15.3: 创建 CHANGELOG.md

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 15.2

**检查清单:**
- [ ] 创建 `CHANGELOG.md`
- [ ] 添加版本历史
- [ ] 添加初始版本 (1.0.0)
- [ ] 列出所有实现的功能
- [ ] 使用 Keep a Changelog 格式

**验证标准:**
- CHANGELOG 完整
- 格式符合标准
- 版本号正确

**涉及文件:**
- 创建: `CHANGELOG.md`

---

### Task 15.4: 创建 API.md

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 15.3

**检查清单:**
- [ ] 创建 `API.md`
- [ ] 记录所有 REST API 端点
- [ ] 记录所有 WebSocket 频道
- [ ] 添加请求/响应示例
- [ ] 添加签名算法说明
- [ ] 添加速率限制说明

**验证标准:**
- API 文档完整
- 示例正确
- 格式清晰

**涉及文件:**
- 创建: `API.md`

---

### Task 15.5: 创建代码注释

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 15.4

**检查清单:**
- [ ] 为所有公共类添加 XML 文档注释
- [ ] 为所有公共方法添加注释
- [ ] 添加参数说明
- [ ] 添加返回值说明
- [ ] 添加异常说明
- [ ] 验证 XML 文档生成

**验证标准:**
- 所有公共 API 都有注释
- XML 文档生成成功
- 注释清晰准确

**涉及文件:**
- 修改: 所有 `.cs` 文件

---

### Task 15.6: 创建使用示例

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 15.5

**检查清单:**
- [ ] 创建 `examples/` 目录
- [ ] 创建现货交易示例
- [ ] 创建期货交易示例
- [ ] 创建市场数据订阅示例
- [ ] 创建 GetHistory 示例
- [ ] 验证所有示例可运行

**验证标准:**
- 示例代码完整
- 所有示例可运行
- 注释清晰

**涉及文件:**
- 创建: `examples/SpotTradingExample.cs`
- 创建: `examples/FuturesTradingExample.cs`
- 创建: `examples/MarketDataExample.cs`
- 创建: `examples/HistoryExample.cs`

---

### Task 15.7: 创建 docs 文件夹

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 15.6

**检查清单:**
- [ ] 创建 `docs/` 目录
- [ ] 创建 `docs/architecture.md` (架构说明)
- [ ] 创建 `docs/websocket.md` (WebSocket 详细说明)
- [ ] 创建 `docs/orderbook.md` (订单簿管理说明)
- [ ] 创建 `docs/reconnection.md` (重连机制说明)

**验证标准:**
- 文档目录存在
- 所有文档完整
- 格式统一

**涉及文件:**
- 创建: `docs/architecture.md`
- 创建: `docs/websocket.md`
- 创建: `docs/orderbook.md`
- 创建: `docs/reconnection.md`

---

### Task 15.8: 创建故障排查指南

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 15.7

**检查清单:**
- [ ] 创建 `TROUBLESHOOTING.md`
- [ ] 添加常见错误及解决方案
- [ ] 添加连接问题排查
- [ ] 添加订单问题排查
- [ ] 添加市场数据问题排查
- [ ] 添加日志收集说明

**验证标准:**
- 故障排查指南完整
- 解决方案可行
- 格式清晰

**涉及文件:**
- 创建: `TROUBLESHOOTING.md`

---

### Task 15.9: 创建贡献指南

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 15.8

**检查清单:**
- [ ] 创建 `CONTRIBUTING.md`
- [ ] 添加代码风格指南
- [ ] 添加提交规范
- [ ] 添加 PR 流程
- [ ] 添加测试要求
- [ ] 添加文档要求

**验证标准:**
- 贡献指南完整
- 规范明确
- 格式正确

**涉及文件:**
- 创建: `CONTRIBUTING.md`

---

### Task 15.10: 文档审查和完善

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 15.9

**检查清单:**
- [ ] 审查所有文档拼写和语法
- [ ] 验证所有链接有效
- [ ] 验证所有代码示例可运行
- [ ] 统一文档格式
- [ ] 补充缺失内容

**验证标准:**
- 所有文档无拼写错误
- 所有链接有效
- 格式统一
- 内容完整

**涉及文件:**
- 修改: 所有文档文件

---

## Phase 16: LEAN 集成

**总任务数:** 14 个
**预计工时:** 22 小时
**依赖:** Phase 15

### Task 16.1: 在 LEAN 中添加 OKXBrokerageModel

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 15.10

**检查清单:**
- [ ] 将 `OKXBrokerageModel.cs` 复制到 `Lean/Common/Brokerages/`
- [ ] 实现 `CanSubmitOrder()` 方法
- [ ] 实现 `CanUpdateOrder()` 方法
- [ ] 实现 `GetLeverage()` 方法
- [ ] 编写单元测试

**验证标准:**
- BrokerageModel 在 LEAN 中正确工作
- 所有方法实现正确
- 测试通过

**涉及文件:**
- 创建: `Lean/Common/Brokerages/OKXBrokerageModel.cs`
- 创建: `Lean/Tests/Common/Brokerages/OKXBrokerageModelTests.cs`

---

### Task 16.2: 在 LEAN 中添加 OKXFeeModel

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 16.1

**检查清单:**
- [ ] 将 `OKXFeeModel.cs` 复制到 `Lean/Common/Orders/Fees/`
- [ ] 验证费率准确
- [ ] 编写单元测试

**验证标准:**
- FeeModel 在 LEAN 中正确工作
- 费用计算准确
- 测试通过

**涉及文件:**
- 创建: `Lean/Common/Orders/Fees/OKXFeeModel.cs`
- 创建: `Lean/Tests/Common/Orders/Fees/OKXFeeModelTests.cs`

---

### Task 16.3: 添加 OKXOrderProperties

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 16.2

**检查清单:**
- [ ] 创建 `Lean/Common/Orders/OKXOrderProperties.cs`
- [ ] 添加 PostOnly 属性
- [ ] 添加 ReduceOnly 属性
- [ ] 编写单元测试

**验证标准:**
- OrderProperties 正确定义
- 属性可用
- 测试通过

**涉及文件:**
- 创建: `Lean/Common/Orders/OKXOrderProperties.cs`
- 创建: `Lean/Tests/Common/Orders/OKXOrderPropertiesTests.cs`

---

### Task 16.4: 更新 symbol-properties-database.csv

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 16.3

**检查清单:**
- [ ] 打开 `Lean/Data/symbol-properties/symbol-properties-database.csv`
- [ ] 添加 OKX 现货交易对（前 100 个）
- [ ] 添加 OKX 永续合约（前 50 个）
- [ ] 设置正确的 lot size, tick size, min order size
- [ ] 验证格式正确

**验证标准:**
- CSV 格式正确
- 所有必需字段填写
- 数据准确

**涉及文件:**
- 修改: `Lean/Data/symbol-properties/symbol-properties-database.csv`

---

### Task 16.5: 创建回归算法 (现货)

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 16.4

**检查清单:**
- [ ] 创建 `Lean/Algorithm.CSharp/OKXSpotRegressionAlgorithm.cs`
- [ ] 继承 `QCAlgorithm`
- [ ] 设置 Brokerage 为 OKX
- [ ] 订阅 BTCUSDT
- [ ] 下市价单和限价单
- [ ] 验证订单执行
- [ ] 运行回归测试

**验证标准:**
- 算法运行成功
- 订单正常执行
- 回归测试通过

**涉及文件:**
- 创建: `Lean/Algorithm.CSharp/OKXSpotRegressionAlgorithm.cs`

---

### Task 16.6: 创建回归算法 (期货)

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 16.5

**检查清单:**
- [ ] 创建 `Lean/Algorithm.CSharp/OKXFuturesRegressionAlgorithm.cs`
- [ ] 继承 `QCAlgorithm`
- [ ] 设置 Brokerage 为 OKX
- [ ] 订阅 BTCUSDT 永续合约
- [ ] 下多空订单
- [ ] 验证订单执行
- [ ] 运行回归测试

**验证标准:**
- 算法运行成功
- 期货订单正常执行
- 回归测试通过

**涉及文件:**
- 创建: `Lean/Algorithm.CSharp/OKXFuturesRegressionAlgorithm.cs`

---

### Task 16.7: 创建 Brokerage 集成测试

- [ ] **状态:** 待完成
- **预计时间:** 3 小时
- **依赖:** Task 16.6

**检查清单:**
- [ ] 创建 `Lean/Tests/Brokerages/OKXBrokerageTests.cs`
- [ ] 测试 Connect/Disconnect
- [ ] 测试 PlaceOrder/CancelOrder
- [ ] 测试 GetAccountHoldings
- [ ] 测试 GetCashBalance
- [ ] 测试 GetOpenOrders
- [ ] 测试 GetHistory

**验证标准:**
- 所有集成测试通过
- Brokerage 功能正常
- 测试覆盖率 > 80%

**涉及文件:**
- 创建: `Lean/Tests/Brokerages/OKXBrokerageTests.cs`

---

### Task 16.8: 创建 DataQueueHandler 测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 16.7

**检查清单:**
- [ ] 创建 `Lean/Tests/Brokerages/OKXDataQueueHandlerTests.cs`
- [ ] 测试 Subscribe/Unsubscribe
- [ ] 测试 Quote 数据接收
- [ ] 测试 Trade 数据接收
- [ ] 测试连接池管理

**验证标准:**
- 所有 DataQueueHandler 测试通过
- 市场数据正常接收
- 测试通过

**涉及文件:**
- 创建: `Lean/Tests/Brokerages/OKXDataQueueHandlerTests.cs`

---

### Task 16.9: 创建 SymbolMapper 测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 16.8

**检查清单:**
- [ ] 创建 `Lean/Tests/Brokerages/OKXSymbolMapperTests.cs`
- [ ] 测试 LEAN → OKX 转换
- [ ] 测试 OKX → LEAN 转换
- [ ] 测试双向转换可逆性
- [ ] 测试所有 SecurityType

**验证标准:**
- 所有 SymbolMapper 测试通过
- 转换准确
- 测试通过

**涉及文件:**
- 创建: `Lean/Tests/Brokerages/OKXSymbolMapperTests.cs`

---

### Task 16.10: 创建历史数据测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 16.9

**检查清单:**
- [ ] 创建 `Lean/Tests/Brokerages/OKXHistoryTests.cs`
- [ ] 测试获取 TradeBar 历史数据
- [ ] 测试获取 Trade Tick 历史数据
- [ ] 测试不同分辨率
- [ ] 验证数据完整性

**验证标准:**
- 历史数据正确获取
- 所有分辨率都支持
- 测试通过

**涉及文件:**
- 创建: `Lean/Tests/Brokerages/OKXHistoryTests.cs`

---

### Task 16.11: 添加 Brokerage 配置示例

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 16.10

**检查清单:**
- [ ] 创建 `Lean/Launcher/config-okx.json`
- [ ] 配置 OKX brokerage
- [ ] 添加 API 凭证占位符
- [ ] 添加注释说明
- [ ] 验证配置格式

**验证标准:**
- 配置文件格式正确
- 所有必需字段存在
- 注释清晰

**涉及文件:**
- 创建: `Lean/Launcher/config-okx.json`

---

### Task 16.12: 更新 LEAN 文档

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 16.11

**检查清单:**
- [ ] 更新 `Lean/readme.md` 添加 OKX 支持
- [ ] 创建 `Lean/Documentation/Brokerages/OKX.md`
- [ ] 添加设置说明
- [ ] 添加配置示例
- [ ] 添加常见问题

**验证标准:**
- 文档完整
- 说明清晰
- 格式正确

**涉及文件:**
- 修改: `Lean/readme.md`
- 创建: `Lean/Documentation/Brokerages/OKX.md`

---

### Task 16.13: 提交 LEAN PR 准备

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 16.12

**检查清单:**
- [ ] 验证所有测试通过
- [ ] 验证代码风格符合 LEAN 规范
- [ ] 创建 PR 描述
- [ ] 列出所有更改
- [ ] 添加截图/示例输出
- [ ] 签署 CLA

**验证标准:**
- 所有测试通过
- 代码风格正确
- PR 描述完整

**涉及文件:**
- 修改: 所有 LEAN 文件

---

### Task 16.14: LEAN 集成测试运行

- [ ] **状态:** 待完成
- **预计时间:** 3 小时
- **依赖:** Task 16.13

**检查清单:**
- [ ] 运行所有单元测试
- [ ] 运行所有集成测试
- [ ] 运行回归算法
- [ ] 验证实时交易（小额）
- [ ] 验证市场数据订阅
- [ ] 验证历史数据获取
- [ ] 收集性能指标

**验证标准:**
- 所有测试通过
- 实时交易正常
- 性能满足要求
- 无内存泄漏

**涉及文件:**
- 测试: 所有 LEAN 测试文件

---

## Phase 17: LEAN CLI 集成

**总任务数:** 6 个
**预计工时:** 6 小时
**依赖:** Phase 16

### Task 17.1: 更新 modules.json

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 16.14

**检查清单:**
- [ ] 打开 LEAN CLI 仓库
- [ ] 修改 `lean/components/config/modules.json`
- [ ] 添加 OKX 模块定义
- [ ] 设置 product-id: 184 (假设值)
- [ ] 添加配置项: api-key, api-secret, passphrase, environment
- [ ] 验证 JSON 格式

**验证标准:**
- JSON 格式正确
- 模块定义完整
- 配置项正确

**涉及文件:**
- 修改: `lean-cli/lean/components/config/modules.json`

---

### Task 17.2: 测试 lean cloud push

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 17.1

**检查清单:**
- [ ] 安装 LEAN CLI
- [ ] 创建测试项目
- [ ] 配置 OKX brokerage
- [ ] 运行 `lean cloud push`
- [ ] 验证项目上传成功

**验证标准:**
- 项目上传成功
- 配置正确同步
- 无错误

**涉及文件:**
- 测试: LEAN CLI 功能

---

### Task 17.3: 测试 lean live deploy

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 17.2

**检查清单:**
- [ ] 运行 `lean live deploy`
- [ ] 选择 OKX brokerage
- [ ] 输入 API 凭证
- [ ] 验证实时交易启动
- [ ] 监控运行状态

**验证标准:**
- 实时交易启动成功
- 凭证正确传递
- 算法正常运行

**涉及文件:**
- 测试: LEAN CLI 实时交易功能

---

### Task 17.4: 测试 lean backtest

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 17.3

**检查清单:**
- [ ] 运行 `lean backtest`
- [ ] 选择 OKX 项目
- [ ] 验证回测运行
- [ ] 检查结果输出

**验证标准:**
- 回测运行成功
- 结果正确
- 无错误

**涉及文件:**
- 测试: LEAN CLI 回测功能

---

### Task 17.5: 创建 CLI 文档

- [ ] **状态:** 待完成
- **预计时间:** 1 小时
- **依赖:** Task 17.4

**检查清单:**
- [ ] 创建 OKX CLI 使用文档
- [ ] 添加配置说明
- [ ] 添加命令示例
- [ ] 添加常见问题
- [ ] 提交到 LEAN CLI 仓库

**验证标准:**
- 文档完整
- 示例可运行
- 格式正确

**涉及文件:**
- 创建: `lean-cli/docs/brokerages/okx.md`

---

### Task 17.6: CLI 集成测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 17.5

**检查清单:**
- [ ] 测试完整工作流: create → configure → backtest → deploy
- [ ] 测试错误处理（无效凭证等）
- [ ] 验证日志输出
- [ ] 验证配置持久化

**验证标准:**
- 完整工作流正常
- 错误正确处理
- 测试通过

**涉及文件:**
- 测试: 所有 CLI 功能

---

## Phase 18: 手动测试

**总任务数:** 17 个
**预计工时:** 24 小时
**依赖:** Phase 17

### Task 18.1: 基础功能测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 17.6

**检查清单:**
- [ ] 测试连接/断开
- [ ] 测试账户模式检测
- [ ] 测试获取余额
- [ ] 测试获取持仓
- [ ] 测试符号转换
- [ ] 记录测试结果

**验证标准:**
- 所有基础功能正常
- 无错误日志
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/BasicFunctionalityTests.md`

---

### Task 18.2: 现货订单测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.1

**检查清单:**
- [ ] 测试现货市价买单
- [ ] 测试现货限价卖单
- [ ] 测试订单查询
- [ ] 测试订单撤销
- [ ] 测试订单修改
- [ ] 验证费用计算
- [ ] 记录测试结果

**验证标准:**
- 所有订单类型正常
- 费用计算准确
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/SpotOrderTests.md`

---

### Task 18.3: 期货订单测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.2

**检查清单:**
- [ ] 测试永续合约做多
- [ ] 测试永续合约做空
- [ ] 测试平仓
- [ ] 测试止损单
- [ ] 验证持仓更新
- [ ] 验证未实现盈亏
- [ ] 记录测试结果

**验证标准:**
- 所有期货操作正常
- 持仓数据准确
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/FuturesOrderTests.md`

---

### Task 18.4: 市场数据测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.3

**检查清单:**
- [ ] 测试 Quote 数据订阅
- [ ] 测试 Trade 数据订阅
- [ ] 测试 Ticker 数据订阅
- [ ] 测试订单簿数据
- [ ] 验证数据实时性（延迟 < 100ms）
- [ ] 测试多符号订阅（100+）
- [ ] 记录测试结果

**验证标准:**
- 所有数据类型正常
- 延迟满足要求
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/MarketDataTests.md`

---

### Task 18.5: 历史数据测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 18.4

**检查清单:**
- [ ] 测试获取 1 周分钟数据
- [ ] 测试获取 1 个月小时数据
- [ ] 测试获取 1 年日线数据
- [ ] 验证数据完整性
- [ ] 验证数据准确性（对比 OKX 网站）
- [ ] 记录测试结果

**验证标准:**
- 历史数据正确获取
- 数据完整且准确
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/HistoricalDataTests.md`

---

### Task 18.6: 订单簿测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.5

**检查清单:**
- [ ] 测试订单簿初始化（snapshot）
- [ ] 测试增量更新
- [ ] 测试 checksum 验证
- [ ] 测试 sequence gap 恢复
- [ ] 验证最优价格准确性
- [ ] 测试多订单簿并发
- [ ] 记录测试结果

**验证标准:**
- 订单簿功能正常
- 所有验证机制工作
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/OrderBookTests.md`

---

### Task 18.7: 订单更新测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 18.6

**检查清单:**
- [ ] 测试订单提交事件
- [ ] 测试部分成交事件
- [ ] 测试完全成交事件
- [ ] 测试订单取消事件
- [ ] 验证事件顺序
- [ ] 验证费用计算
- [ ] 记录测试结果

**验证标准:**
- 所有订单事件正常触发
- 顺序正确
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/OrderUpdateTests.md`

---

### Task 18.8: 连接稳定性测试（1 小时）

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 18.7

**检查清单:**
- [ ] 保持连接 1 小时
- [ ] 监控 ping/pong
- [ ] 监控消息接收
- [ ] 验证无断线
- [ ] 记录测试结果

**验证标准:**
- 连接稳定
- 无异常断线
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/ConnectionStabilityTests.md`

---

### Task 18.9: 连接稳定性测试（8 小时）

- [ ] **状态:** 待完成
- **预计时间:** 1 小时（设置后台运行）
- **依赖:** Task 18.8

**检查清单:**
- [ ] 启动后台运行（8 小时）
- [ ] 设置日志记录
- [ ] 监控断线和重连
- [ ] 验证数据流连续性
- [ ] 分析日志
- [ ] 记录测试结果

**验证标准:**
- 8 小时稳定运行
- 重连机制正常（如有断线）
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/LongRunStabilityTests.md`

---

### Task 18.10: 周末测试

- [ ] **状态:** 待完成
- **预计时间:** 1 小时（设置 + 分析）
- **依赖:** Task 18.9

**检查清单:**
- [ ] 周五晚启动连接
- [ ] 保持连接整个周末
- [ ] 监控周末市场数据
- [ ] 周一早晨检查状态
- [ ] 分析日志
- [ ] 记录测试结果

**验证标准:**
- 周末连接稳定
- 市场数据正常（如果 OKX 周末交易）
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/WeekendTests.md`

---

### Task 18.11: 速率限制测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 18.10

**检查清单:**
- [ ] 快速下 50 个订单
- [ ] 验证速率限制生效
- [ ] 验证无 429 错误
- [ ] 测试不同端点速率限制
- [ ] 记录测试结果

**验证标准:**
- 速率限制正常工作
- 无 API 错误
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/RateLimitTests.md`

---

### Task 18.12: 错误处理测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.11

**检查清单:**
- [ ] 测试无效 API 凭证
- [ ] 测试网络断开
- [ ] 测试无效订单（数量过大等）
- [ ] 测试不支持的交易对
- [ ] 验证错误消息清晰
- [ ] 验证恢复机制
- [ ] 记录测试结果

**验证标准:**
- 所有错误正确处理
- 错误消息清晰
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/ErrorHandlingTests.md`

---

### Task 18.13: 并发测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 18.12

**检查清单:**
- [ ] 同时订阅 200 个符号
- [ ] 同时下 20 个订单
- [ ] 同时管理 50 个订单簿
- [ ] 监控 CPU 和内存使用
- [ ] 验证无数据竞争
- [ ] 记录测试结果

**验证标准:**
- 并发操作正常
- 性能满足要求
- 无内存泄漏
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/ConcurrencyTests.md`

---

### Task 18.14: 内存泄漏测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.13

**检查清单:**
- [ ] 运行 4 小时持续测试
- [ ] 监控内存使用
- [ ] 进行多次订阅/取消订阅循环
- [ ] 进行多次下单/撤单循环
- [ ] 使用内存分析工具
- [ ] 验证无内存泄漏
- [ ] 记录测试结果

**验证标准:**
- 内存使用稳定
- 无明显泄漏
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/MemoryLeakTests.md`

---

### Task 18.15: 性能基准测试

- [ ] **状态:** 待完成
- **预计时间:** 1.5 小时
- **依赖:** Task 18.14

**检查清单:**
- [ ] 测试订单延迟（下单到确认）
- [ ] 测试市场数据延迟（交易所 → 本地）
- [ ] 测试订单簿更新速率
- [ ] 测试消息处理吞吐量
- [ ] 与其他券商对比（Gate, Binance）
- [ ] 记录基准数据

**验证标准:**
- 性能满足要求
- 延迟 < 100ms
- 吞吐量 > 1000 msg/s
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/PerformanceBenchmarks.md`

---

### Task 18.16: Edge Case 测试

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.15

**检查清单:**
- [ ] 测试极小订单（最小数量）
- [ ] 测试极大订单（接近持仓限制）
- [ ] 测试价格精度边界
- [ ] 测试多账户模式切换
- [ ] 测试交易所维护期间行为
- [ ] 记录测试结果

**验证标准:**
- 所有边界情况正确处理
- 无崩溃或异常
- 测试结果记录

**涉及文件:**
- 创建: `TestResults/EdgeCaseTests.md`

---

### Task 18.17: 完整测试报告

- [ ] **状态:** 待完成
- **预计时间:** 2 小时
- **依赖:** Task 18.16

**检查清单:**
- [ ] 汇总所有测试结果
- [ ] 创建测试覆盖率报告
- [ ] 列出已知问题和限制
- [ ] 创建性能对比表
- [ ] 添加测试截图
- [ ] 编写总结和建议
- [ ] 审查和完善报告

**验证标准:**
- 测试报告完整
- 覆盖所有功能
- 问题清晰记录
- 格式专业

**涉及文件:**
- 创建: `TestResults/FINAL_TEST_REPORT.md`

---

## 总结

### 任务统计

**总任务数:** 183 个
**总预计工时:** ~380 小时

### 任务完成情况

```
Phase 0:  项目设置                [ ] 0/8   (0%)
Phase 1:  REST API 基础           [ ] 0/12  (0%)
Phase 2:  符号映射器               [ ] 0/5   (0%)
Phase 3:  GetHistory 实现         [ ] 0/8   (0%)
Phase 4:  账户方法                 [ ] 0/9   (0%)
Phase 5:  订单管理-读取            [ ] 0/6   (0%)
Phase 6:  订单管理-写入            [ ] 0/13  (0%)
Phase 7:  WebSocket 基础          [ ] 0/11  (0%)
Phase 8:  WebSocket 消息路由      [ ] 0/8   (0%)
Phase 9:  市场数据订阅             [ ] 0/12  (0%)
Phase 10: 订单簿管理               [ ] 0/15  (0%)
Phase 11: 订单更新                 [ ] 0/9   (0%)
Phase 12: 重连逻辑                 [ ] 0/10  (0%)
Phase 13: Brokerage Factory       [ ] 0/8   (0%)
Phase 14: 高级功能                 [ ] 0/12  (0%)
Phase 15: 文档                     [ ] 0/10  (0%)
Phase 16: LEAN 集成               [ ] 0/14  (0%)
Phase 17: LEAN CLI 集成           [ ] 0/6   (0%)
Phase 18: 手动测试                 [ ] 0/17  (0%)
---------------------------------------------------
总计:                              [ ] 0/183 (0%)
```

### 阶段进度概览

| 阶段 | 名称 | 任务数 | 工时 | 状态 | 完成率 |
|------|------|--------|------|------|--------|
| 0 | 项目设置 | 8 | 8h | 待开始 | 0% |
| 1 | REST API 基础 | 12 | 18h | 待开始 | 0% |
| 2 | 符号映射器 | 5 | 6h | 待开始 | 0% |
| 3 | GetHistory 实现 | 8 | 16h | 待开始 | 0% |
| 4 | 账户方法 | 9 | 14h | 待开始 | 0% |
| 5 | 订单管理-读取 | 6 | 8h | 待开始 | 0% |
| 6 | 订单管理-写入 | 13 | 22h | 待开始 | 0% |
| 7 | WebSocket 基础 | 11 | 20h | 待开始 | 0% |
| 8 | WebSocket 消息路由 | 8 | 12h | 待开始 | 0% |
| 9 | 市场数据订阅 | 12 | 18h | 待开始 | 0% |
| 10 | 订单簿管理 | 15 | 20h | 待开始 | 0% |
| 11 | 订单更新 | 9 | 16h | 待开始 | 0% |
| 12 | 重连逻辑 | 10 | 12h | 待开始 | 0% |
| 13 | Brokerage Factory | 8 | 8h | 待开始 | 0% |
| 14 | 高级功能 | 12 | 16h | 待开始 | 0% |
| 15 | 文档 | 10 | 12h | 待开始 | 0% |
| 16 | LEAN 集成 | 14 | 22h | 待开始 | 0% |
| 17 | LEAN CLI 集成 | 6 | 6h | 待开始 | 0% |
| 18 | 手动测试 | 17 | 24h | 待开始 | 0% |
| **总计** | | **183** | **~380h** | | **0%** |

### 关键里程碑

1. **M1: 基础功能完成** (Phase 0-6, ~92h)
   - REST API 完全实现
   - 订单管理完成
   - 可以进行基本交易

2. **M2: 实时数据完成** (Phase 7-11, ~86h)
   - WebSocket 连接稳定
   - 市场数据实时接收
   - 订单更新实时推送

3. **M3: 生产就绪** (Phase 12-14, ~36h)
   - 重连机制完善
   - Brokerage 集成完成
   - 高级功能可用

4. **M4: 文档和测试** (Phase 15-18, ~64h)
   - 文档完整
   - LEAN 集成完成
   - 全面测试通过

### 使用说明

1. **任务状态更新:**
   - 开始任务时：将 `[ ]` 改为 `[>]`
   - 完成任务时：将 `[>]` 改为 `[x]`

2. **进度跟踪:**
   - 定期更新"任务完成情况"和"阶段进度概览"表格
   - 记录实际耗时与预计耗时的对比

3. **问题记录:**
   - 在对应任务下方记录遇到的问题
   - 标记阻塞任务并注明原因

4. **测试结果:**
   - 所有测试结果保存在 `TestResults/` 目录
   - 保持测试文档更新

---

**最后更新:** 2026-01-09
**文档版本:** 1.0
**状态:** 准备开始开发

