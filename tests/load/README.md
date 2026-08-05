# Load Tests (k6)

Four scripts matching the scenarios in `documentation/todo-phase-7-hardening.md`:

| Script | Scenario |
|--------|----------|
| `normal-load.js` | 50 concurrent users, 10 minutes |
| `peak-load.js` | 200 concurrent users, 5 minutes |
| `stress-test.js` | Ramp to 500 concurrent users |
| `ai-endpoint.js` | 10 concurrent AI chat conversations |

**`normal-load.js` actually run 2026-08-05** against a live Docker Compose stack. First run:
92.6% failure, but not a capacity problem — the general rate limiter (`RateLimitingMiddleware`)
was registered before `UseAuthentication()` in `Program.cs`, so `context.User` was never
populated when it checked the request's identity, and every request silently fell back to
per-IP keying. All 50 VUs ran from one IP, so they shared one 300/min bucket instead of 50.
Separately, these scripts also shared a single login token across every VU (via `setup()`,
which runs once for the whole test, not once per VU) — the same "not actually N independent
identities" mistake, compounding the first bug. Fixed both: moved the middleware after
`UseAuthentication()` in `Program.cs`, and rewrote `helpers.js` so each VU logs in as one of
DbSeeder's 50 distinct seeded accounts (`credentialsForVU()`), caching the token per VU. Re-run
of `normal-load.js`: **100% success, p95 latency 40.83ms**. `peak-load.js`, `stress-test.js`,
and `ai-endpoint.js` use the same fixed helper but have not yet been run themselves.

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
