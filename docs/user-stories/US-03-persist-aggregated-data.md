# US-03 — Persist Aggregated Price Windows

## Context
Aggregated windows must be queryable later from a persistent store.

## Goal
Store aggregated price windows in `SQLite` via `EF Core`.

## Minimum Stored Fields
- `symbol`
- `window_start`
- `window_end`
- `average_price` (or enough fields to derive it)
- `CreatedOn`
- `UpdatedOn`

## Acceptance Criteria
1. Aggregated windows are persisted in database.
2. Writes are idempotent for duplicate processing scenarios.
3. Audit fields are correctly maintained.

## Notes for Implementation
- Repository contract in `Domain`/`Application` boundary.
- EF configuration and persistence logic in `Infrastructure`.
- Prefer unique constraint for `(symbol, window_start)`.
