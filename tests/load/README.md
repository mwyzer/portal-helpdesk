# Load Tests (k6)

Four scripts matching the scenarios in `documentation/todo-phase-7-hardening.md`:

| Script | Scenario |
|--------|----------|
| `normal-load.js` | 50 concurrent users, 10 minutes |
| `peak-load.js` | 200 concurrent users, 5 minutes |
| `stress-test.js` | Ramp to 500 concurrent users |
| `ai-endpoint.js` | 10 concurrent AI chat conversations |

**All four scripts actually run against a live Docker Compose stack, 2026-08-05** (k6 installed
via winget — wasn't available when these were originally written). Found and fixed three real
bugs in the process:

1. **Rate limiter silently keyed by IP instead of user, for every request.**
   `RateLimitingMiddleware` was registered before `UseAuthentication()` in `Program.cs`, so
   `context.User` was never populated when it checked the caller's identity — despite the
   middleware's own doc comment saying it keys by authenticated user ID, every request fell
   back to per-IP keying. `normal-load.js`'s first run: 92.6% failure with all 50 VUs sharing
   one IP's 300/min bucket instead of 50 separate ones. Fixed by moving the middleware after
   `UseAuthentication()`.
2. **The scripts had the same bug independently.** They logged in once via `setup()` (which
   runs once for the whole test, not once per VU) and shared that single token across every
   VU. Rewrote `helpers.js` so each VU authenticates as one of DbSeeder's 50 seeded accounts
   (`credentialsForVU()`), caching the token per VU.
3. **Postgres connection exhaustion under real concurrent load, with no self-recovery.**
   `stress-test.js` at 500 VUs hit Postgres's default `max_connections=100` (the app's Npgsql
   pool had no explicit cap, so it could also try to grow toward ~100 on its own, leaving no
   headroom for anything else) — Postgres started rejecting *all* new connections, including
   the app's own health checks, so the app stayed degraded (`database: false`) even minutes
   after the load stopped; only a manual container restart recovered it. Fixed in two steps:
   capped the app's pool at `Maximum Pool Size=50` in the connection string (so the app can
   never exhaust Postgres on its own — confirmed this alone fixed the *no self-recovery* part,
   the app returned to healthy on its own afterward) and raised Postgres's `max_connections` to
   200 (confirmed this eliminated the exhaustion entirely — 0 "too many clients" errors on the
   next run, versus 189 before).

**Results after all three fixes:**

| Script | Result |
|--------|--------|
| `normal-load.js` (50 users) | **100% success**, p95 latency 40.83ms |
| `peak-load.js` (200 users) | **97% success** (3.01% failure, under the 5% threshold), p95 latency 645ms. Needed one more fix: `/api/employees` is Super Admin/HRD/Manager-only, so the 20/50 seeded accounts without access legitimately get 403 — k6's `http_req_failed` metric doesn't know that's an intended outcome, so it was counted as a failure until the request was annotated with `responseCallback: http.expectedStatuses(200, 403)`. The residual ~3% is 200 VUs sharing only 50 real rate-limit buckets (4 VUs/bucket) — a realistic finding, not a bug. |
| `stress-test.js` (ramp to 500 users) | Postgres survived the full run cleanly (0 connection errors, confirmed healthy throughout and after). The script's own `http_req_failed` threshold (`rate<0.20`) still fails (~66%) — but that's entirely the `login`/`health` checks, both correctly IP-keyed anonymous endpoints, saturating from 500 fake identities sharing this one test machine's real IP. The authenticated `tickets reachable` check passes cleanly the whole time, confirming real app capacity is fine — this is a test-methodology ceiling (one IP can't simulate 500 independent public IPs), not an app bug. |
| `ai-endpoint.js` (10 AI chats) | Not yet run — needs `AI:ApiKey` configured, which this dev environment doesn't have. |

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) installed
- The API running and reachable (defaults to `http://localhost:5192`)
- The demo accounts seeded (default `DbSeeder` behavior — see the root `README.md`)

## Running

```bash
# Against local dev
k6 run tests/load/normal-load.js

# Against a different environment
k6 run -e BASE_URL=https://staging.example.com tests/load/normal-load.js

# With non-default credentials (e.g. staging doesn't have the dev demo accounts)
k6 run -e BASE_URL=https://staging.example.com -e LOGIN_EMAIL=you@example.com -e LOGIN_PASSWORD=... tests/load/normal-load.js
```

Run each script individually — running two at once against the same environment will conflate
their results (and their rate-limit buckets, since they may share demo accounts).

## Interpreting results

k6 prints a summary at the end including `http_req_duration` percentiles and `http_req_failed`
rate. The `thresholds` block in each script encodes a pass/fail bar — k6 exits non-zero if any
threshold fails, so these can be wired into CI once you have a staging environment to point them
at (they are **not** currently part of `.github/workflows/ci.yml`, since running them against
`localhost` in a GitHub-hosted runner wouldn't be a meaningful test).
