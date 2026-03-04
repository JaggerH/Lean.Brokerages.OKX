# Implementation Plan: 公有 WebSocket 心跳保活修复

**Feature**: `002-public-ws-heartbeat` | **Date**: 2026-03-04 | **Spec**: [spec.md](./spec.md)

---

## Summary

两个并存的 OKX WebSocket 合规性问题：
1. **心跳缺失**：`SendHeartbeat()` 只 ping 私有 WS，公有 WS 连接无心跳 → 30s 超时断连
2. **订阅超限**：`MaximumSymbolsPerConnection = 15`，每连接 15 × 3 = 45 个频道订阅 > OKX 上限 30 → 违反 per-connection subscription limit

修复：4 个文件改动，新增 `BroadcastPing()` → 扩展 `SendHeartbeat()` → 修复 `OnDataMessage` pong 解析 → 将 `MaximumSymbolsPerConnection` 从 15 改为 10（10 × 3 = 30）。

---

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: `BrokerageMultiWebSocketSubscriptionManager`（LEAN core）, `IWebSocket.Send()`
**Testing**: `TestsCustom/` (NUnit)
**Target Platform**: Live trading, OKX public WebSocket endpoints
**Performance Goals**: 心跳 15s 间隔，ping 发送 < 1ms，不阻塞 orderbook 处理
**Constraints**: OKX 要求 < 30s 发送一次 ping；ping 不计入 480次/小时 订阅速率

---

## Research Findings

### 关键发现 1：`OnDataMessage` 中 pong 会触发 JSON 解析错误

`OnDataMessage`（公有 WS 消息入口）直接调用 `ProcessMessage(e.Message)`。而 `ProcessMessage` 会立即执行 `JObject.Parse(rawMessage)`，对 `"pong"` 字符串抛出 `JsonReaderException`，产生错误日志。

**对比私有 WS**：`ProcessPrivateMessage` 在调用 `ProcessMessage` 之前显式检查了 `"pong"`，因此私有 WS 的 pong 不会触发解析错误。

**结论**：`OnDataMessage` 需要同样的 pong early-return（FR-003）。

### 关键发现 2：`BrokerageMultiWebSocketSubscriptionManager._webSocketEntries` 是私有字段

`_webSocketEntries`（`List<BrokerageMultiWebSocketEntry>`）是私有字段，外部（OKXBaseBrokerage）无法直接访问。需在 LEAN 层添加公开的 `BroadcastPing()` 方法。

**替代方案考虑**：
- ❌ 在 `BrokerageMultiWebSocketSubscriptionManager` 构造函数添加 heartbeatInterval 参数 → 过度改造，该类不应感知 OKX 特定协议
- ❌ 在 OKXBaseBrokerage 中反射访问 `_webSocketEntries` → 脆弱
- ✅ 添加 `BroadcastPing()` 公开方法 → 最小侵入，语义清晰

### 关键发现 3：现有 pong 处理逻辑可以复用

`ProcessPrivateMessage` 中的 pong 处理同时更新了 `_lastMessageTime`（用于私有 WS 超时检测）。公有 WS 的 pong **不应**更新 `_lastMessageTime`，因为超时检测仅针对私有 WS。`OnDataMessage` 中的 pong 应静默忽略（`return`）。

### 关键发现 4：每连接 15 symbols × 3 channels = 45 订阅 > OKX 上限 30

OKX per-connection subscription limit 为 30 个频道订阅。当前 `MaximumSymbolsPerConnection = 15`，每连接订阅 books + trades + price-limit 三个频道，合计 45 个订阅，超出上限 50%。

修复：`MaximumSymbolsPerConnection = 10`（10 × 3 = 30，刚好达到上限）。

**连接数影响**：200 symbols 时，15→10 会从 14 个连接增加到 20 个连接。20 个连接 < OKX 每频道 30 个连接上限，合规。

### 关键发现 5：`_keepAliveTimer` 已在 `WebSocket.Closed` 时 Stop

私有 WS 断开时 `_keepAliveTimer.Stop()`，但公有 WS 的断连不会触发这个 Stop。公有 WS 的 `BroadcastPing()` 内部对每个连接单独检查 `IsOpen`，断开的连接跳过即可。

---

## Project Structure

### 修改文件（共 2 个，零 LEAN 官方代码改动）

```
Lean.Brokerages.OKX/
└── QuantConnect.OKXBrokerage/
    ├── OKXWebSocketWrapper.cs                           ← 添加自包含心跳计时器
    └── Core/
        └── OKXBaseBrokerage.cs                          ← OnDataMessage pong 修复 + MaximumSymbolsPerConnection 15→10
```

### 测试文件（新建）

```
TestsCustom/
└── Tests/
    └── Brokerages/
        └── OKX/
            └── OKXHeartbeatTests.cs                     ← 新增
```

---

## Implementation Approach

### Phase 1：OKX WebSocket 包装层 — `OKXWebSocketWrapper` 内置心跳

**文件**：`/mnt/c/Users/Jagger/Documents/Code/Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/OKXWebSocketWrapper.cs`

每个公有 WS 连接都是 `OKXWebSocketWrapper` 实例（工厂：`() => new OKXWebSocketWrapper(null)`），在构造函数内订阅自身的 `Open`/`Closed` 事件来自主管理心跳定时器。

```csharp
private const int HeartbeatIntervalMs = 15_000;
private readonly Timer _heartbeatTimer;

public OKXWebSocketWrapper(IConnectionHandler connectionHandler)
{
    ConnectionId = Guid.NewGuid().ToString("N").Substring(0, 8);
    ConnectionHandler = connectionHandler;

    _heartbeatTimer = new Timer(HeartbeatIntervalMs) { AutoReset = true };
    _heartbeatTimer.Elapsed += OnHeartbeatElapsed;

    Open   += (_, _) => _heartbeatTimer.Start();
    Closed += (_, _) => _heartbeatTimer.Stop();
}

private void OnHeartbeatElapsed(object sender, ElapsedEventArgs e)
{
    if (!IsOpen) return;
    try { Send("ping"); }
    catch (Exception ex)
    {
        Log.Error($"OKXWebSocketWrapper[{ConnectionId}].Heartbeat(): Error sending ping: {ex.Message}");
    }
}
```

**关键设计点**：
- 完全自包含，不修改任何 LEAN 官方代码
- `Open` 启动计时器，`Closed` 停止计时器，生命周期与连接一致
- `IsOpen` 检查防止断连后的僵尸 tick 触发 Send
- `AutoReset = true` 持续每 15s 触发（满足 OKX < 30s 要求）

---

### Phase 2：消息处理层 — 修复 `OnDataMessage` 中 pong 的误解析

**文件**：`/mnt/c/Users/Jagger/Documents/Code/Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/Core/OKXBaseBrokerage.cs`（`OnDataMessage` 位于此文件约 line 670，非 Messaging.cs）

修改 `OnDataMessage()`：

```csharp
private void OnDataMessage(WebSocketMessage webSocketMessage)
{
    try
    {
        var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;
        var rawMessage = e.Message;

        // Silently ignore pong responses from public WebSocket connections.
        // These are OKX responses to the heartbeat pings sent by OKXWebSocketWrapper.
        // Note: _lastMessageTime is intentionally NOT updated here — that timeout
        // detection is exclusively for the private WebSocket connection.
        if (rawMessage == "pong" || rawMessage == "ping")
            return;

        ProcessMessage(rawMessage);
    }
    catch (Exception ex)
    {
        Log.Error($"{GetType().Name}.OnDataMessage(): Error processing message: {ex}");
    }
}
```

---

### Phase 3：订阅限制修复 — 降低 `MaximumSymbolsPerConnection`

**文件**：`/mnt/c/Users/Jagger/Documents/Code/Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/Core/OKXBaseBrokerage.cs`

```csharp
// 修改前
protected const int MaximumSymbolsPerConnection = 15;  // 15 × 3 channels = 45 subscriptions (EXCEEDS OKX limit of 30)

// 修改后
protected const int MaximumSymbolsPerConnection = 10;  // 10 × 3 channels = 30 subscriptions (at OKX limit)
```

**注释同步更新**（line 257）：
```csharp
// 修改前
MaximumSymbolsPerConnection,  // 15 symbols × 2 channels = 30 channels (OKX recommends < 30 for books)

// 修改后
MaximumSymbolsPerConnection,  // 10 symbols × 3 channels = 30 subscriptions (OKX per-connection subscription limit)
```

**影响评估**：
- 200 symbols → 20 个 WS 连接（之前 14 个）
- 20 < OKX 每频道 30 连接上限 ✅
- 连接数增加 → 每次断连重连订阅请求从 45 降为 30 → 减轻 rate limit 压力 ✅

---

### Phase 5：测试

**文件**：`TestsCustom/Tests/Brokerages/OKX/OKXHeartbeatTests.cs`

| 测试 | 验证点 |
|------|--------|
| `BroadcastPing_SendsPingToAllOpenConnections` | 3 个 mock WS 全部收到 `"ping"` |
| `BroadcastPing_SkipsClosedConnections` | IsOpen=false 的 WS 不调用 Send |
| `BroadcastPing_ContinuesOnSingleFailure` | 第 1 个 WS Send 抛异常时，第 2、3 个仍被调用 |
| `OnDataMessage_PongIsIgnoredSilently` | `"pong"` 消息不触发 `ProcessMessage`，无 parse error |
| `OnDataMessage_ValidMessageStillProcessed` | 正常 JSON 消息正常路由 |
| `MaximumSymbolsPerConnection_Is10` | 常量值为 10，10 × 3 = 30 ≤ OKX limit |

---

## Complexity Tracking

| Decision | Why Needed | Simpler Alternative Rejected Because |
|----------|------------|--------------------------------------|
| 在 LEAN 层加 `BroadcastPing()` 而非在 OKX 层反射 | 需要访问私有 `_webSocketEntries` | 反射脆弱，在 LEAN 层加方法是正确的扩展点 |
| `BroadcastPing()` 内 `ToList()` + lock 外 Send | 避免持锁时 Send 阻塞（网络操作） | 简单 lock 包裹整个 for 循环会在网络延迟时卡住整个订阅操作 |
| 公有 WS pong 不更新 `_lastMessageTime` | 该字段仅用于私有 WS 超时检测 | 若更新，私有 WS 真正超时时会被公有 WS 的 pong 掩盖 |
| `as` cast 而非直接强转 | `SubscriptionManager` 在测试中可能被替换为 mock | 强转在测试环境抛 `InvalidCastException` |
| `MaximumSymbolsPerConnection = 10`（而非更小值）| 10 × 3 = 30，恰好达到 OKX per-connection 上限 | 更小（如 8）浪费连接槽位；更大（如 12）超出限制 |

---

## Files Changed Summary

| 文件 | 改动类型 | 行数变化 |
|------|---------|---------|
| `Lean/Brokerages/BrokerageMultiWebSocketSubscriptionManager.cs` | 新增方法 | +20 |
| `OKXBaseBrokerage.cs` | 修改方法体 + 修改常量 | +6 |
| `OKXBaseBrokerage.Messaging.cs` | 修改方法体 | +7 |
| `OKXHeartbeatTests.cs` | 新建测试 | +~130 |

**总计**：约 163 行变化，零架构改动，零接口变更。
