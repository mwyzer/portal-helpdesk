# AIHelpdesk — Test Coverage Report

**Generated:** 2026-07-14, refreshed 2026-08-04  
**Total Tests:** 207  
**Overall Status:** ✅ All Passing (counts below re-verified against actual `[Fact]`/`[Theory]` attributes in `tests/AIHelpdesk.Tests`; several 2026-07-14 per-file counts had drifted)

---

## Summary by Phase

| Phase | Backend Unit Tests | Frontend E2E Smoke | Frontend E2E Interaction | Total |
|-------|-------------------|--------------------|--------------------------|-------|
| Phase 1 — Foundation MVP | 23 | 13 | 0 | **36** |
| Phase 2 — HR Administration | 46 | 4 | 26 | **76** |
| Phase 3 — Secretary Module | 44 | 6 | 0 | **50** |
| Phase 4 — AI Helpdesk Chat | 45 | 0 | 0 | **45** |
| Phase 5 — Ticketing | 0 | 0 | 0 | **0** |
| Phase 6 — Recruitment | 0 | 0 | 0 | **0** |
| Phase 7 — Hardening & Deployment | 0 | 0 | 0 | **0** |
| **TOTAL** | **158** | **23** | **26** | **207** |

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

### Backend Unit Tests (44 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `MeetingServiceTests` | 15 | `tests/AIHelpdesk.Tests/Services/MeetingServiceTests.cs` |
| `ActionItemServiceTests` | 11 | `tests/AIHelpdesk.Tests/Services/ActionItemServiceTests.cs` |
| `DocumentServiceTests` | 18 | `tests/AIHelpdesk.Tests/Services/DocumentServiceTests.cs` |

**Covered:**
- Meeting CRUD, pagination, date-range & status filtering, soft delete
- Meeting participants: add/remove, role & attendance tracking
- Meeting notes: add, update, delete
- Action items: create, update, complete (with assignee guard), cancel, overdue detection
- Document templates: CRUD, category filtering, activate/deactivate
- Document requests: full workflow (Draft → AI Draft Ready → Review → Approve → Generate Final → Download)
- Document workflow state guards (invalid transitions throw)
- Letter number auto-generation (format: `{counter}/{code}/MGR/{year}`)

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

### Backend Unit Tests (45 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `AIServiceTests` | 10 | `tests/AIHelpdesk.Tests/Services/AIServiceTests.cs` |
| `ChatServiceTests` | 19 | `tests/AIHelpdesk.Tests/Services/ChatServiceTests.cs` |
| `KnowledgeBaseServiceTests` | 16 | `tests/AIHelpdesk.Tests/Services/KnowledgeBaseServiceTests.cs` |

**Covered:** token estimation, embedding generation, chat response generation (incl. streaming callback and history), chat session CRUD (create/append/rename/soft-delete), feedback submission, escalation, knowledge document upload/list/delete, TXT indexing, keyword search.

**Not covered:** PDF/DOCX text extraction, chunk boundary/overlap behavior, AI guardrails (permission-aware filtering, PII stripping — neither is implemented yet either), rate limiting.

**Frontend E2E:** none.

### No tests yet

| Phase | Status |
|-------|--------|
| Phase 5 — Ticketing | ❌ No tests |
| Phase 6 — Recruitment | ❌ No tests |
| Phase 7 — Hardening & Deployment | ❌ No tests (includes security, performance, CI/CD) |

---

## Test File Map

```
tests/
├── AIHelpdesk.Tests/
│   ├── Services/
│   │   ├── ActionItemServiceTests.cs      (Phase 3 · 11 tests)
│   │   ├── AIServiceTests.cs             (Phase 4 · 10 tests)
│   │   ├── ChatServiceTests.cs           (Phase 4 · 19 tests)
│   │   ├── DepartmentServiceTests.cs     (Phase 1 · 5 tests)
│   │   ├── DocumentServiceTests.cs        (Phase 3 · 18 tests)
│   │   ├── EmployeeServiceTests.cs        (Phase 2 · 13 tests)
│   │   ├── KnowledgeBaseServiceTests.cs   (Phase 4 · 16 tests)
│   │   ├── LeaveBalanceServiceTests.cs    (Phase 2 · 6 tests)
│   │   ├── LeaveRequestServiceTests.cs    (Phase 2 · 14 tests)
│   │   ├── LeaveTypeServiceTests.cs       (Phase 2 · 6 tests)
│   │   ├── MeetingServiceTests.cs         (Phase 3 · 15 tests)
│   │   ├── NotificationServiceTests.cs   (Phase 2 · 7 tests)
│   │   ├── RoleServiceTests.cs           (Phase 1 · 4 tests)
│   │   └── UserServiceTests.cs           (Phase 1 · 5 tests)
│   ├── Domain/
│   │   ├── DepartmentTests.cs            (Phase 1 · 2 tests)
│   │   └── RefreshTokenTests.cs          (Phase 1 · 3 tests)
│   ├── Contracts/
│   │   └── AuthContractsTests.cs          (Phase 1 · 3 tests)
│   ├── TestDataFactory.cs                (Phase 1+2+3 factories)
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
| 🟢 Done | ~~Write backend unit tests for Phase 3 services~~ ✅ 44 tests written |
| 🟢 Done | ~~E2E smoke tests for Phase 3 pages~~ ✅ 6 tests written |
| 🟢 Done | ~~Write backend unit tests for Phase 4 AI Chat services~~ ✅ 45 tests written (`AIServiceTests`, `ChatServiceTests`, `KnowledgeBaseServiceTests`) |
| 🔴 High | Write backend unit tests for Phase 5 Ticketing (currently 0, and the module itself is still uncommitted) |
| 🟡 Medium | Add E2E interaction tests for Phase 3 and Phase 4 pages (mirror Phase 2 pattern) |
| 🟡 Medium | Write backend unit tests for Phase 6 Recruitment |
| 🟡 Medium | Add unit tests for AI guardrails (permission filtering, PII stripping) and PDF/DOCX extraction once those are implemented |
| 🟢 Low | Add Phase 7 k6 load tests, security scan config |
| 🟢 Low | Remove `UnitTest1.cs` placeholder test |
