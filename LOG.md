# 📋 Development Log — AI Helpdesk

> **Project:** AI Helpdesk — Digital Secretary & HR Assistant
>
> **Repository:** `github.com/mwyzer/portal-helpdesk`
>
> **Tech Stack:** ASP.NET Core 9.0 · React 18 · TypeScript · PostgreSQL 17 · Docker

---

## Phase 1 — Foundation MVP

**Status:** ✅ Complete

**Goal:** Working application with login, role-based access, user management, and navigable shell layout.

### What Was Built

| Area | Deliverables |
|------|-------------|
| **Backend** | Clean Architecture solution (Api, Application, Domain, Infrastructure, Contracts) |
| **Frontend** | React + Vite + TypeScript + Tailwind CSS + shadcn/ui |
| **Database** | 26 tables — Users, Roles, Permissions, Departments, Positions, RefreshTokens, ActionItems, Meetings, ChatSessions, KnowledgeDocuments, etc. |
| **Auth** | JWT access + refresh tokens, ASP.NET Core Identity, forgot/reset password |
| **API** | Full CRUD for users, roles, departments, positions; Swagger UI at `/swagger` |
| **Docker** | `docker-compose.yml` with PostgreSQL, Backend, Frontend services |

### Issues & Resolutions

| Issue | Cause | Resolution |
|-------|-------|------------|
| 🐛 Login failed — backend crash on startup | `BaseEntity` had `DateTime.UtcNow` defaults which broke EF Core `HasData` for seed permissions — non-deterministic model each build | Added explicit static `CreatedAt` values to all 15 Permission seeds; suppressed `PendingModelChangesWarning` in DbContext registration |
| 🐛 Swagger 404 at `/swagger/index.html` | .NET 9 `MapOpenApi()` only serves the OpenAPI JSON spec, not Swagger UI | Added `Swashbuckle.AspNetCore` package, `AddSwaggerGen()`, `UseSwagger()`, and `UseSwaggerUI()` |
| 🐛 Build artifacts pushed to GitHub | `.gitignore` committed after files were already tracked | Ran `git rm -r --cached` on all `bin/`, `obj/`, `node_modules/` paths and committed cleanup |

### Database Seed Data

- **5 Roles:** Super Admin, HRD, Secretary, Manager, Employee
- **5 Users:** admin@aihelpdesk.com (Super Admin), hrd@aihelpdesk.com (HRD), secretary@aihelpdesk.com (Secretary), manager@aihelpdesk.com (Manager), employee@aihelpdesk.com (Employee)
- **15 Permissions:** users.read, users.create, users.update, users.delete, roles.read, roles.create, roles.update, roles.delete, departments.read, departments.create, departments.update, departments.delete, documents.read, documents.create, documents.approve
- **Default Password:** `Admin@123` (all seed users)

### Key Commands

```bash
# Start all services
docker compose up -d --build

# View logs
docker compose logs -f backend

# Access database
docker compose exec postgres psql -U helpdesk -d aihelpdesk
```

### Service URLs

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:5192 |
| Swagger UI | http://localhost:5192/swagger |
| PostgreSQL | `localhost:5432` (helpdesk / helpdesk123 / aihelpdesk) |

---

## Phase 2 — HR Administration

**Status:** ✅ Complete

**Goal:** Employee data management, leave & absence workflows, in-app notifications.

### What Was Built

| Area | Deliverables |
|------|-------------|
| **Domain** | 6 new entities: `Employee`, `LeaveType`, `LeaveBalance`, `LeaveRequest`, `LeaveApproval`, `Notification`. 4 new enums: `EmploymentStatus`, `LeaveRequestStatus`, `ApprovalStatus`, `NotificationType`. |
| **Contracts** | DTOs for Employees, LeaveTypes, LeaveBalances, LeaveRequests (with `RejectRequest` wrapper, approval DTOs), Notifications |
| **Application** | 5 new interfaces: `IEmployeeService`, `ILeaveTypeService`, `ILeaveBalanceService`, `ILeaveRequestService`, `INotificationService` |
| **Infrastructure** | 5 new services implementing full CRUD + leave workflow (submit → manager → HR), balance tracking, paginated search/filter, notification creation. `DbSeeder` extended with 8 leave types. |
| **API Controllers** | `EmployeesController` (7 endpoints), `LeaveTypesController` (5), `LeaveBalancesController` (3), `LeaveRequestsController` (9), `NotificationsController` (4). Auth relaxed on department/position GET endpoints. |
| **Database** | EF migration `Phase2_HRModule` applied — 6 new tables with proper FKs, unique constraints, soft delete filters |
| **Seed Data** | 8 leave types seeded: Annual (12d), Sick (14d), Special (5d), Maternity (90d), Paternity (5d), Lateness, Early Leave, WFH |
| **SignalR** | `NotificationHub` registered at `/hubs/notifications` with per-user groups for real-time push |
| **Frontend** | 4 new pages: `EmployeesPage` (CRUD table + dialogs), `LeaveTypesPage` (manage leave config), `LeaveRequestsPage` (apply + track + stats cards), `LeaveApprovalsPage` (approve/reject queue). Sidebar updated with 4 new HR nav items. Routes in `App.tsx`. |

### Verified

- ✅ Backend: `dotnet build` — 0 errors across all 5 projects
- ✅ Frontend: `npx tsc --noEmit` — 0 TypeScript errors
- ✅ Docker: both images build and all 3 containers running
- ✅ Migration auto-applied on container start
- ✅ API tested: `GET /api/leave-types` returns 8 seeded leave types
- ✅ Auth working: JWT login with `admin@aihelpdesk.com` / `Admin@123`
- ✅ Frontend sidebar shows Employees, Leave Types, Leave Requests, Approvals

### Issues Resolved

| Issue | Resolution |
|-------|------------|
| `LeaveRequestsController.GetEmployeeId()` looked for missing JWT "EmployeeId" claim | Changed to use `IEmployeeService.GetMyProfileAsync(GetUserId())` to look up Employee by authenticated UserId |
| `POST /api/leave-requests/{id}/reject` used `[FromBody] string reason` — unreliable JSON binding | Added `RejectRequest` wrapper record DTO for reliable binding |
| `DepartmentsController` and `PositionsController` required Super Admin for GET | Relaxed to `[Authorize]` for read endpoints, kept Super Admin for POST/PUT |

### What Remains (Optional Enhancements)

| # | Task | Status |
|---|------|--------|
| 1 | Excel import/export (ClosedXML in `EmployeeService`) | 📋 Stub exists |
| 2 | Email/SMTP notifications for leave events | 📋 Pending |
| 3 | Employee dashboard (leave balance cards, recent requests) | 📋 Pending |
| 4 | HR dashboard (leave stats, charts, pending verifications) | 📋 Pending |
| 5 | Unit tests for HR module services | 📋 Pending |
| 6 | Notification bell + center frontend (SignalR client) | 📋 Hub ready |

---

## Phase 3 — Secretary Module

**Status:** 📋 Planned

**Goal:** Agenda & meeting management, document/surat request workflows, action item tracking.

### Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Meeting & agenda management | Create, schedule, update meetings with participants |
| 2 | Meeting notes & minutes | Record notes, upload transcripts, AI-generated summaries |
| 3 | Action items | Track follow-ups with PIC and deadline |
| 4 | Document/surat request workflow | Request, AI draft, review, approve, generate PDF/DOCX |
| 5 | Document templates | Manage reusable letter templates |
| 6 | Secretary dashboard | Today's agenda, pending reviews, upcoming meetings, overdue items |
| 7 | Manager dashboard extension | Approval queue, team action items, meeting schedule |

### Prerequisites

- Phase 1 (Foundation) ✅ Complete
- Phase 2 (HR Administration) ✅ Complete

---

## Phase 4 — AI Helpdesk Chat & Knowledge Base

**Status:** 📋 Planned

**Goal:** Conversational AI chat powered by RAG (Retrieval-Augmented Generation) with pgvector, answering employee questions using internal knowledge base.

### Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | AI Chat interface | Conversational UI with streaming responses |
| 2 | Knowledge base management | Upload PDF/DOCX/TXT, indexing, chunking |
| 3 | RAG pipeline | Document → chunk → embed → vector store → retrieve → generate |
| 4 | Source attribution | Show which documents were used for each answer |
| 5 | AI feedback | Thumbs up/down on responses for quality tracking |
| 6 | Human escalation | Transfer chat to human agent when AI cannot answer |
| 7 | Conversation history | Persistent chat sessions per user |
| 8 | AI guardrails | Permission-aware answers, no unauthorized data access |

### Prerequisites

- Phase 1 (Foundation) ✅ Complete
- OpenAI / Azure OpenAI API key required
- pgvector extension already included in PostgreSQL Docker image

---

## Phase 5 — Ticketing Module

**Status:** 📋 Planned

**Goal:** Internal helpdesk ticketing with cross-department submission, assignment, SLA tracking, and AI-powered categorization.

### Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Ticket CRUD | Create, update, view, filter tickets |
| 2 | Ticket assignment | Manual assignment + auto-assignment pool |
| 3 | Comments & attachments | Threaded comments, file uploads |
| 4 | Status workflow | Open → Assigned → In Progress → Resolved → Closed |
| 5 | SLA tracking | Per-category SLA targets with breach detection |
| 6 | AI categorization | Auto-detect category & suggest priority |
| 7 | Agent dashboard | Queue, SLA breaches, performance metrics |
| 8 | Escalation management | Escalate to supervisor or another department |

### Prerequisites

- Phase 1 (Foundation) ✅ Complete
- Phase 4 (AI Chat) — recommended for AI categorization feature

---

## Phase 6 — Recruitment Assistant

**Status:** 📋 Planned

**Goal:** Full recruitment lifecycle — job vacancy posting, candidate pipeline, CV AI summarization, interview scheduling.

### Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Job vacancy management | Create, publish, close job postings |
| 2 | Candidate pipeline | Track candidates through hiring stages |
| 3 | CV upload & storage | Upload CV files, link to candidates |
| 4 | AI CV summarization | Extract skills, experience, education from CV |
| 5 | AI interview questions | Generate role-specific interview questions |
| 6 | Interview scheduling | Schedule interviews, record notes/feedback |
| 7 | Candidate comparison | Compare CVs against job requirements |
| 8 | Notification for candidates | Interview invitation, status updates |

### Prerequisites

- Phase 1 (Foundation) ✅ Complete
- Phase 2 (HR Administration) ✅ Complete
- Phase 4 (AI Chat) — required for AI-powered CV summarization

---

## Phase 7 — Hardening & Production Deployment

**Status:** 📋 Planned

**Goal:** Production-ready system with security hardening, monitoring, CI/CD, backup, and full documentation.

### Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Security hardening | Penetration test, secrets management, CORS, CSP headers, rate limiting |
| 2 | Performance testing | k6 load tests, pagination audit, N+1 query detection, caching |
| 3 | Production infrastructure | VPS/Docker setup, PostgreSQL tuning, Nginx config, SSL certs |
| 4 | Monitoring & alerting | App metrics, logging (Serilog + Seq/Grafana), uptime monitoring |
| 5 | Backup & disaster recovery | Automated DB backup, file storage backup, restore runbook |
| 6 | CI/CD pipeline hardening | Staging + production environments, approval gates, rollback |
| 7 | Documentation | User manual, admin manual, API docs, deployment runbook |
| 8 | UAT & go-live | User acceptance testing, bug fixes, production deployment, sign-off |

### Prerequisites

- All Phases 1–6 must be complete and tested

---

## Appendix

### Git History Summary

```
1495acf  (HEAD -> main, origin/main) Remove build artifacts from tracking
ab4dec2  Initial commit — AI Helpdesk project scaffold
```

### Branch Strategy

| Branch | Purpose |
|--------|---------|
| `main` | Production-ready code |
| `develop` | Integration branch for feature work |
| `feature/*` | Individual feature branches |

### Environment Variables (Docker Compose)

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | Development |
| `ConnectionStrings__DefaultConnection` | Host=postgres;Database=aihelpdesk;Username=helpdesk;Password=helpdesk123 |
| `Jwt__Key` | ThisIsASuperSecretKeyForDevOnly1234567890! |
| `Jwt__Issuer` | AIHelpdesk |
| `Jwt__Audience` | AIHelpdesk |
| `Cors__Origins__0` | http://localhost:5173 |
