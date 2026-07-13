# Phase 3 — Secretary Module — TODO Checklist

## Meeting & Agenda — Backend

- [x] Create `Meeting` entity
- [x] Create `MeetingParticipant` entity
- [x] Create `MeetingNote` entity
- [x] Create `MeetingController` + service + repository
- [x] Implement GET `/api/meetings` (filter by date range, status)
- [x] Implement GET `/api/meetings/{id}` (with participants + notes + action items)
- [x] Implement POST `/api/meetings`
- [x] Implement PUT `/api/meetings/{id}`
- [x] Implement DELETE `/api/meetings/{id}`
- [x] Implement POST `/api/meetings/{id}/participants`
- [x] Implement DELETE `/api/meetings/{id}/participants/{participantId}`
- [x] Implement GET `/api/meetings/today`
- [x] Implement GET `/api/meetings/upcoming`
- [x] Implement date conflict detection
- [x] Implement meeting notes CRUD (in MeetingController)
- [ ] Implement POST `/api/meetings/{id}/generate-summary` (AI hook)

## Action Items — Backend

- [x] Create `ActionItem` entity
- [x] Create `ActionItemController` + service
- [x] Implement action item CRUD
- [x] Implement POST `/api/action-items/{id}/complete`
- [x] Implement POST `/api/action-items/{id}/cancel`
- [x] Implement GET `/api/action-items/overdue`
- [x] Implement GET `/api/action-items/team` (manager)
- [ ] Implement overdue detection background job
- [ ] Implement due-date reminder notification

## Document Module — Backend

- [x] Create `DocumentTemplate` entity
- [x] Create `DocumentRequest` entity
- [x] Create `GeneratedDocument` entity
- [x] Create `DocumentTemplateController` + service
- [x] Create `DocumentRequestController` + service
- [x] Implement template CRUD
- [x] Implement document request CRUD
- [x] Implement POST `/api/document-requests/{id}/generate-draft`
- [x] Implement POST `/api/document-requests/{id}/submit-for-review`
- [x] Implement POST `/api/document-requests/{id}/approve`
- [x] Implement POST `/api/document-requests/{id}/reject`
- [x] Implement POST `/api/document-requests/{id}/generate-final`
- [x] Implement GET `/api/document-requests/{id}/download`
- [x] Implement letter number auto-generation (yearly counter)
- [ ] Implement PDF generation (QuestPDF)
- [ ] Implement DOCX generation (OpenXML)
- [ ] Integrate AI draft generation

## Database

- [x] Create migration (Phase3_SecretaryModule — all entities in one migration)
- [ ] Seed default document templates (5 templates)

## Frontend — Meeting Pages

- [x] Create MeetingListPage with filters (date range, status)
- [ ] Create MeetingCalendar component (weekly/monthly view)
- [ ] Create MeetingCreatePage with form (date, time, location, participants)
- [ ] Create MeetingDetailPage with tabs (info, participants, notes, action items)
- [ ] Create MeetingEditPage
- [ ] Create ParticipantSelector component (searchable employee multi-select)
- [ ] Create MeetingNotesPage with rich text editor
- [ ] Create AISummaryButton component
- [ ] Meeting form validation (end > start, required fields)
- [ ] Create MeetingNotesEditor component

## Frontend — Action Items

- [x] Create ActionItemListPage (my action items)
- [x] ActionItemTable component (status, PIC, deadline, priority)
- [x] Implement complete/cancel actions
- [x] Show overdue items highlighted

## Frontend — Document Pages

- [x] Create DocumentRequestListPage with status badges
- [ ] Create DocumentRequestCreatePage with template selector form
- [ ] Create DocumentDetailPage (draft preview, approval actions)
- [x] Create TemplateListPage
- [ ] Create TemplateCreatePage with variable placeholder editor
- [ ] Create TemplateEditor component
- [ ] Create DocumentPreview component (PDF preview)
- [ ] Create DocumentRequestForm component
- [x] Create download button component
- [x] Implement letter number badge display

## Dashboards

- [ ] Create Secretary dashboard page (today's agenda, pending reviews, upcoming meetings, overdue action items)
- [ ] Extend manager dashboard (approval queue, team action items)

## Backend Tests

- [ ] Unit: Meeting CRUD
- [ ] Unit: Meeting participant management
- [ ] Unit: Meeting date conflict detection
- [ ] Unit: Action item status transitions
- [ ] Unit: Overdue detection
- [ ] Unit: Document workflow states (submit → draft → review → approve → generate)
- [ ] Unit: Letter number generation (format, yearly reset)
- [ ] Unit: PDF generation
- [ ] Unit: DOCX generation
- [ ] Integration: Meeting with participants full CRUD
- [ ] Integration: Full document workflow (request → AI draft → review → approve → download)
- [ ] Integration: Action item lifecycle (create → complete)

## Frontend Tests

- [ ] Meeting form validation
- [ ] Participant selector search/filter
- [ ] Action item complete/cancel flow
- [ ] Document request form submission
- [ ] Template editor renders placeholders
- [ ] AI summary button trigger
- [ ] Document preview renders correctly
