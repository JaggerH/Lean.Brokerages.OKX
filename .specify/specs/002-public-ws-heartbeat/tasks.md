# Tasks: 公有 WebSocket 心跳保活修复

**Prerequisites**: `plan.md`, `spec.md`
**Feature**: `002-public-ws-heartbeat`
**Strategy**: 心跳逻辑内置于 OKX 自有的 `OKXWebSocketWrapper`，零 LEAN 官方代码改动

---

## Phase 1: Foundational — OKXWebSocketWrapper 自包含心跳

> 每个公有 WS 连接都是 OKXWebSocketWrapper 实例，心跳在构造时绑定，生命周期由 Open/Closed 事件驱动

- [x] T001 修改 `Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/OKXWebSocketWrapper.cs`：添加 `using System.Timers`，声明 `private const int HeartbeatIntervalMs = 15_000` 和 `private readonly Timer _heartbeatTimer`；构造函数内初始化计时器（AutoReset=true），订阅自身 `Open` 事件启动计时器、`Closed` 事件停止计时器；添加私有方法 `OnHeartbeatElapsed`：检查 `IsOpen` 后调用 `Send("ping")`，异常时 `Log.Error`

---

## Phase 2: User Story 1 — pong 处理修复 (P0)

**Goal**: 公有 WS 收到 pong 响应时不触发 JSON 解析错误
**Independent Test**: 发送 `"pong"` 到 `OnDataMessage`，确认无 `Log.Error` 产生

- [x] T002 [US1] 修改 `Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/Core/OKXBaseBrokerage.cs` 中的 `OnDataMessage()` 方法（约 line 670）：在 cast 取得 `rawMessage` 后、调用 `ProcessMessage` 前，添加 `if (rawMessage == "pong" || rawMessage == "ping") return;` 早退，附注释说明不更新 `_lastMessageTime` 的原因

---

## Phase 3: FR-004 — 订阅数量合规 (P0)

**Goal**: 每连接频道订阅数 10×3=30，符合 OKX per-connection 上限
**Independent Test**: `MaximumSymbolsPerConnection == 10`，`10 × 3 == 30 ≤ 30`

- [x] T003 [P] [US1] 修改 `Lean.Brokerages.OKX/QuantConnect.OKXBrokerage/Core/OKXBaseBrokerage.cs` 常量 `MaximumSymbolsPerConnection`：值从 `15` 改为 `10`，注释更新为 `"10 symbols × 3 channels = 30 subscriptions (OKX per-connection subscription limit)"`

- [x] T004 [US1] 同文件（约 line 257）更新 SubscriptionManager 构造调用处的行内注释与新常量值一致

---

## Phase 4: Polish — 验证构建

- [ ] T005 在 `Lean.Brokerages.OKX/` 目录执行 `dotnet build` 确认所有改动编译通过，无错误无警告

---

## Dependencies & Execution Order

```
T001 (OKXWebSocketWrapper 心跳)  ← 独立，先行
T002 (OnDataMessage pong fix)    ← 独立，可与 T003/T004 并行
T003 (MaximumSymbols = 10)       ← 独立，可与 T002 并行
T004 (注释更新，同文件 T003)     ← T003 之后
T005 (build)                     ← 依赖全部前置任务
```

## Implementation Strategy

全部 5 个任务均为必须，涉及 2 个文件（`OKXWebSocketWrapper.cs` + `OKXBaseBrokerage.cs`），零 LEAN 官方代码改动。
