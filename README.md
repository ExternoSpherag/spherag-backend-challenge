# RealtimeMarketData

Real-time market data system built with `.NET 10` and `C# 14`, following `Clean Architecture`, `CQRS`, and lightweight `DDD` principles.

## 📚 Documentation

- Architecture guide: `docs/ARCHITECTURE.md`
- Implementation flow: `docs/IMPLEMENTATION-FLOW.md`
- Development implementation guide: `docs/DEVELOPMENT-GUIDELINES.md`
- Repository instructions for Copilot: `.github/copilot-instructions.md`
- User stories: `docs/user-stories/US-*.md`

## 🏗️ Architecture

System layers:

- `src/RealtimeMarketData.Api`: presentation layer (`ASP.NET Core`)
- `src/RealtimeMarketData.Application`: use cases (`MediatR`, validation, results)
- `src/RealtimeMarketData.Domain`: domain model (entities, value objects, events)
- `src/RealtimeMarketData.Infrastructure`: persistence and external services (`EF Core`, SQLite, auth)

## 🔐 Development API Key

For local testing, the seeded key is:

- `keyId`: `seed_default`
- `secret`: `dev-secret`
- Header: `X-Api-Key: seed_default.dev-secret`
- Helper endpoint (Development only): `GET /api/dev/apikeys/seeded`

## 📊 Data Flow

```
Binance WebSocket (stream URL)
  ↓ [continuous ingestion]
BinanceTradeTickStream (ITradeTickStream)
  ↓ [bounded channel, 500 ticks]
TradeTickIngestionBackgroundService (per-tick DI scope)
  ↓ [MediatR dispatch]
IngestTradeTickCommand → Handler → ITradeWindowAggregator
  ↓ [5-second window aggregation + deduplication]
WindowPriceAggregation [Domain aggregate]
  ↓ [if not duplicate, persist to DB]
PriceWindows table [SQLite]
  ↓ [query via REST API with filters]
GET /api/prices?symbol=BTCUSDT&from=...&to=...
```

## 🚀 Prerequisites

- `.NET 10 SDK`
- Visual Studio 2026 or later / VS Code
- Docker (optional)

## 🔧 Installation

1. Clone the repository:

```bash
git clone https://github.com/dherreroab/RealtimeMarketData.git
cd RealtimeMarketData
```

2. Restore packages:

```bash
dotnet restore
```

3. Build:

```bash
dotnet build
```

## ▶️ Run locally

```bash
dotnet run --project src/RealtimeMarketData.Api/RealtimeMarketData.Api.csproj
```

In development, interactive API docs are exposed at:

- `GET http://localhost:5000/scalar/v1` — Scalar UI
- `GET http://localhost:5000/openapi/v1.json` — OpenAPI spec

> The app applies pending `EF Core` migrations automatically at startup.

## 🌐 Binance WebSocket Stream

**Source URL**:
```
wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade
```

**Symbols monitored**: `BTCUSDT`, `ETHUSDT`, `DOGEUSDT`

**Example incoming message**:
```json
{
  "stream": "btcusdt@trade",
  "data": {
    "e": "trade",
    "E": 1672515782136,
    "s": "BTCUSDT",
    "t": 12345,
    "p": "67321.11",
    "q": "0.250",
    "T": 1672515782136,
    "m": true,
    "M": false
  }
}
```

**Field mapping**:
- `s` (symbol) → `BTCUSDT`
- `t` (trade id) → `12345`
- `p` (price) → `67321.11`
- `q` (quantity) → `0.250`
- `T` (trade timestamp ms) → `1672515782136`

## 📡 API Endpoints

### GET `/api/prices` — Retrieve aggregated price windows

**Authorization**: Required (`X-Api-Key` header)

**Query parameters**:
- `symbol` (optional): Filter by symbol (e.g., `BTCUSDT`)
- `from` (optional): Start time (ISO 8601, e.g., `2026-01-01T12:00:00Z`)
- `to` (optional): End time (ISO 8601, e.g., `2026-01-01T13:00:00Z`)

#### Request example — All windows

```bash
curl -X GET "http://localhost:5000/api/prices" \
  -H "X-Api-Key: seed_default.dev-secret"
```

#### Request example — Filter by symbol and time range

```bash
curl -X GET "http://localhost:5000/api/prices?symbol=BTCUSDT&from=2026-01-01T12:00:00Z&to=2026-01-01T12:05:00Z" \
  -H "X-Api-Key: seed_default.dev-secret"
```

#### Response 200 OK — Success

```json
[
  {
    "symbol": "BTCUSDT",
    "windowStart": "2026-01-01T12:00:00Z",
    "windowEnd": "2026-01-01T12:00:05Z",
    "averagePrice": 67321.11,
    "tradeCount": 42
  },
  {
    "symbol": "BTCUSDT",
    "windowStart": "2026-01-01T12:00:05Z",
    "windowEnd": "2026-01-01T12:00:10Z",
    "averagePrice": 67350.50,
    "tradeCount": 38
  }
]
```

#### Response 400 Bad Request — Validation error

**Scenario**: `from` > `to`

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The 'from' value must be lower than or equal to 'to'.",
  "errors": {}
}
```

#### Response 401 Unauthorized — Missing API key

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "API key header is invalid.",
  "errors": {}
}
```

#### Response 403 Forbidden — Invalid API key

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "API key is invalid.",
  "errors": {}
}
```

## 🛡️ Edge Case Handling

### 1. Duplicate Trade Events

**Strategy**: Per-window deduplication by `(symbol, tradeId, windowStart)`

- If a trade with the same `tradeId` arrives twice in the same 5-second window, it is silently ignored.
- The second occurrence does NOT update the average price.
- Logged as `IsDuplicate: true` in aggregation logs.

**Example**:
```
Trade 1: BTCUSDT, tradeId=777, price=100, window=[12:00:00, 12:00:05)
Trade 2: BTCUSDT, tradeId=777, price=999, window=[12:00:00, 12:00:05) ← Duplicate, ignored
Result: Average = 100 (not 549.5)
```

### 2. Late or Out-of-Window Events

**Strategy**: Trades outside the current 5-second window are rejected

- If a trade timestamp falls into a different window than its 5-second-aligned slot, a domain invariant exception is thrown.
- The exception is caught, logged with full context, and processing continues.
- This prevents window boundary violations.

**Example**:
```
Window 1: [12:00:00, 12:00:05)
Window 2: [12:00:05, 12:00:10)
Trade with timestamp 12:00:05.001 → Window 2
Attempting to add to Window 1 → Invariant violation → Rejected
```

### 3. Reconnection under Network Failures

**Strategy**: Bounded exponential backoff

- Base delay: 1 second
- Exponential growth: 1s → 2s → 4s → 8s → 16s → 30s (capped)
- Max attempts: 8
- After 8 consecutive failures, the stream stops and the error is logged at ERROR level.

**Backoff schedule**:
| Attempt | Delay |
|---------|-------|
| 1 | 1s |
| 2 | 2s |
| 3 | 4s |
| 4 | 8s |
| 5 | 16s |
| 6+ | 30s |

### 4. Burst Traffic / Channel Overflow

**Strategy**: Bounded channel with drop-newest policy

- Channel capacity: 500 ticks
- When full, incoming ticks are discarded (not queued)
- WebSocket reader never blocks, ensuring Binance server remains responsive
- Each drop is logged with `Symbol`, `TradeId`, `Price` for monitoring.

**Trade-off**: Latency-optimal over consistency-perfect. Under normal load, the 500-tick buffer absorbs seconds of burst without loss. Under extreme sustained overload, some ticks are silently dropped (logged as warning).

### 5. Empty Windows

**Strategy**: Windows with zero trades are not persisted

- Only windows containing at least one trade are written to the database.
- This prevents cluttering the DB with gaps in time.
- The REST API returns only persisted windows (i.e., windows with trades).

**Example**:
```
Window [12:00:00, 12:00:05): 0 trades → NOT persisted
Window [12:00:05, 12:00:10): 15 trades → persisted (average calculated)
Window [12:00:10, 12:00:15): 0 trades → NOT persisted
Window [12:00:15, 12:00:20): 20 trades → persisted (average calculated)
```

### 6. Consecutive Window Price Alerts

**Strategy**: >5% absolute change triggers warning log

- Alerts are emitted only when comparing consecutive windows (windowEnd[N-1] == windowStart[N]).
- The threshold is strictly `> 5.0%` (not `>= 5.0%`).
- Alerts are emitted at WARNING level with full context (symbols, prices, change %).

## 🧪 Tests

Run all tests:

```bash
dotnet test
```

Test projects:

- `tests/RealtimeMarketData.Application.Tests` — 66 tests covering handlers, validators, authentication, backoff policy
- `tests/RealtimeMarketData.Domain.Tests` — Window alignment, aggregation, idempotency

**Coverage highlights**:
- ✅ Domain: window alignment, deduplication, idempotency
- ✅ Application: command handlers, query handlers, validators
- ✅ Infrastructure: authentication service, trade stream parsing
- ✅ Edge cases: expired keys, inactive keys, invalid formats, burst buffering

## 📦 Package management

This repository uses `Central Package Management` with `Directory.Packages.props`.

1. Define the version in `Directory.Packages.props`
2. Reference the package in the `.csproj` without a `Version` attribute

## 🐳 Docker

```bash
docker build -t realtimemarketdata .
docker run -p 8080:8080 -p 8081:8081 realtimemarketdata
```

## 🤝 Contributing

Before opening a PR:

1. Check architecture rules in `docs/ARCHITECTURE.md`
2. Follow implementation rules in `docs/DEVELOPMENT-GUIDELINES.md`
3. Ensure build and tests pass (`dotnet build`, `dotnet test`)
4. PR title format: `[Challenge] Your Name`

## 📄 License

MIT
