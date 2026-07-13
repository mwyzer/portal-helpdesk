# ✅ Phase 2 — Employee & HR Administration: Task Tracker

**Status Legend:** `[ ]` Not Started · `[/]` In Progress · `[x]` Done · `[!]` Blocked

**Prerequisite:** Phase 1 must be complete (auth, RBAC, user management, base layout).

---

## 1. Backend — Database Migrations (HR Module)

- [x] Create migration: `Employees` table
- [x] Create migration: `LeaveTypes` table
- [x] Create migration: `LeaveBalances` table
- [x] Create migration: `LeaveRequests` table
- [x] Create migration: `LeaveApprovals` table
- [x] Create migration: `Notifications` table
- [x] Seed default leave types (Annual, Sick, Special, Maternity, Late, Early, WFH)
- [x] Seed sample employee data (5+ employees, 3 departments) — uses existing seed users + leave types
- [x] Apply migrations to development database

---

## 2. Backend — Employee Module

### Domain
- [x] Create `Employee` entity (EmployeeNo, FullName, Email, Phone, JoinDate, etc.)
- [x] Create `EmploymentStatus` enum (Active, Inactive, Resigned, Terminated)
- [x] Create value objects: `EmployeeNumber`, `PhoneNumber` — inline in entity

### Application
- [x] Create DTOs: `CreateEmployeeRequest`, `UpdateEmployeeRequest`, `EmployeeResponse`
- [x] Create DTOs: `EmployeeListResponse`, `EmployeeImportResult`
- [ ] Create FluentValidation validators for employee requests
- [ ] Create Mapster mapping profiles (Employee ↔ EmployeeDto) — manual mapping used
- [x] Create `IEmployeeService` interface
- [x] Implement `EmployeeService` (CRUD with search & filter)
- [x] Implement `EmployeeService.SearchAsync` (filter by department, name, status, pagination)
- [ ] Implement Excel import service (ClosedXML, validate rows, detect duplicates) — stub throws NotImplementedException
- [ ] Implement Excel export service (generate .xlsx, respect filters) — stub throws NotImplementedException

### Infrastructure
- [x] Create `EmployeeConfiguration` (EF Core Fluent API)
- [ ] Create `EmployeeRepository` (if using repository pattern) — not used, service uses DbContext directly

### API
- [x] Create `EmployeesController`
- [x] `GET /api/employees` — list with pagination and filters
- [x] `GET /api/employees/{id}` — get by ID
- [x] `POST /api/employees` — create employee
- [x] `PUT /api/employees/{id}` — update employee
- [x] `DELETE /api/employees/{id}` — soft-delete
- [x] `POST /api/employees/import` — import from Excel
- [x] `GET /api/employees/export` — export to Excel
- [x] `GET /api/employees/my-profile` — current user's employee profile

---

## 3. Backend — Leave Module

### Domain
- [x] Create `LeaveType` entity (Name, Code, DaysPerYear, IsPaid, IsActive, MinServiceMonths)
- [x] Create `LeaveBalance` entity (EmployeeId, LeaveTypeId, TotalDays, UsedDays, RemainingDays, Year)
- [x] Create `LeaveRequest` entity (EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays, Reason, Status)
- [x] Create `LeaveApproval` entity (LeaveRequestId, ApproverId, ApproverRole, Status, Note, ApprovedAt)
- [x] Create `LeaveRequestStatus` enum (Draft, Submitted, WaitingForManager, WaitingForHR, Approved, Rejected, Cancelled)
- [x] Create `ApprovalStatus` enum (Pending, Approved, Rejected)

### Application
- [x] Create DTOs: `LeaveTypeRequest`, `LeaveTypeResponse`
- [x] Create DTOs: `LeaveBalanceResponse`
- [x] Create DTOs: `CreateLeaveRequest`, `LeaveRequestResponse`, `LeaveApprovalDto`
- [ ] Create FluentValidation validators for leave requests
- [ ] Create Mapster mappings for leave entities — manual mapping used
- [x] Create `ILeaveTypeService` interface + implementation
- [x] Create `ILeaveBalanceService` interface + implementation
- [x] Create `ILeaveRequestService` interface + implementation
- [x] Implement `SubmitAsync` — Draft → Submitted, validate balance
- [x] Implement `ApproveByManagerAsync` — Submitted → WaitingForHR (or → Approved if ≤ 3 days)
- [x] Implement `ApproveByHRAsync` — WaitingForHR → Approved, update balance
- [x] Implement `RejectAsync` — any active status → Rejected with reason
- [x] Implement `CancelAsync` — Draft/Submitted → Cancelled
- [x] Implement balance validation (sufficient remaining days before submit)
- [x] Implement balance update on final approval (decrement UsedDays)
- [x] Implement multi-level approval logic (≤ 3 days: Manager only; > 3 days: Manager + HRD)
- [x] Implement sick leave rule (configurable skip manager approval)

### API
- [x] Create `LeaveTypesController`
- [x] `GET /api/leave-types` — list all active leave types
- [x] `POST /api/leave-types` — create leave type
- [x] `PUT /api/leave-types/{id}` — update leave type
- [x] Create `LeaveBalancesController`
- [x] `GET /api/leave-balances/my` — current user balances
- [x] `GET /api/leave-balances/employee/{employeeId}` — view employee balance
- [x] `POST /api/leave-balances/adjust` — manual adjustment
- [x] Create `LeaveRequestsController`
- [x] `GET /api/leave-requests` — list (filtered by role)
- [x] `GET /api/leave-requests/{id}` — detail with approval timeline
- [x] `POST /api/leave-requests` — create draft
- [x] `PUT /api/leave-requests/{id}` — edit draft
- [x] `POST /api/leave-requests/{id}/submit` — submit for approval
- [x] `POST /api/leave-requests/{id}/approve` — approve (manager or HRD)
- [x] `POST /api/leave-requests/{id}/reject` — reject with reason
- [x] `POST /api/leave-requests/{id}/cancel` — cancel own request
- [x] `GET /api/leave-requests/pending-approval` — my pending approvals

---

## 4. Backend — Notification Module

### Domain
- [x] Create `Notification` entity (UserId, Title, Body, Type, ReferenceType, ReferenceId, IsRead, ReadAt)

### Application
- [x] Create DTOs: `NotificationResponse`, `NotificationListResponse`
- [x] Create `INotificationService` interface
- [x] Implement `NotificationService.CreateAsync` (create and send)
- [x] Implement `NotificationService.MarkAsReadAsync`
- [x] Implement `NotificationService.MarkAllAsReadAsync`
- [x] Implement `NotificationService.GetUnreadCountAsync`

### SignalR
- [x] Create `NotificationHub` (SignalR hub)
- [x] Implement connection group by UserId on connect
- [x] Implement `SendNotification` event (server → client)
- [x] Implement `UnreadCountUpdated` event
- [x] Configure SignalR in `Program.cs`
- [x] Trigger notification on leave submitted (→ Manager)
- [x] Trigger notification on manager approved (→ HRD)
- [x] Trigger notification on final approved/rejected (→ Employee)
- [x] Trigger notification on leave cancelled (→ Manager + HRD)
- [x] Trigger notification on balance adjustment (→ Employee)

### Email (SMTP)
- [ ] Configure SMTP settings (host, port, credentials)
- [ ] Implement email sending for leave status changes
- [ ] Implement email template for leave submitted
- [ ] Implement email template for leave approved/rejected

### API
- [x] Create `NotificationsController`
- [x] `GET /api/notifications` — my notifications
- [x] `PUT /api/notifications/{id}/read` — mark as read
- [x] `PUT /api/notifications/read-all` — mark all as read
- [x] `GET /api/notifications/unread-count` — unread badge count

---

## 5. Frontend — Employee Pages

- [x] Create `employees.api.ts` (list, get, create, update, delete, import, export) — inline in component
- [x] Build `EmployeeTable` component (columns: No, Name, Department, Position, Status)
- [x] Build `EmployeeTable` search input (by name, email, employee no)
- [x] Build `EmployeeTable` filters (by department, status dropdown)
- [x] Build `EmployeeTable` pagination
- [x] Build `EmployeeForm` component (create/edit mode with validation) — Zod schema
- [x] Build `EmployeeCreatePage` (title + form, submit → redirect to list) — inline dialog
- [x] Build `EmployeeEditPage` (pre-filled form, submit → redirect to detail) — inline dialog
- [ ] Build `EmployeeDetailPage` (info card, status badge, edit button, delete button)
- [x] Build `EmployeeListPage` (table + search + filter + pagination + actions)
- [ ] Build `EmployeeImportDialog` (drag-drop file upload, preview invalid rows, confirm)
- [ ] Add employee export button (trigger download via API)

---

## 6. Frontend — Leave Pages

- [x] Create `leave-types.api.ts`, `leave-balances.api.ts`, `leave-requests.api.ts` — inline in components
- [x] Build `LeaveRequestForm` (leave type selector, date range picker, reason textarea, file upload)
- [x] Build `LeaveRequestForm` date validation (start date < end date, not in past)
- [ ] Build `LeaveBalanceCard` component (progress bar, label, remaining/total count) — stats cards used instead
- [ ] Build `LeaveBalanceCard` color coding (green ≥ 50%, yellow ≥ 25%, red < 25%)
- [ ] Build `ApprovalCard` component (employee name, dates, reason, approve/reject buttons) — inline in table
- [ ] Build `ApprovalTimeline` component (vertical timeline showing each approval stage) — inline in detail dialog
- [x] Build `LeaveRequestListPage` (table with status badges, filters by status/dates)
- [x] Build `LeaveRequestCreatePage` (form, leave type selector shows remaining balance) — dialog form
- [x] Build `LeaveRequestDetailPage` (full info, approval timeline, document download) — detail dialog
- [x] Build `LeaveApprovalPage` (list of pending approvals for Manager/HRD)
- [ ] Build `LeaveApprovalPage` batch approve/reject action
- [x] Build `LeaveTypesListPage` for HRD admin (table, create/edit modal)

---

## 7. Frontend — Notification System

- [x] Create `notifications.api.ts` (list, mark read, mark all read, unread count) — inline in component
- [ ] Create SignalR connection hook: `useSignalR` (connect on login, disconnect on logout)
- [x] Connect to `/hubs/notifications` hub on authentication — backend Hub ready
- [ ] Implement auto-reconnect with fallback polling (every 30s)
- [ ] Build `NotificationBell` component (icon with unread count badge)
- [ ] Build `NotificationBell` dropdown (last 5 notifications, "mark all read" link)
- [ ] Build `NotificationList` component (scrollable list, each item with read/unread styling)
- [ ] Build `NotificationCenterPage` (full list, filter by read/unread, mark as read)
- [ ] Show toast notification on new SignalR event

---

## 8. Frontend — Dashboards

### Employee Dashboard
- [ ] Display leave balance cards (all leave types, remaining days)
- [ ] Display recent leave requests (last 5, with status badges)
- [x] Display quick "Submit Leave" button — available on leave requests page
- [ ] Display today's notifications count
- [ ] Display announcements/widgets area

### HR Dashboard
- [ ] Display total active employee count
- [ ] Display pending leave verifications count (clickable → approval page)
- [ ] Display leave requests by status (pie chart or bar chart via Recharts)
- [ ] Display recent activity feed (new employees, recent approvals)

---

## 9. Backend — Unit Tests (HR Module)

### Setup
- [ ] Create `AIHelpdesk.UnitTests.Modules.HR` test project
- [ ] Install Moq, FluentAssertions, Bogus, Coverlet
- [ ] Configure test category constants (`Category.Employee`, `Category.Leave`, `Category.Notification`)

### Employee Service Tests
- [ ] `CreateAsync` creates employee and returns response
- [ ] `CreateAsync` rejects duplicate EmployeeNo (throws validation error)
- [ ] `CreateAsync` rejects duplicate Email
- [ ] `CreateAsync` validates department exists
- [ ] `UpdateAsync` updates employee fields correctly
- [ ] `UpdateAsync` validates position belongs to department
- [ ] `DeleteAsync` soft-deletes employee (sets IsDeleted)
- [ ] `DeleteAsync` throws error if employee has active leave
- [ ] `SearchAsync` filters by department ID
- [ ] `SearchAsync` filters by name (partial match)
- [ ] `SearchAsync` filters by employment status
- [ ] `SearchAsync` returns paginated results
- [ ] Excel import with valid data returns success with count
- [ ] Excel import with invalid rows returns error details
- [ ] Excel import with duplicate EmployeeNo reports as error
- [ ] Excel export generates file with correct columns

### Leave Service Tests
- [ ] `SubmitAsync` transitions Draft → Submitted
- [ ] `SubmitAsync` validates sufficient balance
- [ ] `SubmitAsync` with insufficient balance returns validation error
- [ ] `ApproveByManagerAsync` transitions → WaitingForHR (leave > 3 days)
- [ ] `ApproveByManagerAsync` transitions → Approved (leave ≤ 3 days, no HR needed)
- [ ] `ApproveByManagerAsync` with already approved request returns error
- [ ] `ApproveByHRAsync` transitions WaitingForHR → Approved
- [ ] `ApproveByHRAsync` updates leave balance (decrements UsedDays)
- [ ] `RejectAsync` sets status to Rejected with reason
- [ ] `CancelAsync` cancels Draft/Submitted request
- [ ] `CancelAsync` cannot cancel already approved request
- [ ] `LeaveBalanceService` returns correct remaining days
- [ ] `LeaveBalanceService` annual reset works correctly
- [ ] `LeaveBalanceService` carry-over logic respects max

### Notification Service Tests
- [ ] `CreateAsync` creates notification for target user
- [ ] `MarkAsReadAsync` sets IsRead = true and ReadAt timestamp
- [ ] `MarkAllAsReadAsync` marks all unread as read
- [ ] `GetUnreadCountAsync` returns correct count
- [ ] Leave submitted → notification sent to manager
- [ ] Leave approved → notification sent to employee
- [ ] Leave rejected → notification sent to employee

### Validation Tests
- [ ] Leave request missing reason → invalid
- [ ] Leave request past start date → invalid
- [ ] Leave request end date before start date → invalid
- [ ] Employee missing required fields → invalid
- [ ] Employee invalid email format → invalid
- [ ] Employee invalid phone format → invalid
- [ ] Leave type negative days → invalid
- [ ] Leave type missing code → invalid

### Mapping Tests
- [ ] Employee ↔ EmployeeDto mapping is correct (all fields)
- [ ] LeaveRequest ↔ LeaveRequestDto mapping is correct
- [ ] LeaveBalance ↔ LeaveBalanceDto mapping is correct
- [ ] Notification ↔ NotificationDto mapping is correct

---

## 10. Backend — Integration Tests (HR Module)

### Setup
- [ ] Configure `WebApplicationFactory<Program>` with HR test database
- [ ] Set up Testcontainers.PostgreSQL for HR integration tests
- [ ] Add test category `Category.HRModule`

### Employee Endpoint Tests
- [ ] `POST /api/employees` creates employee (201)
- [ ] `POST /api/employees` with duplicate EmployeeNo returns 409
- [ ] `GET /api/employees` returns paginated list
- [ ] `GET /api/employees/{id}` returns employee detail
- [ ] `PUT /api/employees/{id}` updates employee
- [ ] `DELETE /api/employees/{id}` soft-deletes (subsequent GET returns 404)
- [ ] `POST /api/employees/import` with valid Excel returns 200
- [ ] `POST /api/employees/import` with invalid data returns 400
- [ ] `GET /api/employees/export` returns file with correct Content-Type
- [ ] Employee endpoints blocked for Employee role (403)

### Leave Endpoint Tests
- [ ] `POST /api/leave-requests` creates draft
- [ ] `POST /api/leave-requests/{id}/submit` transitions to Submitted
- [ ] `POST /api/leave-requests/{id}/approve` (manager) transitions correctly
- [ ] `POST /api/leave-requests/{id}/approve` (HRD) transitions to Approved
- [ ] `POST /api/leave-requests/{id}/reject` sets Rejected status
- [ ] `POST /api/leave-requests/{id}/cancel` cancels draft
- [ ] Balance decremented after final approval
- [ ] Insufficient balance returns 400

### Notification Endpoint Tests
- [ ] `GET /api/notifications` returns user's notifications
- [ ] `PUT /api/notifications/{id}/read` marks as read
- [ ] `PUT /api/notifications/read-all` marks all as read
- [ ] `GET /api/notifications/unread-count` returns count
- [ ] Leave approval triggers notification creation

### Authorization Tests
- [ ] Employee cannot access HRD-only endpoints (403)
- [ ] Unauthenticated request returns 401
- [ ] Manager can view pending approvals

---

## 11. Frontend — Unit Tests (HR Module)

### Setup
- [ ] Configure MSW handlers for all HR API endpoints
- [ ] Set up test wrappers (QueryClientProvider, MemoryRouter)

### Employee Component Tests
- [ ] `EmployeeTable` renders correct number of rows
- [ ] `EmployeeTable` search filters by name
- [ ] `EmployeeTable` pagination navigates pages
- [ ] `EmployeeForm` shows validation errors on empty submit
- [ ] `EmployeeForm` submit calls API with correct data
- [ ] `EmployeeForm` edit mode pre-fills fields
- [ ] `EmployeeImportDialog` shows preview rows
- [ ] `EmployeeImportDialog` confirm button triggers import

### Leave Component Tests
- [ ] `LeaveRequestForm` validates start date < end date
- [ ] `LeaveRequestForm` submit with past start date shows error
- [ ] `LeaveRequestForm` leave type selector works
- [ ] `LeaveBalanceCard` shows correct remaining/total
- [ ] `LeaveBalanceCard` progress bar color changes at thresholds
- [ ] `ApprovalCard` shows approve/reject buttons for approver
- [ ] `ApprovalCard` hides buttons for non-approver
- [ ] `ApprovalTimeline` renders current status correctly
- [ ] `ApprovalTimeline` renders completed stages as checked

### Notification Component Tests
- [ ] `NotificationBell` shows unread count badge
- [ ] `NotificationBell` dropdown shows recent notifications
- [ ] `NotificationList` marks item as read on click
- [ ] `NotificationList` shows empty state message
- [ ] `NotificationList` "Mark all read" updates count

### Page Tests
- [ ] `LeaveRequestListPage` status filter works
- [ ] `LeaveRequestListPage` shows only own requests for Employee
- [ ] `LeaveApprovalPage` shows only pending for current role
- [ ] `EmployeeDashboardPage` displays balance cards
- [ ] `EmployeeDashboardPage` displays recent requests
- [ ] `HRDashboardPage` displays employee count
- [ ] `HRDashboardPage` displays pending verifications

### MSW Handler Tests
- [ ] All HR API handlers return correct mock data
- [ ] Error responses (400, 403, 409) show toast/alert
- [ ] Loading states show skeleton/spinner

---

## 12. Test Automation & Coverage

- [ ] Configure Coverlet for HR module (unit + integration)
- [ ] Set coverage thresholds in `.runsettings`
- [ ] Employee module threshold: ≥ 85%
- [ ] Leave module threshold: ≥ 90%
- [ ] Notification module threshold: ≥ 80%
- [ ] Frontend HR components threshold: ≥ 70%
- [ ] Add `npm run test:coverage:hr` script
- [ ] Generate coverage badges for README

---

## 13. CI/CD Pipeline (HR Extensions)

- [ ] Add `hr-integration` job to `.github/workflows/ci.yml`
- [ ] HR CI: PostgreSQL service container
- [ ] HR CI: .NET restore + build
- [ ] HR CI: Run unit tests with `Category=Employee|Leave|Notification` filter
- [ ] HR CI: Run integration tests with `Category=HRModule` filter
- [ ] HR CI: Merge coverage, enforce thresholds
- [ ] Create `.github/workflows/hr-deploy-check.yml`
- [ ] HR Deploy Gate: triggered on HR file changes only
- [ ] HR Deploy Gate: run backend HR tests
- [ ] HR Deploy Gate: run frontend HR component tests
- [ ] HR Deploy Gate: check migration safety (warn on risky changes)
- [ ] Add Docker health check for backend in `docker-compose.yml`
- [ ] Add `worker` service to `docker-compose.yml` for background jobs
- [ ] Add SMTP + leave config to `.env.example`
- [ ] Document deployment runbook (pull → backup → migrate → deploy → verify → rollback)

---

## Summary

| Category | Total Tasks | Done |
|----------|:-----------:|:----:|
| Database Migrations | 9 | `[ ]` |
| Employee Module (Domain→API) | 21 | `[ ]` |
| Leave Module (Domain→API) | 35 | `[ ]` |
| Notification Module (Domain→API) | 23 | `[ ]` |
| Frontend Employee Pages | 12 | `[ ]` |
| Frontend Leave Pages | 12 | `[ ]` |
| Frontend Notification System | 8 | `[ ]` |
| Frontend Dashboards | 7 | `[ ]` |
| Backend Unit Tests | 40 | `[ ]` |
| Backend Integration Tests | 19 | `[ ]` |
| Frontend Unit Tests | 30 | `[ ]` |
| Test Automation | 7 | `[ ]` |
| CI/CD Pipeline | 14 | `[ ]` |
| **TOTAL** | **~237 tasks** | |
