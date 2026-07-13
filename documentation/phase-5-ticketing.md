# Phase 5 — Ticketing Module

**Tech Stack:** React (TypeScript) + ASP.NET Core Web API + PostgreSQL

**Prerequisite:** Phases 1 (Foundation) + 4 (AI Chat) recommended for AI categorization.

---

## 1. Overview

Phase 5 delivers the internal helpdesk ticketing system: cross-department ticket submission, assignment, SLA tracking, comments/attachments, and AI-powered categorization & priority suggestion.

**Goal:** Employees can submit tickets to any department (HR, IT, Finance, etc.) and track resolution; agents can manage, assign, and resolve tickets with SLA awareness.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Ticket CRUD | Create, update, view, filter tickets |
| 2 | Ticket assignment | Manual assignment + auto-assignment pool |
| 3 | Comments & attachments | Threaded comments, file uploads |
| 4 | Status workflow | Open → Assigned → In Progress → Resolved → Closed |
| 5 | SLA tracking | Per-category SLA targets with breach detection |
| 6 | AI categorization | Auto-detect category & suggest priority |
| 7 | Agent dashboard | Queue, SLA breaches, performance metrics |
| 8 | Escalation management | Escalate to supervisor or another department |

---

## 3. New Database Tables

| Table | Key Columns | Description |
|-------|-------------|-------------|
| `Tickets` | Id, Title, Description, CategoryId, SubCategory, Priority (Low/Normal/High/Urgent), Status, AssignedToId, SubmittedById, DepartmentId, SLADeadline, SLAStatus, EscalatedAt, ResolvedAt, ClosedAt | Core tickets |
| `TicketCategories` | Id, Name, Description, DefaultPriority, SLAHours, DepartmentId | Ticket categories |
| `TicketComments` | Id, TicketId, AuthorId, Content, IsInternal (notes for agents only) | Comments & notes |
| `TicketAttachments` | Id, TicketId, FileName, FilePath, FileSize, ContentType, UploadedById | File attachments |
| `TicketHistory` | Id, TicketId, Field, OldValue, NewValue, ChangedById, ChangedAt | Audit trail |
| `TicketSLA` | Id, TicketId, CategoryId, TargetHours, BreachedAt, NotifiedAt | SLA tracking |
| `Escalations` | Id, TicketId, EscalatedById, AssignedToId, Reason, Status, ResolvedAt | Escalation chain |
| `AgentAssignments` | Id, UserId, DepartmentId, IsActive, MaxTickets, CurrentLoad | Agent configuration |

All tables include: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

### Ticket Status Workflow
```
Open → Assigned → In Progress → Resolved → Closed
   ↑                                ↓
   └────────── Reopened ←──────────┘
                    ↓
                Rejected
```

### Escalation Levels
```
Level 1: Ticket Agent
Level 2: Department Supervisor
Level 3: Super Admin
```

---

## 4. API Endpoints

### Tickets

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/tickets` | All | List (filtered by role, department, status, priority) |
| GET | `/api/tickets/{id}` | All (owner/agent) | Detail with comments, attachments, history |
| POST | `/api/tickets` | All | Create ticket |
| PUT | `/api/tickets/{id}` | Owner/Agent | Update ticket |
| POST | `/api/tickets/{id}/assign` | Agent, Super Admin | Assign to agent |
| POST | `/api/tickets/{id}/start` | Agent | Start progress |
| POST | `/api/tickets/{id}/resolve` | Agent | Mark resolved |
| POST | `/api/tickets/{id}/close` | Submitter | Confirm close |
| POST | `/api/tickets/{id}/reopen` | Submitter, Agent | Re-open ticket |
| POST | `/api/tickets/{id}/reject` | Agent | Reject with reason |
| POST | `/api/tickets/{id}/escalate` | Agent | Escalate to supervisor |
| POST | `/api/tickets/{id}/ai-suggest` | All | AI suggests category + priority |

### Comments & Attachments

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/tickets/{id}/comments` | All (owner/agent) | List comments |
| POST | `/api/tickets/{id}/comments` | All (owner/agent) | Add comment (with isInternal) |
| DELETE | `/api/tickets/{id}/comments/{commentId}` | Author, Admin | Delete comment |
| POST | `/api/tickets/{id}/attachments` | All (owner/agent) | Upload attachment |
| GET | `/api/tickets/{id}/attachments/{attachmentId}` | All (owner/agent) | Download attachment |
| DELETE | `/api/tickets/{id}/attachments/{attachmentId}` | Author, Admin | Delete attachment |

### Categories & Agents

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/ticket-categories` | All | List categories |
| POST | `/api/ticket-categories` | Super Admin | Create category |
| PUT | `/api/ticket-categories/{id}` | Super Admin | Update category |
| GET | `/api/agents` | Admin | List agents with load |
| POST | `/api/agents` | Admin | Configure agent |
| PUT | `/api/agents/{id}` | Admin | Update agent config |

### SLA & Reports

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/tickets/sla-breaches` | Admin, Agent | SLA breach report |
| GET | `/api/tickets/stats` | Admin, Agent | Ticket stats by status/category/agent |
| GET | `/api/tickets/my-queue` | Agent | Current agent's queue |
| GET | `/api/tickets/export` | Admin | Export to Excel |

---

## 5. Frontend Pages

| Route | Page | Access |
|-------|------|--------|
| `/tickets` | TicketListPage | All |
| `/tickets/new` | TicketCreatePage | All |
| `/tickets/:id` | TicketDetailPage | All (owner/agent) |
| `/tickets/:id/edit` | TicketEditPage | Owner, Agent |
| `/admin/tickets/categories` | CategoryManagementPage | Super Admin |
| `/admin/tickets/agents` | AgentManagementPage | Admin |
| `/admin/tickets/sla` | SLAMonitoringPage | Admin, Agent |
| `/admin/tickets/reports` | TicketReportsPage | Admin |

### New Components

| Component | Description |
|-----------|-------------|
| `TicketForm` | Create/edit form with category, priority, description |
| `TicketTable` | Filterable/sortable table with status badges |
| `TicketStatusBadge` | Color-coded status indicator |
| `PriorityIndicator` | Priority badge (Low=gray, Normal=blue, High=orange, Urgent=red) |
| `SLACountdown` | SLA timer showing remaining time |
| `CommentThread` | Threaded comments with internal agent notes |
| `FileAttachment` | Upload/drag-drop with preview |
| `TicketHistoryTimeline` | Activity log timeline |
| `AgentQueueCard` | Agent's current ticket load |
| `CategoryManager` | CRUD for categories with SLA config |
| `EscalationModal` | Escalation reason & target selector |
| `AISuggestionCard` | AI predicted category + priority (user can accept/reject) |

---

## 6. Business Logic

### SLA Calculation
- SLA deadline = `CreatedAt + SLAHours` (from `TicketCategories`)
- Background job checks every 5 minutes for breached tickets
- On breach: update `SLAStatus` to Breached, send notification to agent + supervisor

### Auto-Assignment
- Round-robin among active agents in the same department
- Respect `MaxTickets` per agent
- Skip agents at max capacity

### AI Integration (with Phase 4)
- `POST /api/tickets/{id}/ai-suggest`: Send title + description to LLM
- Receives suggested category, priority, and department
- User can accept suggestion or override
- Suggestion logged in ticket history

### Status Transition Rules
| Current | To | Who |
|---------|----|-----|
| Open | Assigned | Agent, Admin |
| Assigned | In Progress | Agent (assigned) |
| In Progress | Resolved | Agent (assigned) |
| Resolved | Closed | Submitter |
| Closed | Reopened | Submitter, Agent |
| Any (except Closed) | Rejected | Agent (with reason) |

---

## 7. Implementation Steps

### Step 1: Backend — Database
- Create migration: Tickets, TicketCategories, TicketComments, TicketAttachments, TicketHistory, TicketSLA, Escalations, AgentAssignments
- Seed default categories (HRD, Sekretariat, IT Support, Finance, General Affair, Legal, Operasional)

### Step 2: Backend — Ticket Core
- Create `TicketController` + service + repository
- Implement CRUD with status transitions
- Implement permission checks (role + ownership)
- Implement filtered listing (status, category, priority, department, date range)

### Step 3: Backend — Assignment & SLA
- Create `AssignmentService` (manual + auto-assign)
- Create `SLAService` (calculation, monitoring, breach detection)
- Implement background job (Hangfire/Quartz) for SLA monitoring
- Create `AgentAssignmentController`

### Step 4: Backend — Comments & Attachments
- Create comments CRUD with isInternal flag
- Create file upload with validation (type, size) and secure storage
- Implement download with access control

### Step 5: Backend — AI Integration
- Create `TicketAIService` (hook into Phase 4 AI service)
- Implement categorization + priority suggestion
- Store suggestions in ticket history

### Step 6: Backend — History & Reports
- Implement `TicketHistory` auto-logging via interceptor/event handler
- Create stats endpoint (aggregate queries)
- Create export to Excel (ClosedXML)

### Step 7: Frontend — Ticket Pages
- Ticket list with filters (status, category, priority, date)
- Ticket create form with AI suggestion integration
- Ticket detail with tabs: info, comments, attachments, history
- Status transition buttons (with confirmation modals)

### Step 8: Frontend — Admin Pages
- Category management (CRUD + SLA hours)
- Agent configuration
- SLA monitoring dashboard
- Reports page with charts

### Step 9: Backend — Tests
- Unit: Status transition rules (valid/invalid transitions)
- Unit: SLA calculation and breach detection
- Unit: Auto-assignment round-robin logic
- Unit: Comment isInternal access control
- Integration: Full ticket lifecycle (create → assign → progress → resolve → close)
- Integration: File upload + download

### Step 10: Frontend — Tests
- Ticket form validation
- Status transition button visibility per role
- SLA countdown display
- AI suggestion accept/reject flow
- Comment thread rendering

---

## 8. Seed Data

### Ticket Categories

| Category | Default SLA | Department |
|----------|:-----------:|------------|
| HRD | 24h | HR |
| Sekretariat | 24h | Secretary |
| IT Support | 8h | IT |
| Finance | 48h | Finance |
| General Affair | 24h | GA |
| Legal | 72h | Legal |
| Operasional | 8h | Operations |

---

## 9. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | Employee can create a ticket and select category |
| 2 | Ticket is auto-assigned to available agent in that department |
| 3 | Agent can update ticket status following workflow rules |
| 4 | Comments (internal & public) work with proper visibility |
| 5 | File attachments can be uploaded and downloaded |
| 6 | SLA targets are calculated and displayed as countdown |
| 7 | SLA breaches are detected and flagged |
| 8 | AI suggests category and priority based on ticket content |
| 9 | Escalation routes ticket to supervisor |
| 10 | Ticket history shows all changes with timestamps |
| 11 | Agent dashboard shows queue with SLA status |
| 12 | Reports can be exported to Excel |

---

## 10. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| Ticket core (backend) | 4 days |
| Assignment + SLA (backend) | 3 days |
| Comments + attachments (backend) | 2 days |
| AI integration (backend) | 1 day |
| History + reports (backend) | 2 days |
| Ticket pages (frontend) | 4 days |
| Admin pages (frontend) | 2 days |
| Backend tests | 3 days |
| Frontend tests | 2 days |
| **Total** | **~23 days** |

---

## 11. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| SLA breaches not noticed | Background monitoring + notification + dashboard highlight |
| File upload abuse | Type validation (images, PDFs only), max 10MB, scan before save |
| Agent overload | MaxTickets configurable; supervisor can reassign |
| Complex state machine bugs | Exhaustive unit tests for all status transitions |
| AI suggestions wrong | Show confidence score; user always has final say |
