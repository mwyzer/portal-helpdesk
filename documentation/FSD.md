# Functional Specification Document (FSD)

## AI Helpdesk — Digital Secretary & HR Assistant

| | |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-08-11 |
| **Status** | Reflects the system as built (Phases 1–8 implemented; Phase 7 hardening partially complete) |
| **Scope of this document** | Functional behavior only. For data model, API contracts, and tech stack see [`README.md`](../README.md); for a narrative build history see [`LOG.md`](../LOG.md) |

This document describes **what the system actually does today**, verified against the current
codebase rather than the original proposal. Where the pre-development plan
([`project-scope.md`](project-scope.md)) or the phase planning docs
(`documentation/phase-*.md`) described something that was never implemented, or was implemented
differently, this document follows the code and calls out the discrepancy in
[§10 Known Gaps](#10-known-gaps-vs-original-planning-docs) rather than silently repeating it.

---

## 1. Purpose

Define the functional requirements of AI Helpdesk — an internal application combining a
**digital secretary** (agendas, meetings, letters) and an **HR assistant** (employee data, leave,
recruitment), with an **AI chat/RAG layer** and an internal **helpdesk ticketing system** — to a
level of detail sufficient for QA test design, onboarding new engineers, and stakeholder
sign-off.

## 2. System Overview

Two client surfaces talk to one ASP.NET Core Web API backed by one PostgreSQL database (with
pgvector for embeddings):

- **Staff React SPA** (`frontend/`) — the default JWT audience (`AIHelpdesk`), used by all five
  internal roles.
- **Candidate Portal** (`/portal/*` routes, same SPA bundle) — a separate JWT audience
  (`AIHelpdesk-CandidatePortal`) issued to `CandidateAccount` records, structurally unable to
  satisfy `[Authorize]` on any staff endpoint.

Full entity-relationship diagrams and the complete REST API surface are in
[`README.md`](../README.md#data--architecture-diagrams) and are not duplicated here; this
document instead describes **behavior** — workflows, state machines, validation rules, and who
is allowed to do what.

## 3. Actors & Roles

| Role | Seeded demo account | Distinguishing capability |
|---|---|---|
| **Super Admin** | `admin@aihelpdesk.com` | Full access: users, roles/permissions, departments, positions, AI usage stats, KB document deletion |
| **HRD** | `hrd@aihelpdesk.com` | Employee data, leave type/balance administration, HR-stage leave approval, recruitment, KB upload |
| **Secretary** | `secretary@aihelpdesk.com` | Meetings, document templates & requests, letter generation |
| **Manager** | `manager@aihelpdesk.com` | Approves direct reports' leave requests, views team dashboards, can act as a ticket agent/supervisor |
| **Employee** | `employee@aihelpdesk.com` | Self-service: own leave, own tickets, AI chat, document requests, meeting participation |
| **Candidate** *(portal-only)* | provisioned per-candidate via a one-time setup link | No role/permission concept — a single flat identity scoped to `/api/candidate-portal/*`: check application status, upload/view own documents, book interview slots |

There is **no separate "Agent" role**. Any user with the Manager, HRD, or Super Admin role can be
assigned as a ticket agent via `AgentAssignment`; ticket visibility/authorization is enforced by
being the submitter, the assigned agent, or holding one of those three roles — not by a distinct
role name.

---

## 4. Functional Requirements

Each requirement is grounded in the corresponding service class so it can be traced back to code
during test design.

### 4.1 Authentication & Access Control (`AuthController`, `UsersController`, `RolesController`)

| ID | Requirement |
|---|---|
| FR-AUTH-1 | Users authenticate with email + password; a successful login returns a short-lived JWT access token plus a rotating refresh token. |
| FR-AUTH-2 | `POST /api/auth/refresh-token` issues a new access token and rotates the refresh token; a revoked/expired refresh token is rejected. |
| FR-AUTH-3 | Users can request a password reset link (`forgot-password`) and complete it with a one-time token (`reset-password`), or change their password while logged in. |
| FR-AUTH-4 | Access to every non-public endpoint requires a valid JWT; role-restricted endpoints additionally check `[Authorize(Roles=...)]` or a granular permission via `RolePermissions`. |
| FR-AUTH-5 | Super Admin manages users (create/update/deactivate/reactivate, soft delete), roles, and per-role permission assignment; departments and positions are managed the same way, with GET endpoints relaxed to any authenticated user. |
| FR-AUTH-6 | Role names are matched **verbatim, including the space** in `"Super Admin"` — role checks are name-based, not ID-based, both in backend authorization attributes and the frontend `RoleGuard`. |

### 4.2 HR & Leave Management (`EmployeeService`, `LeaveRequestService`, `LeaveBalanceService`)

| ID | Requirement |
|---|---|
| FR-HR-1 | HRD/Super Admin manage employee records (CRUD, search/filter by department/status), including bulk import/export via Excel. `Employee` is distinct from `ApplicationUser` — an employee may have no login, and a login may have no employee record. |
| FR-HR-2 | Each `LeaveType` configures `DaysPerYear`, `IsPaid`, `MinServiceMonths`, `RequiresAttachment`, and `SkipManagerApproval`. Seeded types: Annual (12d), Sick (14d, attachment required, **skips manager approval — goes straight to HR**), Special (5d), Maternity (90d, 12 months min. service), Paternity (5d, 6 months), Lateness/Early Leave (0d, unpaid tracking only), Work From Home (0d, paid). |
| FR-HR-3 | **Leave request workflow:** `Draft → Submitted → WaitingForManager → WaitingForHR → Approved` (or `Rejected`/`Cancelled` at applicable points). A request whose leave type has `SkipManagerApproval=true` skips straight to `WaitingForHR` on submit. |
| FR-HR-4 | **Approval routing rule:** at the Manager step, a request of **≤3 days is approved outright** (no HR step); a request of **>3 days moves to `WaitingForHR`** for a second approval. This is evaluated in code (`LeaveRequestService`), not via configuration. |
| FR-HR-5 | Submitting a request validates the employee's remaining balance for that leave type/year (`RemainingDays = TotalDays − UsedDays − PendingDays`) and reserves the days as `PendingDays`; final approval moves them to `UsedDays`; rejection/cancellation releases the reservation. |
| FR-HR-6 | Only the employee's actual manager (`Employee.ManagerId` → `ApplicationUser`) can approve/reject at the Manager step; only an HRD/Super Admin can act at the HR step. |
| FR-HR-7 | Approvers and the submitting employee are notified in-app (and via SignalR push) at submission, approval, and rejection. |

### 4.3 Secretary — Meetings & Documents (`MeetingService`, `DocumentService`, `ActionItemService`)

| ID | Requirement |
|---|---|
| FR-SEC-1 | Any staff user can create a meeting (title, date, organizer, participants); participants can be added/removed and their attendance tracked (`Pending/Accepted/Declined/Attended/Absent`). |
| FR-SEC-2 | Meeting notes can be logged manually, or generated from notes by AI (`POST /api/meetings/{id}/generate-summary`, flagged `IsAISummary=true`). |
| FR-SEC-3 | Action items belong to a meeting, have an assignee, priority, and due date, and progress `Open → InProgress → Completed` (or `Cancelled`). Overdue items are surfaced on the assignee's and their manager's/team dashboard. |
| FR-SEC-4 | Document requests use a configured `DocumentTemplate` (with placeholder fields) and progress `Draft → Submitted → AiDraftReady → Review → Approved → Generated` (or `Rejected`). AI only ever produces the `AiDraftReady` draft; a human must move it through `Review`/`Approved` before `GenerateFinalAsync` produces the official file. |
| FR-SEC-5 | On final generation, the system assigns a sequential **letter number** in the form `{seq:D3}/{TemplateCode}/MGR/{year}` — `seq` is one more than the count of existing letter numbers already ending in `/{year}`, so numbering restarts each calendar year — and renders both a PDF and a DOCX via `LetterDocumentGenerator`. |

### 4.4 AI Chat & Knowledge Base (`ChatService`, `KnowledgeBaseService`, `AIService`)

| ID | Requirement |
|---|---|
| FR-AI-1 | Any authenticated user can converse with the AI assistant (`POST /api/ai/chat` or the streaming `/api/ai/chat/stream`, SSE). Each conversation is a `ChatSession` (`Active/Resolved/Escalated`) containing ordered `ChatMessage`s. |
| FR-AI-2 | Every assistant reply retrieves relevant context from `KnowledgeChunk`s via pgvector cosine similarity search (HNSW index), scoped to the caller's department where the source document was department-restricted, before calling the LLM — a Retrieval-Augmented Generation (RAG) pattern. |
| FR-AI-3 | Each assistant message records a linked `AIResponse` (1:1) with the actual configured model (`AIOptions.ChatModel`, e.g. `gpt-4o-mini`), estimated prompt/completion/total tokens, and latency, for cost/usage tracking (`GET /api/ai/usage`, Super Admin only). |
| FR-AI-4 | Users can rate a response (thumbs up/down feedback) and escalate a conversation to a human agent, which moves the session to `Escalated` and notifies staff — the AI never resolves the escalation itself. |
| FR-AI-5 | Secretary/HRD/Super Admin can upload PDF/DOCX/TXT documents (max 20 MB) into the Knowledge Base, optionally scoped to a department (`null` = global/visible to all). Uploading triggers indexing: extract text → chunk → embed → store. |
| FR-AI-6 | A `KnowledgeDocument` moves `Pending → Indexing → Ready`, or `Failed` with a stored `ErrorMessage` if extraction yields no usable text (e.g. a scanned PDF with no text layer) or the embedding call errors — it is never silently indexed as empty/placeholder content. |
| FR-AI-7 | PDF text extraction uses real parsing (PdfPig) for both KB indexing and CV summarization (§4.6), not a heuristic byte scan — this matters because most real-world PDFs (Word/Docs/Canva exports) use compressed content streams that a naive scanner cannot read at all. |
| FR-AI-8 | `AIOptions` supports pointing chat completions and embeddings at **different providers/endpoints/keys** (`EmbeddingEndpoint`/`EmbeddingApiKey`, falling back to the main `Endpoint`/`ApiKey`), so a chat-only provider without an embeddings API can be paired with one that has it. |
| FR-AI-9 | AI chat requests are rate-limited per user (`AIOptions.RateLimit.MaxRequestsPerMinute`, default 30/min) and subject to a configurable budget (`AIOptions.Budget`). |
| FR-AI-10 | AI-generated content is never presented as a final action: it drafts letters, summarizes meetings/CVs, suggests ticket category/priority, and generates interview questions — a human always reviews/approves/accepts before anything becomes official (see also §5 AI guardrails). |
| FR-AI-11 | Before the retrieved KB context is sent to the AI provider, `ChatService` runs it through `PiiRedactor` — a regex-based, best-effort redactor (not a dedicated "guardrail" module) that masks email addresses, Indonesian 16-digit NIK numbers, Indonesian mobile numbers, and credit-card-like digit sequences. It is applied only in the chat/RAG path, not to KB indexing or CV summarization. |

### 4.5 Ticketing & SLA (`TicketService`, `TicketSlaBackgroundService`, `EscalationService`)

| ID | Requirement |
|---|---|
| FR-TIX-1 | Any user can create a ticket (title, description, category, priority) or accept an AI-suggested category/priority (`POST /api/tickets/ai-suggestion`) before submitting. |
| FR-TIX-2 | **Status flow:** `Open → Assigned → InProgress → Resolved → Closed`, with `Reopened` looping back from `Closed`, and `Rejected` reachable with a reason. |
| FR-TIX-3 | Each `TicketCategory` carries a default priority and an `SLAHours` target (seed values: IT Support 8h, HR 24h, Facilities 48h, Finance 24h, Legal 72h, General 48h, Security 4h); some categories are department-scoped. |
| FR-TIX-4 | A ticket's SLA deadline is `CreatedAt + Category.SLAHours`. `TicketSlaBackgroundService` periodically scans open tickets and marks breaches (`SLAStatus = Breached`), driving the SLA compliance report (`GET /api/tickets/sla-report`, Manager/Super Admin). |
| FR-TIX-5 | Assignment is **manual**: a Manager/Super Admin/agent assigns a ticket to a specific agent (`POST /api/tickets/{id}/assign`); `AgentAssignment.CurrentLoad`/`MaxTickets` tracks per-agent capacity for the queue view but assignment itself is not automated. |
| FR-TIX-6 | Comments can be internal (staff-only, `IsInternal=true`) or visible to the submitter; attachments are limited to 10 MB. |
| FR-TIX-7 | **Escalation is single-level**, not a fixed hierarchy: any staff user escalates a ticket to a chosen assignee (`AssignedToId`) with a reason; the target then `Accept`s, `Resolve`s, or `Decline`s it (`EscalationStatus: Pending → Accepted/Declined`, then `Resolved`). |
| FR-TIX-8 | Tickets can be exported to Excel (`GET /api/tickets/export`) with filtering. |

### 4.6 Recruitment (`CandidateService`, `RecruitmentAIService`, `InterviewService`, `JobVacancyService`)

| ID | Requirement |
|---|---|
| FR-REC-1 | Staff create/publish/close job vacancies (`Draft → Published → Closed`, plus a `Filled` status reachable off `Published`); a vacancy tracks `OpeningsCount`. |
| FR-REC-2 | Candidates are recorded per vacancy with a CV upload (PDF/DOCX, max 5 MB); a CV can later be deleted (`DELETE /api/candidates/{id}/cv/{documentId}`, removing both the DB record and the file on disk). |
| FR-REC-3 | **Pipeline is strictly forward, one stage at a time:** `Applied → Screening → Test → Interview → Offering → Hired`. `AdvanceStageAsync` only ever moves a candidate to the *next* entry in that fixed order — stages cannot be skipped. `Rejected` is reachable from any non-terminal stage (not part of the forward sequence) but not from `Hired` or an already-`Rejected` candidate. Every transition is logged to `CandidateStageHistory`. |
| FR-REC-4 | AI CV summarization (`POST /api/candidates/{id}/ai-summarize`) extracts a structured summary (skills, experience, education, etc.) into `Candidate.AISummaryJson`. If the CV has no extractable text (e.g. a scanned image), the endpoint returns an explicit "could not extract readable text" result rather than letting the LLM fabricate a plausible-looking summary from nothing. |
| FR-REC-5 | AI candidate–vacancy matching (`POST /api/job-vacancies/{id}/ai-match`) scores candidates against the vacancy's stated requirements. AI interview question generation (`POST /api/interviews/{id}/ai-questions`) produces role-specific questions from the job title/requirements/CV summary — the interviewer chooses which to actually ask. |
| FR-REC-6 | Interview scheduling validates the interviewer is not double-booked for an overlapping time window (`InterviewService.EnsureNoConflictAsync`) — enforced both for staff-created interviews and for candidate-booked `InterviewSlot`s. |
| FR-REC-7 | Completing an interview records a rating and one of four recommendations (`StrongYes/Yes/No/StrongNo`). |
| FR-REC-8 | AI never accepts, rejects, or hires a candidate — `AdvanceStageAsync`/`RejectAsync` are explicit staff actions; AI output is advisory only. |

### 4.7 Candidate Portal (external, `/api/candidate-portal/*`)

| ID | Requirement |
|---|---|
| FR-PORTAL-1 | A candidate account (`CandidateAccount`, 1:1 with `Candidate`) is provisioned automatically when staff create/invite a candidate, with a single-use `SetupToken` the candidate uses to activate and set a password. There is no SMTP configured — staff currently copy the activation link from the candidate detail page rather than it being emailed automatically. |
| FR-PORTAL-2 | Candidate login issues a JWT under the **`CandidatePortal` scheme** — same signing key, different audience (`AIHelpdesk-CandidatePortal`) than staff tokens — so a candidate token cannot satisfy `[Authorize]` on any internal (staff) endpoint, and a staff token cannot satisfy the candidate-portal endpoints. Verified by manual cross-token testing (see [`README.md`](../README.md#test-coverage)). |
| FR-PORTAL-3 | Candidates can view their own application status/stage, upload/view their own documents (`CandidateDocument.UploadedById = null` distinguishes portal self-uploads from staff uploads), and book an open `InterviewSlot` for their vacancy. Booking is transactional: it re-checks the slot is still `Open` before converting it to a confirmed `Interview` (race-safe against two candidates booking the same slot) and reuses the same interviewer double-booking check as staff-created interviews. |
| FR-PORTAL-4 | The candidate portal has no role/permission concept — one flat authenticated identity, scoped only to that candidate's own records. |

### 4.8 Notifications (`NotificationService`, `NotificationHub`)

| ID | Requirement |
|---|---|
| FR-NOTIF-1 | In-app notifications are created for leave submission/approval/rejection, ticket comments, meeting/action-item reminders, and candidate-stage changes, and are pushed in real time over SignalR (`/hubs/notifications`) to the affected user, in addition to being persisted and listable/markable-read via REST. |
| FR-NOTIF-2 | The SignalR client shares a single connection per browser tab: concurrent mount points (e.g. the app shell and the notification bell) await one shared in-flight connect promise instead of each independently opening a `HubConnection` for the same user. |

---

## 5. AI Behavior Rules (cross-cutting)

These hold across every AI-touching feature (chat, meeting summaries, letter drafts, CV
summaries/matching, interview questions, ticket categorization):

1. AI drafts, suggests, or summarizes — it never finalizes an administrative decision (leave
   approval, document approval, candidate hire/reject, ticket resolution) without an explicit
   human action.
2. Chat answers are grounded in retrieved Knowledge Base content (RAG); there is no bare
   general-knowledge chat mode exposed to end users.
3. An AI response that cannot extract usable source content (empty CV, empty/scanned document)
   returns an explicit "could not read this" result rather than fabricating a plausible-sounding
   answer — enforced in both `RecruitmentAIService` and `KnowledgeBaseService`.
4. Every assistant chat response is attributed to the actually-configured model
   (`AIResponse.ModelUsed`) and logged with token/latency metrics for audit and cost tracking.
5. AI usage is rate-limited per user (default 30 requests/minute) and subject to a configurable
   budget ceiling.

---

## 6. Non-Functional Requirements

See [`README.md` — Phase 7](../README.md#phase-7--hardening--production-deployment) for the full
hardening checklist and current status. Summary of what's actually in place today:

- **Auth:** JWT access + rotating refresh tokens, ASP.NET Core Identity password hashing, dual
  JWT schemes isolating the candidate portal from staff endpoints.
- **Transport:** HTTPS/HSTS/CSP headers, CORS restricted to a single configured origin
  (`http://localhost:5173` in the current environment config).
- **Rate limiting:** a general per-user request limiter (300 req/min after load-testing raised it
  from an initial 100) plus a separate 30 req/min AI-specific limiter.
- **File upload limits:** enforced both at the reverse proxy (`client_max_body_size 20m` in
  `nginx.conf`) and per-endpoint (`[RequestSizeLimit]` — 20 MB KB documents, 10 MB ticket
  attachments, 5 MB CVs).
- **Testing:** 301 backend xUnit tests + 57 Playwright E2E tests passing as of the last full run
  (2026-08-05); see [`test-coverage-report.md`](../test-coverage-report.md).
- **Not yet in place:** application metrics/alerting, a staging environment with CI/CD approval
  gates, and formal UAT — these require a live staging/production environment that doesn't exist
  yet.

## 7. Data Requirements

Not duplicated here — see the six grouped Entity Relationship Diagrams (Identity & Access; HR &
Leave; Secretary; AI Chat & Knowledge Base; Ticketing; Recruitment & Candidate Portal) in
[`README.md`](../README.md#entity-relationship-diagrams).

## 8. Out of Scope

Carried over unchanged from the original planning scope — none of the following exist in the
current system: full payroll processing, tax calculation, BPJS integration, biometric attendance
hardware, full performance-appraisal workflows, a learning management system, automated video
interviewing, an official WhatsApp chatbot, a voice assistant, a native mobile app, or
multi-tenant support.

## 9. Traceability

| Module | Design/planning doc | Backend service(s) |
|---|---|---|
| Candidate Portal | [`context-candidate.md`](context-candidate.md) | `CandidatePortalAuthService`, `CandidatePortalService`, `InterviewService` |
| Deployment/Infra | [`deployment-runbook.md`](deployment-runbook.md) | — |
| E2E test strategy | [`e2e-testing.md`](e2e-testing.md) | — |
| Original MVP scope (Indonesian) | [`project-scope.md`](project-scope.md) | — |
| Per-phase build notes | `phase-1-foundation-mvp.md` … `phase-7-hardening-deployment.md` | — |

## 10. Known Gaps vs. Original Planning Docs

The pre-development planning docs (`project-scope.md`, `phase-*.md`) describe some behavior more
ambitiously than what was actually built. Documented here so nobody designs a test case or a new
feature assuming these exist:

| Described in planning docs | Actual state |
|---|---|
| A dedicated `AIGuardrailService` performing permission-aware answers + PII redaction | No class by that name exists, but the PII-redaction half is real: `PiiRedactor` (regex-based) strips emails/NIK/phone/card numbers from KB context in `ChatService` before it reaches the LLM — see FR-AI-11. There is no separate permission-aware-answer guardrail layer beyond department-scoped KB retrieval. |
| Three-level ticket escalation (Agent → Supervisor → Super Admin), auto-populated from SLA breaches | `EscalationService` implements a single-level escalate → accept/resolve/decline flow to one chosen assignee, not a fixed hierarchy, and it is **never triggered automatically** — `TicketSlaBackgroundService` (every 5 min) only flips `SLAStatus`/notifies the agent on breach, it does not create an `Escalation` row. Escalating is always an explicit staff action. |
| Round-robin auto-assignment of tickets by department capacity | Assignment is a manual, explicit action (`AssignAgentAsync`); `AgentAssignment.CurrentLoad` tracks capacity for display but nothing assigns automatically. |
| Leave rules configurable via environment variables (e.g. `LEAVE_MIN_DAYS_FOR_HR_APPROVAL`, `LEAVE_SICK_SKIP_MANAGER`, `LEAVE_CARRYOVER_MAX`) | The ≤3-day/>3-day HR-approval threshold is a hardcoded constant in `LeaveRequestService`; `SkipManagerApproval` is a per-`LeaveType` database flag (seeded true for Sick Leave), not an env var; there is no leave-carryover implementation at all. |
| SMTP/email notifications for candidate activation and leave events | Not configured; candidate activation links are surfaced in the UI for staff to copy/share manually, and notifications are in-app + SignalR only. |
| Formal audit log module / `AuditLogs` table | Not implemented — only `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy` on each row; no dedicated audit trail. |

---

*Questions about a specific workflow's edge cases should be resolved by reading the cited service
class directly — this document summarizes verified behavior but the code is the final source of
truth.*
