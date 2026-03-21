# US-07 — Add Automated Test Coverage

## Context
Each new behavior must be protected by automated tests.

## Goal
Cover domain, application, and API behavior introduced by this challenge.

## Acceptance Criteria
1. Domain tests validate window alignment and alert threshold rules.
2. Application tests validate handlers and validators.
3. API integration tests validate `GET /api/prices` contract and filtering.
4. Auth-related tests validate protected endpoint behavior.

## Notes for Implementation
- Keep tests aligned with existing test project structure.
- Prefer small deterministic tests for core logic.
