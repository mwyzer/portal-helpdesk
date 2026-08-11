# Phase 5 — Ticketing Module — TODO Checklist

> For confirmed as-built behavior rather than a task checklist, see [`FSD.md`](FSD.md).
>
> **Status (2026-08-04, updated):** Core backend + 5 frontend pages committed. Since the initial audit, the following were closed: AI categorization now calls the real Phase 4 `IAIService` (with graceful fallback), a `TicketSlaBackgroundService` runs every 5 min to detect breaches/at-risk tickets and notify the assigned agent, attachment upload now validates extension/size with new download + delete endpoints, and Excel export was added (backend + frontend button). 54 backend tests now cover `TicketService`, `TicketCategoryService`, `EscalationService`, `AgentAssignmentService`, and the SLA job. Remaining real gaps: no dedicated `/start`/`/reject`/`/escalate` endpoints (generic status endpoint used instead), no formal status-transition state machine, and several frontend UI components (AI suggestion card, escalation modal, SLA countdown as a live timer, standalone reports/monitoring pages) are still missing.
>
> **Status (2026-08-07, updated):** The "ownership-level checks not fully verified" note below is now confirmed, not just suspected — `TicketsController.GetById`/`Update` have no ownership or role check at all; any authenticated user can read or edit any ticket by id. Not fixed yet at the REST layer. Separately, an MCP (Model Context Protocol) server was added at `POST /mcp` exposing `create_ticket`/`get_ticket`/`update_ticket`/`get_sla` as agent-callable tools (`src/AIHelpdesk.Api/Mcp/TicketMcpTools.cs`) — these tools do enforce ownership (submitter, assigned agent, or Agent/Manager/Super Admin role) independently of the REST gap above, since MCP tool calls don't pass through `[Authorize(Roles=...)]` action filters. See the new "Backend — MCP Agent Tools" section below.

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
- [ ] Implement permission checks (role + ownership) — `[Authorize(Roles=...)]` on all actions; ownership-level checks confirmed missing 2026-08-07: `GetById`/`Update` accept any authenticated caller regardless of whether they submitted, are assigned to, or have any staff role on the ticket. Not fixed yet

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

## Backend — MCP Agent Tools

> Added 2026-08-07, not part of the original Phase 5 plan — grew out of a design discussion on
> agentic RAG / MCP tool access for role-specific agents (HR, Recruitment, Ticket). Only the
> Ticket Agent is built so far.

- [x] Add `ModelContextProtocol.AspNetCore` (2.1.0, official SDK) to `AIHelpdesk.Api`
- [x] Host MCP server at `POST /mcp`, behind the same JWT bearer auth as the REST API (`app.MapMcp("/mcp").RequireAuthorization()` in `Program.cs`)
- [x] Create `TicketMcpTools` (`src/AIHelpdesk.Api/Mcp/TicketMcpTools.cs`) with `create_ticket`, `get_ticket`, `update_ticket`, `get_sla`
- [x] Enforce per-tool ownership scoping (submitter, assigned agent, or Agent/Manager/Super Admin role) via `IHttpContextAccessor`, independently of the REST-layer gap noted above — `get_ticket`/`update_ticket`/`get_sla` all deny with the same "Ticket not found" message for both a nonexistent id and an unauthorized one, so the tool can't be used to enumerate other users' ticket ids
- [x] `get_sla` reuses the ownership-scoped `GetByIdAsync` and projects `SLADeadline`/`SLAStatus`, rather than wrapping `GetSLAReportAsync` (which is a Manager-only, department-wide report, not a per-ticket lookup) — the tool contract only needed the latter
- [x] Verified end-to-end against a live instance (not just build-tested): `tools/list` schema, `create_ticket` actually persists, `get_ticket` as owner (allowed), as Manager/staff (allowed), as an unrelated non-staff account — Secretary — (denied), test ticket cleaned up afterward
- [ ] HR Agent (`get_employee`, `get_leave_balance`, `create_leave_request`) — designed, not built
- [ ] Recruitment Agent (`search_candidates`, `evaluate_candidate`, `get_candidate`) — designed, not built
- [ ] Fix the underlying `TicketsController.GetById`/`Update` ownership gap at the REST layer (see permission-checks note above) — MCP tools don't inherit it, but the REST endpoint itself still has it

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
