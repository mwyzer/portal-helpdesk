# Deployment Runbook

Operational reference for standing up, updating, backing up, and restoring the AI Helpdesk
production stack. Written against `docker/production/docker-compose.prod.yml`.

This is a runbook, not a tutorial — it assumes familiarity with Docker, Linux server
administration, and the app's architecture (see `README.md` and `documentation/*` phase docs
for that background).

---

## 1. Server Setup (first-time)

1. Provision a VPS: 4 cores / 8GB RAM / 100GB SSD minimum (matches the Postgres tuning in
   `docker-compose.prod.yml`; scale up if the AI usage or ticket volume is heavy).
2. Install Docker Engine (24+) and the Docker Compose plugin.
3. Point DNS for your domain at the server's IP.
4. Clone the repo to e.g. `/opt/aihelpdesk`.
5. Copy `.env.example` to `.env` at the repo root and fill in every value — in particular
   `POSTGRES_PASSWORD`, `JWT_KEY` (generate with `openssl rand -base64 64`), `FRONTEND_ORIGIN`
   (your public HTTPS domain), and `AI_API_KEY`. Never commit this file.
6. Copy `docker/production/nginx-ssl.conf.example` to `docker/production/nginx-ssl.conf` and
   replace `YOUR_DOMAIN` with your real domain. Update the `volumes:` mount for the `nginx`
   service in `docker-compose.prod.yml` to point at this file if you renamed it.

### First TLS certificate

The `nginx` container as configured expects certs to already exist at
`/etc/letsencrypt/live/YOUR_DOMAIN/`, which won't be true on a fresh server — so bring it up
in two steps:

```bash
# 1. Temporarily comment out the `listen 443` server block in nginx-ssl.conf (keep only the
#    :80 block with the acme-challenge location), then start nginx + certbot's dependencies:
docker compose -f docker/production/docker-compose.prod.yml up -d frontend backend nginx

# 2. Issue the certificate via the certbot webroot flow:
docker compose -f docker/production/docker-compose.prod.yml run --rm certbot \
  certonly --webroot -w /var/www/certbot -d YOUR_DOMAIN

# 3. Uncomment the 443 block in nginx-ssl.conf, then reload:
docker compose -f docker/production/docker-compose.prod.yml exec nginx nginx -s reload

# 4. Start the certbot renewal daemon and everything else:
docker compose -f docker/production/docker-compose.prod.yml up -d
```

Renewal after this is automatic (the `certbot` service checks twice daily and no-ops until
the cert is close to expiry), but nginx needs a reload afterward to pick up a renewed cert —
add to root's crontab:

```cron
0 3 * * * docker compose -f /opt/aihelpdesk/docker/production/docker-compose.prod.yml exec nginx nginx -s reload
```

### First deploy

```bash
cd /opt/aihelpdesk
docker compose -f docker/production/docker-compose.prod.yml --env-file .env up -d --build
```

EF Core migrations run automatically on backend startup (see `Program.cs`'s
`db.Database.Migrate()` call) — no separate migration step is needed.

Verify: `curl -f https://YOUR_DOMAIN/api/health` should return `{"status":"Healthy",...}`.

---

## 2. Routine Deploys (updates)

```bash
cd /opt/aihelpdesk
git pull
docker compose -f docker/production/docker-compose.prod.yml --env-file .env up -d --build
```

This rebuilds only images whose source changed and recreates only those containers;
`postgres`'s data volume is untouched. Watch `docker compose ... logs -f backend` during
rollout to catch migration or startup failures immediately.

### Rollback

```bash
git log --oneline -5          # find the last known-good commit
git checkout <commit>
docker compose -f docker/production/docker-compose.prod.yml --env-file .env up -d --build
```

If the bad deploy included a destructive EF Core migration (column/table drop), a code
rollback alone won't undo the schema change — restore the database from the most recent
backup taken before the deploy (§4) instead of just rolling back code.

---

## 3. Monitoring & Health

- `GET /api/health` — DB connectivity + disk usage; returns 503 if the DB is unreachable or
  disk usage is ≥95%. Wire an uptime monitor (UptimeRobot/BetterStack/etc.) to poll this.
- `docker compose -f docker/production/docker-compose.prod.yml ps` — container health status
  (each service defines a `healthcheck`).
- Logs: `docker compose -f docker/production/docker-compose.prod.yml logs -f backend` (all
  services log via the `json-file` driver with rotation already configured, so `docker logs`
  won't grow unbounded).
- Structured log aggregation (Serilog → Seq) and metrics/alerting (response-time, error-rate,
  AI-latency thresholds) are **not implemented** — see `documentation/todo-phase-7-hardening.md`
  for what's still outstanding there.

---

## 4. Backup & Restore

Scripts live in `docker/production/scripts/`. All assume they're run from a checkout on the
production host, with `.env` present at the repo root (or `ENV_FILE` pointed at it).

### Automated backups (cron)

```cron
0 2 * * * /opt/aihelpdesk/docker/production/scripts/backup-db.sh    >> /var/log/aihelpdesk-backup.log 2>&1
0 3 * * 0 /opt/aihelpdesk/docker/production/scripts/backup-files.sh >> /var/log/aihelpdesk-backup.log 2>&1
```

- `backup-db.sh` — daily `pg_dump -Fc | gzip` to `/var/backups/aihelpdesk/daily/`. Sundays
  also copy into `weekly/`, and the 1st of the month also copies into `monthly/`. Retention:
  daily 14 days, weekly 90 days, monthly 365 days (pruned automatically by the script).
- `backup-files.sh` — weekly tarball of the `uploads` Docker volume (ticket attachments, KB
  documents, candidate CVs, generated letters) to `/var/backups/aihelpdesk/files/`. Retention:
  90 days.
- Both scripts push to offsite storage via `rclone` if `RCLONE_REMOTE` is set in `.env` (e.g.
  `RCLONE_REMOTE=b2:aihelpdesk-backups` after running `rclone config` once on the host).
  **Without `RCLONE_REMOTE` configured, backups only exist on the same VPS as the data they
  protect — not sufficient disaster recovery on its own.** Set this up before go-live.

### Restore procedure

**Database** — `restore-db.sh <path-to-.dump.gz>`. This stops the `backend` container, drops
and recreates the database, restores via `pg_restore`, then restarts `backend`. It requires
typing the database name to confirm (destructive). To verify a backup without touching
production, restore into a scratch database first:

```bash
docker compose -f docker/production/docker-compose.prod.yml exec postgres \
  createdb -U "$POSTGRES_USER" aihelpdesk_verify
gunzip -c /var/backups/aihelpdesk/daily/aihelpdesk-<date>.dump.gz | \
  docker compose -f docker/production/docker-compose.prod.yml exec -T postgres \
  pg_restore -U "$POSTGRES_USER" -d aihelpdesk_verify --no-owner
# spot-check row counts, then drop it:
docker compose -f docker/production/docker-compose.prod.yml exec postgres \
  dropdb -U "$POSTGRES_USER" aihelpdesk_verify
```

**Files** — `restore-files.sh <path-to-uploads-*.tar.gz>`. Stops `backend`, wipes and
repopulates the `uploads` volume from the archive, restarts `backend`. Requires typing
`restore` to confirm (destructive).

Run a full restore drill (both scripts, against a copy of the stack, not production) at least
quarterly — an untested backup is not a verified backup.

---

## 5. Known Gaps (as of 2026-08-04)

Carried over from `documentation/todo-phase-7-hardening.md` — these are real, not yet closed:

- **No structured audit log.** There is no `AuditLog` entity/table anywhere in the codebase —
  data mutations are not recorded with who/when/old-value/new-value. Application-level audit
  logging (not just DB-level `CreatedAt`/`UpdatedAt`) would need to be designed and built.
- **No Redis caching layer.** Reference-data caching (roles, departments, lookups) is
  unimplemented; deliberately not added to `docker-compose.prod.yml` until the caching code
  exists (see that file's comments).
- **No ClamAV file scanning** on uploads (extension/size validation exists, but not
  malware scanning).
- **No log aggregation / metrics / alerting stack** (Seq, uptime monitoring, threshold alerts)
  is deployed.
- **No formal security testing** has been run: no OWASP ZAP scan, no `dotnet list
  --vulnerable` pass, no container image scan (Trivy/Snyk), no manual pentest.
- **Staging environment / CI approval gates** are not set up — CI (`.github/workflows/ci.yml`)
  builds and tests on every push/PR but there's no staging auto-deploy or production approval
  gate.
- **All four k6 scripts actually run 2026-08-05** against a live Docker Compose stack (see
  `tests/load/README.md` for the full writeup) — found and fixed four real bugs:
  1. The general rate limiter's own doc comment says it keys by authenticated user ID, but it
     was registered in `Program.cs` *before* `UseAuthentication()`, so `context.User` was never
     populated when it ran — every request silently fell back to per-IP keying regardless of
     auth. 50 concurrent users behind one IP (or one NAT/corporate network in production) shared
     a single 300/min bucket instead of getting 50 separate ones. Fixed by moving the middleware
     registration after `UseAuthentication()`.
  2. Postgres connection exhaustion under real concurrent load, with no self-recovery: at 500
     VUs, Postgres's default `max_connections=100` was exhausted (the app's pool had no
     explicit cap) and Postgres began rejecting *all* new connections, including the app's own
     health checks — the app stayed degraded minutes after load stopped, needing a manual
     restart. Fixed by capping the app's connection pool (`Maximum Pool Size=50`) and raising
     Postgres's `max_connections` to 200; confirmed each fix independently (the pool cap alone
     restored self-recovery, the `max_connections` raise eliminated the exhaustion entirely — 0
     "too many clients" errors, down from 189).
  3. & 4. Two bugs in the k6 scripts themselves (shared login token across all VUs; a k6
     threshold not accounting for an intentionally-403'd request) — script-only, no app impact.

  Final results: `normal-load.js` (50 users) 100% success, p95 41ms. `peak-load.js` (200 users)
  97% success, p95 645ms. `stress-test.js` (ramp to 500 users) — Postgres survived cleanly with
  zero connection errors; the script's own loose threshold still "fails" on login/health
  saturating one shared per-IP bucket from 500 fake identities behind one test machine's real
  IP, which is a test-methodology ceiling, not an app bug (the authenticated `tickets` check
  passed throughout). `ai-endpoint.js` not yet run — needs `AI:ApiKey` configured.

None of these block a first production deploy, but they should be prioritized before
handling sensitive data at scale or opening the system to a large user base.
