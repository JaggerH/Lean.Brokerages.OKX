# OKX v5 API Request & Response Formats

**Document Version**: 2026-01-09
**API Version**: v5
**Sources**: [OKX Official Documentation](https://www.okx.com/docs-v5/en/)

---

## Table of Contents

1. [WebSocket - Received Only (Push Data)](#websocket---received-only-push-data)
2. [WebSocket - Send & Receive](#websocket---send--receive)
3. [REST API](#rest-api)
4. [Common Envelope Structure](#common-envelope-structure)
5. [Error Handling](#error-handling)

---

## WebSocket - Received Only (Push Data)

These channels only receive data pushes after subscription. No send operations except initial subscription.

### 1. Orders Channel (`orders`)

**Subscribe Request**:
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "orders",
      "instType": "ANY"
    }
  ]
}
```

**Subscribe Success Response**:
```json
{
  "event": "subscribe",
  "arg": {
    "channel": "orders",
    "instType": "ANY"
  },
  "connId": "a4d3ae55"
}
```

**Data Push Format**:
```json
{
  "arg": {
    "channel": "orders",
    "instType": "ANY"
  },
  "data": [
    {
      "instId": "BTC-USDT",
      "ordId": "312269865356374016",
      "clOrdId": "b15",
      "tag": "",
      "px": "30000",
      "sz": "0.001",
      "ordType": "limit",
      "side": "buy",
      "posSide": "net",
      "tdMode": "cash",
      "state": "live",
      "accFillSz": "0",
      "fillPx": "",
      "avgPx": "",
      "feeCcy": "USDT",
      "fee": "-0.001",
      "cTime": "1597026383085",
      "uTime": "1597026383085"
    }
  ]
}
```

**Order States**:
- `live` - Order submitted
- `partially_filled` - Partial fill
- `filled` - Fully filled
- `canceled` - Canceled
- `canceling` - Cancel pending

**Key Fields**:
- `instId` (string): Instrument ID
- `ordId` (string): Exchange order ID
- `clOrdId` (string): Client order ID
- `state` (string): Order state
- `accFillSz` (string): Accumulated fill size
- `avgPx` (string): Average filled price
- `fee` (string): Fee amount (negative = paid)
- `feeCcy` (string): Fee currency
- `cTime` / `uTime` (string): Create/Update timestamp (milliseconds)

**Data Format**: **Array** - `data` is always an array

---

### 2. Account Channel (`account`)

**Subscribe Request**:
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "account"
    }
  ]
}
```

**Data Push Format**:
```json
{
  "arg": {
    "channel": "account"
  },
  "data": [
    {
      "uTime": "1597026383085",
      "totalEq": "41624.32",
      "details": [
        {
          "ccy": "USDT",
          "availBal": "1000.50",
          "cashBal": "1200.50",
          "frozenBal": "200",
          "eq": "1200.50"
        }
      ]
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array

---

### 3. Positions Channel (`positions`)

**Subscribe Request**:
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "positions",
      "instType": "ANY"
    }
  ]
}
```

**Data Push Format**:
```json
{
  "arg": {
    "channel": "positions",
    "instType": "ANY"
  },
  "data": [
    {
      "instId": "BTC-USDT-SWAP",
      "posSide": "long",
      "pos": "10",
      "availPos": "10",
      "avgPx": "50000",
      "upl": "500.5",
      "mgnMode": "cross",
      "lever": "10",
      "uTime": "1597026383085"
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array

---

### 4. Public Channels (Market Data)

#### Tickers Channel (`tickers`)

**Subscribe Request**:
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "tickers",
      "instId": "BTC-USDT"
    }
  ]
}
```

**Data Push Format**:
```json
{
  "arg": {
    "channel": "tickers",
    "instId": "BTC-USDT"
  },
  "data": [
    {
      "instId": "BTC-USDT",
      "last": "43250.1",
      "bidPx": "43250.0",
      "askPx": "43250.2",
      "bidSz": "1.5",
      "askSz": "2.0",
      "vol24h": "123456.78",
      "ts": "1597026383085"
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array

---

#### Trades Channel (`trades`)

**Subscribe Request**:
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "trades",
      "instId": "BTC-USDT"
    }
  ]
}
```

**Data Push Format**:
```json
{
  "arg": {
    "channel": "trades",
    "instId": "BTC-USDT"
  },
  "data": [
    {
      "instId": "BTC-USDT",
      "tradeId": "123456789",
      "px": "43250.1",
      "sz": "0.5",
      "side": "buy",
      "ts": "1597026383085"
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array

---

#### Order Book Channel (`books5`)

**Subscribe Request**:
```json
{
  "op": "subscribe",
  "args": [
    {
      "channel": "books5",
      "instId": "BTC-USDT"
    }
  ]
}
```

**Data Push Format**:
```json
{
  "arg": {
    "channel": "books5",
    "instId": "BTC-USDT"
  },
  "action": "snapshot",
  "data": [
    {
      "instId": "BTC-USDT",
      "bids": [
        ["43250.0", "1.5", "0", "2"],
        ["43249.9", "2.0", "0", "1"]
      ],
      "asks": [
        ["43250.2", "2.5", "0", "3"],
        ["43250.3", "1.0", "0", "1"]
      ],
      "ts": "1597026383085",
      "checksum": 12345678
    }
  ]
}
```

**Notes**:
- `action` field: `snapshot` (initial) or `update` (incremental)
- Bids/Asks format: `[price, size, liquidation_orders, orders_count]`

**Data Format**: **Array** - `data` is always an array

---

## WebSocket - Send & Receive

### 1. Place Order (NOT AVAILABLE via WebSocket)

**Note**: Order placement via WebSocket is NOT used in our implementation. We use REST API for order operations.

---

### 2. Amend Order (NOT AVAILABLE via WebSocket)

**Note**: Order amendment via WebSocket is NOT used in our implementation. We use REST API.

---

### 3. Cancel Order (NOT AVAILABLE via WebSocket)

**Note**: Order cancellation via WebSocket is NOT used in our implementation. We use REST API.

---

## REST API

All REST responses follow the **OKX v5 API Standard Envelope**:

```json
{
  "code": "0",
  "msg": "",
  "data": [...]
}
```

- `code`: `"0"` = success, other codes = error
- `msg`: Error message (empty on success)
- `data`: **Always an array**, even for single object responses

---

### 1. Place Order

**Endpoint**: `POST /api/v5/trade/order`

**Request Headers**:
```
OK-ACCESS-KEY: <API Key>
OK-ACCESS-SIGN: <Signature>
OK-ACCESS-TIMESTAMP: <UTC timestamp>
OK-ACCESS-PASSPHRASE: <Passphrase>
Content-Type: application/json
```

**Request Body**:
```json
{
  "instId": "BTC-USDT",
  "tdMode": "cash",
  "side": "buy",
  "ordType": "limit",
  "sz": "0.01",
  "px": "30000",
  "clOrdId": "b15"
}
```

**Request Parameters**:
- `instId` (string, required): Instrument ID
- `tdMode` (string, required): Trade mode (`cash`, `cross`, `isolated`)
- `side` (string, required): `buy` or `sell`
- `ordType` (string, required): `market`, `limit`, `post_only`, `fok`, `ioc`
- `sz` (string, required): Order size
- `px` (string, conditional): Price (required for limit orders)
- `clOrdId` (string, optional): Client order ID

**Success Response**:
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "ordId": "312269865356374016",
      "clOrdId": "b15",
      "tag": "",
      "sCode": "0",
      "sMsg": ""
    }
  ]
}
```

**Response Fields**:
- `ordId` (string): Exchange-assigned order ID
- `clOrdId` (string): Client order ID (if provided)
- `sCode` (string): Individual order status code (`"0"` = accepted)
- `sMsg` (string): Individual order status message

**Error Response**:
```json
{
  "code": "51000",
  "msg": "Parameter instId  error",
  "data": []
}
```

**Data Format**: **Array** - `data` is always an array, containing one object for single order

**Important Notes**:
1. Success response (`code=0`) only means request accepted, NOT that order is live
2. Check `sCode` in data array for individual order status
3. Order state should be checked via orders channel WebSocket push

---

### 2. Amend Order

**Endpoint**: `POST /api/v5/trade/amend-order`

**Request Body**:
```json
{
  "instId": "BTC-USDT",
  "ordId": "312269865356374016",
  "newSz": "0.02",
  "newPx": "31000"
}
```

**Request Parameters**:
- `instId` (string, required): Instrument ID
- `ordId` (string, conditional): Order ID (use ordId OR clOrdId)
- `clOrdId` (string, conditional): Client order ID
- `newSz` (string, optional): New size
- `newPx` (string, optional): New price

**Success Response**:
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "ordId": "312269865356374016",
      "clOrdId": "b15",
      "sCode": "0",
      "sMsg": ""
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array

---

### 3. Cancel Order

**Endpoint**: `POST /api/v5/trade/cancel-order`

**Request Body**:
```json
{
  "instId": "BTC-USDT",
  "ordId": "312269865356374016"
}
```

**Request Parameters**:
- `instId` (string, required): Instrument ID
- `ordId` (string, conditional): Order ID (use ordId OR clOrdId)
- `clOrdId` (string, conditional): Client order ID

**Success Response**:
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "ordId": "312269865356374016",
      "clOrdId": "b15",
      "sCode": "0",
      "sMsg": ""
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array

---

### 4. Get Pending Orders

**Endpoint**: `GET /api/v5/trade/orders-pending`

**Query Parameters**:
- `instType` (string, optional): `SPOT`, `MARGIN`, `SWAP`, `FUTURES`, `OPTION`
- `instId` (string, optional): Instrument ID

**Example**: `GET /api/v5/trade/orders-pending?instType=SPOT`

**Success Response**:
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "instId": "BTC-USDT",
      "ordId": "312269865356374016",
      "clOrdId": "b15",
      "tag": "",
      "px": "30000",
      "sz": "0.001",
      "ordType": "limit",
      "side": "buy",
      "posSide": "net",
      "tdMode": "cash",
      "state": "live",
      "accFillSz": "0",
      "fillPx": "",
      "avgPx": "",
      "cTime": "1597026383085",
      "uTime": "1597026383085",
      "instType": "SPOT",
      "lever": "1"
    }
  ]
}
```

**Data Format**: **Array** - `data` is always an array, can contain multiple orders

---

### 5. Get Balance

**Endpoint**: `GET /api/v5/account/balance`

**Query Parameters**:
- `ccy` (string, optional): Currency filter (e.g., `BTC`, `USDT`)

**Example**: `GET /api/v5/account/balance?ccy=USDT`

**Success Response**:
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "totalEq": "41624.32",
      "details": [
        {
          "ccy": "USDT",
          "availBal": "1000.50",
          "cashBal": "1200.50",
          "frozenBal": "200",
          "eq": "1200.50"
        },
        {
          "ccy": "BTC",
          "availBal": "0.5",
          "cashBal": "0.5",
          "frozenBal": "0",
          "eq": "0.5"
        }
      ]
    }
  ]
}
```

**Response Fields**:
- `totalEq` (string): Total equity in USD
- `details` (array): Balance details per currency
  - `ccy` (string): Currency
  - `availBal` (string): Available balance
  - `cashBal` (string): Cash balance
  - `frozenBal` (string): Frozen balance (in orders)
  - `eq` (string): Equity of currency

**Data Format**: **Array** - `data` is always an array, containing ONE object

---

### 6. Get Positions

**Endpoint**: `GET /api/v5/account/positions`

**Query Parameters**:
- `instType` (string, optional): `MARGIN`, `SWAP`, `FUTURES`, `OPTION`
- `instId` (string, optional): Instrument ID

**Example**: `GET /api/v5/account/positions?instType=SWAP`

**Success Response**:
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "instId": "BTC-USDT-SWAP",
      "instType": "SWAP",
      "pos": "10",
      "availPos": "10",
      "avgPx": "50000",
      "upl": "500.5",
      "posSide": "long",
      "mgnMode": "cross",
      "lever": "10",
      "last": "50050.5",
      "ccy": "USDT",
      "uTime": "1597026383085"
    }
  ]
}
```

**Response Fields**:
- `instId` (string): Instrument ID
- `instType` (string): Instrument type
- `pos` (string): Position quantity (positive = long, negative = short)
- `availPos` (string): Available position
- `avgPx` (string): Average open price
- `upl` (string): Unrealized P&L
- `posSide` (string): Position side (`long`, `short`, `net`)
- `mgnMode` (string): Margin mode (`cross`, `isolated`)
- `lever` (string): Leverage
- `last` (string): Last/mark price
- `ccy` (string): Currency
- `uTime` (string): Update timestamp (milliseconds)

**Data Format**: **Array** - `data` is always an array, can contain multiple positions

---

## Common Envelope Structure

### WebSocket Envelope

#### Subscription Response
```json
{
  "event": "subscribe" | "unsubscribe" | "error" | "login",
  "arg": { "channel": "...", "instId": "..." },
  "code": "0",
  "msg": "",
  "connId": "a4d3ae55"
}
```

#### Data Push
```json
{
  "arg": { "channel": "...", "instId": "..." },
  "data": [...],
  "action": "snapshot" | "update"  // Only for order book
}
```

**Key Points**:
- `event` field present in control messages (subscribe, error, login)
- `arg` + `data` present in data push messages
- `data` is **ALWAYS an array**

---

### REST Envelope

```json
{
  "code": "0",
  "msg": "",
  "data": [...]
}
```

**Key Points**:
- `code`: `"0"` = success, other values = error
- `msg`: Error message (empty on success)
- `data`: **ALWAYS an array**, even for single-object responses

---

## Error Handling

### REST API Errors

#### Top-Level Errors (Request Rejected)
```json
{
  "code": "51000",
  "msg": "Parameter instId error",
  "data": []
}
```

**Common Error Codes**:
- `51000` - Parameter error
- `51001` - Instrument ID not found
- `51008` - Order does not exist
- `51020` - Order already canceled

---

#### Individual Order Errors (Request Accepted, Order Rejected)
```json
{
  "code": "0",
  "msg": "",
  "data": [
    {
      "ordId": "",
      "clOrdId": "b15",
      "sCode": "51004",
      "sMsg": "Insufficient balance"
    }
  ]
}
```

**Important**:
- When `code=0` but `sCode≠0`, order request was accepted but order failed
- Always check **both** `code` AND `sCode` fields

---

### WebSocket Errors

```json
{
  "event": "error",
  "code": "60012",
  "msg": "Invalid request: {...}",
  "connId": "a4d3ae55"
}
```

**Common Error Codes**:
- `60012` - Invalid request
- `60009` - Login failed
- `60022` - Unsubscribe failed

---

## Data Format Summary

| Endpoint/Channel | `data` Format | Notes |
|------------------|---------------|-------|
| **WebSocket - All Channels** | **Array** | Always array, even single object |
| **REST - Place Order** | **Array** | Single object in array |
| **REST - Amend Order** | **Array** | Single object in array |
| **REST - Cancel Order** | **Array** | Single object in array |
| **REST - Get Pending Orders** | **Array** | Multiple objects |
| **REST - Get Balance** | **Array** | Single object with `details` array inside |
| **REST - Get Positions** | **Array** | Multiple objects or empty array |

**Critical Rule**: OKX v5 API **ALWAYS** returns `data` as an array, never a single object.

---

## References

- [OKX Official Documentation](https://www.okx.com/docs-v5/en/)
- [WebSocket API Overview](https://www.okx.com/docs-v5/en/#websocket-api)
- [REST API Trading](https://www.okx.com/docs-v5/en/#rest-api-trade)
- [REST API Account](https://www.okx.com/docs-v5/en/#rest-api-account)

---

**Document Generated**: 2026-01-09
**API Version**: OKX v5
