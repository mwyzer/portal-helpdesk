# Phase 2 — Employee & HR Administration ✅ Complete

**Tech Stack:** React (TypeScript) + ASP.NET Core Web API + PostgreSQL

**Prerequisite:** Phase 1 (Foundation) must be complete — authentication, RBAC, user management, base layout.

---

## 1. Overview

Phase 2 builds the core HR functionality: employee data management, leave & absence management with approval workflows, and in-app notifications.

**Goal:** Employees can manage their profile and submit leave requests. Managers and HRD can process approvals with a clear workflow.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Employee management | Full CRUD, import/export Excel, search & filter |
| 2 | Leave types & balance | Configurable leave types, balance tracking |
| 3 | Leave request workflow | Submit, approve, reject, cancel with multi-level approval |
| 4 | Manager approval dashboard | Pending approvals, batch action |
| 5 | HR verification step | HR validates after manager approval |
| 6 | In-app notifications | Real-time alerts via SignalR |
| 7 | Employee dashboard | Leave balance, recent requests, quick actions |
| 8 | HR dashboard | Employee count, pending verifications, stats |

---

## 3. New Database Tables

| Table | Key Columns | Description |
|-------|-------------|-------------|
| `Employees` | Id, UserId, EmployeeNo, FullName, Email, Phone, JoinDate, DepartmentId, PositionId, ManagerId, EmploymentStatus, WorkLocation, LeaveBalance | Employee master data |
| `LeaveTypes` | Id, Name, Code, DaysPerYear, IsPaid, IsActive, MinServiceMonths | Leave categories |
| `LeaveBalances` | Id, EmployeeId, LeaveTypeId, TotalDays, UsedDays, RemainingDays, Year | Per-year leave balance |
| `LeaveRequests` | Id, EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays, Reason, Status, DocumentUrl | Leave applications |
| `LeaveApprovals` | Id, LeaveRequestId, ApproverId, ApproverRole, Status, Note, ApprovedAt | Approval trail |
| `Notifications` | Id, UserId, Title, Body, Type, ReferenceType, ReferenceId, IsRead, ReadAt | In-app notifications |

All tables include: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

### LeaveRequest Status Flow

```
Draft → Submitted → Waiting for Manager → Waiting for HR → Approved
                                              ↓                ↓
                                           Rejected         Rejected
                                              ↕
                                           Cancelled
```

---

## 4. API Endpoints

### Employees

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/employees` | HRD, Manager, Super Admin | List employees (paginated, filterable) |
| GET | `/api/employees/{id}` | HRD, Manager, Super Admin | Employee detail |
| POST | `/api/employees` | HRD, Super Admin | Create employee |
| PUT | `/api/employees/{id}` | HRD, Super Admin | Update employee |
| DELETE | `/api/employees/{id}` | Super Admin | Soft-delete employee |
| POST | `/api/employees/import` | HRD, Super Admin | Import from Excel |
| GET | `/api/employees/export` | HRD, Super Admin | Export to Excel |
| GET | `/api/employees/my-profile` | Employee | Get own employee profile |

### Leave Types

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/leave-types` | All authenticated | List leave types |
| POST | `/api/leave-types` | HRD, Super Admin | Create leave type |
| PUT | `/api/leave-types/{id}` | HRD, Super Admin | Update leave type |

### Leave Balances

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/leave-balances/my` | Employee | My leave balances |
| GET | `/api/leave-balances/employee/{employeeId}` | HRD, Manager | View employee balance |
| POST | `/api/leave-balances/adjust` | HRD, Super Admin | Manual balance adjustment |

### Leave Requests

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/leave-requests` | All authenticated | List (filtered by role) |
| GET | `/api/leave-requests/{id}` | All authenticated | Detail |
| POST | `/api/leave-requests` | Employee | Create draft |
| PUT | `/api/leave-requests/{id}` | Owner | Edit draft only |
| POST | `/api/leave-requests/{id}/submit` | Owner | Submit for approval |
| POST | `/api/leave-requests/{id}/approve` | Manager/HRD | Approve |
| POST | `/api/leave-requests/{id}/reject` | Manager/HRD | Reject with reason |
| POST | `/api/leave-requests/{id}/cancel` | Owner | Cancel (if not yet approved) |
| GET | `/api/leave-requests/pending-approval` | Manager/HRD | My pending approvals |

### Notifications

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/notifications` | All authenticated | My notifications |
| PUT | `/api/notifications/{id}/read` | All authenticated | Mark as read |
| PUT | `/api/notifications/read-all` | All authenticated | Mark all as read |
| GET | `/api/notifications/unread-count` | All authenticated | Unread count badge |

---

## 5. Frontend Pages

| Route | Page | Access |
|-------|------|--------|
| `/employees` | EmployeeListPage | HRD, Manager, Super Admin |
| `/employees/new` | EmployeeCreatePage | HRD, Super Admin |
| `/employees/:id` | EmployeeDetailPage | HRD, Manager, Super Admin |
| `/employees/:id/edit` | EmployeeEditPage | HRD, Super Admin |
| `/employees/import` | EmployeeImportPage | HRD, Super Admin |
| `/leave-types` | LeaveTypeListPage | HRD, Super Admin |
| `/leave-requests` | LeaveRequestListPage | All (filtered by role) |
| `/leave-requests/new` | LeaveRequestCreatePage | Employee |
| `/leave-requests/:id` | LeaveRequestDetailPage | All (owner/approver) |
| `/approvals/leave` | LeaveApprovalPage | Manager, HRD |
| `/notifications` | NotificationCenterPage | All |

### New Components

| Component | Description |
|-----------|-------------|
| `EmployeeTable` | Searchable, filterable table with action buttons |
| `EmployeeForm` | Create/edit form with validation |
| `EmployeeImportDialog` | Drag-drop Excel upload with preview |
| `LeaveRequestForm` | Date picker, leave type selector, document upload |
| `LeaveBalanceCard` | Shows remaining leave per type (progress bars) |
| `ApprovalCard` | Leave request summary with approve/reject buttons |
| `ApprovalTimeline` | Visual timeline showing approval stages |
| `NotificationBell` | Topbar icon with unread count dropdown |
| `NotificationList` | Scrollable notification list |
| `FileUpload` | Drag-drop file upload component |

---

## 6. Business Logic

### Leave Balance Calculation
```
RemainingDays = TotalDays - UsedDays
```
- Used days are calculated from approved leave requests in the current year
- Leave balance resets annually (configurable per leave type)
- Unused paid leave may carry over (configurable)

### Multi-Level Approval Workflow

```
1. Employee submits leave request
       ↓
2. Manager receives notification
   → Approve: moves to "Waiting for HR"
   → Reject: status becomes "Rejected"
       ↓
3. HRD receives notification
   → Approve: status becomes "Approved", balance updated
   → Reject: status becomes "Rejected"
       ↓
4. Employee receives notification of result
```

- **Sick leave**: Can skip manager approval (configurable)
- **Leave > 3 days**: Requires both manager + HRD approval
- **Leave ≤ 3 days**: Manager approval only

### Notification Triggers

| Event | Receiver | Channel |
|-------|----------|---------|
| Leave request submitted | Manager | In-app + Email |
| Leave approved by manager | HRD | In-app + Email |
| Leave approved/rejected | Employee | In-app + Email |
| Leave cancelled | Manager, HRD | In-app |
| Balance adjustment | Employee | In-app |

---

## 7. SignalR Integration

### Hub: `/hubs/notifications`

| Event | Direction | Payload |
|-------|-----------|---------|
| `ReceiveNotification` | Server → Client | `{ id, title, type, referenceId }` |
| `UnreadCountUpdated` | Server → Client | `{ count: number }` |

- Frontend connects on login, disconnects on logout
- Group by UserId for targeted notifications
- Fallback to polling if WebSocket unavailable

---

## 8. Import/Export Excel

### Import (POST `/api/employees/import`)
- Accept `.xlsx` file
- Validate columns: EmployeeNo, FullName, Email, Department, Position, JoinDate
- Preview invalid rows before committing
- Transactional: all or nothing
- Duplicate detection by EmployeeNo or Email

### Export (GET `/api/employees/export`)
- Returns `.xlsx` file
- Optional filters via query params (department, status)
- Includes all employee fields

**Library:** ClosedXML (open-source, no Excel dependency)

---

## 9. Implementation Steps

### Step 1: Backend — Database
- Create migration: `Employees`, `LeaveTypes`, `LeaveBalances`, `LeaveRequests`, `LeaveApprovals`, `Notifications`
- Seed default leave types (Annual, Sick, Special, etc.)
- Seed sample employee data for testing

### Step 2: Backend — Employee Module
- Create `EmployeeController` + service + repository
- Implement CRUD with search & filter
- Implement Excel import (ClosedXML)
- Implement Excel export
- Add Mapster mapping profiles

### Step 3: Backend — Leave Module
- Create `LeaveTypeController` + service
- Create `LeaveRequestController` + service
- Implement submit/approve/reject/cancel logic
- Implement balance validation (sufficient remaining days)
- Implement multi-level approval workflow
- Implement balance update on final approval
- Create FluentValidation validators

### Step 4: Backend — Notification Module
- Create `NotificationController` + service
- Implement SignalR hub
- Trigger notifications on leave status changes
- Implement email sending (SMTP configurable)

### Step 5: Frontend — Employee Pages
- Employee list with search, filter, pagination
- Employee create/edit form
- Employee detail view
- Employee import dialog
- Employee export button

### Step 6: Frontend — Leave Pages
- Leave request form with date picker and leave type selector
- Leave balance cards on dashboard
- Leave request list with status badges
- Leave request detail with approval timeline
- Approval page for Manager/HRD with batch actions

### Step 7: Frontend — Notification System
- Connect to SignalR hub on login
- Notification bell with unread count badge
- Notification dropdown
- Notification center page

### Step 8: Frontend — Dashboards
- Employee dashboard: leave balance cards, recent requests, quick submit
- HR dashboard: employee count, pending verifications, recent activity

### Step 9: Backend — Unit Tests
- Set up xUnit test project: `AIHelpdesk.UnitTests.Modules.HR`
- Write tests for **Employee Module**:
  - `EmployeeService.CreateAsync` — creates employee, validates unique EmployeeNo & Email
  - `EmployeeService.UpdateAsync` — updates fields, validates department/position exist
  - `EmployeeService.DeleteAsync` — soft-deletes, cannot delete with active leave
  - `EmployeeService.SearchAsync` — filters by department, name, status; pagination works
  - Excel import — valid file parses correctly, invalid rows reported, duplicate detection
  - Excel export — generates correct columns, respects filters
- Write tests for **Leave Module**:
  - `LeaveRequestService.SubmitAsync` — transitions Draft→Submitted, validates balance
  - `LeaveRequestService.ApproveAsync` — Manager approval, HRD final approval
  - `LeaveRequestService.RejectAsync` — returns to previous status with reason
  - `LeaveRequestService.CancelAsync` — only allowed if not yet fully approved
  - `LeaveBalanceService` — calculates remaining days, annual reset, carry-over logic
  - Leave balance validation — insufficient balance returns validation error
  - Multi-level approval — leave > 3 days requires both Manager + HRD; ≤ 3 days Manager only
  - Sick leave rule — configurable to skip manager approval
- Write tests for **Notification Module**:
  - `NotificationService.CreateAsync` — creates notification for target user
  - `NotificationService.MarkAsReadAsync` — updates IsRead and ReadAt
  - Notification triggers — leave submitted → Manager notified; approved → Employee notified
- Write tests for **FluentValidation**:
  - Leave request: missing reason, past start date, end date before start date
  - Employee: invalid email, missing required fields, invalid phone format
  - Leave type: negative days, missing name/code
- Write tests for **Mapster mappings**:
  - Employee ↔ EmployeeDto, LeaveRequest ↔ LeaveRequestDto, etc.

### Step 10: Backend — Integration Tests
- Use `WebApplicationFactory<Program>` + Testcontainers.PostgreSQL
- Write tests for:
  - **Employee endpoints**: CRUD with auth, duplicate detection returns 409
  - **Leave request endpoints**: submit → approve → verify full workflow
  - **Leave balance update**: final approval correctly decrements balance
  - **Import endpoint**: valid Excel → 200 with count; invalid → 400 with errors
  - **Export endpoint**: returns file with correct Content-Type
  - **Notification endpoints**: create, mark read, unread count
  - **Authorization**: HRD-only endpoints blocked for Employee role
  - **Validation**: 400 responses with proper error messages

### Step 11: Frontend — Unit Tests
- Set up Vitest + React Testing Library + MSW
- Write tests for **Employee Components**:
  - `EmployeeTable` — renders rows, search filters correctly, pagination buttons work
  - `EmployeeForm` — validation errors display, submit calls API, edit mode pre-fills
  - `EmployeeImportDialog` — file selection, preview invalid rows, confirm import
- Write tests for **Leave Components**:
  - `LeaveRequestForm` — date validation (start < end), leave type selection, file upload
  - `LeaveBalanceCard` — displays correct remaining/total with progress bar colors
  - `ApprovalCard` — approve/reject buttons disabled for non-approver role
  - `ApprovalTimeline` — renders correct stages for current status
- Write tests for **Notification Components**:
  - `NotificationBell` — shows unread count badge, dropdown lists recent notifications
  - `NotificationList` — mark as read updates count, empty state message
- Write tests for **Pages**:
  - `LeaveRequestListPage` — status filter works, only own requests for Employee role
  - `LeaveApprovalPage` — only shows pending for current user's role
  - `EmployeeDashboardPage` — displays balance cards and recent requests
- Write tests for **API layer / MSW handlers**:
  - All HR API handlers return correct mock data
  - Error responses handled gracefully (toast/alert)

### Step 12: Test Automation & Coverage
- Configure Coverlet with minimum thresholds:
  - Employee module: 85%+
  - Leave module: 90%+ (critical business logic)
  - Notification module: 80%+
  - Integration tests: cover all happy + unhappy paths
  - Frontend components: 70%+
- Add `coverage` script to `package.json` with thresholds
- Set up GitHub Actions workflow to run both backend and frontend tests on PR
- Generate coverage badges for README

### Step 13: CI/CD Pipeline

Add to the existing `.github/workflows/ci.yml` from Phase 1 — extend with HR-specific test jobs:

```yaml
# Add this job to the existing ci.yml
  hr-integration:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: aihelpdesk_hr_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Restore & Build
        run: |
          dotnet restore src/AIHelpdesk.sln
          dotnet build src/AIHelpdesk.sln --configuration Release --no-restore

      - name: Run HR module unit tests
        run: |
          dotnet test tests/AIHelpdesk.UnitTests.Modules.HR/AIHelpdesk.UnitTests.Modules.HR.csproj \
            --configuration Release \
            --no-build \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage-hr-unit \
            --filter "Category=Employee|Category=Leave|Category=Notification"

      - name: Run HR integration tests
        run: |
          dotnet test tests/AIHelpdesk.IntegrationTests/AIHelpdesk.IntegrationTests.csproj \
            --configuration Release \
            --no-build \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage-hr-integration \
            --filter "Category=HRModule"
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=aihelpdesk_hr_test;Username=postgres;Password=postgres"

      - name: Upload HR coverage
        uses: actions/upload-artifact@v4
        with:
          name: hr-coverage
          path: |
            ./coverage-hr-unit
            ./coverage-hr-integration

      - name: Enforce coverage thresholds
        run: |
          # Fail if leave module coverage < 90%
          # Fail if employee module coverage < 85%
          dotnet tool install --global dotnet-coverage
          dotnet coverage merge ./coverage-hr-unit/**/coverage.cobertura.xml -o merged-hr.cobertura.xml -f cobertura
          # Parse and check thresholds (custom script or use ReportGenerator)
```

Create `.github/workflows/hr-deploy-check.yml` — deployment gate that verifies HR tests pass:

```yaml
name: HR Deploy Gate

on:
  pull_request:
    branches: [ main ]
    paths:
      - 'src/Modules/HR/**'
      - 'src/AIHelpdesk.Api/Controllers/HR/**'
      - 'frontend/src/features/employees/**'
      - 'frontend/src/features/leave/**'

jobs:
  hr-deploy-gate:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Run backend HR unit tests
        run: |
          dotnet test tests/AIHelpdesk.UnitTests.Modules.HR \
            --configuration Release \
            --filter "Category=Leave" \
            --no-restore

      - name: Run frontend HR component tests
        run: |
          cd frontend
          npx vitest run --reporter=verbose src/features/employees/
          npx vitest run --reporter=verbose src/features/leave/

      - name: Check migration safety
        run: |
          # Warn if new migrations could cause downtime
          # e.g., long-running locks, nullable column changes
          echo "Checking migration safety..."
```

#### Docker Compose Extension (`docker-compose.yml`)

Add to the existing Phase 1 docker-compose:

```yaml
  # Add to services:
  backend:
    # ... existing config ...
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M

  # New service for background jobs
  worker:
    image: ${DOCKER_USERNAME}/aihelpdesk-backend:latest
    container_name: aihelpdesk-worker
    command: dotnet AIHelpdesk.Worker.dll
    restart: unless-stopped
    depends_on:
      backend:
        condition: service_healthy
    env_file:
      - .env.production
    networks:
      - aihelpdesk-network
```

#### Environment Variables (Phase 2 additions)

Add to `.env.example`:

```env
# SMTP (Notifications)
SMTP_HOST=smtp.company.com
SMTP_PORT=587
SMTP_USER=noreply@company.com
SMTP_PASSWORD=
SMTP_FROM="AI Helpdesk <noreply@company.com>"

# Leave Configuration
LEAVE_ANNUAL_DAYS=12
LEAVE_SICK_DAYS=14
LEAVE_CARRYOVER_MAX=5
LEAVE_MIN_DAYS_FOR_HR_APPROVAL=4
LEAVE_SICK_SKIP_MANAGER=false

# SignalR
SIGNALR_BACKPLANE=InMemory  # or Redis for multi-instance
```

#### Deployment Runbook

```bash
# 1. Pull latest images
ssh user@vps "cd /opt/aihelpdesk && docker compose pull"

# 2. Backup database
ssh user@vps "docker exec aihelpdesk-db pg_dump -U postgres aihelpdesk > backup_$(date +%Y%m%d).sql"

# 3. Run database migrations
ssh user@vps "docker compose run --rm backend dotnet ef database update"

# 4. Deploy new containers
ssh user@vps "docker compose up -d --remove-orphans"

# 5. Verify health
ssh user@vps "curl -f http://localhost:8080/api/health"

# 6. Rollback if needed
ssh user@vps "docker compose down && docker compose -f docker-compose.rollback.yml up -d"
```

---

## 10. Seed Data

### Default Leave Types

| Leave Type | Code | Days/Year | Paid | Min Service |
|------------|------|:---------:|:----:|:-----------:|
| Cuti Tahunan | ANNUAL | 12 | Yes | 0 months |
| Cuti Sakit | SICK | 14 | Yes | 0 months |
| Cuti Khusus | SPECIAL | 3 | Yes | 0 months |
| Cuti Melahirkan | MATERNITY | 90 | Yes | 0 months |
| Izin Terlambat | LATE | — | No | 0 months |
| Izin Pulang Awal | EARLY | — | No | 0 months |
| Work From Home | WFH | — | Yes | 0 months |

### Sample Employees
- At least 5 employees across 3 departments
- Mix of roles: Employee, Manager, HRD
- Varied leave balances (some used, some full)

---

## 11. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | HRD can create, edit, and deactivate employees |
| 2 | HRD can import employees from Excel with validation |
| 3 | HRD can export employee list to Excel |
| 4 | Employee can see their leave balance on dashboard |
| 5 | Employee can submit a leave request |
| 6 | System validates sufficient leave balance before submit |
| 7 | Manager sees pending approvals on their dashboard |
| 8 | Manager can approve or reject with a reason |
| 9 | If leave > 3 days, HRD must also approve |
| 10 | Employee is notified when leave status changes |
| 11 | Leave balance is updated after final approval |
| 12 | Employee can cancel their own draft/submitted request |
| 13 | HRD can manually adjust leave balance |
| 14 | Notifications appear in-app (bell icon + SignalR) |
| 15 | All leave requests are trackable with status history |
| 16 | Backend unit tests for leave workflow pass with ≥90% coverage |
| 17 | Integration tests verify full submit→approve→balance-update flow |
| 18 | Frontend unit tests for all HR components pass with ≥70% coverage |

---

## 12. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| Employee module (backend) | 3 days |
| Leave module (backend) | 4 days |
| Notification module (backend) | 2 days |
| Employee pages (frontend) | 2 days |
| Leave pages (frontend) | 3 days |
| Notification UI + SignalR | 2 days |
| Dashboards | 2 days |
| Import/Export Excel | 1 day |
| Backend unit + integration tests | 3 days |
| Frontend unit tests | 2 days |
| CI/CD pipeline (HR gate, deploy extensions) | 1 day |
| **Total** | **~27 days** |

---

## 13. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| Complex approval workflow logic | Use state machine pattern (Enum + valid transitions map) |
| Excel import with malformed data | Validate all rows before first insert, show detailed errors |
| SignalR connection loss | Auto-reconnect with fallback to polling every 30s |
| Leave balance race conditions | Use database locking (optimistic concurrency with RowVersion) |
| Email delivery failures | Queue emails in database table, retry with background job |
| CI pipeline slow for HR tests | Run HR integration tests in parallel with other jobs |
| Migration issues in production | Use `dotnet ef migrations script` for review; deploy with deploy gate |
| Secrets sprawl across environments | Use single `.env.production` template + GitHub environment secrets |
| Rollback complexity | Tag Docker images with git SHA; keep last 3 compose files |
