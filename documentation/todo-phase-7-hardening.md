# Phase 7 — Hardening & Production Deployment — TODO Checklist

> **Status (2026-08-05): 40/103 tasks done (~39%)** (103, not 102 — one item, the CI pipeline itself, was added as a prerequisite not in the original list). This pass implemented HTTPS/HSTS/CSP,
> general rate limiting, response compression, upload validation/size limits, Dependabot, a
> CI pipeline, expanded health checks, k6 load tests (now actually executed — see Performance
> Testing below, which found and fixed a real rate-limiter bug), production
> docker-compose + nginx templates (including a previously-missing SignalR `/hubs` proxy —
> see below), backup/restore scripts, and the deployment/admin/user manual docs. Still not
> started: Redis caching, audit logging, ClamAV scanning, formal security testing
> (ZAP/Trivy/pentest), monitoring/alerting stack, staging environment + CD approval gates,
> and all of UAT & go-live (those require a running staging environment and real users, not
> just code).
>
> **Bug found and fixed in this pass (unrelated to the Phase 7 checklist, but discovered
> while auditing the app):** the frontend checked `roles.includes('SuperAdmin')` (no space)
> in `App.tsx`, `AppLayout.tsx`, `DashboardPage.tsx`, and `VacanciesPage.tsx`, but the role is
> actually seeded and issued in the JWT as `"Super Admin"` (with a space) — see
> `DbSeeder.cs`. This meant Super Admin users fell through to the Employee-level sidebar nav
> and were blocked by most admin route guards; only `TicketsPage.tsx`/`TicketDetailPage.tsx`
> happened to use the correct spelling already. Fixed by aligning all four files to the
> correct `'Super Admin'` string.
>
> **Also discovered:** neither `frontend/vite.config.ts` (dev) nor `frontend/nginx.conf`
> (Docker image) proxied `/hubs/*`, only `/api/*` — meaning the SignalR real-time
> notification path could never actually connect in local dev or Docker Compose, silently
> falling back to 30s polling with no visible error. Fixed both configs; also added the
> matching `/hubs/` route to the new `docker/production/nginx-ssl.conf.example`.
>
> **E2E suite actually run (2026-08-04)** against a live Docker Compose stack rebuilt from
> current code — this is what caught the Super Admin bug above, plus a second, separate one:
> `authStore.user` never got populated on a hard page load (`loadUser()` was defined but never
> called anywhere), so even after fixing the role-string bug, direct navigation to any admin
> route still bounced Super Admin to `/dashboard` because `RoleGuard` read a `null` user. Fixed
> by reading the persisted user from `localStorage` synchronously at store creation. First run:
> 18/50 passing; after both fixes: 42/50. The remaining 8 failures were all the general rate
> limiter's bucket getting exhausted by rapid back-to-back logins sharing one demo account —
> confirmed even on a clean run (fresh backend restart, no leftover state), a 23-test smoke run
> alone tripped the 100/min default within about a minute, which is realistic for a person
> rapidly clicking through admin pages, not just automated-test load. Raised the general default
> to 300/min; re-ran the full suite and got **49/50 passing**, with total runtime dropping from
> ~8min to 4.4min. The one remaining failure is an unrelated pre-existing test-data race (the
> demo Super Admin account has zero real leave requests, and `getRowCount()` occasionally reads
> a stale non-zero count before a background refetch corrects it). Also fixed three unrelated
> test/UI-copy mismatches found along the way (missing `title` attribute on `LeaveTypesPage` row
> actions, a test expecting "Cancel" on a dialog button actually labeled "Close", an ambiguous
> `text=` locator matching both a heading and an empty-state row). Full writeup:
> `test-coverage-report.md`.

## Security Hardening

- [x] Enforce HTTPS (redirect HTTP → HTTPS) — `app.UseHttpsRedirection()` added, gated to non-Development environments
- [x] Add HSTS header — `AddHsts()` + `app.UseHsts()` added (365 days, includeSubDomains), non-Development only
- [ ] Restrict CORS to production domain only — still config-driven (`Cors:Origins`); the production compose file sets it from `FRONTEND_ORIGIN`, but nothing in code enforces it can't be `*` or localhost — operational discipline, not a code gate
- [x] Add Content Security Policy (CSP) headers — middleware added in `Program.cs` (also sets `X-Content-Type-Options`, `Referrer-Policy`)
- [x] Configure rate limiting middleware (300 req/min general, 10 req/min AI) — `RateLimitingMiddleware` rewritten as two-tier: existing AI-specific limit plus a new general limit (configurable via `RateLimiting:GeneralMaxRequestsPerMinute`), keyed by user ID or client IP for anonymous requests. Original spec said 100/min general; raised to 300/min after E2E testing showed 100/min tripping under realistic rapid-navigation load, not just abuse — see the status note above
- [ ] Move all secrets to environment variables / Docker secrets — done for the new `docker-compose.prod.yml` (all secrets from `.env`, see `.env.example`); the dev-only `docker-compose.yml`'s hardcoded JWT key is unchanged (intentionally — it's dev-only and documented as such)
- [x] Remove any `.env` or secrets from repository — confirmed none tracked; `.env.example` documents required keys without values
- [x] Shorten JWT access token expiry (15 minutes) — `AccessTokenExpiryMinutes: 15` in `appsettings.json`
- [x] Implement refresh token rotation — `AuthService.RefreshTokenAsync` revokes the old token and issues a new one on every refresh
- [x] Implement refresh token revocation list — `RefreshToken.IsRevoked`/`RevokedAt`, checked via `IsActive`
- [x] Enforce password policy (min 8 chars, complexity) — `RequireDigit`, `RequiredLength = 8`, `RequireUppercase` configured
- [x] Implement account lockout (5 failed attempts) — `Lockout.MaxFailedAccessAttempts = 5`, 15 min lockout
- [ ] Add ClamAV file scanning for uploads — not implemented (extension/size validation only)
- [x] Restrict file upload extensions — `EmployeesController.ImportEmployees` (had zero validation — real bug, fixed with extension/size checks), `KnowledgeBaseController` upload (added `[RequestSizeLimit(20MB)]`), ticket attachments and candidate CVs already validated in Phases 5/6
- [x] Audit all EF Core queries for SQL injection safety — only one raw-SQL call exists (`KnowledgeBaseService`'s pgvector similarity query), confirmed parameterized via EF's `{0}`/`{1}`/`{2}` placeholders, not string interpolation
- [ ] Verify all data mutations are audit-logged — **confirmed not implemented**: no `AuditLog` entity exists anywhere in the codebase, only per-record `CreatedAt`/`UpdatedAt`. Flagged in `documentation/deployment-runbook.md` as a known gap; building a real audit trail is a bigger effort than this pass covers
- [x] Set up Dependabot for dependency vulnerability scanning — `.github/dependabot.yml` added (nuget, npm, github-actions, 3x docker ecosystems, weekly)
- [ ] Run `dotnet list --vulnerable` and fix findings — not run (needs a live NuGet feed check, not done in this pass)
- [ ] Run Trivy/Snyk container image scan
- [ ] Run OWASP ZAP baseline scan against staging — no staging environment exists yet
- [ ] Manual penetration test: auth bypass
- [ ] Manual penetration test: IDOR
- [ ] Manual penetration test: XSS
- [ ] Manual penetration test: CSRF
- [ ] Verify all endpoints enforce role/permission checks — spot-checked via the Super Admin bug above, but no systematic pass done

## Performance Testing

- [x] Write k6 load test: normal load (50 users, 10 min) — `tests/load/normal-load.js`
- [x] Write k6 load test: peak load (200 users, 5 min) — `tests/load/peak-load.js`
- [x] Write k6 load test: stress test (ramp to 500 users) — `tests/load/stress-test.js`
- [x] Write k6 load test: AI endpoint (10 concurrent chats) — `tests/load/ai-endpoint.js`
- [x] Run load tests and analyze results — **three of four scripts actually run 2026-08-05** (k6
  installed via winget), finding and fixing four real bugs:
  - Rate limiter registered before `UseAuthentication()` in `Program.cs`, so every request fell
    back to per-IP keying regardless of the authenticated user — `normal-load.js`'s first run
    saw 92.6% failure with all 50 VUs sharing one IP's 300/min bucket. Fixed the middleware
    order. Re-run: **100% success, p95 41ms**.
  - The k6 scripts had the same bug independently (`setup()` runs once for the whole test, not
    once per VU, so all VUs shared one login token). Fixed via `credentialsForVU()` in
    `helpers.js`, using DbSeeder's 50 seeded accounts.
  - `peak-load.js` then surfaced a k6-only measurement issue: `/api/employees` correctly 403s
    for roles without access, but k6's `http_req_failed` didn't know that was intentional.
    Fixed with `responseCallback: http.expectedStatuses(200, 403)`. Re-run: **97% success
    (3.01% failure), p95 645ms** — residual is 200 VUs sharing only 50 real rate-limit buckets.
  - `stress-test.js` (500 VUs) then found a real Postgres connection-exhaustion bug: default
    `max_connections=100` with an uncapped app pool meant the app could exhaust Postgres
    entirely under peak load, and — worse — Postgres then rejected the app's own health checks
    too, so it stayed degraded (`database: false`) even minutes after load stopped, needing a
    manual restart. Fixed in two independently-verified steps: capped the app's pool
    (`Maximum Pool Size=50`, fixed the no-self-recovery problem) and raised Postgres's
    `max_connections` to 200 (eliminated the exhaustion — 0 "too many clients" errors, down
    from 189). Re-run: Postgres survived the full 11-minute run cleanly; the script's own loose
    threshold still "fails" (~66%) but that's login/health saturating one shared per-IP bucket
    from 500 fake identities behind this one test machine's real IP, not an app problem (the
    authenticated `tickets` check passed throughout).
  - `ai-endpoint.js` not yet run — needs `AI:ApiKey` configured, which this dev environment
    doesn't have. Full writeup: `tests/load/README.md`.
- [ ] Fix N+1 query issues (EF Core `.Include()` / `.ThenInclude()`) — not audited this pass
- [ ] Add missing database indexes (FK + status + date composite) — existing indexes look reasonable on inspection (status/priority/SLA/composite indexes already present on Ticket, Candidate, Interview, etc.) but no systematic audit against real query patterns was done
- [x] Ensure all list endpoints have pagination — `page`/`pageSize` params confirmed consistently used across Employee, Leave, Meeting, Ticket, Chat, and Knowledge Base list endpoints; no explicit max-page-size cap verified
- [ ] Configure Redis caching for reference data (roles, departments, lookups) — deliberately not started; no caching code exists yet, so no Redis service was added to `docker-compose.prod.yml` either (see that file's comments — add both together when this is built)
- [x] Enable response compression (`app.UseResponseCompression()`) — added to `Program.cs`
- [ ] Tune PostgreSQL connection pool (`MaxPoolSize`) — not configured; only server-side `max_connections` is tuned in `docker-compose.prod.yml`

## Production Infrastructure

- [ ] Provision VPS (4 cores, 8GB RAM, 100GB SSD) — operational step, not applicable until an actual deploy target exists
- [ ] Install Docker Engine (24+) — operational step
- [ ] Install Docker Compose — operational step
- [x] Create production `docker-compose.yml` (postgres, backend, frontend, nginx, worker) — `docker/production/docker-compose.prod.yml`; no separate `redis`/`worker` services (see file's own comments on why: no caching code yet, background jobs already run in-process)
- [x] Create production `nginx.conf` (SPA serving, API proxy, WebSocket) — `docker/production/nginx-ssl.conf.example` (TLS-terminating) + `frontend/nginx.conf` (fixed to also proxy `/hubs/`, previously missing)
- [ ] Obtain SSL certificate (Let's Encrypt / Certbot) — operational step; procedure documented in `documentation/deployment-runbook.md` §1
- [x] Configure SSL certificate auto-renewal — `certbot` service in `docker-compose.prod.yml` renews twice daily; nginx reload-on-renewal documented as a cron entry in the runbook
- [x] Configure Nginx security headers — CSP/X-Content-Type-Options/Referrer-Policy set at the app level (`Program.cs`); TLS config (protocols/ciphers) set in `nginx-ssl.conf.example`
- [x] Tune PostgreSQL (shared_buffers, work_mem, max_connections) — baseline values set in `docker-compose.prod.yml`'s postgres `command:` for a 4-core/8GB target
- [ ] Test full stack deployment with Docker Compose — not run (no target server/environment available in this pass)
- [ ] Verify health check endpoint returns OK — verified locally against `/api/health` during earlier phases; not re-verified against the production compose stack specifically

## Monitoring Setup

- [ ] Configure Serilog to send logs to Seq
- [ ] Set up Seq dashboard (structured log viewer)
- [x] Create health check endpoint (`GET /api/health`) — checks DB connectivity, returns `{status, database, timestamp}`
- [x] Add health checks: database connectivity
- [ ] Add health checks: Redis connectivity — N/A until Redis is added
- [ ] Add health checks: AI provider connectivity
- [x] Add health checks: disk space — `/api/health` now reports `diskUsedPercent`, unhealthy at ≥95%
- [ ] Set up uptime monitoring (UptimeRobot/BetterStack) — operational step; `/api/health` is ready to be polled
- [ ] Configure alerts for: API response time > 3s (p95)
- [ ] Configure alerts for: error rate > 5%
- [ ] Configure alerts for: AI API latency > 15s
- [ ] Configure alerts for: disk usage > 85%
- [ ] Configure alerts for: SSL expiry < 30 days
- [ ] Set up AI daily token budget alert

## Backup & Disaster Recovery

- [x] Create daily PostgreSQL backup script (`pg_dump` + gzip) — `docker/production/scripts/backup-db.sh`
- [x] Set up cron job for daily backup (02:00) — crontab line documented in `documentation/deployment-runbook.md` §4 (not installed on any actual server, since none exists yet)
- [x] Set up weekly backup to cloud storage (S3/Backblaze B2) — `backup-db.sh`/`backup-files.sh` both push to `RCLONE_REMOTE` if configured; opt-in since it needs a real bucket + `rclone config` on the host
- [x] Create file storage backup script (`rclone`) — `docker/production/scripts/backup-files.sh`
- [x] Set up file backup cron job — documented alongside the DB backup cron line
- [x] Configure backup retention (daily: 14d, weekly: 3mo, monthly: 12mo) — implemented via `find -mtime +N -delete` pruning in both backup scripts
- [ ] Test database restore procedure — `restore-db.sh` written and includes a scratch-DB verification path in the runbook, but not exercised against a real backup (no live environment to generate one from)
- [ ] Test file storage restore procedure — same caveat as above, `restore-files.sh` written but unexercised
- [x] Write restore runbook — folded into `documentation/deployment-runbook.md` §4 rather than a separate file (covers both DB and file restore, plus a quarterly drill recommendation)

## CI/CD Pipeline Hardening

- [x] ~~Add CI pipeline~~ — not an original checklist item, but built as the prerequisite for everything below: `.github/workflows/ci.yml` runs backend restore/build/test against a real Postgres service container, frontend `npm ci`/`npm run build`, and a docker buildx build for both images, on every push/PR. **Lint is deliberately excluded** — `package.json`'s `lint` script references ESLint, but it's not installed and no config file exists; a pre-existing gap, not fixed here.
- [ ] Add staging environment to GitHub Actions — no staging environment exists to add
- [ ] Add production environment with manual approval gate
- [ ] Configure staging auto-deploy from `develop` branch
- [ ] Configure production deploy from release tag
- [ ] Add post-deploy smoke tests to pipeline
- [ ] Add vulnerability scanning step to pipeline
- [ ] Implement rollback procedure — documented manually in `documentation/deployment-runbook.md` §2 (git checkout + rebuild, with a note on when a DB restore is required instead); not automated in CI
- [ ] Test rollback on staging
- [ ] Add pre-deployment checklist to pipeline

## Documentation

- [x] Write user manual (employee-facing features) — `documentation/user-manual.md`
- [x] Write admin manual (configuration, user management, templates) — `documentation/admin-manual.md`
- [x] Write deployment runbook (server setup, docker, backup/restore) — `documentation/deployment-runbook.md`
- [ ] Review auto-generated Swagger/OpenAPI documentation — not reviewed this pass
- [x] Create README with project overview and setup instructions — already existed, kept up to date across this whole implementation effort
- [ ] Review and update all code comments — not done as a dedicated pass

## UAT & Go-Live

> Everything in this section requires a running staging/production environment, real
> stakeholders, and elapsed calendar time (alpha/beta windows) — none of it can be
> "implemented" as code. Left entirely unchecked; revisit once a deploy target exists.

- [ ] Set up staging environment for UAT
- [ ] Conduct alpha testing (internal team, 1 week)
- [ ] Fix all critical/blocker bugs found in alpha
- [ ] Conduct beta testing (pilot users, 2 weeks)
- [ ] Collect feedback from beta users
- [ ] Fix reported issues from beta
- [ ] Conduct UAT with key stakeholders (1 week)
- [ ] Obtain UAT sign-off per module
- [ ] Verify all go-live criteria are met
- [ ] Schedule production deployment window
- [ ] Announce planned downtime (if any)
- [ ] Deploy to production
- [ ] Verify health check on production
- [ ] Verify all features on production
- [ ] Monitor first week: daily health check
- [ ] Monitor first month: weekly review
- [ ] Set up monthly dependency update schedule
