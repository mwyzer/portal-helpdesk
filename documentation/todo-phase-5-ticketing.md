# Phase 5 — Ticketing Module — TODO Checklist

## Database

- [ ] Create migration for Tickets, TicketCategories
- [ ] Create migration for TicketComments, TicketAttachments
- [ ] Create migration for TicketHistory, TicketSLA
- [ ] Create migration for Escalations, AgentAssignments
- [ ] Seed default categories (HRD, Sekretariat, IT Support, Finance, GA, Legal, Operasional)

## Backend — Ticket Core

- [ ] Create `Ticket` entity
- [ ] Create `TicketCategory` entity
- [ ] Create `TicketController` + service + repository
- [ ] Implement GET `/api/tickets` (filterable: status, category, priority, department, date range)
- [ ] Implement GET `/api/tickets/{id}` (detail with comments, attachments, history)
- [ ] Implement POST `/api/tickets`
- [ ] Implement PUT `/api/tickets/{id}`
- [ ] Implement POST `/api/tickets/{id}/assign`
- [ ] Implement POST `/api/tickets/{id}/start`
- [ ] Implement POST `/api/tickets/{id}/resolve`
- [ ] Implement POST `/api/tickets/{id}/close`
- [ ] Implement POST `/api/tickets/{id}/reopen`
- [ ] Implement POST `/api/tickets/{id}/reject`
- [ ] Implement POST `/api/tickets/{id}/escalate`
- [ ] Implement status transition validation rules
- [ ] Implement permission checks (role + ownership)

## Backend — Assignment & SLA

- [ ] Create `AssignmentService`
- [ ] Implement manual assignment
- [ ] Implement auto-assignment (round-robin)
- [ ] Create `AgentAssignment` entity
- [ ] Create `SLAService`
- [ ] Implement SLA calculation (deadline = CreatedAt + SLAHours)
- [ ] Implement SLA breach detection background job
- [ ] Create `AgentAssignmentController`
- [ ] Create `TicketSLA` entity

## Backend — Comments & Attachments

- [ ] Create `TicketComment` entity
- [ ] Implement comments CRUD with `isInternal` flag
- [ ] Create `TicketAttachment` entity
- [ ] Implement file upload with validation (type, size)
- [ ] Implement file download with access control
- [ ] Implement file deletion

## Backend — AI Integration

- [ ] Create `TicketAIService` (hook into Phase 4 AI service)
- [ ] Implement POST `/api/tickets/{id}/ai-suggest` (category + priority)
- [ ] Store AI suggestions in ticket history

## Backend — History & Reports

- [ ] Create `TicketHistory` entity
- [ ] Implement auto-logging via event handler/interceptor
- [ ] Create stats endpoint (`GET /api/tickets/stats`)
- [ ] Create SLA breach report (`GET /api/tickets/sla-breaches`)
- [ ] Create agent queue endpoint (`GET /api/tickets/my-queue`)
- [ ] Implement Excel export
- [ ] Create `TicketCategoryController` (CRUD)

## Frontend — Ticket Pages

- [ ] Create TicketListPage with filters (status, category, priority, date range)
- [ ] Create TicketTable component (sortable, filterable)
- [ ] Create TicketStatusBadge component (color-coded)
- [ ] Create PriorityIndicator component
- [ ] Create TicketCreatePage with AI suggestion card
- [ ] Create TicketForm component
- [ ] Create TicketDetailPage with tabs (info, comments, attachments, history)
- [ ] Create status transition buttons (with confirmation modals)
- [ ] Create SLACountdown component
- [ ] Create CommentThread component (internal + public)
- [ ] Create FileAttachment component (upload/download)
- [ ] Create TicketHistoryTimeline component
- [ ] Create AISuggestionCard component (accept/reject)
- [ ] Create EscalationModal component

## Frontend — Admin Pages

- [ ] Create CategoryManagementPage (CRUD + SLA hours)
- [ ] Create CategoryManager component
- [ ] Create AgentManagementPage (configure agents)
- [ ] Create AgentQueueCard component
- [ ] Create SLAMonitoringPage (breach dashboard)
- [ ] Create TicketReportsPage with charts
- [ ] Implement Excel export button

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
