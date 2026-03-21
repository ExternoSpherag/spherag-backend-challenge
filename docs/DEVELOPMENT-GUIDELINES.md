# Development guidelines

Practical guide to implement changes while preserving repository architecture.

## 1. Recommended flow for a new feature

1. Create/adjust the domain model (`Domain`) if the business rule requires it.
2. Define the use case in `Application` (Command or Query + Handler + Validator).
3. Extend contract/repository in `Domain`/`Infrastructure`.
4. Expose endpoint in `Api`.
5. Add domain and/or application tests.

## 2. Mandatory patterns

- Use `MediatR` for all use cases invoked from controllers.
- Use `FluentValidation` for input validation in `Application`.
- Return `Result` or `Result<T>` for controlled business errors.
- Map results to HTTP using `ResultExtensions`.
- In `BackgroundService`, always resolve `ISender` via `IServiceScopeFactory` (never inject it directly). See [Architecture §10](ARCHITECTURE.md#10-streaming-pipeline-and-di-lifetimes).

## 3. Coding conventions

- Follow feature-based structure.
- Keep clear naming:
  - `CreateXCommand`, `GetXByIdQuery`, etc.
- Avoid complex logic in controllers.
- Avoid per-use-case `try/catch` unless there is an explicit strategy.

## 4. Persistence and migrations

- Schema changes must be done only via `EF Core` migrations.
- Review snapshot and designer files after generating migrations.
- Do not manually edit binary files (`*.db`, `*.db-shm`, `*.db-wal`) unless explicitly required.

## 5. Pre-commit checklist

- `dotnet restore`
- `dotnet build`
- `dotnet test`
- Validate that layer rules are not broken
- Document relevant flow changes in `README.md`

## 6. Background services and scoped dependencies

When implementing a `BackgroundService` that dispatches MediatR commands:

1. **Do not inject `ISender` directly** into the constructor — it would be captured by the singleton and unable to resolve scoped services (repositories, `DbContext`).
2. **Inject `IServiceScopeFactory`** instead, which is always safe in singleton context.
3. **Create one `AsyncScope` per unit of work** (e.g., per tick, per message):

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
await mediator.Send(command, cancellationToken);
```

4. Any new repository or scoped service added to a handler is automatically covered by this pattern — no changes required in the background service.
5. Validators must be registered as `ServiceLifetime.Transient` in `Application/DependencyInjection.cs` to avoid captive dependency errors.

> See [Architecture §10](ARCHITECTURE.md#10-streaming-pipeline-and-di-lifetimes) for the full lifetime reference table.
