# US-05 — Expose REST API to Retrieve Aggregated Prices

## Context
Clients need a stable endpoint to retrieve persisted aggregated windows.

## Goal
Expose `GET /api/prices` with optional filtering.

## Endpoint
`GET /api/prices`

## Optional Query Parameters
- `symbol`
- `from`
- `to`

## Example Calls
- `GET /api/prices`
- `GET /api/prices?symbol=BTCUSDT`
- `GET /api/prices?symbol=ETHUSDT&from=2026-01-01T12:00:00Z&to=2026-01-01T12:05:00Z`

## Example Success Response (`200`)
```json
[
  {
    "symbol": "BTCUSDT",
    "window_start": "2026-01-01T12:00:00Z",
    "window_end": "2026-01-01T12:00:05Z",
    "average_price": 67321.11
  }
]
```

## Example Error Response (`400`, ProblemDetails)
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "The 'from' value must be lower than or equal to 'to'."
}
```

## Acceptance Criteria
1. Endpoint returns persisted aggregated prices.
2. Filtering by `symbol`, `from`, `to` works correctly.
3. Controller remains thin and delegates to `ISender`.
4. Errors use existing `ProblemDetails` mapping.
5. Endpoint is protected with ApiKey auth policy.

## Notes for Implementation
- Use `Query + Handler + Validator` in `Application`.
- Keep persistence query logic in `Infrastructure`.
