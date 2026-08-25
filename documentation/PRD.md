# Product Requirements Document (PRD)

## AI Helpdesk — Digital Secretary & HR Assistant

| | |
|---|---|
| **Purpose of this document** | A single status view — what's actually working today, what's partially working, and what's still planned. For functional *behavior* detail see [`FSD.md`](FSD.md); for system/deployment design see [`architecture-design.md`](architecture-design.md); for the full hardening task list see [`todo-phase-7-hardening.md`](todo-phase-7-hardening.md). |
| **Last verified against** | Code + docs as of commit `ce73459` (2026-08-25) |
| **Status legend** | ✅ Working — shipped, verified against code · 🟡 Working, incomplete — usable but with known gaps · 🔴 Blocked — code exists but can't run end-to-end yet · 📋 Planned — not started |

> This document intentionally re-checks claims against the codebase rather than trusting other
> docs' status notes — `FSD.md` (2026-08-11) still lists the audit log as "not implemented" but
> the code (`AuditLog.cs`, `AuditSaveChangesInterceptor.cs`, `AuditLogsController.cs`) says
> otherwise; that correction is reflected below.

---

## 1. Product Summary

Internal application combining a **digital secretary** (agenda, meetings, letters) and an
**HR assistant** (employee data, leave, recruitment), with an **AI chat/RAG layer** and an
internal **helpdesk ticketing system**. Five staff roles (Super Admin, HRD, Secretary, Manager,
Employee) plus an external, separately-scoped Candidate Portal.

---

## 2. Feature Modules — Currently Working

| Module | Status | Notes |
|---|:---:|---|
| Auth & RBAC | ✅ | JWT + rotating refresh tokens, role/permission-based authorization, dual JWT audiences (staff vs candidate portal) |
| Employee Management | ✅ | CRUD, Excel import/export, search/filter |
| Leave & Approval Workflow | ✅ | Full `Draft → Submitted → WaitingForManager → WaitingForHR → Approved` flow, balance validation, ≤3-day auto-manager-approval rule |
| Secretary — Meetings & Action Items | ✅ | Meeting notes, AI-generated summaries, action item tracking with due dates |
| Document Requests & Letter Generation | ✅ | Template-based requests, AI draft → human review/approve → PDF/DOCX with sequential letter numbering |
| Ticketing & SLA | ✅ | Full status flow, SLA breach detection (background service), manual assignment, single-level escalation, Excel export |
| Recruitment | ✅ | Vacancies, candidate pipeline, CV upload, AI CV summary/matching/interview questions (all advisory only) |
| Candidate Portal | ✅ | Separate JWT audience, self-service status/documents/interview booking, race-safe slot booking |
| Notifications | ✅ | In-app + SignalR real-time push (leave, tickets, meetings, candidate stage changes) |
| Audit Log | ✅ | `AuditSaveChangesInterceptor` logs all entity create/update/soft-delete automatically; Super-Admin-only viewer page. *(Corrects a stale note in `FSD.md` §10 — this is implemented, not missing.)* |
| Knowledge Base (upload, chunking, indexing pipeline) | ✅ | Upload → extract → chunk → embed → `Ready`/`Failed` state machine works end-to-end *in code* |
| **AI Chat / RAG answers** | 🔴 | Code path is complete (retrieval, PII redaction, rate limit, usage logging) but `AIOptions:ApiKey` is empty in `appsettings.json` — no live provider configured, so chat/embedding calls fail. This is the single biggest blocker to demoing the product's core value prop. |
| **AI CV Summarization / Matching** | 🔴 | Same blocker as above — same `AIOptions.ApiKey`. |
| **k6 AI-endpoint load test** | 🔴 | Written but never run, same blocker. |

---

## 3. Hardening & Non-Functional — Status

Condensed from [`todo-phase-7-hardening.md`](todo-phase-7-hardening.md) (last detailed status: 2026-08-09, 45/103 tasks).

| Area | Status | Notes |
|---|:---:|---|
| Transport security (HTTPS/HSTS/CSP, CORS lockdown) | ✅ | Fail-fast startup checks in Production |
| Rate limiting | ✅ | 300/min general, 30/min AI-specific, keyed by user |
| Auth hardening (short-lived JWT, refresh rotation, lockout, password policy) | ✅ | |
| Endpoint authorization audit | ✅ | Found & fixed a real IDOR class (Tickets/Leave/Documents/ActionItems) and an HRD self-escalation path |
| Dependency vulnerability scan (`dotnet list --vulnerable`) | ✅ | Found & fixed one real `SixLabors.ImageSharp` vuln |
| Load testing (normal/peak/stress) | ✅ | 3 of 4 scripts run; found & fixed rate-limiter middleware ordering, k6 VU-sharing bug, Postgres connection exhaustion |
| DB connection pool tuning (app `Maximum Pool Size` + Postgres `max_connections`) | ✅ | `Maximum Pool Size=50` set in `appsettings.json`/both compose files, `max_connections=200` on the server. *(Corrects `todo-phase-7-hardening.md`, which still shows this unchecked — it was done as part of the connection-exhaustion fix.)* |
| Backup scripts (DB + files) | ✅ | Written, cron documented; **restore never tested against a real backup** |
| CI pipeline (build/test/docker smoke test) | ✅ | `.github/workflows/ci.yml` |
| ClamAV file scanning | 📋 | Extension/size validation only, no malware scan |
| Trivy/Snyk container scan | 📋 | |
| OWASP ZAP baseline scan | 📋 | Needs a staging environment first |
| Manual pentest (auth bypass, IDOR, XSS, CSRF) | 📋 | |
| Redis caching | 📋 | Deliberately deferred, no code yet |
| N+1 query / index audit | ✅ | Audited 2026-08-25: fixed 4 real N+1s (`UserService.GetUsersAsync` per-user role lookup, `TicketService.AddCommentAsync` + both background services doing one `SaveChangesAsync` per notification, `EmployeeService.ImportFromExcelAsync` doing 3 queries/row) and added 4 missing indexes (`LeaveRequests(Status, EmployeeId)`, `Tickets(AssignedAgentId, Status)`, `Tickets(DepartmentId, Status)`, `ActionItems(AssignedToId, Status, DueDate)`) via migration `AddPerformanceIndexes`. 322 backend tests still passing. |
| Staging environment | 📋 | Does not exist |
| Full-stack deployment test (`docker-compose.prod.yml`) | 📋 | Never run against a real server |
| Monitoring/alerting (Seq, uptime, response-time/error-rate/SSL-expiry alerts) | 📋 | Only `/api/health` exists; nothing polls it |
| CI/CD approval gates, automated rollback | 📋 | |
| UAT & go-live | 📋 | Blocked on staging environment + real stakeholders |

---

## 4. Roadmap — What Will Be Working Next

**Near-term (unblocks the product's core AI value prop):**
1. Configure a real `AI:ApiKey` (OpenAI-compatible) — unlocks AI Chat, KB answers, CV summarization/matching, interview questions, and the last k6 load test in one move.
2. ClamAV + Trivy scans — both can run without a live environment, closes two Security Hardening gaps cheaply.
3. Stand up a staging environment — unblocks ZAP scan, pentest, full deployment test, and everything in UAT & Go-Live.

**Mid-term (Phase 7 completion):**
- Redis caching for reference data
- N+1 / index audit
- Monitoring & alerting stack
- CI/CD approval gates + rollback automation

**Explicitly out of scope for now** (carried from [`project-scope.md`](project-scope.md) §14, confirmed still absent from the codebase):
- Full payroll processing, tax calculation, BPJS integration
- Biometric attendance hardware
- Full performance-appraisal workflows, learning management system
- Automated video interviewing
- Official WhatsApp/Telegram/Teams/Slack chatbot channels
- Voice assistant
- Native mobile app
- Multi-tenant / multi-company support

---

## 5. Sources

| Claim type | Verified against |
|---|---|
| Feature behavior | `FSD.md` §4, cross-checked against service classes |
| Hardening status | `todo-phase-7-hardening.md` (2026-08-09 status note) |
| AI API key gap | `src/AIHelpdesk.Api/appsettings.json` (`AI:ApiKey` empty), commit `1ba9c80` |
| Audit log correction | `src/AIHelpdesk.Domain/Entities/AuditLog.cs`, `AuditSaveChangesInterceptor.cs`, `AuditLogsController.cs` (all present in code) |
| Future/out-of-scope | `project-scope.md` §14 |
