# Feature Specification: 公有 WebSocket 心跳保活修复

**Feature**: `002-public-ws-heartbeat`
**Created**: 2026-03-04
**Status**: Draft
**Input**: 实盘监控中小流动性币种（NIGHTUSDT、XPLUSDT 等）的 `security.Price`（Mid）长期停留在历史旧值，与实时 BidPrice/AskPrice 严重不符，导致监控界面和订单记录中的价格显示错误。

---

## 背景

### OKX WebSocket 保活要求

OKX 文档明确要求：

- 客户端必须每 **< 30 秒**发送字符串 `"ping"`
- 服务端响应字符串 `"pong"`
- 若 30 秒内未收到客户端消息，服务端**主动断开连接**

本项目 Constitution（§4.2）也已明确此规则，并规定 `KeepAliveIntervalSeconds = 20`。

### 当前实现的问题

`OKXBaseBrokerage.cs` 中的 `_keepAliveTimer`（15 秒间隔）和 `SendHeartbeat()` **仅向私有 WebSocket 发送 ping**：

```csharp
// OKXBaseBrokerage.cs
private void SendHeartbeat()
{
    // ...
    WebSocket.Send("ping");  // ← WebSocket 是私有连接（orders/account）
}
```

由 `BrokerageMultiWebSocketSubscriptionManager` 管理的**所有公有 WebSocket 连接**（orderbook/trades/price-limit 频道）**没有任何心跳机制**。

### 故障链路

```
公有 WS 连接无心跳
    → 30s 无数据推送（小流动性币种）
    → OKX 服务端关闭连接
    → WebSocketClientWrapper 感知到断开（ReceiveMessage 返回 null）
    → 等待 waitTimeOnError（初始 2s，指数增长至 120s）后重连
    → OnOpen 触发 → Task.Factory.StartNew() 重新订阅所有频道
    → OKX 推送 snapshot → security.Price 恢复
    → （重复，每 ~32s 一次）
```

**断连期间**（2~120s）：
- `security.BidPrice` / `security.AskPrice`：最后一次成功更新的值（相对较新）
- `security.Price`：同上，但监控快照每 5s 写一次 Redis，断连期间读到的是旧值

**最坏情况**：若 `waitTimeOnError` 因多次失败累积到 120s，价格将冻结长达 2 分钟。

### 为什么只影响小流动性币种

对于流动性好的标的（BTC-USDT、ETH-USDT 等），OKX 每隔数毫秒推送一次 orderbook 更新，连接上的数据流从未中断，因此 30s 超时不会触发。

小流动性币种（NIGHT、XPLUS 等）可能数分钟无任何 orderbook 变化，连接上长时间无任何消息，OKX 服务端在 30s 后主动断开。

---

## 现有基础设施

- `BrokerageMultiWebSocketSubscriptionManager`：管理所有公有 WS 连接池，`MaximumSymbolsPerConnection = 15`
- `WebSocketClientWrapper.HandleConnection()`：外层 while 循环实现自动重连，初始等待 2s，指数增长至 120s
- `BrokerageMultiWebSocketSubscriptionManager.OnOpen()`：重连后通过 `Task.Factory.StartNew()` 重新订阅所有 symbol
- `_keepAliveTimer`（15s）：仅作用于私有 WS，**不覆盖公有 WS**

---

## User Scenarios & Testing

### User Story 1 - 公有 WebSocket 连接保持存活（P0）

作为套利算法运营者，我希望所有公有 WebSocket 连接（orderbook/trades/price-limit 频道）能在 OKX 规定的 30 秒超时前定期收到 ping，使连接始终保持活跃，避免因心跳缺失导致的价格数据中断。

**Why this priority**: 连接中断直接导致 `security.Price` 在监控和订单记录中显示错误值，是影响运营可信度的 P0 Bug。

**Independent Test**: 启动算法，订阅 10 个低流动性的测试币种，观察 60 秒内公有 WS 连接是否被 OKX 断开（日志中搜索 `WebSocketClientWrapper.HandleConnection` 中的 Connecting... 重连日志）。修复后，60 秒内不应出现重连日志。

**Acceptance Scenarios**:
1. **Given** 公有 WS 连接已建立且 25 秒内无任何 orderbook 推送，**When** 心跳计时器触发，**Then** 向该连接发送字符串 `"ping"`，OKX 响应 `"pong"`，连接保持活跃
2. **Given** 公有 WS 连接上有活跃数据流（液态币种持续推送），**When** 心跳计时器触发，**Then** 仍发送 ping（无副作用）
3. **Given** 公有 WS 连接已有私有 WS 的 `"pong"` 响应处理逻辑，**Then** 公有 WS 的 `"pong"` 响应**不应**被误路由到私有频道处理器

---

### Edge Cases

- 公有 WS 连接在发送 ping 时已断开 → 捕获异常，记录警告，不中断整体保活循环
- 多个公有 WS 连接同时需要发送 ping → 并行发送，互不阻塞
- OKX 响应 `"pong"` 被路由到公有 WS 消息处理器 → 静默忽略（不解析为 JSON）
- 私有 WS 和公有 WS 的保活定时器合并为一个 → 可以，但需确保两类连接都覆盖

---

## Requirements

### Functional Requirements

- **FR-001**: `OKXBaseBrokerage` 的保活机制 MUST 同时覆盖私有 WebSocket 和所有公有 WebSocket 连接（由 SubscriptionManager 管理）
- **FR-002**: 保活间隔 MUST < 30 秒（当前 `_keepAliveTimer` 为 15s，SHOULD 保持或调整为 Constitution 规定的 20s）
- **FR-003**: 公有 WS 的 `"pong"` 响应 MUST 被正确识别并静默处理（不触发 JSON 解析错误）
- **FR-004**: 每个公有 WebSocket 连接的频道订阅总数 MUST ≤ 30（OKX per-connection subscription limit）。当前 `MaximumSymbolsPerConnection = 15`，15 symbols × 3 channels = 45 > 30，MUST 改为 10（10 × 3 = 30）

### Non-Functional Requirements

- **NFR-001**: 心跳发送不得阻塞 orderbook 处理线程（异步发送或使用独立线程）
- **NFR-002**: 心跳机制不得增加 OKX 订阅速率消耗（ping 不计入 480次/小时 限制）

### Key Entities

- **`OKXBaseBrokerage.cs`**: `SendHeartbeat()` 和 `_keepAliveTimer`，需扩展以覆盖公有 WS；`MaximumSymbolsPerConnection` 需从 15 改为 10
- **`BrokerageMultiWebSocketSubscriptionManager`**: 公有 WS 连接池，需暴露 ping 能力或由外部遍历连接发送
- **`OKXBaseBrokerage.Messaging.cs`**: `"pong"` 响应处理，需确保公有 WS 的 pong 不触发错误

---

## Root Cause Summary

| 层次 | 问题 | 影响 |
|------|------|------|
| **OKX Brokerage（根因）** | 公有 WS 无心跳，30s 后 OKX 断开连接 | 所有公有频道数据中断 |
| **重连恢复** | 自动重连 + 重订阅已实现，但有 2~120s 间隙 | 断连期间价格冻结 |
| **监控展示** | 正确使用 `security.Price`，该字段本身是正确设计 | 心跳修复后 `security.Price` 将始终准确，无需修改监控层 |

---

## Success Criteria

- **SC-001**: 低流动性币种（24h 内无 orderbook 变动的标的）的公有 WS 连接在 60 秒内不出现断连重连日志
- **SC-002**: 监控 `Price` 字段（Mid = `security.Price`）与 `BidPrice`/`AskPrice` 的偏差 < 0.01%（允许单 tick 大小误差）
- **SC-003**: 公有 WS 收到 `"pong"` 响应后不产生 JSON 解析错误日志
- **SC-004**: 每个公有 WS 连接的订阅频道数 = symbols_per_connection × 3 ≤ 30
