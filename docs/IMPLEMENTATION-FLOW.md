# Implementation Flow Log

This document describes the end-to-end runtime flow of data in the system.

It is intended to be updated incrementally after each completed user story so the team keeps a single, evolving operational view.

---

## Covered scope

- US-01 — Binance WebSocket Ingestion
- US-02 — Aggregate Trades into 5-Second Aligned Windows
- US-03 — Persist Aggregated Price Windows
- US-04 — Generate Alert on >5% Consecutive Window Change
- US-05 — Expose REST API to Retrieve Aggregated Prices

---

## 1. High-level pipeline (US-01 + US-02 + US-03)

```text
Binance WebSocket
   -> BinanceTradeTickStream (Infrastructure, Singleton)
   -> TradeTickIngestionBackgroundService (Api, Singleton)
   -> MediatR ISender (resolved from per-tick scope)
   -> IngestTradeTickCommandHandler (Application)
   -> ITradeWindowAggregator / InMemoryTradeWindowAggregator (Infrastructure, Singleton)
   -> WindowPriceAggregation + FiveSecondWindow (Domain)
   -> TradeWindowAggregationSnapshot
   -> [if not duplicate] IPriceWindowRepository / PriceWindowRepository (Infrastructure, Scoped)  [US-03]
   -> PriceWindows table — SQLite via EF Core                                                      [US-03]
   -> [if previous consecutive window exists and abs change > 5%] warning alert log (US-04)
```

---

## 2. Step-by-step data lifecycle

### Step 0 — Database schema initialization (startup)

Before any background service starts consuming trades, `Program.cs` applies all pending EF Core migrations:

```csharp
await db.Database.MigrateAsync();
```

This runs automatically on every boot — locally and inside the Docker container — without manual intervention.

| Migration | Effect |
|---|---|
| `InitialCreate` | Creates `ApiKeys` and `PriceWindows` tables; seeds the default API key. |
| `NormalizeWindowDatesToUtcTicks` | Alters `WindowStart` and `WindowEnd` from `TEXT` to `INTEGER` (UTC ticks) for SQLite compatibility. |

### Step 1 — Trade message is received from Binance (US-01)

- Source URL:
  - `wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade`
- `BinanceTradeTickStream` consumes raw WebSocket text messages.
- Valid messages are parsed into `TradeTick` with:
  - `Symbol`
  - `Price`
  - `Quantity`
  - `TradeTimestamp`
  - `TradeId`

### Step 2 — Invalid payload handling (US-01)

- If message parsing fails, the message is ignored.
- A warning is logged.
- Stream processing continues without crashing the pipeline.

### Step 3 — Background consumption and per-tick DI scope (US-01)

- `TradeTickIngestionBackgroundService` runs continuously.
- For each tick:
  1. Creates an `AsyncScope` via `IServiceScopeFactory`.
  2. Resolves `ISender` from that scope.
  3. Builds and sends `IngestTradeTickCommand`.
  4. Disposes the scope after command completion.

This preserves lifetime correctness (`Singleton -> Scoped`) and prevents captive dependency issues.

### Step 4 — Command validation (US-02)

`IngestTradeTickCommandValidator` enforces:

- `Symbol` required, max length and pattern.
- `Price > 0`
- `Quantity > 0`
- `TradeId > 0`
- `TradeTimestamp != default`

If validation fails, command execution stops in the pipeline with a failure result.

### Step 5 — Application orchestration (US-02 + US-03)

`IngestTradeTickCommandHandler`:

1. Normalizes/validates symbol through `Symbol.Create(...)`.
2. Calls feature contract `ITradeWindowAggregator.AddTrade(...)`.
3. Logs current aggregation snapshot.
4. If snapshot is **not** a duplicate, calls `PersistWindowAsync` via `IPriceWindowRepository`. **(US-03)**
5. Returns `Result.Success()`.

### Step 6 — Window alignment rule in Domain (US-02)

`FiveSecondWindow.FromTradeTimestamp(...)` aligns each trade timestamp to fixed 5-second windows anchored at `00:00:00`.

Examples:

- `12:00:04.999` -> `[12:00:00, 12:00:05)`
- `12:00:05.000` -> `[12:00:05, 12:00:10)`

Boundary behavior is deterministic by construction.

### Step 7 — Aggregation and idempotency in Domain aggregate (US-02)

`WindowPriceAggregation` (aggregate with identity):

- Tracks:
  - `Id`
  - `Symbol`
  - `Window`
  - `PriceSum`
  - `TradeCount`
  - `AveragePrice`
- Handles idempotency with processed `TradeId` set.
- `TryAddTrade(...)` behavior:
  - Duplicate trade id -> returns `false` (no state change).
  - New trade id in same window -> updates sum/count and returns `true`.
  - Trade outside current window -> throws invariant exception.

### Step 8 — Infrastructure state container (US-02)

`InMemoryTradeWindowAggregator` stores active aggregates in a `ConcurrentDictionary` keyed by:

- `(Symbol, WindowStart)`

It performs thread-safe updates and returns `TradeWindowAggregationSnapshot` with:

- `AggregationId`
- `Symbol`
- `WindowStart`
- `WindowEnd`
- `AveragePrice`
- `TradeCount`
- `IsDuplicate`

### Step 9 — Persistence of aggregated windows (US-03)

`IngestTradeTickCommandHandler` calls `PersistWindowAsync` only when `IsDuplicate == false`:

1. Calls `IPriceWindowRepository.GetBySymbolAndWindowStartAsync(symbol, windowStart)`.
2. **No existing window** → creates `PriceWindow` via `PriceWindow.Create(...)` and calls `AddAsync`.
3. **Existing window** → calls `existing.ApplySnapshot(averagePrice, tradeCount)` and `Update`.
4. Calls `SaveChangesAsync` to flush changes into SQLite.

**Idempotency layers**:

| Layer | Mechanism |
|---|---|
| Application | `IsDuplicate == true` → persistence skipped entirely; no DB round-trip |
| Database | `UNIQUE(Symbol, WindowStart)` constraint prevents concurrent duplicate inserts |

**Audit fields**:

- `CreatedOn`: set by `AuditableEntityInterceptor` on `EntityState.Added`.
- `UpdatedOn`: set by `AuditableEntityInterceptor` on `EntityState.Modified`; `CreatedOn` is never overwritten.

**SQLite `DateTimeOffset` handling**:

The SQLite EF Core provider cannot translate `DateTimeOffset` expressions in `WHERE` or `ORDER BY` clauses.
`WindowStart` and `WindowEnd` are stored as `INTEGER` (UTC ticks) via a value converter in `PriceWindowConfiguration`:

```csharp
.HasConversion(
    v => v.UtcTicks,
    v => new DateTimeOffset(v, TimeSpan.Zero))
```

All incoming `DateTimeOffset` values are normalized to UTC via `.ToUniversalTime()` before any repository query, making comparisons and ordering fully SQLite-compatible.

### Step 10 — Consecutive-window alert evaluation (US-04)
1. Handler obtains previous window by `(symbol, current.WindowStart == previous.WindowEnd)`.
2. If previous does not exist, alert is skipped.
3. Calculates `abs((current_avg - previous_avg) / previous_avg) * 100`.
4. Triggers alert only when value is strictly `> 5`.
5. Alert is emitted as structured warning log.

### Step 11 — REST API read endpoint (US-05)

```text
HTTP Client
   -> GET /api/prices?symbol=BTCUSDT&from=...&to=...
   -> [Authorize] ApiKey scheme validated by ApiKeyAuthenticationHandler
   -> PricesController.GetPrices (Api, thin controller)
   -> ISender.Send(GetPricesQuery) via MediatR pipeline
   -> GetPricesQueryValidator: from <= to guard (400 ProblemDetails on failure)
   -> GetPricesQueryHandler (Application)
   -> IPriceWindowRepository.GetFilteredAsync (Infrastructure, Scoped)
   -> EF Core LINQ query with optional WHERE clauses on PriceWindows table
   -> IReadOnlyList<GetPricesResponse> -> 200 OK (JSON snake_case)
```

**Filter semantics**:

| Parameter | Applied as |
|---|---|
| `symbol` | `WHERE Symbol = @symbol` |
| `from` | `WHERE WindowStart >= @from` — normalized to UTC via `.ToUniversalTime()` before query |
| `to` | `WHERE WindowStart <= @to` — normalized to UTC via `.ToUniversalTime()` before query |

`WindowStart` is stored as `INTEGER` (UTC ticks); comparisons are integer-based and SQLite-compatible regardless of the offset carried by the incoming `DateTimeOffset` value.

**Error flow**:

- `from > to` → `ValidationBehavior` returns `Result<T>.Failure(Error.Validation(...))` → `ResultExtensions.ToActionResult` maps to HTTP 400 ProblemDetails.
- Missing/invalid ApiKey → `ApiKeyAuthenticationHandler` returns 401.

---

## 3. DI lifetimes involved in the flow

| Component | Lifetime | Reason |
|---|---|---|
| `ITradeTickStream` / `BinanceTradeTickStream` | **Singleton** | WebSocket connection must persist for the application lifetime. |
| `TradeTickIngestionBackgroundService` | **Singleton** | `AddHostedService<T>()` always registers as singleton. |
| `IServiceScopeFactory` | **Singleton** | Provided by the framework. The only safe mechanism for a singleton to consume scoped services. |
| `ISender` | **Transient** | Resolved per tick from the created scope, never from the root container. |
| `IngestTradeTickCommandHandler` | **Transient** | MediatR default. |
| `ITradeWindowAggregator` / `InMemoryTradeWindowAggregator` | **Singleton** | In-memory state must survive across ticks for the entire application lifetime. |
| `IPriceWindowRepository` / `PriceWindowRepository` | **Scoped** | EF Core repositories must be scoped to avoid cross-tick `DbContext` tracking conflicts. |
| `AppDbContext` | **Scoped** | `AddDbContext<T>()` default. One instance per DI scope (one per tick). |
| `GetPricesQueryHandler` | **Transient** | MediatR default. Receives scoped `IPriceWindowRepository` from the HTTP request scope. |

---

## 4. Acceptance criteria mapping

### US-01

1. Continuous stream ingestion: **Satisfied**
2. Malformed message handling without crash: **Satisfied**
3. Reconnection strategy for disconnects: **Implemented in streaming infrastructure base**
4. No controllers for ingestion logic: **Satisfied**

### US-02

1. Correct assignment to aligned 5-second windows: **Satisfied**
2. Average price per `(symbol, window_start, window_end)`: **Satisfied**
3. Deterministic boundary rule (`xx:xx:05.000` -> next window): **Satisfied**

### US-03

1. Aggregated windows persisted in DB: **Satisfied** — `AddAsync` on new windows, `Update` on existing ones, followed by `SaveChangesAsync`.
2. Writes are idempotent: **Satisfied** — duplicate ticks skip persistence entirely; `UNIQUE(Symbol, WindowStart)` provides a DB-level safety net.
3. Audit fields maintained: **Satisfied** — `AuditableEntityInterceptor` sets `CreatedOn` on insert and `UpdatedOn` on update without overwriting `CreatedOn`.

### US-04

1. Alerts on >5% consecutive window changes: **Satisfied** — structured warning logs are emitted for significant changes between consecutive windows.

### US-05

1. Endpoint returns persisted aggregated prices: **Satisfied** — `GetPricesQueryHandler` queries `PriceWindows` table via `GetFilteredAsync`.
2. Filtering by `symbol`, `from`, `to` works correctly: **Satisfied** — three independent optional WHERE clauses applied via EF Core LINQ.
3. Controller remains thin and delegates to `ISender`: **Satisfied** — `PricesController` contains zero business logic.
4. Errors use existing `ProblemDetails` mapping: **Satisfied** — `ValidationBehavior` + `ResultExtensions.ToActionResult` + `GlobalExceptionHandler` cover all error paths.
5. Endpoint is protected with ApiKey auth policy: **Satisfied** — `[Authorize]` on `PricesController` uses the existing `ApiKeyAuthenticationHandler`.

---

## 5. Current limitations and next evolution points

**In-memory aggregation state**:

Current aggregation/idempotency state is process-local in memory.

- Pros: simple and fast for current scope.
- Limitation: in-memory aggregation state is lost on restart and is not shared across multiple instances.

Persistent windows (US-03) survive restarts at the read level, but the in-memory aggregation state resets on each boot.
Future stories should evolve this toward distributed-safe idempotency by rebuilding in-memory state from the persisted `PriceWindows` table on startup.

**SQLite `DateTimeOffset` constraint**:

The SQLite EF Core provider does not support `DateTimeOffset` expressions in `WHERE` (`>=`, `<=`) or `ORDER BY` clauses.
This is resolved at the persistence layer — not visible to Application or Domain — through:

- A value converter in `PriceWindowConfiguration` that maps `DateTimeOffset ↔ long (UTC ticks)` for `WindowStart` and `WindowEnd`.
- Explicit `.ToUniversalTime()` normalization on all `DateTimeOffset` parameters in `PriceWindowRepository` before LINQ predicates are applied.

If the database engine is replaced (e.g. PostgreSQL), the value converter and normalization calls can be removed without any changes to Application or Domain layers.
