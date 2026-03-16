# Implementation Details

## Trade Stream

You will consume trades from three cryptocurrency pairs:

* BTC/USDT
* ETH/USDT
* DOGE/USDT

Each trade event contains price and quantity information.

---

## Time Windows

Trades must be aggregated into **fixed 5-second windows**.

Important constraint:

Time windows must be **aligned to the wall clock**, starting from:

00:00:00

Examples:

Window 1
00:00:00 → 00:00:05

Window 2
00:00:05 → 00:00:10

Window 3
00:00:10 → 00:00:15

This ensures windows can be compared consistently.

---

## Aggregation

For each window and symbol compute:

Average trade price

Example record:

symbol: BTCUSDT
window_start: 12:00:10
window_end: 12:00:15
average_price: 67321.11

---

## Alerts

If the difference between **two consecutive windows** for the same symbol exceeds **5%**, generate an alert.

Example:

Window 1 average: 100
Window 2 average: 106

Price change: +6% → trigger alert.

The alert can be:

* stored in the database
* logged
* emitted as an event

Implementation choice is up to you.

---

## API

Expose at least one endpoint:

GET /prices

Example response:

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

Optional query parameters:

symbol
from
to

---

## Edge Cases to Consider

You may want to think about:

* WebSocket disconnections
* Duplicate trade events
* High message throughput
* Late arriving events
* Empty windows (no trades)
* Out of order events

You do not need to solve everything perfectly, but explain your decisions.

---
