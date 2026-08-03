# Phase 5 — Ticketing Module — TODO Checklist

> **Status (2026-08-04):** Core backend (entities, migration, controllers, services for tickets/categories/escalations/agent assignments) and 5 frontend pages are implemented, ahead of this checklist. All of it is **currently uncommitted** (`git status` shows it as untracked). Confirmed real gaps: AI categorization is a hardcoded stub not wired to the working Phase 4 AI service, no SLA breach background job, no attachment download/delete, no Excel export, and no tests.

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
- [ ] Implement SLA breach detection background job — confirmed: no `BackgroundService`/`IHostedService` anywhere in the codebase
- [x] Create `AgentAssignmentController`
- [x] Create `TicketSLA` entity

## Backend — Comments & Attachments

- [x] Create `TicketComment` entity
- [x] Implement comments CRUD with `isInternal` flag — flag is stored/returned; server-side filtering of internal comments by role not verified as enforced
- [x] Create `TicketAttachment` entity
- [ ] Implement file upload with validation (type, size) — upload endpoint exists but writes any file with no type/size check
- [ ] Implement file download with access control — no download endpoint exists yet
- [ ] Implement file deletion — no delete endpoint exists yet

## Backend — AI Integration

- [ ] Create `TicketAIService` (hook into Phase 4 AI service) — not hooked up
- [x] Implement POST `/api/tickets/{id}/ai-suggest` — endpoint exists as `POST /api/tickets/ai-suggestion`, but `GetAISuggestionAsync` is a **hardcoded stub** (`"AI categorization not yet configured"`), does not call the real Phase 4 `IAIService`
- [ ] Store AI suggestions in ticket history — not implemented (stub has nothing to store)

## Backend — History & Reports

- [x] Create `TicketHistory` entity
- [x] Implement auto-logging — via direct `_context.TicketHistories.Add()` calls in service methods, not an event handler/interceptor
- [x] Create stats endpoint (`GET /api/tickets/stats`)
- [x] Create SLA breach report — implemented as `GET /api/tickets/sla-report`
- [x] Create agent queue endpoint — implemented as `GET /api/tickets/queue`
- [ ] Implement Excel export — confirmed missing (no Excel/export references in `TicketsController` or `TicketService`, despite the shared `IExcelService` from Phase 2 being readily reusable)
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
- [ ] Create FileAttachment component (upload/download) — upload only; no download/delete UI (matches backend gap)
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
- [ ] Implement Excel export button — matches missing backend endpoint

## Backend Tests

- [ ] Unit: Status transition rules (valid/invalid transitions)
- [ ] Unit: SLA calculation and breach detection
- [ ] Unit: Auto-assignment round-robin logic
- [ ] Unit: Comment isInternal access control
- [ ] Unit: File validation (type, size)
- [ ] Unit: Ticket history auto-logging
- [ ] Integration: Full ticket lifecycle (create → assign → progress → resolve → close)
- [ ] Integration: File upload + download
- [ ] Integration: Comment CRUD with role checks
- [ ] Integration: Escalation flow

## Frontend Tests

- [ ] Ticket form validation
- [ ] Status transition button visibility per role
- [ ] SLA countdown display
- [ ] AI suggestion accept/reject flow
- [ ] Comment thread rendering (public vs internal)
- [ ] File upload validation
- [ ] Filter/sort functionality
