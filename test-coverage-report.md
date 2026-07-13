# AIHelpdesk — Test Coverage Report

**Generated:** 2026-07-14  
**Total Tests:** 112  
**Overall Status:** ✅ All Passing

---

## Summary by Phase

| Phase | Backend Unit Tests | Frontend E2E Smoke | Frontend E2E Interaction | Total |
|-------|-------------------|--------------------|--------------------------|-------|
| Phase 1 — Foundation MVP | 22 | 13 | 0 | **35** |
| Phase 2 — HR Administration | 47 | 4 | 26 | **77** |
| Phase 3 — Secretary Module | 0 | 0 | 0 | **0** |
| Phase 4 — AI Helpdesk Chat | 0 | 0 | 0 | **0** |
| Phase 5 — Ticketing | 0 | 0 | 0 | **0** |
| Phase 6 — Recruitment | 0 | 0 | 0 | **0** |
| Phase 7 — Hardening & Deployment | 0 | 0 | 0 | **0** |
| **TOTAL** | **69** | **17** | **26** | **112** |

---

## Phase 1 — Foundation MVP

### Backend Unit Tests (22 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `UserServiceTests` | 5 | `tests/AIHelpdesk.Tests/Services/UserServiceTests.cs` |
| `RoleServiceTests` | 4 | `tests/AIHelpdesk.Tests/Services/RoleServiceTests.cs` |
| `DepartmentServiceTests` | 4 | `tests/AIHelpdesk.Tests/Services/DepartmentServiceTests.cs` |
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

### Backend Unit Tests (47 tests)

| Test Class | Tests | File |
|------------|-------|------|
| `EmployeeServiceTests` | 13 | `tests/AIHelpdesk.Tests/Services/EmployeeServiceTests.cs` |
| `LeaveRequestServiceTests` | 16 | `tests/AIHelpdesk.Tests/Services/LeaveRequestServiceTests.cs` |
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

## Phase 3–7 — Not Yet Tested

| Phase | Status |
|-------|--------|
| Phase 3 — Secretary Module | ❌ No tests |
| Phase 4 — AI Helpdesk Chat | ❌ No tests |
| Phase 5 — Ticketing | ❌ No tests |
| Phase 6 — Recruitment | ❌ No tests |
| Phase 7 — Hardening & Deployment | ❌ No tests (includes security, performance, CI/CD) |

---

## Test File Map

```
tests/
├── AIHelpdesk.Tests/
│   ├── Services/
│   │   ├── DepartmentServiceTests.cs     (Phase 1 · 4 tests)
│   │   ├── EmployeeServiceTests.cs        (Phase 2 · 13 tests)
│   │   ├── LeaveBalanceServiceTests.cs    (Phase 2 · 4 tests) — fix: actually 6
│   │   ├── LeaveRequestServiceTests.cs    (Phase 2 · 16 tests)
│   │   ├── LeaveTypeServiceTests.cs       (Phase 2 · 6 tests)
│   │   ├── NotificationServiceTests.cs   (Phase 2 · 7 tests) — fix: actually 8
│   │   ├── RoleServiceTests.cs           (Phase 1 · 4 tests)
│   │   └── UserServiceTests.cs           (Phase 1 · 5 tests)
│   ├── Domain/
│   │   ├── DepartmentTests.cs            (Phase 1 · 2 tests)
│   │   └── RefreshTokenTests.cs          (Phase 1 · 3 tests)
│   ├── Contracts/
│   │   └── AuthContractsTests.cs          (Phase 1 · 3 tests)
│   ├── TestDataFactory.cs                (Phase 1+2 factories)
│   └── UnitTest1.cs                       (Phase 1 · 1 test)
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
| 🔴 High | Write backend unit tests for Phase 3 services (Meeting, Document, KnowledgeBase) |
| 🔴 High | Write backend unit tests for Phase 4 AI Chat services |
| 🟡 Medium | Write backend unit tests for Phase 5 Ticket & Phase 6 Recruitment |
| 🟡 Medium | Add E2E interaction tests for Phase 3–6 pages (mirror Phase 2 pattern) |
| 🟢 Low | Add Phase 7 k6 load tests, security scan config |
| 🟢 Low | Remove `UnitTest1.cs` placeholder test |
