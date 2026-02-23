# Feature Specification: FOK 限价基于 Orderbook + PriceLimit 的 Quantity-Aware 定价

**Feature**: `001-fok-orderbook-pricing`
**Created**: 2026-02-23
**Status**: Draft
**Input**: Crypto Spot Buy 的 FOK 限价使用固定 0.3% buffer，不考虑订单量和实际订单簿深度，导致大单 FOK 反复失败

## 背景

### 当前 FOK 转换机制

OKX Brokerage 对 Crypto Spot Buy MarketOrder 转为 FOK Limit Order（`BuildSpotMarketBuyAsFokLimitRequest`）：

```
IsSpotMarketBuy = Market + Quantity > 0 + SecurityType.Crypto
limitPrice = BestAskPrice × (1 + DefaultMarketBuyPriceBuffer)
DefaultMarketBuyPriceBuffer = 0.003 (0.3%)
```

价格来源优先级：
1. 本地 orderbook (`_orderBooks[symbol].BestAskPrice`)
2. REST ticker fallback (`RestApiClient.GetTicker(instId).LowestAsk`)

### 存在的问题

1. **限价不感知订单量**：无论下 10 XRP 还是 1000 XRP，limitPrice 都是 `bestAsk × 1.003`。如果第一层 50@0.500 深度不够，需要吃到第二层 0.502 才能成交，但 `0.500 × 1.003 = 0.5015 < 0.502` → FOK 失败
2. **GetTicker fallback 多余**：本地已维护实时 orderbook 快照，比 REST ticker 更及时更完整。当 orderbook 为空时下单本身就不该发生（算法层应已拦截）
3. **与 OKX 限价系统脱节**：brokerage 已通过 `_priceLimitSync` 维护实时 `buyLmt`/`sellLmt`，但 FOK 定价完全没有利用它
4. **0.3% 硬编码与设计目标矛盾**：设计目的是"保证成交"，实际效果是"限制成交"。太窄无法覆盖深层流动性，太宽则滑点保护失效

### 已有基础设施

- `_orderBooks[symbol]`：本地 orderbook 快照，Asks 按价格升序排列
- `_priceLimitSync.GetState(symbol)`：实时 OKX 限价（`buyLmt`/`sellLmt`），已通过 REST 初始化 + WS 持续更新
- `TruncateByPriceLimit`：已在订单簿层截断超限层级（但 FOK 定价未利用）

## User Scenarios & Testing

### User Story 1 - FOK 限价基于订单量和 Orderbook 深度定价 (Priority: P1)

作为套利算法运营者，我希望 Crypto Spot Buy 的 FOK 限价能根据实际下单量和订单簿深度来确定，而非使用固定百分比 buffer，使 FOK 订单能覆盖所需的全部深度层级。

**Why this priority**: 当前固定 buffer 是 FOK 反复失败的直接原因——限价覆盖不了下单量实际需要吃到的深度层级。

**Independent Test**: 构造 orderbook [0.500×50, 0.502×100, 0.510×500]，下单 120 XRP。验证 FOK 限价至少覆盖到 0.502（而非 0.500 × 1.003 = 0.5015）。

**Acceptance Scenarios**:
1. **Given** 下单 30 XRP，Asks=[0.500×50, 0.502×100]，**When** 构建 FOK 请求，**Then** limitPrice 基于第一层 0.500（30 < 50，第一层足够），加微小 buffer
2. **Given** 下单 120 XRP，Asks=[0.500×50, 0.502×100, 0.510×500]，**When** 构建 FOK 请求，**Then** limitPrice 基于第二层 0.502（50+100=150 ≥ 120），加微小 buffer
3. **Given** 下单 200 XRP，Asks=[0.500×50, 0.502×100]，深度总计 150 不够，**When** 构建 FOK 请求，**Then** limitPrice 基于最深层 0.502 加 buffer（尽力覆盖，算法层已约束量在可成交范围内）

---

### User Story 2 - FOK 限价受 OKX PriceLimit 上界约束 (Priority: P1)

作为套利算法运营者，我希望 FOK 限价不超过 OKX 交易所允许的最高买入价（buyLmt），避免因限价超出交易所规则而被拒绝。

**Why this priority**: OKX 会拒绝价格超过 buyLmt 的订单，FOK 限价必须在合规范围内。

**Independent Test**: 构造 orderbook 最深层 0.510，buyLmt=0.508，下单量需要吃到 0.510 层。验证 limitPrice 被截断到 0.508。

**Acceptance Scenarios**:
1. **Given** 需吃到的最深层 worstPrice=0.510，buyLmt=0.520，**When** 构建 FOK 请求，**Then** limitPrice = worstPrice + buffer（buyLmt 不构成限制）
2. **Given** 需吃到的最深层 worstPrice=0.510，buyLmt=0.508，**When** 构建 FOK 请求，**Then** limitPrice = buyLmt（被截断）
3. **Given** PriceLimit 未启用（Enabled=false），**When** 构建 FOK 请求，**Then** limitPrice = worstPrice + buffer（无 PriceLimit 约束）

---

### User Story 3 - 移除 GetTicker REST fallback (Priority: P2)

作为系统维护者，我希望移除 FOK 定价中的 GetTicker REST fallback，简化代码路径，因为：本地 orderbook 已是更优数据源，当 orderbook 为空时算法层应已拦截下单。

**Why this priority**: 简化代码，消除不一致的数据源。REST 调用增加延迟且数据不如本地 orderbook 及时。

**Independent Test**: 验证本地 orderbook 为空时直接抛出异常（而非 fallback 到 REST ticker），且算法层在此之前已阻止下单。

**Acceptance Scenarios**:
1. **Given** 本地 orderbook 有数据，**When** 构建 FOK 请求，**Then** 使用本地 orderbook 逐层计算价格（不调用 REST）
2. **Given** 本地 orderbook 为空，**When** 构建 FOK 请求，**Then** 抛出异常（防御性，正常流程不应到达此处）

---

### Edge Cases
- 订单量恰好等于某一层的累计深度 → worstPrice = 该层价格
- 订单量超过全部深度 → worstPrice = 最深层价格（算法层已限制量，此处为防御性兜底）
- 本地 orderbook Asks 为空 → 抛出异常
- buyLmt < bestAsk（极端情况，限价低于当前最优价）→ limitPrice = buyLmt，FOK 预期会失败，但这是交易所限制
- worstPrice 的 buffer 需要考虑 tickSize 对齐

## Requirements

### Functional Requirements

- **FR-001**: `BuildSpotMarketBuyAsFokLimitRequest` MUST 使用本地 orderbook 按 order.Quantity 逐层累加，确定需要吃到的最深层价格（worstPrice）
- **FR-002**: FOK limitPrice MUST 基于 worstPrice 加微小 buffer（如 1 个 tickSize 或固定小百分比），而非固定的 bestAsk × 1.003
- **FR-003**: FOK limitPrice MUST 不超过 `_priceLimitSync.GetState(symbol).buyLmt`（当 PriceLimit 启用时）
- **FR-004**: 当本地 orderbook 无 Asks 数据时，MUST 抛出异常而非 fallback 到 REST ticker
- **FR-005**: `DefaultMarketBuyPriceBuffer` 常量 SHOULD 被移除或改为 worstPrice 的微小 buffer 用途
- **FR-006**: limitPrice MUST 符合交易品种的 tickSize 精度要求

### Key Entities

- **`BuildSpotMarketBuyAsFokLimitRequest`**: 需要重构的核心方法，当前使用 bestAsk × 1.003
- **`_orderBooks[symbol]`**: 本地 orderbook 快照，Asks 按价格升序
- **`_priceLimitSync`**: 实时 OKX 限价同步器，提供 buyLmt/sellLmt
- **worstPrice**: 新概念——按 order.Quantity 逐层累加后需要吃到的最深层 Ask 价格

## Success Criteria

### Measurable Outcomes
- **SC-001**: FOK limitPrice 能覆盖 order.Quantity 所需的全部深度层级
- **SC-002**: FOK limitPrice 不超过 OKX buyLmt
- **SC-003**: 消除 GetTicker REST fallback 调用
- **SC-004**: Crypto Spot Sell 和 CryptoFuture 订单（不走 FOK）行为不受影响
