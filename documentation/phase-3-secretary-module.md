# Phase 3 — Secretary Module

**Tech Stack:** React (TypeScript) + ASP.NET Core Web API + PostgreSQL

**Prerequisite:** Phases 1 (Foundation) + 2 (HR Admin) must be complete.

---

## 1. Overview

Phase 3 delivers the digital secretary functionality: agenda & meeting management, document/surat request workflows, action item tracking, and the secretary dashboard.

**Goal:** Secretaries can manage executive agendas, create meetings, record notes & action items, and handle document requests end-to-end.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Meeting & agenda management | Create, schedule, update meetings with participants |
| 2 | Meeting notes & minutes | Record notes, upload transcripts, AI-generated summaries |
| 3 | Action items | Track follow-ups with PIC and deadline |
| 4 | Document/surat request workflow | Request, AI draft, review, approve, generate PDF/DOCX |
| 5 | Document templates | Manage reusable letter templates |
| 6 | Secretary dashboard | Today's agenda, pending reviews, upcoming meetings, overdue action items |
| 7 | Manager dashboard extension | Approval queue, team action items, meeting schedule |

---

## 3. New Database Tables

| Table | Key Columns | Description |
|-------|-------------|-------------|
| `Meetings` | Id, Title, Date, StartTime, EndTime, OrganizerId, Location, MeetingLink, Description, Status, Notes, TranscriptUrl | Meeting records |
| `MeetingParticipants` | Id, MeetingId, EmployeeId, Role (Organizer/Presenter/Attendee), IsRequired, AttendanceStatus | Who attends |
| `MeetingNotes` | Id, MeetingId, Title, Content, CreatedBy, IsAISummary | Notes & minutes |
| `ActionItems` | Id, MeetingId, Title, Description, AssignedToId, DueDate, Priority, Status (Open/InProgress/Completed/Cancelled), CompletedAt | Follow-up tasks |
| `DocumentTemplates` | Id, Name, Code, Category, ContentTemplate, Variables (JSON), IsActive | Letter templates |
| `DocumentRequests` | Id, EmployeeId, TemplateId, Title, ContentDraft, ContentFinal, Status, LetterNumber, Notes | Document requests |
| `GeneratedDocuments` | Id, DocumentRequestId, FileName, FilePath, FileFormat (PDF/DOCX), Version, GeneratedAt | Generated output files |

All tables include: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

### DocumentRequest Status Flow

```
Draft → Submitted → AI Draft Ready → Review → Approved → Generated
                                              ↓
                                           Rejected
```

### ActionItem Status Flow
```
Open → In Progress → Completed
  ↓
Cancelled
```

---

## 4. API Endpoints

### Meetings

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/meetings` | All | List (filter by date range, status, role-based) |
| GET | `/api/meetings/{id}` | All | Detail with participants + notes + action items |
| POST | `/api/meetings` | Secretary, Manager | Create meeting |
| PUT | `/api/meetings/{id}` | Secretary, Manager | Update meeting |
| DELETE | `/api/meetings/{id}` | Secretary, Super Admin | Cancel/delete meeting |
| POST | `/api/meetings/{id}/participants` | Secretary | Add participants |
| DELETE | `/api/meetings/{id}/participants/{participantId}` | Secretary | Remove participant |
| GET | `/api/meetings/today` | All | Today's meetings for current user |
| GET | `/api/meetings/upcoming` | All | Upcoming meetings (7 days) |

### Meeting Notes

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/meetings/{id}/notes` | All participants | List notes |
| POST | `/api/meetings/{id}/notes` | Secretary, Organizer | Add note |
| PUT | `/api/meetings/{id}/notes/{noteId}` | Secretary, Organizer | Edit note |
| DELETE | `/api/meetings/{id}/notes/{noteId}` | Secretary, Organizer | Delete note |
| POST | `/api/meetings/{id}/generate-summary` | Secretary, Organizer | AI-generated summary |

### Action Items

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/action-items` | All | My action items (assigned to me) |
| GET | `/api/action-items/team` | Manager | Team action items |
| GET | `/api/action-items/overdue` | All | Overdue action items |
| POST | `/api/action-items` | Secretary, Manager | Create action item |
| PUT | `/api/action-items/{id}` | Secretary, Manager | Update action item |
| POST | `/api/action-items/{id}/complete` | Assignee | Mark complete |
| POST | `/api/action-items/{id}/cancel` | Secretary, Manager | Cancel action item |

### Document Templates

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/document-templates` | All | List templates (by category) |
| POST | `/api/document-templates` | Secretary, Super Admin | Create template |
| PUT | `/api/document-templates/{id}` | Secretary, Super Admin | Update template |
| DELETE | `/api/document-templates/{id}` | Super Admin | Archive template |

### Document Requests

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/document-requests` | All | List (role-filtered) |
| GET | `/api/document-requests/{id}` | All | Detail with generated documents |
| POST | `/api/document-requests` | Employee | Submit document request |
| PUT | `/api/document-requests/{id}` | Owner | Edit draft |
| POST | `/api/document-requests/{id}/generate-draft` | Secretary | Generate AI draft from template |
| POST | `/api/document-requests/{id}/submit-for-review` | Secretary | Submit for review |
| POST | `/api/document-requests/{id}/approve` | Manager, HRD | Approve document |
| POST | `/api/document-requests/{id}/reject` | Reviewer | Reject with reason |
| POST | `/api/document-requests/{id}/generate-final` | Secretary | Generate final PDF/DOCX |
| GET | `/api/document-requests/{id}/download` | All | Download final document |

---

## 5. Frontend Pages

| Route | Page | Access |
|-------|------|--------|
| `/meetings` | MeetingListPage | All |
| `/meetings/new` | MeetingCreatePage | Secretary, Manager |
| `/meetings/:id` | MeetingDetailPage | All (participants) |
| `/meetings/:id/edit` | MeetingEditPage | Secretary, Manager |
| `/meetings/:id/notes` | MeetingNotesPage | All participants |
| `/action-items` | ActionItemListPage | All |
| `/documents/requests` | DocumentRequestListPage | All |
| `/documents/requests/new` | DocumentRequestCreatePage | Employee |
| `/documents/requests/:id` | DocumentRequestDetailPage | All (owner/reviewer) |
| `/documents/templates` | TemplateListPage | Secretary, Super Admin |
| `/documents/templates/new` | TemplateCreatePage | Secretary, Super Admin |

### New Components

| Component | Description |
|-----------|-------------|
| `MeetingForm` | Date/time picker, location, participant selector |
| `MeetingCalendar` | Calendar view of meetings (weekly/monthly) |
| `ParticipantSelector` | Searchable employee multi-select |
| `MeetingNotesEditor` | Rich text editor for notes |
| `AISummaryButton` | Button to trigger AI summary generation |
| `ActionItemTable` | Table with status badges, PIC, deadline, priority |
| `DocumentRequestForm` | Template selector, reason, notes |
| `DocumentPreview` | PDF preview of generated document |
| `TemplateEditor` | Rich text editor with variable placeholders |
| `LetterNumberBadge` | Auto-generated letter number display |

---

## 6. Business Logic

### Document Numbering
- Format: `XXX/CompanyCode/MM/YYYY` (auto-increment per year)
- Configurable prefix per document type
- Reset counter annually

### AI Integration Points
- **Meeting Summary**: Send meeting notes to LLM → return structured summary with decisions and action items
- **Document Draft**: Select template + fill variables → LLM generates draft content
- **Action Item Extraction**: From meeting notes → LLM extracts tasks, assigns PIC, suggests deadlines

### Reminder Notifications
- Meeting reminder: 15 minutes before (in-app + email)
- Action item due: 1 day before deadline (in-app + email)
- Action item overdue: daily until completed
- Document ready for review: notify reviewer
- Document approved: notify requester

---

## 7. Implementation Steps

### Step 1: Backend — Database
- Create migration: Meetings, MeetingParticipants, MeetingNotes, ActionItems, DocumentTemplates, DocumentRequests, GeneratedDocuments
- Seed default document templates (Surat Keterangan Kerja, Surat Rekomendasi, Surat Tugas, Surat Izin, Memo Internal)

### Step 2: Backend — Meeting Module
- Create `MeetingController` + service + repository
- Implement CRUD with participant management
- Implement date conflict detection
- Implement `GET /today` and `GET /upcoming`
- Create `MeetingNotesController` + service

### Step 3: Backend — Action Item Module
- Create `ActionItemController` + service
- Implement CRUD with status transitions
- Implement overdue detection (background job via Hangfire/Quartz)

### Step 4: Backend — Document Module
- Create `DocumentTemplateController` + service
- Create `DocumentRequestController` + service
- Implement document workflow (submit → draft → review → approve → generate)
- Implement letter number auto-generation
- Implement PDF generation (QuestPDF or DinkToPdf)
- Implement DOCX generation (OpenXML)
- Integrate AI draft generation (hook into Phase 4 AI service)

### Step 5: Frontend — Meeting Pages
- Meeting list with calendar view
- Meeting create/edit form with participant selector
- Meeting detail with tabs (info, participants, notes, action items)
- Notes editor (rich text)
- AI summary trigger button
- Action item list with status management

### Step 6: Frontend — Document Pages
- Document request form with template selector
- Document request list with status badges
- Document detail with draft preview and approval actions
- Template list with create/edit modal
- Template editor with variable placeholders
- Document download button

### Step 7: Backend — Tests
- Unit: Meeting CRUD, participant management, document workflow states
- Unit: Action item status transitions, overdue detection
- Unit: Letter number generation (yearly reset, format)
- Unit: PDF/DOCX generation
- Integration: Full document workflow (request → draft → review → approve → generate → download)
- Integration: Meeting with participants CRUD

### Step 8: Frontend — Tests
- Meeting form validation (end > start, required fields)
- Participant selector search/filter
- Action item complete/cancel flow
- Document request form submits correctly
- Template editor renders variable placeholders

---

## 8. Seed Data

### Default Document Templates

| Template | Code | Category | Variables |
|----------|------|----------|-----------|
| Surat Keterangan Kerja | SKK | HR | {employee_name}, {employee_id}, {position}, {department}, {date} |
| Surat Rekomendasi | REC | HR | {employee_name}, {position}, {purpose}, {date} |
| Surat Tugas | TUG | Operasional | {employee_name}, {task}, {location}, {start_date}, {end_date} |
| Surat Izin | IZIN | HR | {employee_name}, {reason}, {date} |
| Memo Internal | MEMO | Umum | {subject}, {content}, {date}, {sender_name} |

---

## 9. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | Secretary can create meetings with multiple participants |
| 2 | Participants receive notification when added to a meeting |
| 3 | Meeting notes can be added and edited by secretary/organizer |
| 4 | AI summary generates structured output (decisions, action items) |
| 5 | Action items can be assigned, completed, and tracked |
| 6 | Overdue action items are highlighted on dashboard |
| 7 | Employee can submit a document request with template selection |
| 8 | AI generates draft content based on selected template |
| 9 | Reviewer can approve or reject document draft |
| 10 | Final document can be downloaded as PDF and DOCX |
| 11 | Letter number follows configured format with yearly reset |
| 12 | Secretary dashboard shows today's agenda, pending reviews, overdue items |

---

## 10. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| Meeting module (backend) | 4 days |
| Action item module (backend) | 2 days |
| Document module (backend) | 4 days |
| Meeting & action item pages (frontend) | 3 days |
| Document pages (frontend) | 3 days |
| Dashboards & polish | 2 days |
| Backend tests | 3 days |
| Frontend tests | 2 days |
| **Total** | **~23 days** |

---

## 11. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| PDF/DOCX formatting issues | Generate in staging first; use QuestPDF (programmatic, reliable) |
| AI summary quality varies | Provide structured prompt template; allow manual edits |
| Letter number conflicts | Use database-level unique constraint on (Code, Year) |
| Meeting scheduling conflicts | Validate overlapping meetings for same room/participants |
