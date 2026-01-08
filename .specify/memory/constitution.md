# Lean.Brokerages.OKX Constitution

**项目：** OKX 券商插件开发
**方法论：** Spec-Driven Development
**创建日期：** 2026-01-09

本文档定义 Lean.Brokerages.OKX 项目的不可妥协原则。所有架构决策、代码实现和设计选择必须遵守这些原则。

---

## 1. 架构原则

### 1.1 核心架构决策

**关键简化：OKX API v5 完全统一**
- OKX API 所有工具类型共享统一端点（`/api/v5/trade/order`）
- 通过 `instId` 参数区分现货/期货（如 "BTC-USDT" vs "BTC-USDT-SWAP"）
- **因此不需要**像 Gate 那样的抽象基类 + 多个实现类
- 单一 `OKXBrokerage` 类处理所有工具类型

**架构对比：**
```
Gate.io（复杂架构）:
├── GateBaseBrokerage (abstract)
├── GateSpotBrokerage
├── GateFuturesBrokerage
├── GateCrossMarginBrokerage
└── GateUnifiedBrokerage

OKX（简化架构）:
└── OKXBrokerage (单一类，运行时检测账户模式)
```

### 1.2 必须遵守的架构规则

#### 基类继承
- ✅ **必须**继承 `BaseWebsocketsBrokerage`（LEAN 提供的 WebSocket 基类）
- ❌ **禁止**直接继承 `Brokerage` 基类（除非有特殊原因）

#### 部分类组织
- ✅ **必须**使用部分类（partial class）分离关注点
- ✅ **必须**按功能模块拆分为以下文件：
  - `OKXBrokerage.cs` - 核心类、初始化、连接管理
  - `OKXBrokerage.Orders.cs` - 订单管理（PlaceOrder, CancelOrder, UpdateOrder）
  - `OKXBrokerage.Messaging.cs` - WebSocket 消息路由和处理
  - `OKXBrokerage.DataQueueHandler.cs` - 市场数据订阅（IDataQueueHandler）
  - `OKXBrokerage.OrderBook.cs` - 订单簿管理和缓存
  - `OKXBrokerage.History.cs` - 历史数据获取（GetHistory）
  - `OKXBrokerage.Utility.cs` - 辅助方法和转换函数

#### 单一实现类原则
- ✅ **必须**使用单一 `OKXBrokerage` 类（不分 Spot/Futures）
- ✅ **必须**运行时检测账户模式（`GET /api/v5/account/config`）
- ✅ **必须**根据账户模式调整行为（主要影响 GetCashBalance 和 GetAccountHoldings）
- ❌ **禁止**创建 OKXSpotBrokerage、OKXFuturesBrokerage 等多个类

#### REST API 客户端设计
- ✅ **必须**使用单一 `OKXRestApiClient` 类
- ✅ **必须**所有端点方法接受 `instId` 参数区分工具类型
- ❌ **禁止**创建 OKXSpotRestApiClient、OKXFuturesRestApiClient 等多个类
- ❌ **禁止**使用 if/else 判断工具类型后调用不同端点（OKX API 已统一）

---

## 2. 代码风格层级

### 2.1 参考实现优先级

当需要实现某个功能时，按以下优先级参考现有实现：

**1. 主要参考：Lean.Brokerages.Gate**
- 路径：`C:\Users\Jagger\Documents\Code\Lean.Brokerages.Gate`
- 使用场景：所有通用功能（WebSocket、消息路由、订单簿、数据队列）
- 策略：**复制-适配**（Copy-Paste-Adapt）
  - 复制 Gate 的代码结构
  - 重命名 Gate → OKX
  - 适配 OKX 特定的 API 差异
  - 简化多类设计为单类设计

**2. 次要参考：Lean.Brokerages.Binance**
- 路径：`C:\Users\Jagger\Documents\Code\Lean.Brokerages.Binance`
- 使用场景：高级功能（多 WebSocket 连接池、保活机制、配对匹配）
- 策略：参考设计模式和算法，不直接复制代码

**3. 回退参考：Lean.Brokerages.Template**
- 路径：Gate README.md 中的 100+ 条要求
- 使用场景：Gate 和 Binance 都没有的功能
- 策略：从头实现，遵循 LEAN 的接口规范

### 2.2 编码规范

#### 命名约定
- **类名**：PascalCase（如 `OKXBrokerage`、`OKXRestApiClient`）
- **方法名**：PascalCase（如 `PlaceOrder`、`GetAccountBalance`）
- **私有字段**：_camelCase（如 `_apiClient`、`_orderCache`）
- **常量**：PascalCase（如 `MaxConnectionsPerChannel`）

#### 文件组织
```
QuantConnect.OKXBrokerage/
├── OKXBrokerage.cs (及部分类文件)
├── OKXRestApiClient.cs
├── OKXSymbolMapper.cs
├── OKXOrderProperties.cs
├── OKXWebSocketWrapper.cs
├── OKXEnvironment.cs
├── Messages/           (消息模型)
├── Converters/         (JSON + Domain 转换器)
└── Models/             (可选，复杂数据结构)
```

#### 代码注释
- ✅ **必须**为所有公共类和方法添加 XML 文档注释
- ✅ **必须**在复杂逻辑处添加解释性注释
- ❌ **禁止**添加显而易见的注释（如 `// Get balance` 在 GetBalance() 方法上）
- ✅ **必须**在与 Gate 不同的地方添加注释说明原因

---

## 3. 消息模型哲学

### 3.1 面向业务设计（非 API 镜像）

**原则：** 消息模型应反映业务需求，而非 API 响应结构。

**示例：OKX API 返回字符串数字**
```csharp
// ❌ 错误：镜像 API 结构
public class Balance
{
    [JsonProperty("availBal")]
    public string AvailableBalance { get; set; }  // API 返回字符串
}

// ✅ 正确：面向业务设计
public class Balance
{
    [JsonProperty("availBal")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal AvailableBalance { get; set; }  // 业务使用 decimal
}
```

### 3.2 数据类型规则

#### 必须遵守的类型约定
- ✅ **必须**数值字段使用 `decimal`（价格、数量、余额等）
- ✅ **必须**时间字段使用 `DateTime`（UTC）
- ✅ **必须**枚举使用 C# enum 类型（如 `OrderType`、`OrderStatus`）
- ❌ **禁止**在业务模型中使用 `string` 表示数值
- ❌ **禁止**在业务模型中使用 `long` 表示时间戳（转换为 DateTime）

#### 属性命名
- ✅ **必须**使用可读的属性名（如 `AvailableBalance` 而非 API 的 `availBal`）
- ✅ **必须**使用 `[JsonProperty]` 特性映射 API 字段名
- ✅ **必须**使用完整单词（避免缩写，除非是行业标准如 `ID`）

### 3.3 转换器责任

**原则：** 所有数据解析和转换在 Converter 中完成，业务代码只处理强类型对象。

#### JSON Converter（解析 API 响应）
- ✅ **必须**放在 `Converters/` 目录
- ✅ **必须**命名为 `<MessageType>Converter`（如 `BalanceConverter`）
- ✅ **必须**处理所有字符串到强类型的转换
- ✅ **必须**处理解析失败（返回 null 或默认值，记录警告）
- ❌ **禁止**在 Converter 中访问外部依赖（如数据库、API）

#### Domain Converter（转换为 LEAN 类型）
- ✅ **必须**提供扩展方法（如 `ToCashAmount()`、`ToHolding()`）
- ✅ **必须**返回可空类型（如 `CashAmount?`）表示转换可能失败
- ✅ **必须**在转换失败时返回 null（不抛异常）
- ✅ **必须**记录转换失败的警告日志

**示例：**
```csharp
// JSON Converter
public class BalanceConverter : JsonConverter<Balance>
{
    public override Balance ReadJson(...)
    {
        // 解析 JSON → Balance 对象
        // 处理字符串 → decimal 转换
    }
}

// Domain Converter
public static class BalanceExtensions
{
    public static CashAmount? ToCashAmount(this Balance balance)
    {
        if (balance == null || balance.Currency == null)
            return null;

        return new CashAmount(balance.AvailableBalance, balance.Currency);
    }
}
```

---

## 4. 性能与可靠性

### 4.1 速率限制实现（基于 OKX 官方文档）

**原则：** 必须严格遵守 OKX API 速率限制，避免触发限流。

#### REST API 速率限制
```csharp
// ✅ 必须实现的 RateGate 实例
private readonly RateGate _orderRateLimiter = new RateGate(1000, TimeSpan.FromSeconds(2));     // 订单操作
private readonly RateGate _accountRateLimiter = new RateGate(10, TimeSpan.FromSeconds(2));     // 账户查询
private readonly RateGate _instrumentRateLimiter = new RateGate(20, TimeSpan.FromSeconds(2));  // 工具信息
```

**关键规则：**
- ✅ **必须**为不同类型的端点使用独立的 RateGate
- ✅ **必须**下单/撤单/改单使用相同的 RateGate（OKX 共享限制）
- ⚠️ **警告**：REST 和 WebSocket 订单操作共享速率限制！
- ✅ **必须**在速率限制触发时记录警告日志
- ✅ **必须**实现指数退避重试（仅对幂等操作）

#### WebSocket 速率限制
```csharp
// ✅ 必须实现的限制
private const int MaxConnectionsPerChannel = 30;                                     // 每通道最多 30 连接
private readonly RateGate _subscriptionRateLimiter = new RateGate(480, TimeSpan.FromHours(1));  // 订阅操作
```

**关键规则：**
- ✅ **必须**跟踪每个通道的连接数（不超过 30）
- ✅ **必须**限制订阅速率（480 次/小时）
- ✅ **必须**订阅消息总大小 < 64 KB
- ✅ **必须**实现保活机制（下一节）

### 4.2 WebSocket 保活机制（OKX 特殊要求）

**原则：** OKX WebSocket 30 秒无消息自动断开，必须实现保活。

#### 必须实现的保活逻辑
```csharp
// ✅ 必须实现
private readonly Timer _keepAliveTimer;
private const int KeepAliveIntervalSeconds = 20;  // < 30 秒

private void InitializeKeepAlive()
{
    _keepAliveTimer = new Timer(KeepAliveIntervalSeconds * 1000);
    _keepAliveTimer.Elapsed += (s, e) => SendPing();
    _keepAliveTimer.Start();
}

private void SendPing()
{
    // ✅ 发送字符串 "ping"
    WebSocket.Send("ping");

    // ✅ 期待 "pong" 响应
    // ✅ 如果 30 秒内没有收到 pong，触发重连
}
```

**关键规则：**
- ✅ **必须**保活间隔 < 30 秒（推荐 20-25 秒）
- ✅ **必须**发送字符串 `"ping"`（不是 JSON）
- ✅ **必须**期待响应 `"pong"`（不是 JSON）
- ✅ **必须**在任何消息到达时重置定时器
- ✅ **必须**在 pong 超时时触发重连

### 4.3 连接池管理

**原则：** 每通道最多 30 个连接，根据订阅数量动态创建。

#### 连接池策略
```csharp
// ✅ 必须实现
private readonly Dictionary<string, List<WebSocketConnection>> _connectionsByChannel;
private const int MaxSymbolsPerConnection = 100;  // 根据实际测试调整

private WebSocketConnection GetOrCreateConnection(string channel)
{
    if (!_connectionsByChannel.ContainsKey(channel))
        _connectionsByChannel[channel] = new List<WebSocketConnection>();

    var connections = _connectionsByChannel[channel];

    // ✅ 查找未满的连接
    var connection = connections.FirstOrDefault(c => c.SubscriptionCount < MaxSymbolsPerConnection);

    // ✅ 如果所有连接已满且未达到上限，创建新连接
    if (connection == null && connections.Count < MaxConnectionsPerChannel)
    {
        connection = CreateNewConnection(channel);
        connections.Add(connection);
    }

    return connection;
}
```

**关键规则：**
- ✅ **必须**每个通道独立管理连接池
- ✅ **必须**单个连接订阅数量 < MaxSymbolsPerConnection
- ✅ **必须**总连接数 ≤ 30（每通道）
- ✅ **必须**连接达到上限时拒绝新订阅（记录错误）

### 4.4 缓存策略

#### RiskLimitTiers 缓存（仅期货）
```csharp
// ✅ 参考 Gate 实现
private readonly ConcurrentDictionary<string, (List<RiskLimitTier> Tiers, DateTime CachedAt)> _tierCache;
private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);
```

**关键规则：**
- ✅ **必须**缓存期货风险限额数据 24 小时
- ✅ **必须**使用 ConcurrentDictionary 保证线程安全
- ✅ **必须**在缓存过期后自动刷新
- ✅ **必须**提供手动清除缓存的方法（测试用）

#### 订单簿缓存
```csharp
// ✅ 必须实现
private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks;
```

**关键规则：**
- ✅ **必须**使用 LEAN 提供的 `DefaultOrderBook` 类
- ✅ **必须**增量更新（不每次重新获取）
- ✅ **必须**在订单簿不可用时回退到 REST API
- ✅ **必须**处理订单簿快照和增量更新

### 4.5 重连与容错

**原则：** 网络问题在所难免，必须实现健壮的重连机制。

#### 指数退避重连
```csharp
// ✅ 必须实现
private int _reconnectAttempts = 0;
private readonly int[] _reconnectDelays = { 1000, 2000, 5000, 10000 };  // 毫秒

private void ScheduleReconnection()
{
    var delay = _reconnectDelays[Math.Min(_reconnectAttempts, _reconnectDelays.Length - 1)];
    _reconnectAttempts++;

    Task.Delay(delay, _cancellationToken).ContinueWith(_ =>
    {
        if (!_cancellationToken.IsCancellationRequested)
            ReconnectAndResubscribe();
    });
}
```

**关键规则：**
- ✅ **必须**使用指数退避（1s, 2s, 5s, 10s）
- ✅ **必须**在成功连接后重置重试计数
- ✅ **必须**重连后恢复所有订阅
- ✅ **必须**私有通道重连后重新认证（login）
- ✅ **必须**支持取消重连（CancellationToken）

---

## 5. 测试要求

### 5.1 测试覆盖率目标

- ✅ **必须**整体测试覆盖率 > 80%
- ✅ **必须**核心功能（订单、账户）覆盖率 > 90%
- ✅ **必须**每个公共方法至少有一个测试用例

### 5.2 测试类型

#### 单元测试（持续运行）
```csharp
// ✅ 必须为每个主要类创建测试
[TestFixture]
public class OKXRestApiClientTests { }

[TestFixture]
public class OKXSymbolMapperTests { }

[TestFixture]
public class OKXBrokerageOrderTests { }
```

**关键规则：**
- ✅ **必须**使用 Mock 模拟外部依赖（API、WebSocket）
- ✅ **必须**测试边界条件（null、空、极值）
- ✅ **必须**测试错误处理（API 失败、网络断开）
- ✅ **必须**每个 Converter 有独立的测试类

#### 集成测试（每周运行）
```csharp
// ✅ 必须有真实 API 集成测试
[TestFixture]
[Category("Integration")]
public class OKXBrokerageIntegrationTests
{
    // 需要 config.json 中的测试网凭证
}
```

**关键规则：**
- ✅ **必须**使用 OKX 测试网（非生产环境）
- ✅ **必须**测试完整订单生命周期
- ✅ **必须**测试 WebSocket 连接和订阅
- ✅ **必须**测试重连逻辑
- ❌ **禁止**在 CI 中运行（需要凭证）

#### 手动测试（Phase 18）
- ✅ **必须**长时间运行测试（过夜、周末）
- ✅ **必须**高容量订阅测试（100, 500, 1000 符号）
- ✅ **必须**网络断开恢复测试
- ✅ **必须**性能和内存泄漏测试

### 5.3 测试驱动开发（TDD）

**原则：** 先写测试，再写实现。

**工作流程：**
1. 🔴 编写失败的测试（Red）
2. 🟢 编写最小实现使测试通过（Green）
3. 🔵 重构代码改进设计（Refactor）
4. 🔁 重复

**示例：**
```csharp
// 步骤 1: 写测试（失败）
[Test]
public void PlaceOrder_WithValidParameters_ReturnsSuccess()
{
    var order = new MarketOrder(...);
    var result = _brokerage.PlaceOrder(order);
    Assert.IsTrue(result);
}

// 步骤 2: 写实现（通过）
public bool PlaceOrder(Order order)
{
    // 最小实现
    return true;
}

// 步骤 3: 重构（改进）
public bool PlaceOrder(Order order)
{
    ValidateOrder(order);
    var response = _apiClient.PlaceOrder(ConvertToApiOrder(order));
    return response.IsSuccess;
}
```

---

## 6. 安全与配置

### 6.1 凭证管理

**原则：** 绝不提交敏感信息到代码仓库。

#### 必须遵守的安全规则
- ❌ **禁止**在代码中硬编码 API key、secret、passphrase
- ❌ **禁止**提交包含真实凭证的 `config.json`
- ✅ **必须**使用 `config.json` 存储凭证（加入 .gitignore）
- ✅ **必须**提供 `config.json.example` 作为模板

**config.json 结构：**
```json
{
  "okx-api-key": "your-api-key",
  "okx-api-secret": "your-api-secret",
  "okx-passphrase": "your-passphrase",
  "okx-api-url": "https://www.okx.com",
  "okx-websocket-url": "wss://ws.okx.com:8443/ws/v5",
  "okx-environment": "production"
}
```

### 6.2 环境管理

#### 环境枚举
```csharp
// ✅ 必须支持生产和测试环境
public enum OKXEnvironment
{
    Production,
    Demo  // OKX 测试网
}
```

**关键规则：**
- ✅ **必须**支持切换生产/测试环境
- ✅ **必须**测试环境使用不同的 URL
- ✅ **必须**在日志中标注当前环境
- ✅ **必须**警告用户生产环境的风险

### 6.3 日志与监控

**原则：** 充分记录日志，便于调试和监控。

#### 日志级别使用
```csharp
// ✅ 正确的日志级别使用
Log.Trace("OKXBrokerage: Subscribing to {0}", symbol);           // 详细调试信息
Log.Debug("OKXBrokerage: Order placed, ID: {0}", orderId);       // 调试信息
Log.Info("OKXBrokerage: Connected to WebSocket");                 // 重要信息
Log.Warning("OKXBrokerage: Rate limit reached, waiting...");      // 警告
Log.Error("OKXBrokerage: Failed to place order: {0}", error);     // 错误
```

**关键规则：**
- ✅ **必须**记录所有 API 错误
- ✅ **必须**记录重连事件
- ✅ **必须**记录速率限制触发
- ❌ **禁止**记录敏感信息（API secret、用户余额）
- ❌ **禁止**在循环中使用 Info 级别（避免日志洪水）

---

## 7. 账户模式处理策略

### 7.1 运行时检测（不是编译时）

**原则：** OKX 有 4 种账户模式，运行时检测而非创建多个类。

#### 账户模式枚举
```csharp
// ✅ 必须实现
public enum OKXAccountMode
{
    Spot,                    // 仅现货
    Futures,                 // 仅期货
    MultiCurrencyMargin,     // 多币种保证金（推荐）
    PortfolioMargin          // 组合保证金
}
```

#### 初始化时检测
```csharp
// ✅ 必须在 Connect() 时检测
public override void Connect()
{
    // 1. 连接 WebSocket
    base.Connect();

    // 2. 查询账户模式
    var config = _apiClient.GetAccountConfig();
    _accountMode = ParseAccountMode(config.AccountLevel);

    // 3. 根据模式调整行为
    Log.Info($"OKXBrokerage: Account mode detected: {_accountMode}");
}
```

### 7.2 按模式调整行为

**关键规则：**
- ✅ **必须**`GetCashBalance()` 根据模式返回不同数据
- ✅ **必须**`GetAccountHoldings()` 根据模式返回现货/期货持仓
- ✅ **必须**在文档中推荐 Multi-currency margin mode
- ❌ **禁止**为不同模式创建不同的 Brokerage 类

---

## 8. 与 Gate 实现的关键差异

### 8.1 架构简化

| 特性 | Gate 实现 | OKX 实现（本项目） |
|-----|---------|-----------------|
| Brokerage 类数量 | 4 个（Spot/Futures/CrossMargin/Unified） | **1 个** |
| REST 客户端类数量 | 4 个 | **1 个** |
| 订单模型 | SpotOrder + FuturesOrder | **Order（统一）** |
| 端点 URL | /spot/orders vs /futures/usdt/orders | **/api/v5/trade/order（统一）** |

**原因：** OKX API v5 完全统一，不需要 Gate 的复杂架构。

### 8.2 WebSocket 差异

| 特性 | Gate | OKX（本项目） |
|-----|------|-----------|
| 保活机制 | 定期重连（24小时） | **强制保活（30秒超时）** |
| 连接限制 | 未明确 | **30 连接/通道** |
| 订阅速率 | 未明确 | **480 次/小时** |

**原因：** OKX 有严格的 30 秒超时，必须实现保活。

### 8.3 必须保留的 Gate 模式

以下 Gate 的设计模式**必须保留**：
- ✅ 部分类组织（.Orders.cs, .Messaging.cs 等）
- ✅ Converter 模式（JSON + Domain 分离）
- ✅ 消息模型面向业务设计
- ✅ 使用 `DefaultOrderBook`
- ✅ 使用 `BrokerageConcurrentMessageHandler`

---

## 9. 提交前检查清单

在提交任何代码前，必须通过以下检查：

### 9.1 代码质量
- [ ] 所有测试通过（`dotnet test`）
- [ ] 无编译警告（`dotnet build`）
- [ ] 代码覆盖率 > 80%
- [ ] 所有公共 API 有 XML 文档注释

### 9.2 架构遵守
- [ ] 使用单一 `OKXBrokerage` 类（非多个实现）
- [ ] 使用单一 `OKXRestApiClient` 类
- [ ] 消息模型使用 `decimal`（非 `string`）
- [ ] 解析在 Converter 中（非业务代码）

### 9.3 安全检查
- [ ] 未提交 API 凭证
- [ ] `config.json` 在 .gitignore 中
- [ ] 日志未包含敏感信息

### 9.4 性能检查
- [ ] 实现了所有必需的 RateGate
- [ ] 实现了 WebSocket 保活（30秒）
- [ ] 连接数 ≤ 30/通道

### 9.5 文档检查
- [ ] README.md 已更新
- [ ] CLAUDE.md 反映架构变化
- [ ] 重要决策已记录注释

---

## 10. 违反 Constitution 的后果

**本 Constitution 是项目的法律文件。任何违反都可能导致：**

1. **代码审查拒绝** - Pull Request 被拒绝
2. **重构要求** - 需要重写不符合规范的代码
3. **技术债务** - 未来维护成本增加

**例外处理：**
- 如果确实需要违反某条规则，必须：
  1. 在代码中添加 `// EXCEPTION:` 注释
  2. 说明违反原因
  3. 记录在 CLAUDE.md 的"架构决策"章节

---

## 附录：快速参考

### 必须使用的库/类
- `BaseWebsocketsBrokerage` - 基类
- `DefaultOrderBook` - 订单簿
- `RateGate` - 速率限制
- `BrokerageConcurrentMessageHandler` - 消息同步
- `EventBasedDataQueueHandlerSubscriptionManager` - 订阅管理

### 关键常量
```csharp
private const int MaxConnectionsPerChannel = 30;
private const int KeepAliveIntervalSeconds = 20;
private const int OrderRateLimitPer2Seconds = 1000;
private const int AccountRateLimitPer2Seconds = 10;
private const int InstrumentRateLimitPer2Seconds = 20;
```

### 关键文件路径
- Gate 参考：`C:\Users\Jagger\Documents\Code\Lean.Brokerages.Gate`
- Binance 参考：`C:\Users\Jagger\Documents\Code\Lean.Brokerages.Binance`
- 计划文件：`C:\Users\Jagger\.claude\plans\curried-yawning-cray.md`

---

**版本：** 1.0
**最后更新：** 2026-01-09
**状态：** 批准 ✅
