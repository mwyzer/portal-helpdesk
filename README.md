# AI Helpdesk — Digital Secretary & HR Assistant

An internal company application that serves as a **digital secretary** and **HR assistant**, powered by AI.

## Overview

AI Helpdesk centralizes administrative and HR services into a single application, reducing repetitive paperwork, accelerating employee request responses, and improving communication between employees, HR, and management.

### Key Capabilities

- **Digital Secretary** — Manage agendas, record & summarize meetings, generate letters & documents, handle internal requests, send work reminders
- **HR Assistant** — Manage employee data, process leave & permits, assist recruitment, answer policy questions, generate HR documents
- **AI-Powered Chat** — Conversational interface for employees to ask questions, submit requests, and search internal knowledge

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core Web API (.NET) |
| **Frontend** | React 18 + TypeScript + Vite |
| **Database** | PostgreSQL 17 with pgvector |
| **AI / LLM** | OpenAI / Azure OpenAI (pluggable) |
| **Auth** | JWT (access + refresh tokens), ASP.NET Core Identity |
| **CSS** | Tailwind CSS + shadcn/ui |
| **State** | Zustand (client), TanStack Query (server) |
| **Forms** | React Hook Form + Zod |
| **Charts** | Recharts |
| **ORM** | Entity Framework Core |
| **Mapping** | Mapster |
| **Validation** | FluentValidation |
| **Logging** | Serilog |
| **Testing** | xUnit, Moq, FluentAssertions, Bogus, Coverlet |
| **E2E / Screenshots** | Playwright |
| **Containerization** | Docker + Docker Compose |

## Architecture

### Backend — Clean Architecture

```
src/
├── AIHelpdesk.Api/            # ASP.NET Core Web API (controllers, middleware, Program.cs)
├── AIHelpdesk.Application/    # Use cases, service interfaces, DTOs
├── AIHelpdesk.Contracts/      # Request/response DTOs shared across layers
├── AIHelpdesk.Domain/         # Entities, enums, value objects, domain logic
└── AIHelpdesk.Infrastructure/ # EF Core, Identity, JWT, repositories, external services
```

### Frontend

```
frontend/src/
├── components/
│   ├── layout/                # AppShell, Sidebar, Topbar
│   └── ui/                    # shadcn/ui components
├── lib/                       # Axios instance, utilities
├── pages/                     # Page components (one per route)
└── store/                     # Zustand stores (auth, etc.)
```

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)
- [Node.js 18+](https://nodejs.org/) (for local frontend development)

### Quick Start (Docker)

```bash
# Clone the repository
git clone <repository-url>
cd portal-helpdesk

# Start all services (PostgreSQL, API, Frontend)
docker compose up -d --build
```

| Service | URL |
|---------|-----|
| **Frontend** | http://localhost:5173 |
| **Backend API** | http://localhost:5192 |
| **Swagger UI** | http://localhost:5192/swagger |
| **PostgreSQL** | `localhost:5432` (user: `helpdesk`, password: `helpdesk123`, db: `aihelpdesk`) |

### Local Development

#### Backend

```bash
cd src/AIHelpdesk.Api
dotnet restore
dotnet run
```

#### Frontend

```bash
cd frontend
npm install
npm run dev
```

#### Database

```bash
# Start only PostgreSQL
docker compose up -d postgres

# Apply EF Core migrations
cd src/AIHelpdesk.Api
dotnet ef database update
```

### Running Tests

#### Unit Tests (Backend)

```bash
cd tests/AIHelpdesk.Tests
dotnet test
```

#### E2E Tests (Playwright)

```bash
cd frontend

# Install Playwright browsers (first time only)
npx playwright install chromium

# Run all 17 E2E tests (headless)
npm run test:e2e

# Interactive UI mode
npm run test:e2e:ui
```

> See [`documentation/e2e-testing.md`](documentation/e2e-testing.md) for the full E2E guide.
> See [`tests/Summary.md`](tests/Summary.md) for the complete testing strategy across all disciplines.

### Test Coverage

> Full report: [`test-coverage-report.md`](test-coverage-report.md)

| Phase | Backend (xUnit) | E2E Smoke | E2E Interaction | Status |
|-------|-----------------|-----------|-----------------|--------|
| Phase 1 — Foundation MVP | 22 | 13 | — | ✅ All passing |
| Phase 2 — HR Administration | 47 | 4 | 26 | ✅ All passing |
| Phase 3 — Secretary Module | 44 | 6 | — | ✅ All passing (4 partial gaps) |
| Phase 4–7 | — | — | — | 📋 Not yet tested |
| **Total** | **113** | **23** | **26** | — |

**Backend (113 tests):** 11 service classes, 2 domain, 1 contract — xUnit + Moq + FluentAssertions + Bogus  
**Phase 3 Code Coverage (coverlet):**
- Services: MeetingService 93.6%, ActionItemService 97.8%, DocumentService 93.8%
- Domain entities: Meeting, MeetingNote, MeetingParticipant, ActionItem, DocumentTemplate, DocumentRequest, GeneratedDocument — **all 100%**
- Contracts: 21 request/response DTOs — 8 at 100%, remainder partial (no integration tests)
- Controllers: 37 action methods — 0% (integration tests planned but not yet written)
- 3 uncovered service methods: `GetNotesAsync`, `GenerateSummaryAsync`, `GetTeamActionItemsAsync`, `DownloadDocumentAsync`  
**E2E (49 tests):** 23 smoke (screenshot + heading) across all pages + 26 interaction (dialog, form, search, CRUD) for Phase 2  
**Grand total:** 162 tests

## API Endpoints (Phase 1 — Foundation MVP)

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login, returns access + refresh token |
| POST | `/api/auth/refresh-token` | Refresh access token |
| POST | `/api/auth/logout` | Revoke refresh token |
| POST | `/api/auth/forgot-password` | Send password reset link |
| POST | `/api/auth/reset-password` | Reset password with token |
| GET | `/api/auth/profile` | Get current user profile |
| PUT | `/api/auth/profile` | Update own profile |
| PUT | `/api/auth/change-password` | Change password |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List users (paginated, filterable) |
| GET | `/api/users/{id}` | Get user detail |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Soft-delete user |
| POST | `/api/users/{id}/activate` | Activate user |
| POST | `/api/users/{id}/deactivate` | Deactivate user |

### Roles & Permissions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/roles` | List roles |
| POST | `/api/roles` | Create role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role |
| GET | `/api/roles/{id}/permissions` | Get role permissions |
| PUT | `/api/roles/{id}/permissions` | Assign permissions |

### Departments & Positions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/departments` | List departments |
| POST | `/api/departments` | Create department |
| PUT | `/api/departments/{id}` | Update department |
| GET | `/api/positions` | List positions |
| POST | `/api/positions` | Create position |
| PUT | `/api/positions/{id}` | Update position |

## API Endpoints (Phase 2 — HR Administration)

### Employees
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/employees` | List employees (paginated, filterable by search, department, status) |
| GET | `/api/employees/{id}` | Get employee detail |
| POST | `/api/employees` | Create employee |
| PUT | `/api/employees/{id}` | Update employee |
| DELETE | `/api/employees/{id}` | Soft-delete employee |
| POST | `/api/employees/import` | Import employees from Excel |
| GET | `/api/employees/export` | Export employees to Excel |

### Leave Types
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leave-types` | List leave types |
| GET | `/api/leave-types/{id}` | Get leave type detail |
| POST | `/api/leave-types` | Create leave type |
| PUT | `/api/leave-types/{id}` | Update leave type |
| DELETE | `/api/leave-types/{id}` | Soft-delete leave type |

### Leave Requests
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leave-requests` | List leave requests (paginated, filterable) |
| GET | `/api/leave-requests/{id}` | Get leave request detail with approval history |
| POST | `/api/leave-requests` | Create leave request (Draft) |
| PUT | `/api/leave-requests/{id}` | Update draft leave request |
| POST | `/api/leave-requests/{id}/submit` | Submit for approval |
| POST | `/api/leave-requests/{id}/approve` | Approve leave request |
| POST | `/api/leave-requests/{id}/reject` | Reject leave request |
| POST | `/api/leave-requests/{id}/cancel` | Cancel leave request |

### Leave Approvals
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leave-approvals` | List pending approvals for current user |
| POST | `/api/leave-approvals/{id}/approve` | Approve a leave approval step |
| POST | `/api/leave-approvals/{id}/reject` | Reject a leave approval step |

### Notifications
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications` | List notifications for current user |
| GET | `/api/notifications/unread-count` | Get unread count |
| PUT | `/api/notifications/{id}/read` | Mark notification as read |
| PUT | `/api/notifications/read-all` | Mark all as read |
| DELETE | `/api/notifications/{id}` | Delete notification |

## API Endpoints (Phase 3 — Secretary Module)

### Meetings
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/meetings` | List meetings (paginated, filterable by date range, status) |
| GET | `/api/meetings/{id}` | Get meeting detail with participants, notes & action items |
| POST | `/api/meetings` | Create meeting |
| PUT | `/api/meetings/{id}` | Update meeting |
| DELETE | `/api/meetings/{id}` | Soft-delete meeting |
| GET | `/api/meetings/today` | Get today's meetings for current user |
| GET | `/api/meetings/upcoming` | Get upcoming meetings (next 7 days) |
| POST | `/api/meetings/{id}/participants` | Add participant |
| DELETE | `/api/meetings/{id}/participants/{participantId}` | Remove participant |
| POST | `/api/meetings/{id}/notes` | Add meeting note |
| PUT | `/api/meetings/{id}/notes/{noteId}` | Update meeting note |
| DELETE | `/api/meetings/{id}/notes/{noteId}` | Delete meeting note |
| GET | `/api/meetings/{id}/notes` | Get all notes for a meeting |
| POST | `/api/meetings/{id}/generate-summary` | Generate AI-powered meeting summary from notes |

### Action Items
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/action-items` | List my action items (paginated, filterable by status) |
| GET | `/api/action-items/{id}` | Get action item detail |
| POST | `/api/action-items` | Create action item |
| PUT | `/api/action-items/{id}` | Update action item |
| POST | `/api/action-items/{id}/complete` | Mark action item as completed |
| POST | `/api/action-items/{id}/cancel` | Cancel action item |
| GET | `/api/action-items/overdue` | Get overdue action items |
| GET | `/api/action-items/team` | Get team action items (manager) |

### Document Templates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/document-templates` | List templates (filterable by category) |
| GET | `/api/document-templates/{id}` | Get template detail |
| POST | `/api/document-templates` | Create template |
| PUT | `/api/document-templates/{id}` | Update template |
| DELETE | `/api/document-templates/{id}` | Soft-delete template |

### Document Requests
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/document-requests` | List requests (paginated, filterable by status) |
| GET | `/api/document-requests/{id}` | Get request detail with generated documents |
| POST | `/api/document-requests` | Create document request (Draft) |
| PUT | `/api/document-requests/{id}` | Update draft content |
| POST | `/api/document-requests/{id}/generate-draft` | Generate AI draft from template |
| POST | `/api/document-requests/{id}/submit-for-review` | Submit for review |
| POST | `/api/document-requests/{id}/approve` | Approve document |
| POST | `/api/document-requests/{id}/reject` | Reject with reason |
| POST | `/api/document-requests/{id}/generate-final` | Generate final document with letter number |
| GET | `/api/document-requests/{id}/download` | Download generated document |

## User Roles

| Role | Description |
|------|-------------|
| **Super Admin** | Full system access — manage users, roles, departments, and all settings |
| **HRD** | Manage employee data, process leave/permits, create HR documents, upload policies |
| **Secretary / Admin** | Manage agendas, meeting minutes, incoming/outgoing letters, announcements |
| **Manager** | View dashboards, approve leave & documents, view reports |
| **Employee** | Submit leave & permit requests, ask policy questions, view announcements |

## Project Phases

| Phase | Focus | Status |
|-------|-------|--------|
| **Phase 1** | Foundation MVP — Auth, users, roles, departments, base layout | ✅ Done |
| **Phase 2** | HR Administration — Employee data, leave management, notifications | ✅ Done |
| **Phase 3** | Secretary Module — Meetings, agendas, documents, action items | ✅ Done |
| **Phase 4** | AI Helpdesk Chat — AI-powered RAG chat & knowledge base | 📋 Planned |
| **Phase 5** | Ticketing System — Request tracking, SLA, agent workflows | 📋 Planned |
| **Phase 6** | Recruitment — Job postings, candidate pipeline, AI CV parsing | 📋 Planned |
| **Phase 7** | Hardening & Deployment — Security, performance, CI/CD, monitoring | 📋 Planned |

Detailed documentation for each phase is available in the [`documentation/`](documentation/) directory.

---

### Phase 1 — Foundation (MVP)

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Backend scaffolding | Clean Architecture: Api → Application → Domain → Infrastructure |
| 2 | Frontend scaffolding | React + Vite + TypeScript + Tailwind CSS + shadcn/ui |
| 3 | Database schema | Users, Roles, Permissions, Departments, Positions, RefreshTokens |
| 4 | Authentication API | Login, logout, refresh token, forgot/reset password, profile management |
| 5 | User management | CRUD users, assign roles, activate/deactivate, pagination & search |
| 6 | Role & permission management | RBAC with granular CRUD permissions |
| 7 | Department & position management | CRUD departments and positions |
| 8 | Base layout & navigation | Role-based sidebar + topbar navigation |
| 9 | Docker setup | Multi-container: PostgreSQL + Backend + Frontend |
| 10 | API documentation | Swagger/OpenAPI at `/swagger` |

**Database tables:** `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `Permissions`, `Departments`, `Positions`, `RefreshTokens`

---

### Phase 2 — Employee & HR Administration

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Employee management | Full CRUD with import/export Excel, search & filter |
| 2 | Leave types & balance | Configurable leave types, per-year balance tracking |
| 3 | Leave request workflow | Submit → Manager approval → HR verification → Approved/Rejected |
| 4 | Manager approval dashboard | Pending approvals queue with batch actions |
| 5 | In-app notifications | Real-time alerts via SignalR for approvals & updates |
| 6 | Employee dashboard | Leave balance, recent requests, quick actions |
| 7 | HR dashboard | Employee count, pending verifications, department stats |

**New tables:** `Employees`, `LeaveTypes`, `LeaveBalances`, `LeaveRequests`, `LeaveApprovals`, `Notifications`

**Leave status flow:** `Draft → Submitted → Waiting for Manager → Waiting for HR → Approved / Rejected`

---

### Phase 3 — Secretary Module

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Meeting & agenda management | Create, schedule, update meetings with participants |
| 2 | Meeting notes & minutes | Record notes, AI-generated meeting summaries (✅ backend + frontend done) |
| 3 | Action items | Track follow-ups with assignee, priority, and deadline |
| 4 | Document/surat request workflow | Request → AI draft → Review → Approve → Generate PDF/DOCX |
| 5 | Document templates | Manage reusable letter templates with variable fields |
| 6 | Secretary dashboard | Today's agenda, pending reviews, overdue action items |
| 7 | AI summary button | One-click AI meeting summary generation from notes (Sparkles button) |
| 8 | Participant selector | Searchable multi-select employee picker for meeting participants |

**New frontend components:** `AISummaryButton`, `ParticipantSelector` (reusable)

**New tables:** `Meetings`, `MeetingParticipants`, `MeetingNotes`, `ActionItems`, `DocumentTemplates`, `DocumentRequests`, `GeneratedDocuments`

**Document request flow:** `Draft → Submitted → AI Draft Ready → Review → Approved → Generated`
**Action item flow:** `Open → In Progress → Completed`

---

### Phase 4 — AI Helpdesk Chat & Knowledge Base

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | AI Chat interface | Conversational UI with streaming responses |
| 2 | Knowledge base management | Upload PDF/DOCX/TXT, automatic chunking & indexing |
| 3 | RAG pipeline | Document → Chunk → Embed (pgvector) → Retrieve → Generate (LLM) |
| 4 | Source attribution | Show which documents were used for each AI answer |
| 5 | AI feedback system | Thumbs up/down on responses for quality tracking |
| 6 | Human escalation | Transfer chat to human agent when AI cannot answer |
| 7 | Conversation history | Persistent chat sessions per user |
| 8 | AI guardrails | Permission-aware answers, no unauthorized data access |

**New tables:** `KnowledgeDocuments`, `KnowledgeChunks` (with `vector(1536)` embedding), `ChatSessions`, `ChatMessages`, `AIResponses`, `AIUsageLog`

**AI stack:** OpenAI/Azure OpenAI + pgvector for semantic search + RAG pattern

---

### Phase 5 — Ticketing System

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Ticket CRUD | Create, update, view, filter tickets across departments |
| 2 | Ticket assignment | Manual assignment + auto-assignment pool by department |
| 3 | Comments & attachments | Threaded comments with file uploads |
| 4 | Status workflow | Open → Assigned → In Progress → Resolved → Closed |
| 5 | SLA tracking | Per-category SLA targets with breach detection & alerts |
| 6 | AI categorization | Auto-detect category & suggest priority on ticket creation |
| 7 | Agent dashboard | Queue view, SLA breaches, performance metrics |
| 8 | Escalation management | Multi-level escalation (Agent → Supervisor → Super Admin) |

**New tables:** `Tickets`, `TicketCategories`, `TicketComments`, `TicketAttachments`, `TicketHistory`, `TicketSLA`, `Escalations`, `AgentAssignments`

**Status flow:** `Open → Assigned → In Progress → Resolved → Closed / Reopened`

---

### Phase 6 — Recruitment Assistant

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Job vacancy management | Create, publish, close job postings with requirements |
| 2 | Candidate pipeline | Track candidates through hiring stages (Kanban-style) |
| 3 | CV upload & storage | Upload CV files with AI summarization |
| 4 | AI CV summarization | Auto-extract skills, experience, education from CV documents |
| 5 | AI interview questions | Generate role-specific interview questions |
| 6 | Interview scheduling | Schedule interviews, record feedback & ratings |
| 7 | Candidate comparison | Compare CVs against job requirements side-by-side |

**New tables:** `JobVacancies`, `Candidates`, `CandidateStages`, `Interviews`, `InterviewQuestions`, `CandidateDocuments`

**Pipeline stages:** `Applied → Screening → Test → Interview → Offering → Hired / Rejected`

---

### Phase 7 — Hardening & Production Deployment

**Deliverables:**
| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Security hardening | Penetration test, secrets management, CORS/CSP headers, rate limiting |
| 2 | Performance testing | k6 load tests (50–500 concurrent users), N+1 query audit, caching |
| 3 | Production infrastructure | VPS/Docker setup, PostgreSQL tuning, Nginx, SSL certificates |
| 4 | Monitoring & alerting | App metrics (Serilog + Seq/Grafana), uptime monitoring |
| 5 | Backup & DR | Automated DB backup, file storage backup, restore runbook |
| 6 | CI/CD pipeline | GitHub Actions: staging + production environments with approval gates |
| 7 | Documentation | User manual, admin manual, API docs, deployment runbook |
| 8 | UAT & go-live | User acceptance testing, bug fixes, production deployment, sign-off |

## License

*[Add license information here]*


## Demo Account **IMPORTANT MUST READ**

http://localhost:5173/login

**Super Admin:**
-   Email: `admin@aihelpdesk.com`
-   Password: `Admin@123`

**HRD:**
-   Email: `hrd@aihelpdesk.com`
-   Password: `Hrd@12345`

**Secretary:**
-   Email: `secretary@aihelpdesk.com`
-   Password: `Secretary@123`

**Manager:**
-   Email: `manager@aihelpdesk.com`
-   Password: `Manager@123`

**Employee:**
-   Email: `employee@aihelpdesk.com`
-   Password: `Employee@123`
