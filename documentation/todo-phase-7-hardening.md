# Phase 7 — Hardening & Production Deployment — TODO Checklist

## Security Hardening

- [ ] Enforce HTTPS (redirect HTTP → HTTPS)
- [ ] Add HSTS header (`max-age=31536000; includeSubDomains`)
- [ ] Restrict CORS to production domain only
- [ ] Add Content Security Policy (CSP) headers
- [ ] Configure rate limiting middleware (100 req/min general, 10 req/min AI)
- [ ] Move all secrets to environment variables / Docker secrets
- [ ] Remove any `.env` or secrets from repository
- [ ] Shorten JWT access token expiry (15 minutes)
- [ ] Implement refresh token rotation
- [ ] Implement refresh token revocation list
- [ ] Enforce password policy (min 8 chars, complexity)
- [ ] Implement account lockout (5 failed attempts)
- [ ] Add ClamAV file scanning for uploads
- [ ] Restrict file upload extensions
- [ ] Audit all EF Core queries for SQL injection safety
- [ ] Verify all data mutations are audit-logged
- [ ] Set up Dependabot for dependency vulnerability scanning
- [ ] Run `dotnet list --vulnerable` and fix findings
- [ ] Run Trivy/Snyk container image scan
- [ ] Run OWASP ZAP baseline scan against staging
- [ ] Manual penetration test: auth bypass
- [ ] Manual penetration test: IDOR
- [ ] Manual penetration test: XSS
- [ ] Manual penetration test: CSRF
- [ ] Verify all endpoints enforce role/permission checks

## Performance Testing

- [ ] Write k6 load test: normal load (50 users, 10 min)
- [ ] Write k6 load test: peak load (200 users, 5 min)
- [ ] Write k6 load test: stress test (ramp to 500 users)
- [ ] Write k6 load test: AI endpoint (10 concurrent chats)
- [ ] Run load tests and analyze results
- [ ] Fix N+1 query issues (EF Core `.Include()` / `.ThenInclude()`)
- [ ] Add missing database indexes (FK + status + date composite)
- [ ] Ensure all list endpoints have pagination (skip/take, max page size)
- [ ] Configure Redis caching for reference data (roles, departments, lookups)
- [ ] Enable response compression (`app.UseResponseCompression()`)
- [ ] Tune PostgreSQL connection pool (`MaxPoolSize`)

## Production Infrastructure

- [ ] Provision VPS (4 cores, 8GB RAM, 100GB SSD)
- [ ] Install Docker Engine (24+)
- [ ] Install Docker Compose
- [ ] Create production `docker-compose.yml` (postgres, redis, backend, frontend, nginx, worker)
- [ ] Create production `nginx.conf` (SPA serving, API proxy, WebSocket)
- [ ] Obtain SSL certificate (Let's Encrypt / Certbot)
- [ ] Configure SSL certificate auto-renewal
- [ ] Configure Nginx security headers
- [ ] Tune PostgreSQL (shared_buffers, work_mem, max_connections)
- [ ] Test full stack deployment with Docker Compose
- [ ] Verify health check endpoint returns OK

## Monitoring Setup

- [ ] Configure Serilog to send logs to Seq
- [ ] Set up Seq dashboard (structured log viewer)
- [ ] Create health check endpoint (`GET /api/health`)
- [ ] Add health checks: database connectivity
- [ ] Add health checks: Redis connectivity
- [ ] Add health checks: AI provider connectivity
- [ ] Add health checks: disk space
- [ ] Set up uptime monitoring (UptimeRobot/BetterStack)
- [ ] Configure alerts for: API response time > 3s (p95)
- [ ] Configure alerts for: error rate > 5%
- [ ] Configure alerts for: AI API latency > 15s
- [ ] Configure alerts for: disk usage > 85%
- [ ] Configure alerts for: SSL expiry < 30 days
- [ ] Set up AI daily token budget alert

## Backup & Disaster Recovery

- [ ] Create daily PostgreSQL backup script (`pg_dump` + gzip)
- [ ] Set up cron job for daily backup (02:00)
- [ ] Set up weekly backup to cloud storage (S3/Backblaze B2)
- [ ] Create file storage backup script (`rclone`)
- [ ] Set up file backup cron job
- [ ] Configure backup retention (daily: 14d, weekly: 3mo, monthly: 12mo)
- [ ] Test database restore procedure
- [ ] Test file storage restore procedure
- [ ] Write restore runbook

## CI/CD Pipeline Hardening

- [ ] Add staging environment to GitHub Actions
- [ ] Add production environment with manual approval gate
- [ ] Configure staging auto-deploy from `develop` branch
- [ ] Configure production deploy from release tag
- [ ] Add post-deploy smoke tests to pipeline
- [ ] Add vulnerability scanning step to pipeline
- [ ] Implement rollback procedure
- [ ] Test rollback on staging
- [ ] Add pre-deployment checklist to pipeline

## Documentation

- [ ] Write user manual (employee-facing features)
- [ ] Write admin manual (configuration, user management, templates)
- [ ] Write deployment runbook (server setup, docker, backup/restore)
- [ ] Review auto-generated Swagger/OpenAPI documentation
- [ ] Create README with project overview and setup instructions
- [ ] Review and update all code comments

## UAT & Go-Live

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
