# US-06 — Resilience and Throughput Handling

## Context
Streaming pipelines must tolerate network issues and traffic spikes.

## Goal
Increase ingestion robustness under disconnects, duplicates, and burst throughput.

## Acceptance Criteria
1. WebSocket reconnection uses bounded backoff.
2. Duplicate trade events are handled with a defined strategy.
3. Processing remains stable under high message rate.
4. Logs provide enough context for diagnostics.

## Notes for Implementation
- Prefer bounded queue/channel between receiver and processor.
- Use trade identifier strategy for deduplication (`symbol` + `tradeId` + time).
- Document chosen trade-offs (latency vs. consistency).
