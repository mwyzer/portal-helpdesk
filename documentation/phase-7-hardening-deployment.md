# Phase 7 — Hardening & Production Deployment

**Tech Stack:** Docker · GitHub Actions · VPS/Linux · PostgreSQL · Nginx · Monitoring Stack

**Prerequisite:** All Phases 1–6 must be complete and tested.

---

## 1. Overview

Phase 7 prepares the application for production: security hardening, performance/load testing, production infrastructure setup, monitoring & alerting, documentation, and UAT sign-off.

**Goal:** Deliver a production-ready system with monitoring, backup, security audit, and operational runbooks.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Security hardening | Penetration test, secrets management, CORS, CSP headers, rate limiting audit |
| 2 | Performance testing | k6 load tests, pagination audit, N+1 query detection, caching |
| 3 | Production infrastructure | VPS/Docker setup, PostgreSQL tuning, Nginx config, SSL certs |
| 4 | Monitoring & alerting | Application metrics, logging (Serilog + Seq/Grafana), uptime monitoring |
| 5 | Backup & disaster recovery | Automated DB backup, file storage backup, restore runbook |
| 6 | CI/CD pipeline hardening | Staging + production environments, approval gates, rollback |
| 7 | Documentation | User manual, admin manual, API documentation, deployment runbook |
| 8 | UAT & go-live | User acceptance testing, bug fixes, production deployment, sign-off |

---

## 3. Security Hardening

### Checklist

| # | Item | Details |
|---|------|---------|
| 1 | HTTPS enforcement | Redirect HTTP → HTTPS; HSTS header |
| 2 | CORS policy | Restrict to production domain only |
| 3 | Content Security Policy | CSP headers to prevent XSS |
| 4 | Rate limiting | Apply to all endpoints: 100 req/min per user, 10 req/min for AI |
| 5 | Secrets management | Azure Key Vault or Docker secrets; no .env in repo |
| 6 | JWT hardening | Short access token (15 min), refresh token rotation, revocation list |
| 7 | Password policy | Min 8 chars, complexity, account lockout after 5 attempts |
| 8 | MFA | TOTP-based MFA for admin accounts (Phase 2 extension) |
| 9 | File upload security | Scan with ClamAV, restrict extensions, max 10MB |
| 10 | SQL injection | Confirmed via EF Core parameterization audit |
| 11 | Audit logging | All data mutations logged with user, IP, timestamp, old/new values |
| 12 | Dependency scanning | Dependabot/GitHub Dependabot + `dotnet list --vulnerable` |
| 13 | Container scanning | Trivy or Snyk for Docker image vulnerabilities |
| 14 | API key rotation | AI provider keys rotated every 90 days |

### Security Testing
- Run OWASP ZAP baseline scan against staging
- Manual penetration test: auth bypass, IDOR, XSS, CSRF, SSRF
- Verify all endpoints enforce role/permission checks

---

## 4. Performance Testing

### k6 Load Test Scenarios

| Scenario | Target | Duration | Threshold |
|----------|--------|----------|-----------|
| Normal load | 50 concurrent users | 10 min | p95 < 2s, error rate < 1% |
| Peak load | 200 concurrent users | 5 min | p95 < 3s, error rate < 2% |
| Stress test | Ramp up to 500 users | 10 min | Identify breaking point |
| AI endpoint | 10 concurrent AI chats | 5 min | p95 < 10s (streaming OK) |

### Performance Optimization

| Item | Action |
|------|--------|
| N+1 queries | Audit with EF Core logging; add `.Include()` / `.ThenInclude()` |
| Missing indexes | Review slow queries; add composite indexes on FK + status + date |
| Pagination | Ensure ALL list endpoints have skip/take with max page size |
| Caching | Redis cache for: roles, permissions, departments, lookup data |
| Response compression | Enable `app.UseResponseCompression()` for API + Nginx gzip |
| Image optimization | Serve uploaded images through thumbnail generation |
| DB connection pooling | Tune `MaxPoolSize` in connection string |

---

## 5. Production Infrastructure

### Docker Compose (Production)

```yaml
services:
  postgres:
    image: postgres:16-alpine
    volumes:
      - pgdata:/var/lib/postgresql/data
      - pgbackup:/backup
    environment:
      POSTGRES_DB: aihelpdesk
    restart: always

  redis:
    image: redis:7-alpine
    restart: always

  backend:
    image: aihelpdesk-backend:latest
    depends_on: [postgres, redis]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default
      - AI__Endpoint
      - AI__ApiKey
    restart: always

  frontend:
    image: aihelpdesk-frontend:latest
    restart: always

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on: [backend, frontend]
    restart: always

  worker:
    image: aihelpdesk-backend:latest
    command: ["dotnet", "AiHelpdesk.Worker.dll"]
    depends_on: [postgres, redis]
    restart: always

  # Optional: monitoring
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:5341"
    volumes:
      - seqdata:/data
    restart: always
```

### Nginx Configuration Highlights
```nginx
# SPA serving with API proxy
location /api/ {
    proxy_pass http://backend:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade"; # WebSocket/SignalR
}

location / {
    root /usr/share/nginx/html;
    try_files $uri $uri/ /index.html;
}

# Security headers
add_header X-Frame-Options "DENY";
add_header X-Content-Type-Options "nosniff";
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
add_header Content-Security-Policy "default-src 'self'; ...";
```

### VPS Requirements

| Resource | Minimum | Recommended |
|----------|:-------:|:-----------:|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Storage | 50 GB SSD | 100 GB SSD |
| OS | Ubuntu 22.04 LTS | Ubuntu 24.04 LTS |
| Docker | 24+ | 24+ |

---

## 6. Monitoring & Alerting

### Application Metrics (via Prometheus + Grafana or App Insights)

| Metric | Source | Alert Threshold |
|--------|--------|-----------------|
| CPU usage | Docker stats | > 80% for 5 min |
| Memory usage | Docker stats | > 80% for 5 min |
| API response time | ASP.NET middleware | p95 > 3s |
| Error rate | Serilog / App Insights | > 5% in 5 min |
| AI API latency | Custom metric | > 15s (streaming) |
| AI token usage | Custom metric | > daily budget |
| Database connections | pg_stat_activity | > 80% of max |
| Disk usage | Node exporter | > 85% |
| SSL expiry | Blackbox exporter | < 30 days |

### Logging
- **Serilog** → Seq (structured logs)
- Log levels: Production = Warning + Error; Debug on-demand
- Never log: passwords, tokens, PII (use `Destructure` policies)
- Centralized log viewer: Seq dashboards per module

### Uptime Monitoring
- Health check endpoint: `GET /api/health`
  - Returns: database OK, Redis OK, AI provider OK, disk space OK
- External uptime monitor: UptimeRobot / BetterStack (5 min interval)

---

## 7. Backup & Disaster Recovery

### Database Backup
```bash
# Daily full backup at 02:00
0 2 * * * pg_dump -U postgres aihelpdesk | gzip > /backup/daily/aihelpdesk_$(date +\%Y\%m\%d).sql.gz

# Weekly to S3/cloud
0 3 * * 0 aws s3 cp /backup/daily/aihelpdesk_$(date +\%Y\%m\%d).sql.gz s3://aihelpdesk-backups/
```

### File Backup
- Uploaded files (CVs, attachments, generated documents) backed up daily
- Use `rclone` to sync to cloud storage (S3/Backblaze B2)

### Retention Policy
| Type | Retention |
|------|-----------|
| Daily backup | 14 days |
| Weekly backup | 3 months |
| Monthly backup | 12 months |

### Restore Runbook
1. Stop application: `docker compose down`
2. Restore DB: `gunzip -c backup.sql.gz | docker exec -i postgres psql -U postgres aihelpdesk`
3. Restore files: `rclone sync backup:/files ./app/files`
4. Start application: `docker compose up -d`
5. Verify health: `curl https://app.example.com/api/health`

---

## 8. CI/CD Pipeline Hardening

### GitHub Actions: Staging + Production

```yaml
# Based on Phase 1 CI/CD, extended with:
deploy-staging:
  trigger: push to develop branch
  steps: build → test → docker → deploy to staging VPS
  post-deploy: run migrations, smoke tests

deploy-production:
  trigger: push to main branch (release tag)
  steps: build → test → docker → push to registry
  approval: required from 1 admin
  deploy: rolling update to production VPS
  post-deploy: health check, rollback on failure

rollback:
  steps: docker compose stop → docker compose -f docker-compose.rollback.yml up
```

### GitHub Environments
- `staging`: auto-deploy from develop
- `production`: manual approval + deploy from tag

### Pre-deployment Checklist
- [ ] All tests pass
- [ ] No critical/high vulnerabilities in `dotnet list --vulnerable`
- [ ] DB migration tested on staging
- [ ] Health check passes
- [ ] SSL certificate valid

---

## 9. Documentation

| Document | Audience | Contents |
|----------|----------|----------|
| User Manual | All employees | How to use chat, tickets, leave, documents, meetings |
| Admin Manual | HR Admins, Secretaries | User management, configuration, templates, reports |
| Technical Documentation | Developers | Architecture, API endpoints, DB schema, deployment |
| Deployment Runbook | DevOps | Server setup, Docker commands, backup/restore, monitoring setup |

### Documentation Tools
- User/Admin Manual: Markdown → GitBook or Docusaurus
- API Documentation: Swagger/OpenAPI (auto-generated)
- Deployment Runbook: `runbook.md` in repository root

---

## 10. UAT & Go-Live

### UAT Phases

| Phase | Participants | Duration | Activities |
|-------|-------------|:--------:|------------|
| Alpha | Internal team | 1 week | Core workflows, bug fixes |
| Beta | Pilot users (5-10) | 2 weeks | All features, feedback collection |
| UAT | Key stakeholders | 1 week | Sign-off per module |

### UAT Test Scenarios

| Module | Scenarios |
|--------|-----------|
| Auth | Login, logout, password reset, MFA, role switching |
| Employee | Create, edit, search, import, export |
| Leave | Submit, approve, reject, balance check, cancel |
| Chat | Ask question, upload document, stream response, feedback, escalate |
| Meeting | Create, invite, notes, AI summary, action items |
| Document | Request, AI draft, review, approve, download |
| Ticket | Create, assign, comment, resolve, close, SLA |
| Candidate | Create, upload CV, advance stage, interview, hire/reject |

### Go-Live Criteria
- [ ] All critical/blocker bugs fixed
- [ ] Performance tests pass thresholds
- [ ] Security scan passes (no high/critical)
- [ ] UAT sign-off obtained
- [ ] Backup system verified
- [ ] Monitoring dashboards operational
- [ ] Rollback procedure tested
- [ ] Deployment runbook reviewed

### Post-Go-Live
- First week: daily health check + bug triage
- First month: weekly monitoring review
- Monthly: dependency updates + security scan

---

## 11. Implementation Steps

### Step 1: Security Hardening
- Run security audit checklist
- Implement CSP headers, HSTS, CORS restrictions
- Add rate limiting middleware
- Set up secrets management (Docker secrets / environment)
- Configure JWT revocation list
- Run OWASP ZAP scan, fix findings

### Step 2: Performance Optimization
- Write k6 load tests for critical endpoints
- Run and analyze results
- Fix N+1 queries, add missing indexes
- Implement Redis caching for reference data
- Enable response compression

### Step 3: Production Infrastructure
- Provision VPS (or cloud instance)
- Install Docker + Docker Compose
- Set up Nginx with SSL certs (Let's Encrypt / Certbot)
- Configure PostgreSQL (tuning, connection pooling)
- Deploy Docker Compose stack

### Step 4: Monitoring Setup
- Configure Serilog → Seq
- Set up Prometheus + Grafana (or use Seq dashboards)
- Configure health check endpoint
- Set up uptime monitoring
- Configure alert notifications (email/Telegram/Slack)

### Step 5: Backup System
- Create automated backup scripts
- Set up cron jobs for DB backup
- Configure cloud storage sync
- Test restore procedure

### Step 6: CI/CD Extension
- Add staging environment to GitHub Actions
- Add production environment with approval gate
- Implement rollback procedure
- Add post-deploy smoke tests
- Add vulnerability scanning to pipeline

### Step 7: Documentation
- Write user manual
- Write admin manual
- Write deployment runbook
- Review API documentation

### Step 8: UAT & Go-Live
- Set up staging environment for UAT
- Conduct alpha testing
- Conduct beta testing with pilot users
- Fix reported issues
- Obtain UAT sign-off
- Deploy to production
- Monitor first week

---

## 12. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | All API endpoints enforce authorization |
| 2 | Rate limiting is active and tested |
| 3 | No high/critical vulnerabilities found |
| 4 | p95 response time < 2s under normal load |
| 5 | System handles 100+ concurrent users without errors |
| 6 | SSL/TLS configured with valid certificate |
| 7 | All environments (dev/staging/prod) are deployable via CI/CD |
| 8 | Database backups run automatically and are restorable |
| 9 | Monitoring dashboards show key metrics |
| 10 | Alerts are configured for critical conditions |
| 11 | User manual covers all employee-facing features |
| 12 | Admin manual covers configuration and management |
| 13 | UAT signed off by stakeholders |
| 14 | Rollback procedure tested and documented |

---

## 13. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| Security hardening | 5 days |
| Performance testing & optimization | 4 days |
| Production infrastructure setup | 3 days |
| Monitoring & alerting | 3 days |
| Backup & DR | 2 days |
| CI/CD hardening | 2 days |
| Documentation | 4 days |
| UAT & go-live | 5 days |
| **Total** | **~28 days** |

---

## 14. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| Security vulnerability missed | Multiple scanning tools + manual review |
| Performance bottleneck in production | Load test before go-live; monitor after |
| Deployment downtime | Rolling update; blue-green if possible |
| Data loss | Automated backups + tested restore procedure |
| User adoption resistance | Training sessions + user manual + pilot program |
| AI cost overrun | Daily budget alert; monitor token usage |
| SSL certificate expiry | Automated renewal (Certbot) + monitoring alert |
