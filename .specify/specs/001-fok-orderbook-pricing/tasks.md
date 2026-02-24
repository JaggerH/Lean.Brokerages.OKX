# Tasks: FOK Orderbook Pricing

**Prerequisites**: plan.md, spec.md

## Phase 1: Core Implementation - Quantity-Aware FOK 定价 (P1)

**Goal**: 将 FOK 限价从 `bestAsk × 1.003` 改为基于 orderbook 逐层累加的 quantity-aware 定价
**Independent Test**: 单元测试验证不同深度/量组合下的 limitPrice 计算

- [X] T001 [US1] 新增 `CalculateFokLimitPrice` internal 方法到 `QuantConnect.OKXBrokerage/Core/OKXBaseBrokerage.Orders.cs`。逐层累加 `orderBook.GetAsks()`，找到覆盖 `quantity` 所需的 worstPrice。当 asks 为空时抛出 `InvalidOperationException`
- [X] T002 [US1] 重构 `BuildSpotMarketBuyAsFokLimitRequest` (`OKXBaseBrokerage.Orders.cs:145-183`)：移除 bestAsk × buffer 逻辑，改为调用 `CalculateFokLimitPrice(orderBook, order.Symbol, Math.Abs(order.Quantity))`。当 `_orderBooks` 无该 symbol 时直接抛出异常
- [X] T003 [US1] 删除 `DefaultMarketBuyPriceBuffer` 常量 (`OKXBaseBrokerage.Orders.cs:33`)

## Phase 2: PriceLimit 上界约束 (P1)

**Goal**: FOK 限价不超过 OKX buyLmt
**Independent Test**: 单元测试验证 worstPrice > buyLmt 时被截断

- [X] T004 [US2] 在 `CalculateFokLimitPrice` 末尾追加 PriceLimit 截断逻辑：通过 `_priceLimitSync?.GetState(symbol)` 获取 buyLmt，当 Enabled 且 worstPrice > buyLmt 时截断到 buyLmt

## Phase 3: 移除 GetTicker Fallback (P2)

**Goal**: 简化数据源，消除 REST ticker 调用
**Independent Test**: 验证 orderbook 为空时抛异常而非 fallback

- [X] T005 [US3] 从 `BuildSpotMarketBuyAsFokLimitRequest` 中移除 `RestApiClient.GetTicker(instId)` fallback 逻辑 (`OKXBaseBrokerage.Orders.cs:156-160`)。现在由 T002 的 `_orderBooks.TryGetValue` 失败路径直接抛异常

## Phase 4: 单元测试

**Goal**: 覆盖所有 FOK 定价场景

- [X] T006 [P] 在 `QuantConnect.OKXBrokerage.Tests/OKXOrderManagementTests.cs` 新增 `#region FOK Pricing Tests`，构造 `TestableOKXBrokerage`（参考 `OKXPriceLimitTests.cs` 模式），注入 OKXOrderBook ask levels 和 PriceLimit state。这些测试无需 API credentials
- [X] T007 [P] 编写 depth walk 测试：`FokPrice_SingleLevel_Sufficient`（qty=30, Asks=[0.500×50] → 0.500）、`FokPrice_MultiLevel_Walk`（qty=120, Asks=[0.500×50, 0.502×100, 0.510×500] → 0.502）、`FokPrice_ExactBoundary`（qty=50, Asks=[0.500×50, 0.502×100] → 0.500）
- [X] T008 [P] 编写边界测试：`FokPrice_InsufficientDepth_UsesDeepest`（qty=200, depth=150 → 最深层 0.502）、`FokPrice_EmptyAsks_Throws`（Asks=[] → InvalidOperationException）、`FokPrice_NoOrderBook_Throws`（无 symbol → InvalidOperationException）
- [X] T009 [P] 编写 PriceLimit 测试：`FokPrice_PriceLimit_Truncates`（worstPrice=0.510, buyLmt=0.508 → 0.508）、`FokPrice_PriceLimit_NoLimit`（worstPrice=0.510, buyLmt=0.520 → 0.510）、`FokPrice_PriceLimit_Disabled`（Enabled=false → 0.510）

## Dependencies & Execution Order

```
T001 → T002 → T003 (顺序：先有方法，再重构调用，再清理常量)
         ↓
        T004 (在 T002 完成后追加 PriceLimit 逻辑)
         ↓
        T005 (T002 已移除旧逻辑后，确认 fallback 也被清除)

T006 → T007, T008, T009 (T006 搭建 test infra 后，三组测试可并行)
```

Phase 4 (测试) 可与 Phase 1-3 (实现) 并行开发，因为测试基于 `CalculateFokLimitPrice` 的 internal 签名，不依赖 Build 方法的具体重构。

## Implementation Strategy

### MVP First (US1 Only)
1. T001 + T002 + T003：完成 quantity-aware 定价核心
2. T006 + T007：验证基本 depth walk 正确性
3. 此时已解决"FOK 反复失败"的核心问题

### Incremental Delivery
4. T004：追加 PriceLimit 约束（安全网）
5. T005：清理 GetTicker fallback
6. T008 + T009：补全边界和 PriceLimit 测试
