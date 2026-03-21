# US-01 — Binance WebSocket Ingestion

## Context
The service must consume real-time trade events from Binance Futures for three pairs.

## Stream URL
`wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade`

## Goal
Ingest trade events continuously and safely for downstream aggregation.

## Required Trade Fields
- `s` → symbol
- `p` → price
- `q` → quantity
- `T` → trade timestamp
- `t` → trade id (recommended for deduplication)

## Example Incoming Event
```json
{
  "e": "trade",
  "E": 1672515782136,
  "s": "BTCUSDT",
  "t": 12345,
  "p": "67321.11",
  "q": "0.250",
  "T": 1672515782136,
  "m": true,
  "M": true
}
```

## Acceptance Criteria
1. Service connects to the Binance stream and receives events continuously.
2. Malformed messages are logged and ignored without crash.
3. Reconnection strategy exists for disconnect scenarios.
4. Ingestion code does not live in controllers.

## Notes for Implementation
- Implement WebSocket client in `Infrastructure`.
- Expose stream internally as `IAsyncEnumerable<TradeTick>`.
- Consume stream via background worker and dispatch commands through `ISender`.
