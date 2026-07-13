# 🔄 Regression Testing

> **Status:** 📋 Planned — strategy defined, automation pending  
> **Definition:** Ensuring new code doesn't break existing functionality

## Overview

Regression testing is NOT a separate test suite — it's a **strategy** of re-running existing tests (unit, integration, E2E, smoke) on every change. The goal: catch unintended side effects before they reach production.

## Regression Test Pyramid

```
         ┌──────────────┐
         │    Smoke     │  ← Every commit / deploy (60s)
         │  ┌────────┐  │
         │  │  E2E   │  │  ← Every PR / merge to main (5 min)
         │  │┌──────┐│  │
         │  ││Integr││  │  ← Every PR (2 min)
         │  ││┌────┐││  │
         │  │││Unit│││  │  ← Every commit (10s)
         │  ││└────┘││  │
         └──┴┴──────┴┴──┘
```

| Layer | Trigger | Max Time | Failure = |
|-------|---------|----------|-----------|
| **Unit** | Every commit (`git push`) | 10s | Can't merge |
| **Integration** | Every PR | 2 min | Can't merge |
| **E2E** | PR + nightly | 5 min | Can't merge |
| **Smoke** | Post-deploy | 60s | Auto-rollback |

## What Changes Trigger Which Tests?

| Change | Unit | Integration | E2E | Notes |
|--------|------|-------------|-----|-------|
| New entity / migration | ✅ | ✅ | — | DB schema tests |
| New API endpoint | ✅ | ✅ | ✅ | Contract tests |
| Frontend page changed | — | — | ✅ | Visual check |
| Auth/permission change | ✅ | ✅ | ✅ | Every role tested |
| Config change | ✅ | ✅ | ✅ | Connection strings, CORS |
| Package update | ✅ | ✅ | — | Dependency scan |
| CSS/font only | — | — | ✅ | Visual regression |
| Documentation only | — | — | — | Skip all tests |

## Regression Test Suite (Current)

### Always Runs
```
tests/AIHelpdesk.Tests/                   # 23 unit tests (xUnit)
  ├── Services/UserServiceTests.cs
  ├── Services/RoleServiceTests.cs
  ├── Services/DepartmentServiceTests.cs
  ├── Domain/DepartmentTests.cs
  ├── Domain/RefreshTokenTests.cs
  └── Contracts/AuthContractsTests.cs
```

### Runs on PR
```
frontend/tests/e2e/all-phases.spec.ts     # 17 E2E tests (Playwright)
```

### Runs Post-Deploy
```
tests/smoke/health-check.sh               # 4 health checks
```

## Phase-Specific Regression

As each phase is completed, its tests become part of the regression suite:

```
Phase 1 (Foundation)     — 13 E2E + 23 unit = 36 tests
Phase 2 (HR)             — +4 E2E = 40 tests
Phase 3 (Secretary)      — +4 E2E + 10 unit = 54 tests
Phase 4 (AI Chat)        — +3 E2E + 15 unit = 72 tests
Phase 5 (Ticketing)      — +5 E2E + 20 unit = 97 tests
Phase 6 (Recruitment)    — +4 E2E + 15 unit = 116 tests
Phase 7 (Hardening)      — Security + perf = 130+ tests
```

## Regression Runbook

### Before merging to `main`:
```bash
# 1. Unit tests
dotnet test --no-restore

# 2. Rebuild & quick smoke
docker compose up -d --build
bash tests/smoke/health-check.sh

# 3. E2E (if frontend/API changed)
cd frontend && npx playwright test --reporter=dot

# 4. All green → merge
```

### Nightly regression:
```yaml
# .github/workflows/nightly.yml (planned)
on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM daily
jobs:
  regression:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: docker compose up -d --build
      - run: dotnet test
      - run: cd frontend && npx playwright test
      - name: Notify on failure
        if: failure()
        run: |
          echo "Nightly regression failed!" | \
          curl -X POST -d @- $SLACK_WEBHOOK
```

## When to Expand Regression Suite

Add new regression tests when:
- A bug reaches production → write a test that reproduces that bug
- A new feature is completed → its tests join the regression suite
- A refactoring is planned → ensure coverage before starting
- A user reports unexpected behavior → reproduce in test, then fix

## Regression Test Data

- **Do NOT use production data** for regression tests
- Maintain a `seed-regression.sql` with known test data
- Reset test database before each regression run

```bash
# Reset test data
docker compose exec postgres psql -U helpdesk -d aihelpdesk_test -f seed-regression.sql
```

## Related Files

- `documentation/unit-testing.md` — Unit test guide
- `documentation/e2e-testing.md` — E2E test guide
- `documentation/smoke-testing.md` — Smoke test guide
- `tests/smoke/health-check.sh` — Shell-based smoke tests
