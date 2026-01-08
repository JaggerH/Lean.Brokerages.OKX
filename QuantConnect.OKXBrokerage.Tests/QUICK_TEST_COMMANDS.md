# 🚀 快速测试命令

## 🎯 新的命名规范（2025-11-18 更新）

所有测试方法现在使用市场前缀以确保精确过滤：
- **Spot** 市场：`Spot{TestName}` (例如：`SpotLongFromZero`)
- **Futures** 市场：`Futures{TestName}` (例如：`FuturesLongFromZero`)
- **Unified** 账户：`Unified{TestName}` (例如：`UnifiedLongFromZero`)

这解决了参数化测试的过滤问题，确保 `dotnet test --no-build --filter` 精确匹配单个测试方法。

---

## 📋 常用测试命令

### Futures 市场测试

```powershell
# LongFromZero - 从零仓位开多仓
dotnet test --no-build --filter "FullyQualifiedName~FuturesLongFromZero"

# ShortFromZero - 从零仓位开空仓
dotnet test --no-build --filter "FullyQualifiedName~FuturesShortFromZero"

# CloseFromLong - 从多仓平仓
dotnet test --no-build --filter "FullyQualifiedName~FuturesCloseFromLong"

# CloseFromShort - 从空仓平仓
dotnet test --no-build --filter "FullyQualifiedName~FuturesCloseFromShort"

# CancelOrders - 取消订单测试
dotnet test --no-build --filter "FullyQualifiedName~FuturesCancelOrders"
```

### Spot 市场测试

```powershell
# LongFromZero - 现货买入
dotnet test --no-build --filter "FullyQualifiedName~SpotLongFromZero"

# CloseFromLong - 现货卖出
dotnet test --no-build --filter "FullyQualifiedName~SpotCloseFromLong"

# CancelOrders - 取消订单测试
dotnet test --no-build --filter "FullyQualifiedName~SpotCancelOrders"
```

### Unified 账户测试

```powershell
# LongFromZero - 测试 Spot + Futures 双市场
dotnet test --no-build --filter "FullyQualifiedName~UnifiedLongFromZero"

# ShortFromZero
dotnet test --no-build --filter "FullyQualifiedName~UnifiedShortFromZero"

# CloseFromLong
dotnet test --no-build --filter "FullyQualifiedName~UnifiedCloseFromLong"

# CloseFromShort
dotnet test --no-build --filter "FullyQualifiedName~UnifiedCloseFromShort"

# CancelOrders
dotnet test --no-build --filter "FullyQualifiedName~UnifiedCancelOrders"
```

---

## 🔍 按测试类过滤

```powershell
# 运行 Spot 市场所有测试
dotnet test --no-build --filter "FullyQualifiedName~GateBrokerageSpotTests"

# 运行 Futures 市场所有测试
dotnet test --no-build --filter "FullyQualifiedName~GateBrokerageFuturesTests"

# 运行 Unified 账户所有测试
dotnet test --no-build --filter "FullyQualifiedName~GateBrokerageUnifiedTests"
```

---

## 🏷️ 按 Category 过滤

```powershell
# Spot 市场所有测试
dotnet test --no-build --filter "TestCategory=Spot"

# Futures 市场所有测试
dotnet test --no-build --filter "TestCategory=Futures"

# Unified 账户所有测试
dotnet test --no-build --filter "TestCategory=Unified"

# Unified 账户基础测试（不包含压力测试）
dotnet test --no-build --filter "TestCategory=Unified-Basic"

# 压力测试
dotnet test --no-build --filter "TestCategory=Stress"
```

---

## ⚙️ 高级选项

```powershell
# 使用完整命名空间（最精确）
dotnet test --no-build --filter "FullyQualifiedName=QuantConnect.Brokerages.Gate.Tests.GateBrokerageFuturesTests.FuturesLongFromZero"

# 不重新编译
dotnet test --no-build --filter "FullyQualifiedName~FuturesLongFromZero" --no-build

# 指定 Release 配置
dotnet test --no-build --filter "FullyQualifiedName~FuturesLongFromZero" -c Release

# 详细日志输出
dotnet test --no-build --filter "FullyQualifiedName~FuturesLongFromZero" --logger "console;verbosity=detailed"

# 组合过滤器（Category + 方法名）
dotnet test --no-build --filter "(TestCategory=Futures)&(FullyQualifiedName~LongFromZero)"
```

---

## 🎲 参数化测试过滤

每个测试方法运行多个测试用例（Market/Limit 订单）：

```powershell
# Spot 市场运行 2 个测试用例（MarketOrder + LimitOrder）
dotnet test --no-build --filter "FullyQualifiedName~SpotLongFromZero"

# Futures 市场运行 2 个测试用例
dotnet test --no-build --filter "FullyQualifiedName~FuturesLongFromZero"

# Unified 账户运行 4 个测试用例（Spot_Market + Spot_Limit + Futures_Market + Futures_Limit）
dotnet test --no-build --filter "FullyQualifiedName~UnifiedLongFromZero"
```

**注意**: 无法直接通过 `dotnet test --no-build --filter` 过滤到单个参数化测试用例（如只运行 Spot_MarketOrder）。如需运行特定用例，建议临时注释 `OrderParameters` 数组中的其他用例。

---

## 注意事项

- ⚠️ 所有测试类标记为 `[Explicit]`，需要使用 `FullyQualifiedName` 过滤器才能运行
- ⚠️ 测试会在 testnet 环境真实下单
- ⚠️ 确保账户有足够的 USDT 余额
- ✅ 新的命名方案确保 100% 精确过滤，不会意外触发其他测试
