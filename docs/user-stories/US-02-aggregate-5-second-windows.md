# US-02 — Aggregate Trades into 5-Second Aligned Windows

## Context
Trades must be grouped into fixed windows aligned to wall-clock boundaries for consistency.

## Goal
Compute average price per symbol for each 5-second window.

## Window Alignment Rule
Windows are aligned from `00:00:00`:
- `00:00:00 → 00:00:05`
- `00:00:05 → 00:00:10`
- `00:00:10 → 00:00:15`

## Example Aggregate Record
- `symbol`: `BTCUSDT`
- `window_start`: `2026-01-01T12:00:10Z`
- `window_end`: `2026-01-01T12:00:15Z`
- `average_price`: `67321.11`

## Acceptance Criteria
1. Each trade is assigned to the correct aligned 5-second window.
2. Average price is computed per `(symbol, window_start, window_end)`.
3. Boundary behavior is deterministic (`xx:xx:05.000` belongs to next window).

## Notes for Implementation
- Keep time-window rules in `Domain`.
- Use an application command/handler for aggregation orchestration.
- Keep calculation rules test-covered at domain/application level.
