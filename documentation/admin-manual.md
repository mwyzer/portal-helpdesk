# Admin Manual

For users in the **Super Admin** or **HRD** role. Covers system configuration and
administrative modules not available to Managers/Secretaries/Employees.

> Note: role checks throughout the app compare against the exact role name `"Super Admin"`
> (with a space) — this is how the role is seeded (`DbSeeder.cs`) and how it appears in your
> JWT. If you're troubleshooting a permissions issue, confirm the role assigned to a user
> in **Users** matches that spelling exactly.

---

## 1. User & Access Management

- **Users** (`/users`) — create/edit employee login accounts, assign one or more roles, and
  activate/deactivate accounts. Deactivated users can no longer log in but their historical
  data (tickets, leave requests, etc.) is preserved.
- **Roles** (`/roles`) — the five built-in roles are Super Admin, HRD, Secretary, Manager,
  Employee. You can view permissions per role here; role *names* are fixed by the seed data —
  renaming a role in this screen does not change what the frontend checks for, so avoid
  renaming the built-in five.
- **Departments** (`/departments`) — create/edit departments. Departments drive: employee
  org-chart grouping, Knowledge Base document visibility (a document scoped to a department
  is only searchable by that department's staff — see §4), and default approval routing.

## 2. Employees

`/employees` (Admin + Manager) — directory of all employees with add/edit forms (name,
employee number, department, position, reporting manager). Use **Import** to bulk-load
employees from an `.xlsx` file (must match the template's column headers; the importer
validates the file extension and a 10MB size limit before processing — malformed rows are
reported back rather than partially applied).

## 3. Leave Configuration

`/leave-types` (Admin only) — define the leave types employees can request (e.g. Annual,
Sick, Unpaid), including default annual allocation. Changes here affect all future leave
requests; existing requests keep the type they were submitted under.

## 4. Knowledge Base Administration

`/knowledge-base` — upload reference documents for the AI assistant to draw on when
answering employee questions. Each document can optionally be scoped to a single department
(leave unset for company-wide visibility); the AI chat only retrieves department-scoped
documents for employees in that same department, and PII (emails, Indonesian NIK, phone
numbers, credit-card-like numbers) is automatically redacted from any retrieved text before
it's sent to the AI provider.

`/ai/conversations` (Admin only) — read-only view of all AI chat conversations across the
organization, for oversight and quality review.

## 5. Ticketing Administration

- **Ticket Categories** (`/tickets/categories`, Admin only) — define the categories tickets
  can be filed under, each with an SLA target.
- **Agent Assignments** (`/tickets/agents`, Admin only) — view current ticket load per staff
  member and assign/reassign tickets. There is no separate "Agent" login role in this system
  — anyone with Manager, HRD, or Super Admin can act as a ticket agent.
- **Escalations** (`/tickets/escalations`, Admin + Manager) — tickets that breached or are at
  risk of breaching their SLA are surfaced here automatically by a background job that runs
  every 5 minutes; escalate manually or reassign as needed.

## 6. Document Templates

`/documents/templates` (Admin/Manager/Secretary) — manage the reusable letter templates used
to generate final PDF/DOCX documents (e.g. employment letters, reference letters) from
approved document requests. Template placeholders are filled in automatically from the
requesting employee's data when a request is approved and generated.

## 7. Recruitment Administration

`/recruitment/vacancies`, `/recruitment/candidates`, `/recruitment/interviews` (Admin +
Manager) — see the User Manual's Recruitment section for the day-to-day pipeline workflow;
Admins/Managers additionally have Publish/Close authority on vacancies and can override
candidate stage assignments.

## 8. System Health & Operations

- `GET /api/health` — returns database connectivity and disk usage status; used by uptime
  monitoring and load balancers. Not a UI page.
- For server-level administration (deploys, backups, restores, TLS renewal), see
  `documentation/deployment-runbook.md` — that's written for whoever has shell access to the
  production host, which may or may not be the same person as the in-app Super Admin.

## 9. Known Limitations to Communicate to Users

- There is no in-app audit trail of who changed what and when beyond each record's own
  `CreatedAt`/`UpdatedAt` timestamps — don't rely on the app for compliance-grade change
  history yet.
- Real-time notifications depend on a working SignalR WebSocket connection; if a user reports
  notifications only appearing after a ~30 second delay instead of instantly, check that
  `/hubs/notifications` is reachable from their network (see the SignalR proxy notes in
  `documentation/deployment-runbook.md` if this is a self-hosted deployment behind a
  restrictive reverse proxy).
