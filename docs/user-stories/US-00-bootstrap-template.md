# US-00 — Bootstrap Production-Ready Template

## Context
This story establishes the technical baseline for all subsequent work in this challenge.
The solution must follow `Clean Architecture + CQRS + lightweight DDD` and repository conventions.

## Goal
Create a production-ready backend template with security, API documentation, validation pipeline, persistence, and logging.

## Scope
- Layered structure: `Api`, `Application`, `Domain`, `Infrastructure`
- `MediatR` setup for command/query handling
- `FluentValidation` + `ValidationBehavior`
- `EF Core` + `SQLite` with initial migration
- `OpenAPI` + `Scalar UI`
- ApiKey authentication + authorization policies
- `ProblemDetails` + global exception handling
- Structured logging

## Acceptance Criteria
1. OpenAPI spec is exposed and Scalar UI works in Development.
2. ApiKey security scheme is documented and usable via `Authorize` in docs UI.
3. Validation behavior runs automatically for commands/queries.
4. Database migration is applied on startup.
5. Logging is enabled for MediatR requests.

## Notes for Implementation
- Keep controllers thin and delegate to `ISender`.
- Do not place business logic in `Api`.
- Keep `Domain` free from web/infrastructure dependencies.
