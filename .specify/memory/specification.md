# Lean.Brokerages.OKX Specification

**项目：** OKX 券商插件需求规格
**版本：** 1.0
**创建日期：** 2026-01-09
**状态：** ✅ Phase 1-8 Completed (2026-01-09)
**最后更新：** 2026-01-09

本文档详细说明 Lean.Brokerages.OKX 项目需要实现的功能、接口、数据结构和行为。

## 实现进度

**已完成：**
- ✅ Phase 1: 环境配置和基础设施
- ✅ Phase 2: Symbol Mapper (OKX v5 API)
- ✅ Phase 3: 历史数据查询
- ✅ Phase 4: 账户查询 REST API
- ✅ Phase 5: 订单管理 (PlaceOrder, UpdateOrder, CancelOrder)
- ✅ Phase 6: WebSocket 实时数据集成
- ✅ Phase 7: 账户管理功能
- ✅ Phase 8: BrokerageFactory 和配置集成

**测试状态：**
- REST API 测试: ✅ 全部通过
- WebSocket 测试: ✅ 15/15 通过
- 订单管理测试: ✅ 全部通过
- 账户管理测试: ✅ 7/7 通过
- Factory 测试: ✅ 9/9 通过

**核心功能完整性：** 100%
- REST API 集成: ✅
- WebSocket 实时数据: ✅
- 订单管理: ✅
- 账户管理: ✅
- 历史数据: ✅
- Factory 和配置: ✅

---

## 目录

1. [支持的市场与工具](#1-支持的市场与工具)
2. [架构规格](#2-架构规格)
3. [REST API 客户端规格](#3-rest-api-客户端规格)
4. [消息模型规格](#4-消息模型规格)
5. [Converter 规格](#5-converter-规格)
6. [接口实现需求](#6-接口实现需求)
7. [WebSocket 规格](#7-websocket-规格)
8. [订单管理规格](#8-订单管理规格)
9. [账户管理规格](#9-账户管理规格)
10. [历史数据规格](#10-历史数据规格)
11. [特殊功能规格](#11-特殊功能规格)
12. [错误处理规格](#12-错误处理规格)
13. [配置规格](#13-配置规格)
14. [测试规格](#14-测试规格)

---

## 1. 支持的市场与工具

### 1.1 证券类型（Security Types）

| LEAN Security Type | OKX Instrument Type | instId 格式 | 示例 | v1.0 支持 |
|-------------------|-------------------|------------|------|----------|
| Crypto (现货) | SPOT | BASE-QUOTE | BTC-USDT | ✅ |
| CryptoFuture (永续) | SWAP | BASE-QUOTE-SWAP | BTC-USDT-SWAP | ✅ |
| CryptoFuture (交割) | FUTURES | BASE-QUOTE-YYMMDD | BTC-USDT-250328 | ✅ |
| Option (期权) | OPTION | BASE-QUOTE-YYMMDD-STRIKE-C/P | BTC-USDT-250328-50000-C | ⏸️ v2.0 |

**注意：**
- v1.0 仅支持 SPOT、SWAP、FUTURES
- Option 支持推迟到 v2.0（复杂度较高）

### 1.2 订单类型（Order Types）

| LEAN Order Type | OKX ordType | 现货支持 | 期货支持 | 备注 |
|----------------|-------------|---------|---------|------|
| MarketOrder | market | ✅ | ✅ | 市价单 |
| LimitOrder | limit | ✅ | ✅ | 限价单 |
| StopMarketOrder | trigger + market | ✅ | ✅ | 止损市价 |
| StopLimitOrder | trigger + limit | ✅ | ✅ | 止损限价 |
| LimitOrder (PostOnly) | post_only | ✅ | ✅ | 只做 Maker |
| LimitOrder (IOC) | ioc | ✅ | ✅ | 立即成交或取消 |
| LimitOrder (FOK) | fok | ✅ | ✅ | 全部成交或取消 |

**OKX 特有参数：**
- `tdMode`: 交易模式
  - `cash` - 现货简单交易
  - `cross` - 全仓模式
  - `isolated` - 逐仓模式
- `reduceOnly`: 只减仓（仅期货）
- `clOrdId`: 客户端订单 ID（映射到 LEAN Order.Id）

### 1.3 时间有效性（Time In Force）

| LEAN TimeInForce | OKX 实现 | 说明 |
|-----------------|---------|------|
| GoodTilCanceled (GTC) | ordType: limit/market | 默认行为 |
| ImmediateOrCancel (IOC) | ordType: ioc | 立即成交 |
| FillOrKill (FOK) | ordType: fok | 全部成交 |
| Day | 不支持 | OKX 无此概念，使用 GTC |
| GoodTilDate | 不支持 | OKX 无此概念 |

### 1.4 支持的交易对

#### 现货（SPOT）
- 主要稳定币对：BTC-USDT, ETH-USDT, BNB-USDT 等
- 主流币对：BTC-USDC, ETH-BTC 等
- **要求：** Symbol Properties Database 需包含所有支持的交易对

#### 永续合约（SWAP）
- USDT 本位：BTC-USDT-SWAP, ETH-USDT-SWAP
- 币本位：BTC-USD-SWAP, ETH-USD-SWAP
- **v1.0：** 仅支持 USDT 本位（币本位推迟到 v2.0）

#### 交割合约（FUTURES）
- 格式：BTC-USDT-250328（2025年3月28日交割）
- 交割日期：每周五、季度末

---

## 2. 架构规格

### 2.1 类层次结构

```
OKXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
  ├── 实现接口
  │   ├── IBrokerage (订单和账户管理)
  │   ├── IDataQueueHandler (市场数据订阅)
  │   └── IDataQueueUniverseProvider (可选，返回空)
  │
  ├── 部分类文件
  │   ├── OKXBrokerage.cs (核心)
  │   ├── OKXBrokerage.Orders.cs (订单管理)
  │   ├── OKXBrokerage.Messaging.cs (WebSocket 消息)
  │   ├── OKXBrokerage.DataQueueHandler.cs (数据订阅)
  │   ├── OKXBrokerage.OrderBook.cs (订单簿)
  │   ├── OKXBrokerage.History.cs (历史数据)
  │   └── OKXBrokerage.Utility.cs (工具方法)
  │
  └── 依赖组件
      ├── OKXRestApiClient (REST API)
      ├── OKXSymbolMapper (符号映射)
      ├── OKXWebSocketWrapper (WebSocket 连接)
      ├── DefaultOrderBook (订单簿)
      └── EventBasedDataQueueHandlerSubscriptionManager (订阅管理)
```

### 2.2 账户模式处理

**运行时检测，非编译时区分**

```csharp
public class OKXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
{
    private OKXAccountMode _accountMode;  // 运行时检测

    public override void Connect()
    {
        // 1. 连接 WebSocket
        base.Connect();

        // 2. 查询账户模式
        var config = _apiClient.GetAccountConfig();
        _accountMode = ParseAccountMode(config.AccountLevel);

        // 3. 记录日志
        Log.Info($"OKXBrokerage: Account mode: {_accountMode}");
    }

    public override List<CashAmount> GetCashBalance()
    {
        // 根据账户模式调整行为
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
}
```

### 2.3 Factory 实现

```csharp
[BrokerageFactory(typeof(OKXBrokerageFactory))]
public partial class OKXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
{
    // 实现
}

public class OKXBrokerageFactory : BrokerageFactory
{
    public override Dictionary<string, string> BrokerageData { get; }
    public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider);
    public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm);
}
```

---

## 3. REST API 客户端规格

### 3.1 OKXRestApiClient 类

**单一类处理所有工具类型（统一端点设计）**

```csharp
public class OKXRestApiClient
{
    // 构造函数
    public OKXRestApiClient(
        string apiKey,
        string apiSecret,
        string passphrase,
        string apiUrl);

    // 认证与签名
    private string GenerateSignature(string timestamp, string method, string path, string body);
    private void AddAuthenticationHeaders(RestRequest request, string method, string path, string body);

    // 公共端点（无需认证）
    public ServerTime GetServerTime();
    public List<Instrument> GetInstruments(string instType);
    public Ticker GetTicker(string instId);
    public List<Candle> GetCandles(string instId, string bar, long? after, long? before, int? limit);
    public List<Trade> GetTrades(string instId, int? limit);

    // 账户端点
    public AccountConfig GetAccountConfig();
    public List<Balance> GetBalance(string currency = null);
    public List<Position> GetPositions(string instType = null, string instId = null);
    public AccountBalance GetAccountBalance();

    // 交易端点（统一）
    public OrderResponse PlaceOrder(PlaceOrderRequest request);
    public CancelOrderResponse CancelOrder(string instId, string ordId = null, string clOrdId = null);
    public AmendOrderResponse AmendOrder(AmendOrderRequest request);
    public List<Order> GetOpenOrders(string instType = null, string instId = null);
    public Order GetOrderDetails(string instId, string ordId = null, string clOrdId = null);

    // 速率限制
    private readonly RateGate _orderRateLimiter;      // 1000 请求/2秒
    private readonly RateGate _accountRateLimiter;    // 10 请求/2秒
    private readonly RateGate _instrumentRateLimiter; // 20 请求/2秒
}
```

### 3.2 请求/响应模型

#### PlaceOrderRequest
```csharp
public class PlaceOrderRequest
{
    public string InstId { get; set; }        // "BTC-USDT" 或 "BTC-USDT-SWAP"
    public string TdMode { get; set; }        // "cash", "cross", "isolated"
    public string Side { get; set; }          // "buy", "sell"
    public string OrdType { get; set; }       // "market", "limit", "post_only", "fok", "ioc"
    public string Sz { get; set; }            // 数量
    public string Px { get; set; }            // 价格（限价单必填）
    public string ClOrdId { get; set; }       // 客户端订单 ID
    public bool? ReduceOnly { get; set; }     // 只减仓（期货）
    public string TpTriggerPx { get; set; }   // 止盈触发价
    public string SlTriggerPx { get; set; }   // 止损触发价
}
```

#### OrderResponse
```csharp
public class OrderResponse
{
    public string Code { get; set; }          // "0" 表示成功
    public string Msg { get; set; }           // 错误消息
    public List<OrderData> Data { get; set; }

    public bool IsSuccess => Code == "0";
}

public class OrderData
{
    public string OrdId { get; set; }         // OKX 订单 ID
    public string ClOrdId { get; set; }       // 客户端订单 ID
    public string SCode { get; set; }         // 订单状态码
    public string SMsg { get; set; }          // 订单状态消息
}
```

### 3.3 认证机制

**OKX 使用 HMAC-SHA256 + Passphrase**

```csharp
private string GenerateSignature(string timestamp, string method, string path, string body)
{
    // 1. 构建签名字符串
    var message = timestamp + method.ToUpper() + path + body;

    // 2. HMAC-SHA256 签名
    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
    {
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}

private void AddAuthenticationHeaders(RestRequest request, string method, string path, string body)
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + "." +
                    DateTimeOffset.UtcNow.Millisecond.ToString("000");
    var signature = GenerateSignature(timestamp, method, path, body);

    request.AddHeader("OK-ACCESS-KEY", _apiKey);
    request.AddHeader("OK-ACCESS-SIGN", signature);
    request.AddHeader("OK-ACCESS-TIMESTAMP", timestamp);
    request.AddHeader("OK-ACCESS-PASSPHRASE", _passphrase);
}
```

---

## 4. 消息模型规格

### 4.1 消息模型列表（预计 22 个）

#### 公共数据模型
1. **ServerTime** - 服务器时间
2. **Instrument** - 工具信息
3. **Ticker** - 行情快照
4. **Trade** - 交易记录
5. **Candle** - K线数据
6. **OrderBookSnapshot** - 订单簿快照
7. **OrderBookUpdate** - 订单簿增量更新

#### 账户数据模型
8. **AccountConfig** - 账户配置（包含账户模式）
9. **Balance** - 余额信息
10. **Position** - 持仓信息
11. **AccountBalance** - 账户余额汇总

#### 订单数据模型
12. **Order** - 订单信息（统一模型，现货+期货）
13. **OrderUpdate** - 订单更新（WebSocket）
14. **OrderData** - 订单数据（API 响应）
15. **OrderResponse** - 下单响应
16. **CancelOrderResponse** - 撤单响应
17. **AmendOrderResponse** - 改单响应

#### WebSocket 消息模型
18. **WebSocketRequest** - WebSocket 订阅请求
19. **WebSocketResponse** - WebSocket 响应
20. **LoginRequest** - WebSocket 登录请求
21. **LoginResponse** - WebSocket 登录响应
22. **ErrorMessage** - 错误消息

#### 期货专用模型
23. **RiskLimitTier** - 风险限额层级（Phase 14）

### 4.2 关键消息模型详细规格

#### Balance（余额）
```csharp
public class Balance
{
    [JsonProperty("ccy")]
    public string Currency { get; set; }              // 币种

    [JsonProperty("availBal")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal AvailableBalance { get; set; }     // 可用余额

    [JsonProperty("frozenBal")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal FrozenBalance { get; set; }        // 冻结余额

    [JsonProperty("bal")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal Balance { get; set; }              // 总余额
}
```

#### Position（持仓）
```csharp
public class Position
{
    [JsonProperty("instId")]
    public string InstrumentId { get; set; }          // 工具 ID

    [JsonProperty("pos")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal PositionSize { get; set; }         // 持仓数量

    [JsonProperty("avgPx")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal AveragePrice { get; set; }         // 持仓均价

    [JsonProperty("upl")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal UnrealizedPnl { get; set; }        // 未实现盈亏

    [JsonProperty("posSide")]
    public string PositionSide { get; set; }          // "long", "short", "net"

    [JsonProperty("mgnMode")]
    public string MarginMode { get; set; }            // "cross", "isolated"
}
```

#### Order（订单 - 统一模型）
```csharp
public class Order
{
    [JsonProperty("instId")]
    public string InstrumentId { get; set; }          // 工具 ID

    [JsonProperty("ordId")]
    public string OrderId { get; set; }               // OKX 订单 ID

    [JsonProperty("clOrdId")]
    public string ClientOrderId { get; set; }         // 客户端订单 ID

    [JsonProperty("px")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal Price { get; set; }                // 价格

    [JsonProperty("sz")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal Size { get; set; }                 // 数量

    [JsonProperty("ordType")]
    public string OrderType { get; set; }             // "limit", "market", 等

    [JsonProperty("side")]
    public string Side { get; set; }                  // "buy", "sell"

    [JsonProperty("state")]
    public string State { get; set; }                 // "live", "filled", "canceled", 等

    [JsonProperty("fillSz")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal FilledSize { get; set; }           // 已成交数量

    [JsonProperty("avgPx")]
    [JsonConverter(typeof(DecimalConverter))]
    public decimal AveragePrice { get; set; }         // 成交均价

    [JsonProperty("uTime")]
    [JsonConverter(typeof(DateTimeConverter))]
    public DateTime UpdateTime { get; set; }          // 更新时间
}
```

---

## 5. Converter 规格

### 5.1 Converter 列表（预计 22 个）

#### JSON Converters（解析 API JSON）
1. **DecimalConverter** - 字符串 → decimal
2. **DateTimeConverter** - Unix 毫秒时间戳 → DateTime
3. **ServerTimeConverter** - 服务器时间解析
4. **InstrumentConverter** - 工具信息解析
5. **TickerConverter** - 行情快照解析
6. **TradeConverter** - 交易记录解析
7. **CandleConverter** - K线数据解析
8. **OrderBookSnapshotConverter** - 订单簿快照解析
9. **OrderBookUpdateConverter** - 订单簿更新解析
10. **AccountConfigConverter** - 账户配置解析
11. **BalanceConverter** - 余额解析
12. **PositionConverter** - 持仓解析
13. **OrderConverter** - 订单解析
14. **OrderUpdateConverter** - 订单更新解析
15. **WebSocketResponseConverter** - WebSocket 响应解析
16. **ErrorMessageConverter** - 错误消息解析

#### Domain Converters（转换为 LEAN 类型）
17. **BalanceToCashAmountConverter** - Balance → CashAmount
18. **PositionToHoldingConverter** - Position → Holding
19. **OrderToLeanOrderConverter** - Order → LEAN Order
20. **TradeToTickConverter** - Trade → Tick (Trade)
21. **TickerToTickConverter** - Ticker → Tick (Quote)
22. **CandleToBarsConverter** - Candle → Bar

### 5.2 核心 Converter 实现规格

#### DecimalConverter
```csharp
public class DecimalConverter : JsonConverter<decimal>
{
    public override decimal ReadJson(JsonReader reader, ...)
    {
        if (reader.TokenType == JsonToken.Null)
            return 0m;

        var value = reader.Value.ToString();
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        Log.Warning($"DecimalConverter: Failed to parse '{value}' as decimal");
        return 0m;
    }
}
```

#### DateTimeConverter
```csharp
public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime ReadJson(JsonReader reader, ...)
    {
        if (reader.TokenType == JsonToken.Null)
            return DateTime.MinValue;

        var value = reader.Value.ToString();
        if (long.TryParse(value, out var timestamp))
        {
            // OKX 使用毫秒时间戳
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        Log.Warning($"DateTimeConverter: Failed to parse '{value}' as timestamp");
        return DateTime.MinValue;
    }
}
```

#### BalanceToCashAmountConverter（扩展方法）
```csharp
public static class BalanceExtensions
{
    public static CashAmount? ToCashAmount(this Balance balance)
    {
        if (balance == null || string.IsNullOrEmpty(balance.Currency))
            return null;

        return new CashAmount(balance.AvailableBalance, balance.Currency);
    }

    public static List<CashAmount> ToCashAmounts(this List<Balance> balances)
    {
        return balances
            .Select(b => b.ToCashAmount())
            .Where(ca => ca != null)
            .ToList();
    }
}
```

---

## 6. 接口实现需求

### 6.1 IBrokerage 接口

#### 必须实现的方法

| 方法 | 返回类型 | 说明 | 实现文件 |
|-----|---------|------|---------|
| `IsConnected` | bool | WebSocket 是否连接 | OKXBrokerage.cs |
| `PlaceOrder` | bool | 下单 | OKXBrokerage.Orders.cs |
| `UpdateOrder` | bool | 改单 | OKXBrokerage.Orders.cs |
| `CancelOrder` | bool | 撤单 | OKXBrokerage.Orders.cs |
| `GetAccountHoldings` | List\<Holding\> | 获取持仓 | OKXBrokerage.cs |
| `GetCashBalance` | List\<CashAmount\> | 获取余额 | OKXBrokerage.cs |
| `GetOpenOrders` | List\<Order\> | 获取未完成订单 | OKXBrokerage.Orders.cs |
| `Connect` | void | 连接 WebSocket | OKXBrokerage.cs |
| `Disconnect` | void | 断开 WebSocket | OKXBrokerage.cs |

#### 事件

| 事件 | 类型 | 触发时机 |
|-----|------|---------|
| `OrderIdChanged` | EventHandler\<BrokerageOrderIdChangedEvent\> | OKX 分配订单 ID 时 |
| `OrdersStatusChanged` | EventHandler\<OrderEvent\> | 订单状态改变时 |
| `AccountChanged` | EventHandler\<AccountEvent\> | 账户余额/持仓改变时 |
| `Message` | EventHandler\<BrokerageMessageEvent\> | 收到错误或警告时 |

### 6.2 IDataQueueHandler 接口

#### 必须实现的方法

| 方法 | 返回类型 | 说明 | 实现文件 |
|-----|---------|------|---------|
| `Subscribe` | IEnumerable\<Symbol\> | 订阅市场数据 | OKXBrokerage.DataQueueHandler.cs |
| `Unsubscribe` | void | 取消订阅 | OKXBrokerage.DataQueueHandler.cs |
| `SetJob` | void | 设置任务（仅数据源模式） | OKXBrokerage.DataQueueHandler.cs |

#### 订阅通道

| 通道名称 | 用途 | 触发 Tick 类型 |
|---------|------|--------------|
| `tickers` | BBO 行情 | Quote |
| `trades` | 成交记录 | Trade |
| `books` | 订单簿（可选） | - |

### 6.3 IDataQueueUniverseProvider 接口（可选）

| 方法 | 返回类型 | 实现 |
|-----|---------|-----|
| `LookupSymbols` | IEnumerable\<Symbol\> | 返回空列表（不支持） |
| `CanPerformSelection` | bool | 返回 false |

---

## 7. WebSocket 规格

### 7.1 连接规格

#### 端点 URL
- **公共通道：** `wss://ws.okx.com:8443/ws/v5/public`
- **私有通道：** `wss://ws.okx.com:8443/ws/v5/private`

#### 连接限制
- 每通道最多 30 个连接
- 连接建立速率：3 请求/秒（基于 IP）
- 订阅速率：480 次/小时（subscribe + unsubscribe + login 总计）

#### 保活机制（强制要求）
```csharp
// ✅ 必须实现
private readonly Timer _keepAliveTimer;
private const int KeepAliveIntervalSeconds = 20;

private void InitializeKeepAlive()
{
    _keepAliveTimer = new Timer(KeepAliveIntervalSeconds * 1000);
    _keepAliveTimer.Elapsed += OnKeepAliveTimerElapsed;
    _keepAliveTimer.AutoReset = true;
    _keepAliveTimer.Start();
}

private void OnKeepAliveTimerElapsed(object sender, ElapsedEventArgs e)
{
    if (_lastMessageTime.AddSeconds(KeepAliveIntervalSeconds) < DateTime.UtcNow)
    {
        WebSocket.Send("ping");
        Log.Trace("OKXBrokerage: Sent ping to keep connection alive");
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

### 7.2 认证规格（私有通道）

#### 登录请求
```json
{
  "op": "login",
  "args": [
    {
      "apiKey": "your-api-key",
      "passphrase": "your-passphrase",
      "timestamp": "1234567890.123",
      "sign": "signature"
    }
  ]
}
```

#### 签名生成
```csharp
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

### 7.3 订阅规格

#### 订阅请求格式
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "tickers",
      "instId": "BTC-USDT"
    },
    {
      "channel": "trades",
      "instId": "BTC-USDT"
    }
  ]
}
```

#### 支持的通道

| 通道 | 类型 | 用途 | 更新频率 | 需要认证 |
|-----|------|------|---------|---------|
| `tickers` | 公共 | BBO 行情 | 100ms | ❌ |
| `trades` | 公共 | 成交记录 | 实时 | ❌ |
| `books` | 公共 | 订单簿 | 100ms | ❌ |
| `orders` | 私有 | 订单更新 | 实时 | ✅ |
| `account` | 私有 | 余额更新 | 实时 | ✅ |
| `positions` | 私有 | 持仓更新 | 实时 | ✅ |

### 7.4 消息路由规格

```csharp
private void OnDataMessage(WebSocketMessage message)
{
    var jObject = JObject.Parse(message.Data);

    // 提取通道名称
    var arg = jObject["arg"];
    var channel = arg?["channel"]?.ToString();
    var instId = arg?["instId"]?.ToString();

    // 路由到对应处理器
    switch (channel)
    {
        case "tickers":
            HandleTickerUpdate(jObject["data"], instId);
            break;
        case "trades":
            HandleTradeUpdate(jObject["data"], instId);
            break;
        case "books":
            HandleOrderBookUpdate(jObject["data"], instId);
            break;
        case "orders":
            HandleOrderUpdate(jObject["data"]);
            break;
        case "account":
            HandleAccountUpdate(jObject["data"]);
            break;
        case "positions":
            HandlePositionUpdate(jObject["data"]);
            break;
        default:
            Log.Warning($"OKXBrokerage: Unknown channel: {channel}");
            break;
    }
}
```

---

## 8. 订单管理规格

### 8.1 PlaceOrder 规格

#### 输入
- `Order order` - LEAN 订单对象

#### 处理流程
```
1. 验证订单参数 (ValidateOrder)
2. 转换为 OKX 格式 (ConvertToOKXOrder)
3. 应用速率限制 (_orderRateLimiter.WaitToProceed)
4. 调用 REST API (PlaceOrder)
5. 解析响应
6. 触发 OrderIdChanged 事件（如果成功）
7. 返回结果
```

#### 订单转换示例
```csharp
private PlaceOrderRequest ConvertToOKXOrder(Order order)
{
    var symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);
    var request = new PlaceOrderRequest
    {
        InstId = symbol,
        Side = order.Direction == OrderDirection.Buy ? "buy" : "sell",
        ClOrdId = order.Id.ToString()
    };

    // 根据订单类型设置参数
    switch (order)
    {
        case MarketOrder marketOrder:
            request.OrdType = "market";
            request.Sz = Math.Abs(marketOrder.Quantity).ToString(CultureInfo.InvariantCulture);
            break;

        case LimitOrder limitOrder:
            request.OrdType = DetermineOrdType(limitOrder);  // "limit", "post_only", "ioc", "fok"
            request.Px = limitOrder.LimitPrice.ToString(CultureInfo.InvariantCulture);
            request.Sz = Math.Abs(limitOrder.Quantity).ToString(CultureInfo.InvariantCulture);
            break;

        case StopLimitOrder stopLimitOrder:
            request.OrdType = "trigger";
            // ... 设置触发价格
            break;

        default:
            throw new NotSupportedException($"Order type {order.Type} not supported");
    }

    // 设置交易模式
    request.TdMode = DetermineTdMode(order.Symbol.SecurityType, _accountMode);

    // 期货专用参数
    if (order.Symbol.SecurityType == SecurityType.CryptoFuture)
    {
        var properties = order.Properties as OKXOrderProperties;
        request.ReduceOnly = properties?.ReduceOnly;
    }

    return request;
}
```

### 8.2 CancelOrder 规格

#### 输入
- `Order order` - LEAN 订单对象

#### 处理流程
```
1. 从缓存获取 OKX 订单 ID
2. 应用速率限制
3. 调用 REST API (CancelOrder)
4. 解析响应
5. 触发 OrdersStatusChanged 事件（如果成功）
6. 返回结果
```

### 8.3 UpdateOrder 规格

#### 输入
- `Order order` - LEAN 订单对象

#### OKX 限制
- 只能改价格和数量
- 不能改订单类型
- 不能改方向

#### 处理流程
```
1. 验证订单可修改
2. 构建修改请求
3. 应用速率限制
4. 调用 REST API (AmendOrder)
5. 解析响应
6. 触发 OrdersStatusChanged 事件（如果成功）
7. 返回结果
```

### 8.4 订单状态映射

| OKX State | LEAN OrderStatus | 说明 |
|-----------|-----------------|------|
| `live` | Submitted | 订单已提交，等待成交 |
| `partially_filled` | PartiallyFilled | 部分成交 |
| `filled` | Filled | 完全成交 |
| `canceled` | Canceled | 已取消 |
| `rejected` | Invalid | 被拒绝 |

---

## 9. 账户管理规格

### 9.1 GetCashBalance 规格

#### 输出
- `List<CashAmount>` - 所有币种的可用余额

#### 按账户模式处理

```csharp
public override List<CashAmount> GetCashBalance()
{
    _accountRateLimiter.WaitToProceed();

    switch (_accountMode)
    {
        case OKXAccountMode.Spot:
            // 仅返回现货余额
            var spotBalances = _apiClient.GetBalance();
            return spotBalances.ToCashAmounts();

        case OKXAccountMode.Futures:
            // 返回期货账户余额（USDT 等保证金）
            var futuresBalance = _apiClient.GetAccountBalance();
            return ConvertFuturesBalanceToCashAmounts(futuresBalance);

        case OKXAccountMode.MultiCurrencyMargin:
        case OKXAccountMode.PortfolioMargin:
            // 返回统一账户所有余额
            var allBalances = _apiClient.GetBalance();
            return allBalances.ToCashAmounts();

        default:
            throw new NotSupportedException($"Account mode {_accountMode} not supported");
    }
}
```

### 9.2 GetAccountHoldings 规格

#### 输出
- `List<Holding>` - 所有持仓

#### 按账户模式处理

```csharp
public override List<Holding> GetAccountHoldings()
{
    _accountRateLimiter.WaitToProceed();

    var holdings = new List<Holding>();

    switch (_accountMode)
    {
        case OKXAccountMode.Spot:
            // 现货持仓 = 非零余额
            var spotBalances = _apiClient.GetBalance();
            holdings.AddRange(ConvertBalancesToHoldings(spotBalances));
            break;

        case OKXAccountMode.Futures:
            // 期货持仓
            var futuresPositions = _apiClient.GetPositions(instType: "SWAP");
            holdings.AddRange(ConvertPositionsToHoldings(futuresPositions));
            break;

        case OKXAccountMode.MultiCurrencyMargin:
        case OKXAccountMode.PortfolioMargin:
            // 现货 + 期货持仓
            var allBalances = _apiClient.GetBalance();
            var allPositions = _apiClient.GetPositions();
            holdings.AddRange(ConvertBalancesToHoldings(allBalances));
            holdings.AddRange(ConvertPositionsToHoldings(allPositions));
            break;
    }

    return holdings;
}
```

### 9.3 账户模式检测规格

```csharp
private OKXAccountMode DetectAccountMode()
{
    try
    {
        var config = _apiClient.GetAccountConfig();
        var acctLv = config.AccountLevel;

        return acctLv switch
        {
            "1" => OKXAccountMode.Spot,
            "2" => OKXAccountMode.Futures,
            "3" => OKXAccountMode.MultiCurrencyMargin,
            "4" => OKXAccountMode.PortfolioMargin,
            _ => throw new InvalidOperationException($"Unknown account level: {acctLv}")
        };
    }
    catch (Exception ex)
    {
        Log.Error($"OKXBrokerage: Failed to detect account mode: {ex.Message}");
        // 默认使用 Multi-currency margin mode
        return OKXAccountMode.MultiCurrencyMargin;
    }
}
```

---

## 10. 历史数据规格

### 10.1 GetHistory 规格

#### 输入
- `HistoryRequest request` - 历史数据请求

#### 支持的分辨率

| Resolution | OKX bar 参数 | 支持 |
|-----------|--------------|------|
| Tick | - | ✅ (从 trades 端点) |
| Second | - | ❌ |
| Minute | 1m | ✅ |
| Hour | 1H | ✅ |
| Daily | 1D | ✅ |

#### 实现逻辑

```csharp
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
    else  // Bar data
    {
        return GetCandles(request);
    }
}

private IEnumerable<Tick> GetTradeTicks(HistoryRequest request)
{
    var symbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
    var trades = _apiClient.GetTrades(symbol, limit: 100);

    return trades.Select(t => t.ToTick(request.Symbol));
}

private IEnumerable<Bar> GetCandles(HistoryRequest request)
{
    var symbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
    var bar = ConvertResolutionToBar(request.Resolution);

    var candles = _apiClient.GetCandles(
        symbol,
        bar,
        after: ConvertToTimestamp(request.StartTimeUtc),
        before: ConvertToTimestamp(request.EndTimeUtc)
    );

    return candles.Select(c => c.ToBar(request.Symbol));
}
```

### 10.2 分辨率转换

| LEAN Resolution | OKX bar | 说明 |
|----------------|---------|------|
| Minute | 1m | 1 分钟 |
| Hour | 1H | 1 小时 |
| Daily | 1D | 1 天 |

---

## 11. 特殊功能规格

### 11.1 OKXSymbolMapper 规格

#### 职责
- LEAN Symbol ↔ OKX instId 双向转换

#### 实现

```csharp
public class OKXSymbolMapper
{
    public string GetBrokerageSymbol(Symbol symbol)
    {
        if (symbol.SecurityType == SecurityType.Crypto)
        {
            // BTCUSDT → BTC-USDT
            return $"{symbol.Value.Replace(symbol.ID.Symbol, "")}-{symbol.ID.Symbol}";
        }
        else if (symbol.SecurityType == SecurityType.CryptoFuture)
        {
            if (symbol.ID.Symbol.EndsWith("PERP") || symbol.Underlying != null)
            {
                // 永续合约：BTCUSDT → BTC-USDT-SWAP
                return $"{symbol.Value.Replace(symbol.ID.Symbol, "")}-{symbol.ID.Symbol}-SWAP";
            }
            else
            {
                // 交割合约：BTCUSDT → BTC-USDT-YYMMDD
                var expiry = symbol.ID.Date.ToString("yyMMdd");
                return $"{symbol.Value.Replace(symbol.ID.Symbol, "")}-{symbol.ID.Symbol}-{expiry}";
            }
        }

        throw new NotSupportedException($"Security type {symbol.SecurityType} not supported");
    }

    public Symbol GetLeanSymbol(string instId, SecurityType securityType)
    {
        // BTC-USDT → BTCUSDT
        // BTC-USDT-SWAP → BTCUSDT (CryptoFuture)
        // BTC-USDT-250328 → BTCUSDT (CryptoFuture with expiry)

        var parts = instId.Split('-');
        var ticker = parts[0] + parts[1];  // BTC + USDT = BTCUSDT

        if (securityType == SecurityType.Crypto)
        {
            return Symbol.Create(ticker, SecurityType.Crypto, Market.OKX);
        }
        else if (securityType == SecurityType.CryptoFuture)
        {
            if (parts.Length == 3 && parts[2] == "SWAP")
            {
                // 永续合约
                return Symbol.Create(ticker, SecurityType.CryptoFuture, Market.OKX);
            }
            else if (parts.Length == 3)
            {
                // 交割合约
                var expiry = DateTime.ParseExact(parts[2], "yyMMdd", CultureInfo.InvariantCulture);
                return Symbol.CreateFuture(ticker, Market.OKX, expiry);
            }
        }

        throw new ArgumentException($"Cannot parse instId: {instId}");
    }
}
```

### 11.2 OKXPairMatcher 规格（Phase 14）

#### 职责
- 匹配现货-期货交易对（用于套利）
- 按成交量过滤

#### 接口

```csharp
public class OKXPairMatcher
{
    public List<SymbolPair> GetSpotFuturePairs(decimal minVolumeUsdt = 1000000);
    public List<Symbol> GetAllSpotSymbols();
    public List<Symbol> GetAllFutureSymbols();
}
```

### 11.3 OKXRiskLimitHelper 规格（Phase 14）

#### 职责
- 计算期货可用风险限额
- 缓存风险限额层级（24小时）

#### 接口

```csharp
public class OKXRiskLimitHelper
{
    public OKXRiskLimitHelper(IAlgorithm algorithm);
    public decimal GetAvailableLimit(Symbol symbol);
    public void ClearCache();
}
```

---

## 12. 错误处理规格

### 12.1 OKX 错误码映射

| 错误码 | 错误类型 | 处理策略 |
|-------|---------|---------|
| 50000 | 服务错误 | 重试（指数退避） |
| 50001 | 服务暂时不可用 | 重试（指数退避） |
| 50004 | API key 无效 | 抛出异常，停止运行 |
| 50011 | 速率限制 | 等待后重试 |
| 50061 | 订单速率限制 | 等待后重试（共享限制） |
| 51000 | 订单参数错误 | 记录错误，返回 false |
| 51001 | 余额不足 | 记录警告，返回 false |
| 51008 | 订单不存在 | 返回 false |

### 12.2 错误处理示例

```csharp
private bool HandleApiError(string code, string message)
{
    switch (code)
    {
        case "50000":
        case "50001":
            Log.Warning($"OKXBrokerage: Service error {code}: {message}. Will retry.");
            return true;  // 可重试

        case "50004":
            Log.Error($"OKXBrokerage: Invalid API key. Please check configuration.");
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, code, message));
            throw new InvalidOperationException("Invalid API credentials");

        case "50011":
        case "50061":
            Log.Warning($"OKXBrokerage: Rate limit reached. Waiting before retry.");
            Thread.Sleep(2000);  // 等待 2 秒
            return true;  // 可重试

        case "51001":
            Log.Warning($"OKXBrokerage: Insufficient balance");
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, code, "Insufficient balance"));
            return false;  // 不可重试

        default:
            Log.Error($"OKXBrokerage: Error {code}: {message}");
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, code, message));
            return false;
    }
}
```

---

## 13. 配置规格

### 13.1 配置文件格式

**config.json**
```json
{
  "okx-api-key": "your-api-key",
  "okx-api-secret": "your-api-secret",
  "okx-passphrase": "your-passphrase",
  "okx-api-url": "https://www.okx.com",
  "okx-websocket-url": "wss://ws.okx.com:8443/ws/v5",
  "okx-environment": "production",
  "live-holdings": "{\"BTCUSDT\": 0.1, \"ETHUSDT\": 1.0}"
}
```

**config.json.example**（提交到仓库）
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

### 13.2 环境枚举

```csharp
public enum OKXEnvironment
{
    Production,  // 生产环境
    Demo         // 模拟盘（测试网）
}
```

### 13.3 环境 URL 映射

| Environment | API URL | WebSocket URL |
|------------|---------|---------------|
| Production | https://www.okx.com | wss://ws.okx.com:8443/ws/v5 |
| Demo | https://www.okx.com (模拟盘用 API key 区分) | wss://wspap.okx.com:8443/ws/v5/public?brokerId=9999 |

---

## 14. 测试规格

### 14.1 单元测试清单

#### OKXRestApiClientTests
- [ ] GenerateSignature_WithValidInput_ReturnsCorrectSignature
- [ ] PlaceOrder_WithValidMarketOrder_ReturnsSuccess
- [ ] PlaceOrder_WithValidLimitOrder_ReturnsSuccess
- [ ] CancelOrder_WithValidOrderId_ReturnsSuccess
- [ ] GetBalance_WithValidCredentials_ReturnsBalances
- [ ] GetAccountConfig_ReturnsValidConfig

#### OKXSymbolMapperTests
- [ ] GetBrokerageSymbol_Spot_ReturnsCorrectFormat (BTCUSDT → BTC-USDT)
- [ ] GetBrokerageSymbol_SwapFuture_ReturnsCorrectFormat (BTCUSDT → BTC-USDT-SWAP)
- [ ] GetLeanSymbol_Spot_ReturnsCorrectSymbol (BTC-USDT → BTCUSDT)
- [ ] GetLeanSymbol_Swap_ReturnsCorrectSymbol (BTC-USDT-SWAP → BTCUSDT)
- [ ] Bidirectional_Conversion_IsReversible

#### ConverterTests
- [ ] DecimalConverter_WithValidString_ReturnsDecimal
- [ ] DecimalConverter_WithNull_ReturnsZero
- [ ] DateTimeConverter_WithValidTimestamp_ReturnsDateTime
- [ ] BalanceToCashAmount_WithValidBalance_ReturnsCashAmount

#### OKXBrokerageOrderTests
- [ ] PlaceOrder_MarketOrder_Success
- [ ] PlaceOrder_LimitOrder_Success
- [ ] PlaceOrder_StopLimitOrder_Success
- [ ] CancelOrder_ExistingOrder_Success
- [ ] UpdateOrder_ChangePriceAndQuantity_Success

### 14.2 集成测试清单

**要求：OKX 测试网账户和凭证**

#### OKXBrokerageIntegrationTests
- [ ] Connect_WithValidCredentials_Success
- [ ] GetBalance_ReturnsNonEmptyList
- [ ] GetAccountHoldings_ReturnsCorrectPositions
- [ ] PlaceAndCancelOrder_FullLifecycle_Success
- [ ] WebSocketReconnection_AfterDisconnect_RestoresSubscriptions

### 14.3 手动测试清单（Phase 18）

- [ ] 长时间运行（过夜，8+ 小时）
- [ ] 长时间运行（周末，48+ 小时）
- [ ] 高容量订阅（100 符号）
- [ ] 高容量订阅（500 符号）
- [ ] 网络断开恢复（断网 5 分钟后恢复）
- [ ] 保活机制验证（30 秒无消息时发送 ping）
- [ ] 订单生命周期（下单 → 部分成交 → 全部成交）
- [ ] 订单生命周期（下单 → 改单 → 撤单）

---

## 验证检查点

规格文档完成后，必须确认：

- [ ] 所有支持的工具类型已明确
- [ ] 所有必需的接口方法已列出
- [ ] 消息模型数量估算准确（22 个）
- [ ] Converter 数量估算准确（22 个）
- [ ] REST API 端点完整
- [ ] WebSocket 通道完整
- [ ] 错误处理策略明确
- [ ] 配置格式清晰
- [ ] 测试用例覆盖所有功能
- [ ] 特殊情况（账户模式、市场订单）已识别

---

**下一步：** Phase 3 - 编写详细的实施计划（plan.md）

**版本：** 1.0
**最后更新：** 2026-01-09
**状态：** Draft ✅
