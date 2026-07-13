# 📸 Screenshots — All Phases

> **Date:** 2026-07-13  
> **Browser:** http://localhost:5173  
> **User:** Super Admin (`admin@aihelpdesk.com`)  
> **Automation:** Playwright 1.61.1 — `frontend/tests/e2e/all-phases.spec.ts` (17 tests)

---

## Phase 1 — Foundation MVP

### 1. Dashboard
**URL:** `/dashboard` | ![phase1-01-dashboard](screenshots/phase1-01-dashboard.png)

### 2. Users
**URL:** `/users` | ![phase1-02-users](screenshots/phase1-02-users.png)

### 3. Roles
**URL:** `/roles` | ![phase1-03-roles](screenshots/phase1-03-roles.png)

### 4. Departments
**URL:** `/departments` | ![phase1-04-departments](screenshots/phase1-04-departments.png)

### 5. Meetings
**URL:** `/meetings` | ![phase1-05-meetings](screenshots/phase1-05-meetings.png)

### 6. Action Items
**URL:** `/action-items` | ![phase1-06-action-items](screenshots/phase1-06-action-items.png)

### 7. Document Requests
**URL:** `/document-requests` | ![phase1-07-document-requests](screenshots/phase1-07-document-requests.png)

### 8. Document Templates
**URL:** `/document-templates` | ![phase1-08-document-templates](screenshots/phase1-08-document-templates.png)

### 9. AI Chat
**URL:** `/chat` | ![phase1-09-ai-chat](screenshots/phase1-09-ai-chat.png)

### 10. Knowledge Base
**URL:** `/knowledge-base` | ![phase1-10-knowledge-base](screenshots/phase1-10-knowledge-base.png)

### 11. Login Page
**URL:** `/login` | ![phase1-11-login](screenshots/phase1-11-login.png)

### 12. Forgot Password
**URL:** `/forgot-password` | ![phase1-12-forgot-password](screenshots/phase1-12-forgot-password.png)

### 13. Reset Password
**URL:** `/reset-password?token=test&email=test@test.com` | ![phase1-13-reset-password](screenshots/phase1-13-reset-password.png)

---

## Phase 2 — HR Administration

### 1. Employees Page
**URL:** `/employees` | ![phase2-01-employees](screenshots/phase2-01-employees.png)

| Feature | Status |
|---------|--------|
| Table with Employee No, Name, Email, Department, Position, Status, Actions | ✅ |
| Search input ("Search employees...") | ✅ |
| Add Employee button | ✅ |
| Refresh button | ✅ |

### 2. Leave Types Page
**URL:** `/leave-types` | ![phase2-02-leave-types](screenshots/phase2-02-leave-types.png)

| Feature | Status |
|---------|--------|
| All 8 seeded leave types displayed | ✅ |
| Annual Leave (12 days), Sick Leave (14 days), Special Leave (5 days) | ✅ |
| Maternity Leave (90 days), Paternity Leave (5 days) | ✅ |
| Lateness, Early Leave, Work From Home | ✅ |
| Add Type button | ✅ |
| Edit / Delete actions per row | ✅ |

### 3. Leave Requests Page
**URL:** `/leave-requests` | ![phase2-03-leave-requests](screenshots/phase2-03-leave-requests.png)

| Feature | Status |
|---------|--------|
| Stats cards (Annual 12d, Maternity 90d, Paternity 5d) | ✅ |
| Apply Leave button | ✅ |
| Leave History table (Type, Dates, Days, Reason, Status, Actions) | ✅ |

### 4. Leave Approvals Page
**URL:** `/leave-approvals` | ![phase2-04-approvals](screenshots/phase2-04-approvals.png)

| Feature | Status |
|---------|--------|
| "Pending Approvals (0)" heading | ✅ |
| Table: Employee, Type, Dates, Days, Reason, Status, Actions | ✅ |
| "No pending approvals" empty state | ✅ |

---

## Notes

- All screenshots are captured automatically via `npx playwright test` — see `frontend/tests/e2e/all-phases.spec.ts`.
- All pages load correctly with valid JWT authentication.
- Leave Requests and Approvals show empty states because admin user has no linked `Employee` record — this is expected behavior.
- To populate data, create an employee record linked to the user, then create and submit leave requests.
- Screenshots are stored in `/screenshots/` (git-ignored per `.gitignore`).
