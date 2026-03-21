# Repository architecture

This document defines the architecture rules that must be respected in all changes.

## 1. Principles

- `Clean Architecture`: dependencies point inward.
- `CQRS` for read/write use cases.
- Lightweight `DDD` in domain modeling (entities, value objects, domain events).
- `Result Pattern` for controlled application errors.

## 2. Layer structure

- `RealtimeMarketData.Api`
  - Controllers, host configuration, authentication/authorization.
  - Must not contain business logic.
  - Calls use cases through `ISender` (`MediatR`).

- `RealtimeMarketData.Application`
  - Commands, Queries, Handlers, Validators.
  - Pipeline behaviors (`LoggingBehavior`, `ValidationBehavior`).
  - Defines application contracts needed to execute use cases.

- `RealtimeMarketData.Domain`
  - Pure domain model.
  - Entities/aggregates/value objects/domain events.
  - No infrastructure or web dependencies.

- `RealtimeMarketData.Infrastructure`
  - Persistence (`EF Core`, SQLite), repositories, and external services.
  - Implements contracts required by inner layers.

## 3. Dependency rules

Mandatory rules:

- `Api` may depend on `Application` and `Infrastructure`.
- `Infrastructure` may depend on `Application` and `Domain`.
- `Application` may depend on `Domain`.
- `Domain` must not depend on external layers.

## 4. Application conventions (CQRS)

- Each use case belongs to its feature folder.
- Write use cases: `Commands/<UseCase>/` with:
  - `...Command`
  - `...CommandHandler`
  - `...CommandValidator`
  - `...Response` (if needed)
- Read use cases: `Queries/<UseCase>/` with:
  - `...Query`
  - `...QueryHandler`
  - `...Response`
- Handlers should return `Result` or `Result<T>` for expected errors.

## 5. Domain conventions

- Invariant validation belongs to the domain.
- `ValueObjects` encapsulate format, normalization, and validation.
- Aggregates expose behavior, not only data.
- Domain events are raised inside aggregates.

## 6. Persistence

- `EF Core` with SQLite.
- Entity configurations under `Infrastructure/Persistence/Configurations`.
- Explicit value object mapping via `OwnsOne`.
- Audit fields (`CreatedOn`, `UpdatedOn`) managed through interceptor.

## 7. API and error handling

- Business resource endpoints are protected by default.
- `ApiKey` is the authentication scheme.
- Errors are returned using `ProblemDetails`.
- Controllers use `ResultExtensions` to map `Result` to `IActionResult`.

## 8. Testing

- Domain tests in `tests/RealtimeMarketData.Domain.Tests`.
- Application tests in `tests/RealtimeMarketData.Application.Tests`.
- Every new feature must include relevant tests.

## 9. Anti-patterns

- Do not move domain logic into controllers.
- Do not access `DbContext` directly from `Api` or `Application` (use repositories/services).
- Do not introduce circular dependencies between layers.
- Do not bypass domain validations with unnecessary public setters.

## 10. Streaming pipeline and DI lifetimes

### 10.1 Streaming subscription flow

The real-time ingestion pipeline follows a unidirectional flow from the external WebSocket source to the application command handler:

```
BinanceTradeTickStream  →  TradeTickIngestionBackgroundService  →  IngestTradeTickCommandHandler
     (Singleton)                     (Singleton)                        (Transient, per tick)
  Infrastructure layer                Api layer                         Application layer
```

1. **`BinanceTradeTickStream`** opens a persistent WebSocket connection to Binance on startup and exposes an `IAsyncEnumerable<TradeTick>` stream via `ITradeTickStream`.
2. **`TradeTickIngestionBackgroundService`** iterates that stream indefinitely, creating one DI scope per tick and dispatching an `IngestTradeTickCommand` through `ISender`.
3. **`IngestTradeTickCommandHandler`** processes each tick through the MediatR pipeline (logging → validation → business logic → persistence).

### 10.2 DI lifetime reference

| Component | Lifetime | Reason |
|---|---|---|
| `ITradeTickStream` / `BinanceTradeTickStream` | **Singleton** | WebSocket connection must persist for the application lifetime. Recreating it per scope or per tick would break the stream. |
| `TradeTickIngestionBackgroundService` | **Singleton** | `AddHostedService<T>()` always registers as singleton. |
| `IServiceScopeFactory` | **Singleton** | Provided by the framework. The only safe mechanism for a singleton to consume scoped services. |
| `ISender` / `IMediator` | **Transient** | MediatR default. Must be resolved from the per-tick scope, never from the root container. |
| `LoggingBehavior<,>` / `ValidationBehavior<,>` | **Transient** | MediatR `AddOpenBehavior` default. |
| `IValidator<T>` | **Transient** | Registered explicitly via `AddValidatorsFromAssembly(..., ServiceLifetime.Transient)` so they are resolvable from any lifetime context. |
| Command handlers | **Transient** | MediatR default. Resolved within the per-tick scope. |
| `IMarketDataRepository` / `IPriceWindowRepository` | **Scoped** | EF Core repositories must be scoped to avoid cross-tick `DbContext` tracking conflicts. |
| `AppDbContext` | **Scoped** | `AddDbContext<T>()` default. One instance per DI scope (one per tick). |

### 10.3 Per-tick scope rule

Because `TradeTickIngestionBackgroundService` is a **Singleton**, it cannot directly resolve **Scoped** services (repositories, `DbContext`). The mandatory pattern is:

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
await mediator.Send(command, cancellationToken);
// scope disposed here → DbContext and repositories are released cleanly
```

- One scope is created **per tick**, not per symbol or per batch.
- The scope is disposed with `await using`, guaranteeing `DbContext` is flushed and released after each tick.
- `ISender` must always be resolved **from the scope**, never injected directly into the background service constructor.

### 10.4 Validator lifetime rule

FluentValidation validators must be registered as **Transient** (not Scoped, not Singleton):

```csharp
services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient);
```

Scoped validators would prevent `ValidationBehavior<,>` (Transient) from being resolved in singleton-originated call chains, causing a runtime captive dependency exception.
