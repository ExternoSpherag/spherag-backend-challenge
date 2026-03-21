# US-04 — Generate Alert on >5% Consecutive Window Change

## Context
The system must detect relevant price movements between consecutive windows.

## Goal
Generate an alert when average price change between two consecutive windows of the same symbol exceeds 5%.

## Rule
`abs((current_avg - previous_avg) / previous_avg) * 100 > 5`

## Example
- Previous window average: `100`
- Current window average: `106`
- Change: `+6%` → alert triggered

## Acceptance Criteria
1. Comparison uses consecutive windows for the same symbol only.
2. Alert is triggered when absolute change is strictly greater than 5%.
3. No alert when previous window does not exist.
4. Alert is persisted and/or logged.

## Notes for Implementation
- Keep threshold logic deterministic and covered by tests.
- Include tests for `+6%`, `-6%`, and exactly `5%`.
