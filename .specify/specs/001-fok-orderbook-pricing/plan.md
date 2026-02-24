# Implementation Plan: FOK Orderbook Pricing

**Feature**: `001-fok-orderbook-pricing` | **Date**: 2026-02-23 | **Spec**: [spec.md](./spec.md)

## Summary

重构 `BuildSpotMarketBuyAsFokLimitRequest`，将 FOK 限价从固定 `bestAsk × 1.003` 改为基于 orderbook 逐层累加的 quantity-aware 定价，并用 PriceLimit(`buyLmt`) 做上界截断。移除 GetTicker REST fallback。

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: OKXBaseBrokerage（partial class，分布在多个 .cs 文件）
**Testing**: NUnit（已有 `OKXPriceLimitTests.cs` 可参考模式）
**Target Platform**: Linux (WSL2) / Live trading
**Constraints**: `PlaceOrder` 在 `_messageHandler.WithLockedStream` 内执行，需保持线程安全

## Project Structure

### 需要修改的文件

```
QuantConnect.OKXBrokerage/
├── Core/
│   └── OKXBaseBrokerage.Orders.cs        # 核心：重构 BuildSpotMarketBuyAsFokLimitRequest
│
QuantConnect.OKXBrokerage.Tests/
└── OKXOrderManagementTests.cs            # 追加 FOK 定价单元测试 region
```

### 只读参考的文件（不修改）

```
QuantConnect.OKXBrokerage/
├── Core/
│   ├── OKXBaseBrokerage.PriceLimit.cs    # _priceLimitSync.GetState() 接口
│   ├── OKXBaseBrokerage.OrderBook.cs     # _orderBooks / OKXOrderBook
│   └── OKXBaseBrokerage.cs              # _orderBooks 字典定义
├── Messages/
│   └── PriceLimit.cs                     # PriceLimit DTO (BuyLimit/SellLimit/Enabled)
└── OKXOrderBook.cs                       # GetAsks() 返回 List<KVP<decimal,decimal>>
```

## Implementation Approach

### Phase 0: Research

**Decision 1: worstPrice buffer 策略**

- **选择**: worstPrice 本身即可，不加额外 buffer
- **理由**: FOK 语义是"在 limitPrice 及以内全量成交"。worstPrice 是按 order.Quantity 走完深度后的最深层价格。只要 limitPrice >= worstPrice，FOK 就能覆盖所有需要的层级。额外 buffer 没有实际作用——OKX 撮合引擎会在 limitPrice 以内按最优价成交，不会因为 limitPrice 恰好等于某层价格而拒绝
- **排除**: 加 1 个 tickSize 的 buffer（需要额外获取 tickSize，增加复杂度，无实际收益）

**Decision 2: orderbook 为空时的行为**

- **选择**: 抛出 `InvalidOperationException`（与当前 bestAsk <= 0 的处理一致）
- **理由**: 算法层应在此之前拦截。如果仍然到达此处，说明调用链有 bug，应及早暴露而非静默降级

**Decision 3: buyLmt 截断后的含义**

- **选择**: 直接用 `buyLmt` 作为 limitPrice（当 worstPrice > buyLmt 时）
- **理由**: buyLmt 是交易所允许的最高价格，超过会被拒绝。截断后 FOK 可能因深度不完全在 buyLmt 内而失败，但这是交易所规则，不应绕过

### Phase 1: Core Implementation

#### 1.1 重构 `BuildSpotMarketBuyAsFokLimitRequest`

**当前代码** (`OKXBaseBrokerage.Orders.cs:145-183`):

```csharp
private Messages.PlaceOrderRequest BuildSpotMarketBuyAsFokLimitRequest(Order order, string instId, string tdMode)
{
    var bestAsk = 0m;
    if (_orderBooks.TryGetValue(order.Symbol, out var orderBook))
        bestAsk = orderBook.BestAskPrice;
    if (bestAsk <= 0)
    {
        var ticker = RestApiClient.GetTicker(instId)?.FirstOrDefault();
        bestAsk = ticker?.LowestAsk ?? 0m;
    }
    if (bestAsk <= 0) throw ...;

    var limitPrice = bestAsk * (1 + DefaultMarketBuyPriceBuffer);
    ...
}
```

**改为**:

```csharp
private Messages.PlaceOrderRequest BuildSpotMarketBuyAsFokLimitRequest(Order order, string instId, string tdMode)
{
    // 1. 获取本地 orderbook
    if (!_orderBooks.TryGetValue(order.Symbol, out var orderBook))
        throw new InvalidOperationException($"No order book available for {instId}");

    // 2. 逐层累加 Asks，找到覆盖 order.Quantity 所需的 worstPrice
    var limitPrice = CalculateFokLimitPrice(orderBook, order.Symbol, Math.Abs(order.Quantity));

    Log.Trace($"OKXBaseBrokerage.BuildSpotMarketBuyAsFokLimitRequest(): " +
        $"{instId} qty={Math.Abs(order.Quantity)} limitPrice={limitPrice}");

    return new Messages.PlaceOrderRequest
    {
        InstrumentId = instId,
        TradeMode = tdMode,
        Side = "buy",
        OrderType = "fok",
        Size = Math.Abs(order.Quantity).ToStringInvariant(),
        Price = limitPrice.ToStringInvariant(),
        ClientOrderId = order.Id.ToStringInvariant(),
        Tag = HashOrderTag(order.Tag)
    };
}
```

#### 1.2 新增 `CalculateFokLimitPrice` 方法

```csharp
/// <summary>
/// 根据 orderbook 深度和 PriceLimit 计算 FOK 限价。
/// 逐层累加 Asks 直到覆盖所需数量，取最深层价格。
/// 结果不超过 OKX buyLmt（启用时）。
/// </summary>
internal decimal CalculateFokLimitPrice(OKXOrderBook orderBook, Symbol symbol, decimal quantity)
{
    var asks = orderBook.GetAsks();  // List<KVP<decimal,decimal>>, ascending by price

    if (asks == null || asks.Count == 0)
        throw new InvalidOperationException($"No ask levels in order book for {symbol}");

    // Walk depth: accumulate quantity, track worst price
    var accumulated = 0m;
    var worstPrice = asks[0].Key;  // fallback: best ask

    foreach (var level in asks)
    {
        worstPrice = level.Key;
        accumulated += level.Value;
        if (accumulated >= quantity) break;
    }

    // Apply PriceLimit ceiling
    var limit = _priceLimitSync?.GetState(symbol);
    if (limit?.Enabled == true)
    {
        var buyLmt = ParseHelper.ParseDecimal(limit.BuyLimit);
        if (buyLmt > 0 && worstPrice > buyLmt)
        {
            worstPrice = buyLmt;
        }
    }

    return worstPrice;
}
```

**关键设计点**:
- `GetAsks()` 是线程安全的（内部有 lock）
- `accumulated >= quantity` 时 break，找到恰好覆盖所需量的层级
- 深度不够时（accumulated < quantity），worstPrice 停在最深层——算法层已限量，此处防御性兜底
- `_priceLimitSync?.GetState()` 可能为 null（Spot 不一定有 PriceLimit），安全处理

#### 1.3 清理

- 删除 `DefaultMarketBuyPriceBuffer` 常量（第 33 行）
- 删除 `GetTicker` fallback 逻辑（第 156-160 行的 REST 调用）

### Phase 2: Integration

无外部集成点。改动完全在 `PlaceOrder → BuildSpotMarketBuyAsFokLimitRequest` 内部路径。

**影响分析**:
- `IsSpotMarketBuy` 判断逻辑不变 → 路由不受影响
- `BuildStandardOrderRequest` 不变 → Spot Sell / CryptoFuture 不受影响
- FOK 请求结构不变（仍是 ordType="fok"，只是 Price 字段计算方式变了）

### Phase 3: Validation

#### 单元测试（追加到 `OKXOrderManagementTests.cs`）

在现有文件中新增 `#region FOK Pricing Tests` region。这些测试不需要 API credentials，使用 `OKXPriceLimitTests.cs` 中已有的 `TestableOKXBrokerage` mock 模式（需要将该内部类提取为共享 helper，或在 `OKXOrderManagementTests` 中重新构造）。

| 测试 | 场景 | 预期 |
|------|------|------|
| FokPrice_SingleLevel_Sufficient | qty=30, Asks=[0.500×50] | limitPrice = 0.500 |
| FokPrice_MultiLevel_Walk | qty=120, Asks=[0.500×50, 0.502×100, 0.510×500] | limitPrice = 0.502 |
| FokPrice_InsufficientDepth_UsesDeepest | qty=200, Asks=[0.500×50, 0.502×100] | limitPrice = 0.502 (防御性) |
| FokPrice_ExactBoundary | qty=50, Asks=[0.500×50, 0.502×100] | limitPrice = 0.500 (恰好够) |
| FokPrice_PriceLimit_Truncates | worstPrice=0.510, buyLmt=0.508 | limitPrice = 0.508 |
| FokPrice_PriceLimit_NoLimit | worstPrice=0.510, buyLmt=0.520 | limitPrice = 0.510 |
| FokPrice_PriceLimit_Disabled | worstPrice=0.510, Enabled=false | limitPrice = 0.510 |
| FokPrice_EmptyAsks_Throws | Asks=[] | InvalidOperationException |
| FokPrice_NoOrderBook_Throws | _orderBooks 无该 symbol | InvalidOperationException |

**测试模式**: 参考 `OKXPriceLimitTests.cs` 的 `TestableOKXBrokerage` 模式，构造 OKXOrderBook 并注入 ask levels + PriceLimit state。FOK 定价测试无需 API credentials，不受 `OneTimeSetUp` 的 `Assert.Ignore` 影响（需独立 setup 或标记为不依赖 credentials）。

## Complexity Tracking

| Decision | Why Needed | Simpler Alternative Rejected Because |
|----------|------------|-------------------------------------|
| worstPrice 不加 buffer | 减少复杂度 | 加 tickSize buffer 需要额外获取 instrument 数据，无实际收益 |
| 抛异常而非 fallback | 快速暴露 bug | GetTicker fallback 掩盖了上游未正确初始化 orderbook 的问题 |
| `CalculateFokLimitPrice` 提取为 internal 方法 | 可单元测试 | inline 在 Build 方法中无法独立测试 |
