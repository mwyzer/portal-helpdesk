# AIHelpdesk — Test Coverage Report

**Generated:** 2026-07-14, refreshed 2026-08-04 (three passes — the second adds Phases 5–6 and several bugfix-driven test additions to Phases 1/3/4; the third adds Phase 7's rate-limiting middleware tests)  
**Total Tests:** 328  
**Overall Status:** ✅ All Passing (counts below verified via `dotnet test --filter` per phase, cross-checked against `[Fact]`/`[Theory]` attribute counts; a filter substring collision between `AIServiceTests` and `RecruitmentAIServiceTests` was caught and corrected during the second pass)

---

## Summary by Phase

| Phase | Backend Unit Tests | Frontend E2E Smoke | Frontend E2E Interaction | Total |
|-------|-------------------|--------------------|--------------------------|-------|
| Phase 1 — Foundation MVP | 31 | 13 | 0 | **44** |
| Phase 2 — HR Administration | 46 | 4 | 26 | **76** |
| Phase 3 — Secretary Module | 54 | 6 | 0 | **60** |
| Phase 4 — AI Helpdesk Chat | 56 | 0 | 0 | **56** |
| Phase 5 — Ticketing | 49 | 0 | 0 | **49** |
| Phase 6 — Recruitment | 37 | 0 | 0 | **37** |
| Phase 7 — Hardening & Deployment | 6 | 0 | 0 | **6** |
| **TOTAL** | **279** | **23** | **26** | **328** |

---

## Phase 1 — Foundation MVP

### Backend Unit Tests (23 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `UserServiceTests` | 5 | `tests/AIHelpdesk.Tests/Services/UserServiceTests.cs` |
| `RoleServiceTests` | 4 | `tests/AIHelpdesk.Tests/Services/RoleServiceTests.cs` |
| `DepartmentServiceTests` | 5 | `tests/AIHelpdesk.Tests/Services/DepartmentServiceTests.cs` |
| `DepartmentTests` (Domain) | 2 | `tests/AIHelpdesk.Tests/Domain/DepartmentTests.cs` |
| `RefreshTokenTests` (Domain) | 3 | `tests/AIHelpdesk.Tests/Domain/RefreshTokenTests.cs` |
| `AuthContractsTests` | 3 | `tests/AIHelpdesk.Tests/Contracts/AuthContractsTests.cs` |
| `UnitTest1` | 1 | `tests/AIHelpdesk.Tests/UnitTest1.cs` |

**Covered:** User CRUD, Roles, Departments, Positions, Refresh Tokens, Auth Contracts

### Frontend E2E Smoke Tests (13 tests)

| # | Page | Screenshot |
|---|------|------------|
| 01 | Dashboard | `phase1-01-dashboard.png` |
| 02 | Users | `phase1-02-users.png` |
| 03 | Roles | `phase1-03-roles.png` |
| 04 | Departments | `phase1-04-departments.png` |
| 05 | Meetings | `phase1-05-meetings.png` |
| 06 | Action Items | `phase1-06-action-items.png` |
| 07 | Document Requests | `phase1-07-document-requests.png` |
| 08 | Document Templates | `phase1-08-document-templates.png` |
| 09 | AI Chat | `phase1-09-ai-chat.png` |
| 10 | Knowledge Base | `phase1-10-knowledge-base.png` |
| 11 | Login Page | `phase1-11-login.png` |
| 12 | Forgot Password | `phase1-12-forgot-password.png` |
| 13 | Reset Password | `phase1-13-reset-password.png` |

**Type:** Navigate → Screenshot → Assert heading

---

## Phase 2 — HR Administration

### Backend Unit Tests (46 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `EmployeeServiceTests` | 13 | `tests/AIHelpdesk.Tests/Services/EmployeeServiceTests.cs` |
| `LeaveRequestServiceTests` | 14 | `tests/AIHelpdesk.Tests/Services/LeaveRequestServiceTests.cs` |
| `NotificationServiceTests` | 7 | `tests/AIHelpdesk.Tests/Services/NotificationServiceTests.cs` |
| `LeaveTypeServiceTests` | 6 | `tests/AIHelpdesk.Tests/Services/LeaveTypeServiceTests.cs` |
| `LeaveBalanceServiceTests` | 6 | `tests/AIHelpdesk.Tests/Services/LeaveBalanceServiceTests.cs` |

**Covered:**
- Employee CRUD, search/filter, pagination, Excel import/export
- Leave type CRUD, soft delete
- Leave request: draft → submit → manager approval (short/long) → HRD approval → reject → cancel
- Leave balance: query, adjust, initialize yearly
- Notification: create, read, mark all read, unread count, filter

### Frontend E2E Smoke (4 tests)

| # | Page | Screenshot |
|---|------|------------|
| 14 | Employees | `phase2-01-employees.png` |
| 15 | Leave Types | `phase2-02-leave-types.png` |
| 16 | Leave Requests | `phase2-03-leave-requests.png` |
| 17 | Leave Approvals | `phase2-04-approvals.png` |

### Frontend E2E Interaction Tests (26 tests)

| Spec File | Tests | What It Covers |
|-----------|-------|----------------|
| `employee.spec.ts` | 7 | Dialog open/close, search, import, export download, form fields, validation |
| `leave-type.spec.ts` | 6 | Dialog open/close, form fields, edit existing row, refresh |
| `leave-request.spec.ts` | 7 | Balance cards, apply dialog, date pickers, leave type select, view detail, refresh |
| `leave-approvals.spec.ts` | 6 | Approve/Reject buttons, approval timeline dialog, table display, refresh |

**Shared helpers:** `frontend/tests/e2e/phase-2/helpers.ts`

---

## Phase 3 — Secretary Module

### Backend Unit Tests (54 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `MeetingServiceTests` | 15 | `tests/AIHelpdesk.Tests/Services/MeetingServiceTests.cs` |
| `ActionItemServiceTests` | 11 | `tests/AIHelpdesk.Tests/Services/ActionItemServiceTests.cs` |
| `DocumentServiceTests` | 22 | `tests/AIHelpdesk.Tests/Services/DocumentServiceTests.cs` |
| `LetterDocumentGeneratorTests` | 6 | `tests/AIHelpdesk.Tests/Services/LetterDocumentGeneratorTests.cs` |

**Covered:**
- Meeting CRUD, pagination, date-range & status filtering, soft delete
- Meeting participants: add/remove, role & attendance tracking
- Meeting notes: add, update, delete
- Action items: create, update, complete (with assignee guard), cancel, overdue detection
- Document templates: CRUD, category filtering, activate/deactivate
- Document requests: full workflow (Draft → AI Draft Ready → Review → Approve → Generate Final → Download)
- Document workflow state guards (invalid transitions throw)
- Letter number auto-generation (format: `{counter}/{code}/MGR/{year}`)
- Real PDF/DOCX generation (magic-byte verification: `%PDF`, zip `PK`), format-selection on download (added 2026-08-04, alongside the fix for `GenerateFinalAsync` producing files that were never actually written to disk)

### Frontend E2E Smoke Tests (6 tests)

| # | Page | Screenshot |
|---|------|------------|
| 18 | Meetings List | `phase3-01-meetings.png` |
| 19 | Meeting Detail | `phase3-02-meeting-detail.png` |
| 20 | Action Items | `phase3-03-action-items.png` |
| 21 | Document Requests | `phase3-04-document-requests.png` |
| 22 | Document Templates | `phase3-05-document-templates.png` |
| 23 | Dashboard (Secretary) | `phase3-06-dashboard.png` |

**Type:** Navigate → Screenshot → Assert heading

**New frontend pages:** `MeetingDetailPage.tsx` (4 tabs: Info, Participants, Notes, Action Items)

---

## Phase 4 — AI Helpdesk Chat

### Backend Unit Tests (56 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `AIServiceTests` | 10 | `tests/AIHelpdesk.Tests/Services/AIServiceTests.cs` |
| `ChatServiceTests` | 19 | `tests/AIHelpdesk.Tests/Services/ChatServiceTests.cs` |
| `KnowledgeBaseServiceTests` | 19 | `tests/AIHelpdesk.Tests/Services/KnowledgeBaseServiceTests.cs` |
| `PiiRedactorTests` | 8 | `tests/AIHelpdesk.Tests/Services/PiiRedactorTests.cs` |

**Covered:** token estimation, embedding generation, chat response generation (incl. streaming callback and history), chat session CRUD (create/append/rename/soft-delete), feedback submission, escalation, knowledge document upload/list/delete, TXT indexing, keyword search, department-scoped search filtering, PII redaction (email/NIK/phone/credit-card patterns) — the last two added 2026-08-04 alongside the guardrails fix.

**Not covered:** PDF/DOCX text extraction quality (extraction itself is a lightweight best-effort approach, not a full parser), chunk boundary/overlap behavior, permission-aware filtering beyond department scoping, rate limiting.

**Frontend E2E:** none.

---

## Phase 5 — Ticketing

### Backend Unit Tests (49 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `TicketServiceTests` | 17 | `tests/AIHelpdesk.Tests/Services/TicketServiceTests.cs` |
| `TicketCategoryServiceTests` | 8 | `tests/AIHelpdesk.Tests/Services/TicketCategoryServiceTests.cs` |
| `EscalationServiceTests` | 8 | `tests/AIHelpdesk.Tests/Services/EscalationServiceTests.cs` |
| `AgentAssignmentServiceTests` | 8 | `tests/AIHelpdesk.Tests/Services/AgentAssignmentServiceTests.cs` |
| `TicketSlaBackgroundServiceTests` | 4 | `tests/AIHelpdesk.Tests/Services/TicketSlaBackgroundServiceTests.cs` |
| `ActionItemReminderBackgroundServiceTests` | 4 | `tests/AIHelpdesk.Tests/Services/ActionItemReminderBackgroundServiceTests.cs` |

**Covered:** ticket CRUD, SLA deadline calculation, status transitions, comments, attachment upload validation (type/size) + download/delete round-trip, Excel export with filters, AI suggestion (real `IAIService` call + parse + fallback), category CRUD, escalation lifecycle, least-loaded agent assignment, SLA breach/at-risk background job, overdue action-item reminder background job.

**Frontend E2E:** none.

---

## Phase 6 — Recruitment

### Backend Unit Tests (37 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `JobVacancyServiceTests` | 8 | `tests/AIHelpdesk.Tests/Services/JobVacancyServiceTests.cs` |
| `CandidateServiceTests` | 13 | `tests/AIHelpdesk.Tests/Services/CandidateServiceTests.cs` |
| `InterviewServiceTests` | 8 | `tests/AIHelpdesk.Tests/Services/InterviewServiceTests.cs` |
| `RecruitmentAIServiceTests` | 8 | `tests/AIHelpdesk.Tests/Services/RecruitmentAIServiceTests.cs` |

**Covered:** vacancy status transitions (Draft → Published → Closed/Filled, incl. auto-Filled detection), candidate pipeline stage advancement (forward-only, no skipping) and rejection (from any active stage), CV upload validation (type/size) + download round-trip, interview scheduling with interviewer double-booking conflict detection, interview complete/cancel, recruitment stats aggregation, Excel export, AI CV summarization/interview questions/candidate matching (all with mocked `IAIService`, success + failure-fallback paths).

**Frontend E2E:** none.

---

## Phase 7 — Hardening & Deployment

**File:** `tests/Middleware/RateLimitingMiddlewareTests.cs` (6 tests)

**Covered:** general per-user/per-IP rate limit enforcement, the separate AI-endpoint limit,
and that requests under the limit pass through untouched.

**Not covered by automated tests** (not code that fits a unit-test harness — see
`documentation/todo-phase-7-hardening.md` and `documentation/deployment-runbook.md` for
details): HTTPS/HSTS/CSP header presence (would need an integration test host, not written
this pass), the k6 load-test scripts (written but not executed against a live environment),
backup/restore scripts (shell scripts, would need a real Postgres+Docker environment to
exercise), and the CI pipeline itself (validated by it running successfully on push, not by
a test suite).

**Frontend E2E:** none.

---

## Test File Map

```
tests/
├── AIHelpdesk.Tests/
│   ├── Services/
│   │   ├── ActionItemReminderBackgroundServiceTests.cs (Phase 5 · 4 tests)
│   │   ├── ActionItemServiceTests.cs      (Phase 3 · 11 tests)
│   │   ├── AgentAssignmentServiceTests.cs (Phase 5 · 8 tests)
│   │   ├── AIServiceTests.cs             (Phase 4 · 10 tests)
│   │   ├── AuthServiceTests.cs           (Phase 1 · 8 tests)
│   │   ├── CandidateServiceTests.cs      (Phase 6 · 13 tests)
│   │   ├── ChatServiceTests.cs           (Phase 4 · 19 tests)
│   │   ├── DepartmentServiceTests.cs     (Phase 1 · 5 tests)
│   │   ├── DocumentServiceTests.cs        (Phase 3 · 22 tests)
│   │   ├── EmployeeServiceTests.cs        (Phase 2 · 13 tests)
│   │   ├── EscalationServiceTests.cs     (Phase 5 · 8 tests)
│   │   ├── InterviewServiceTests.cs      (Phase 6 · 8 tests)
│   │   ├── JobVacancyServiceTests.cs     (Phase 6 · 8 tests)
│   │   ├── KnowledgeBaseServiceTests.cs   (Phase 4 · 19 tests)
│   │   ├── LeaveBalanceServiceTests.cs    (Phase 2 · 6 tests)
│   │   ├── LeaveRequestServiceTests.cs    (Phase 2 · 14 tests)
│   │   ├── LeaveTypeServiceTests.cs       (Phase 2 · 6 tests)
│   │   ├── LetterDocumentGeneratorTests.cs (Phase 3 · 6 tests)
│   │   ├── MeetingServiceTests.cs         (Phase 3 · 15 tests)
│   │   ├── NotificationServiceTests.cs   (Phase 2 · 7 tests)
│   │   ├── PiiRedactorTests.cs            (Phase 4 · 8 tests)
│   │   ├── RecruitmentAIServiceTests.cs  (Phase 6 · 8 tests)
│   │   ├── RoleServiceTests.cs           (Phase 1 · 4 tests)
│   │   ├── TicketCategoryServiceTests.cs (Phase 5 · 8 tests)
│   │   ├── TicketServiceTests.cs         (Phase 5 · 17 tests)
│   │   ├── TicketSlaBackgroundServiceTests.cs (Phase 5 · 4 tests)
│   │   └── UserServiceTests.cs           (Phase 1 · 5 tests)
│   ├── Domain/
│   │   ├── DepartmentTests.cs            (Phase 1 · 2 tests)
│   │   └── RefreshTokenTests.cs          (Phase 1 · 3 tests)
│   ├── Contracts/
│   │   └── AuthContractsTests.cs          (Phase 1 · 3 tests)
│   ├── TestDataFactory.cs                (shared factories, all phases)
│   └── UnitTest1.cs                       (Phase 1 · 1 test — placeholder)
│
└── frontend/
    └── tests/e2e/
        ├── all-phases.spec.ts             (Phase 1+2 · 17 smoke tests)
        └── phase-2/
            ├── helpers.ts                 (Shared E2E utilities)
            ├── employee.spec.ts           (Phase 2 · 7 tests)
            ├── leave-type.spec.ts         (Phase 2 · 6 tests)
            ├── leave-request.spec.ts      (Phase 2 · 7 tests)
            └── leave-approvals.spec.ts    (Phase 2 · 6 tests)
```

---

## Running Tests

### Backend
```bash
# All tests
dotnet test tests/AIHelpdesk.Tests/AIHelpdesk.Tests.csproj

# Phase 2 only
dotnet test tests/AIHelpdesk.Tests/AIHelpdesk.Tests.csproj --filter "FullyQualifiedName~Employee|FullyQualifiedName~Leave|FullyQualifiedName~Notification"
```

### Frontend E2E
```bash
cd frontend

# All E2E
npx playwright test

# Phase 2 interaction tests only
npx playwright test tests/e2e/phase-2/
```

---

## Test Gaps & Recommendations

| Priority | Action |
|----------|--------|
| 🟢 Done | ~~Write backend unit tests for Phase 3 services~~ ✅ 54 tests written |
| 🟢 Done | ~~E2E smoke tests for Phase 3 pages~~ ✅ 6 tests written |
| 🟢 Done | ~~Write backend unit tests for Phase 4 AI Chat services~~ ✅ 56 tests written (`AIServiceTests`, `ChatServiceTests`, `KnowledgeBaseServiceTests`, `PiiRedactorTests`) |
| 🟢 Done | ~~Write backend unit tests for Phase 5 Ticketing~~ ✅ 49 tests written, module committed |
| 🟢 Done | ~~Write backend unit tests for Phase 6 Recruitment~~ ✅ 37 tests written, module built from scratch |
| 🟢 Done | ~~Add unit tests for PII stripping and department-scoped permission filtering~~ ✅ `PiiRedactorTests` + department-scoping tests in `KnowledgeBaseServiceTests` |
| 🟡 Medium | Add E2E interaction/smoke tests for Phase 4, 5, and 6 pages (mirror Phase 2 pattern) — none of these phases have any Playwright coverage yet |
| 🟡 Medium | Add backend integration tests (`WebApplicationFactory`) — every phase's tests, including this pass's 165 new ones, are unit-level only against an in-memory `DbContext` |
| 🟡 Medium | Add frontend unit tests (Vitest/RTL) — still entirely unset up, see `todo-phase-1-foundation.md` |
| 🟢 Low | Run the existing Phase 7 k6 load test scripts against a live environment and add CI-based security scan config (Trivy/ZAP) |
| 🟢 Low | Remove `UnitTest1.cs` placeholder test |
