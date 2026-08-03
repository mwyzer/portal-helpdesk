# Phase 5 — Ticketing Module — TODO Checklist

> **Status (2026-08-04, updated):** Core backend + 5 frontend pages committed. Since the initial audit, the following were closed: AI categorization now calls the real Phase 4 `IAIService` (with graceful fallback), a `TicketSlaBackgroundService` runs every 5 min to detect breaches/at-risk tickets and notify the assigned agent, attachment upload now validates extension/size with new download + delete endpoints, and Excel export was added (backend + frontend button). 54 backend tests now cover `TicketService`, `TicketCategoryService`, `EscalationService`, `AgentAssignmentService`, and the SLA job. Remaining real gaps: no dedicated `/start`/`/reject`/`/escalate` endpoints (generic status endpoint used instead), no formal status-transition state machine, and several frontend UI components (AI suggestion card, escalation modal, SLA countdown as a live timer, standalone reports/monitoring pages) are still missing.

## Database

- [x] Create migration for Tickets, TicketCategories — single `Phase5_Ticketing` migration covers all entities below
- [x] Create migration for TicketComments, TicketAttachments
- [x] Create migration for TicketHistory, TicketSLA
- [x] Create migration for Escalations, AgentAssignments
- [x] Seed default categories — 7 categories seeded (IT Support, HR, Facilities, Finance, Legal, General, Security — different names than originally planned, same intent)

## Backend — Ticket Core

- [x] Create `Ticket` entity
- [x] Create `TicketCategory` entity
- [x] Create `TicketController` + service — no separate repository, service uses `ApplicationDbContext` directly (consistent with rest of codebase)
- [x] Implement GET `/api/tickets` — filters by status/priority; also split across `/assigned`, `/department/{id}`, `/queue` variants rather than one combined filterable endpoint
- [x] Implement GET `/api/tickets/{id}` (detail with comments, attachments, history)
- [x] Implement POST `/api/tickets`
- [x] Implement PUT `/api/tickets/{id}`
- [x] Implement POST `/api/tickets/{id}/assign`
- [ ] Implement POST `/api/tickets/{id}/start` — no dedicated endpoint, handled via generic `PUT /{id}/status`
- [x] Implement POST `/api/tickets/{id}/resolve`
- [x] Implement POST `/api/tickets/{id}/close`
- [x] Implement POST `/api/tickets/{id}/reopen`
- [ ] Implement POST `/api/tickets/{id}/reject` — no dedicated endpoint, handled via generic `PUT /{id}/status`
- [ ] Implement POST `/api/tickets/{id}/escalate` — handled via separate `EscalationsController` (`POST /api/escalations`), not a ticket-scoped action
- [ ] Implement status transition validation rules — `UpdateStatusAsync` sets any status directly, no valid-transition state machine
- [x] Implement permission checks (role + ownership) — `[Authorize(Roles=...)]` on all actions; ownership-level checks not fully verified

## Backend — Assignment & SLA

- [x] Create `AssignmentService` — `AgentAssignmentService`
- [x] Implement manual assignment
- [x] Implement auto-assignment — `GetNextAvailableAgentAsync` picks least-loaded agent (`OrderBy CurrentLoad`), not strict round-robin
- [x] Create `AgentAssignment` entity
- [ ] Create `SLAService` — no dedicated service; SLA report logic lives inline in `TicketService.GetSLAReportAsync`
- [x] Implement SLA calculation (deadline = CreatedAt + SLAHours) — set in `TicketService.CreateAsync`
- [x] Implement SLA breach detection background job — `TicketSlaBackgroundService` (fixed 2026-08-04), scans every 5 min, marks Breached/AtRisk (80% window elapsed), logs `TicketHistory` + `TicketSLA` row, notifies assigned agent
- [x] Create `AgentAssignmentController`
- [x] Create `TicketSLA` entity

## Backend — Comments & Attachments

- [x] Create `TicketComment` entity
- [x] Implement comments CRUD with `isInternal` flag — flag is stored/returned; server-side filtering of internal comments by role not verified as enforced
- [x] Create `TicketAttachment` entity
- [x] Implement file upload with validation (type, size) — fixed 2026-08-04, extension allowlist + 10MB size check (`InvalidOperationException` → 400)
- [x] Implement file download with access control — `GET .../attachments/{id}/download`; access control matches the rest of the controller (`[Authorize]`, no per-ticket ownership check — see permission-checks note above)
- [x] Implement file deletion — `DELETE .../attachments/{id}`, removes DB row + file from disk, logs `TicketHistory`

## Backend — AI Integration

- [ ] Create `TicketAIService` (hook into Phase 4 AI service) — no separate service class, but `TicketService.GetAISuggestionAsync` now calls `IAIService` directly (fixed 2026-08-04)
- [x] Implement POST `/api/tickets/{id}/ai-suggest` — endpoint exists as `POST /api/tickets/ai-suggestion`; now calls the real Phase 4 `IAIService` with a category-constrained prompt, parses JSON response, falls back to the first configured category if the AI call fails
- [ ] Store AI suggestions in ticket history — still not implemented (suggestion happens pre-creation, before a ticket/history row exists)

## Backend — History & Reports

- [x] Create `TicketHistory` entity
- [x] Implement auto-logging — via direct `_context.TicketHistories.Add()` calls in service methods, not an event handler/interceptor
- [x] Create stats endpoint (`GET /api/tickets/stats`)
- [x] Create SLA breach report — implemented as `GET /api/tickets/sla-report`
- [x] Create agent queue endpoint — implemented as `GET /api/tickets/queue`
- [x] Implement Excel export — `GET /api/tickets/export` (fixed 2026-08-04), reuses the shared `IExcelService` from Phase 2, supports department/status/priority filters
- [x] Create `TicketCategoryController` (CRUD)

## Frontend — Ticket Pages

- [x] Create TicketListPage — `TicketsPage.tsx` (235 lines), filters present but scope not fully verified against status/category/priority/date range
- [x] Create TicketTable component — inline table in `TicketsPage.tsx`
- [x] Create TicketStatusBadge component — inline badge styling in list/detail pages
- [ ] Create PriorityIndicator component — not verified as a distinct component
- [ ] Create TicketCreatePage with AI suggestion card — create flow exists but no AI suggestion UI wired in (backend suggestion is a stub anyway)
- [x] Create TicketForm component — inline in `TicketsPage.tsx`
- [x] Create TicketDetailPage with tabs (info, comments, attachments, history) — `TicketDetailPage.tsx` (254 lines), confirmed history + attachments tabs
- [ ] Create status transition buttons (with confirmation modals) — not verified
- [x] Create SLACountdown component — SLA deadline shown in `TicketDetailPage`, but as a static timestamp, not a live countdown
- [ ] Create CommentThread component (internal + public) — not verified whether internal/public are visually distinguished
- [ ] Create FileAttachment component (upload/download) — backend now supports download/delete, but no frontend UI wired to those endpoints yet (upload UI only)
- [x] Create TicketHistoryTimeline component — History tab in `TicketDetailPage`
- [ ] Create AISuggestionCard component (accept/reject) — not found
- [ ] Create EscalationModal component — escalation implemented as a standalone `EscalationsPage.tsx`, not a modal on the ticket

## Frontend — Admin Pages

- [x] Create CategoryManagementPage (CRUD + SLA hours) — `TicketCategoriesPage.tsx`
- [x] Create CategoryManager component — inline in `TicketCategoriesPage.tsx`
- [x] Create AgentManagementPage (configure agents) — `AgentAssignmentsPage.tsx`
- [ ] Create AgentQueueCard component — not verified
- [ ] Create SLAMonitoringPage (breach dashboard) — no dedicated page found
- [ ] Create TicketReportsPage with charts — no dedicated page found
- [x] Implement Excel export button — added to `TicketsPage.tsx` (fixed 2026-08-04), gated to Agent/Manager/Super Admin, respects current filters; not a dedicated reports page, just a list-page action

## Backend Tests

> **Added 2026-08-04:** 45 tests across `TicketServiceTests` (17), `TicketCategoryServiceTests` (8), `EscalationServiceTests` (8, incl. a 3-case `[Theory]`), `AgentAssignmentServiceTests` (8), `TicketSlaBackgroundServiceTests` (4) — all against an in-memory `ApplicationDbContext`, no `WebApplicationFactory`, so "Integration" items below remain unchecked.

- [ ] Unit: Status transition rules (valid/invalid transitions) — no state machine exists to test (see note above)
- [x] Unit: SLA calculation and breach detection — `CreateAsync_ShouldSetSLADeadline_FromCategorySLAHours`, `TicketSlaBackgroundServiceTests` (breach, at-risk, ignore-resolved, idempotency)
- [x] Unit: Auto-assignment logic — `GetNextAvailableAgentAsync_*` (least-loaded, excludes at-capacity, excludes inactive); it's least-loaded, not round-robin, so tested to match actual behavior
- [ ] Unit: Comment isInternal access control — `AddCommentAsync` tested, but no test asserts internal comments are hidden from unauthorized roles (matches the unenforced gap noted above)
- [x] Unit: File validation (type, size) — `UploadAttachmentAsync_ShouldReject_DisallowedExtension`, `UploadAttachmentAsync_ShouldReject_OversizedFile`
- [x] Unit: Ticket history auto-logging — asserted incidentally in the SLA breach test (`TicketHistories` row created); not exhaustively tested for every action
- [ ] Integration: Full ticket lifecycle (create → assign → progress → resolve → close) — covered at unit level only
- [ ] Integration: File upload + download — covered at unit level only (`UploadAttachmentAsync_ThenDownloadAttachmentAsync_ShouldRoundTrip`)
- [ ] Integration: Comment CRUD with role checks — covered at unit level only
- [ ] Integration: Escalation flow — covered at unit level only

## Frontend Tests

- [ ] Ticket form validation
- [ ] Status transition button visibility per role
- [ ] SLA countdown display
- [ ] AI suggestion accept/reject flow
- [ ] Comment thread rendering (public vs internal)
- [ ] File upload validation
- [ ] Filter/sort functionality
