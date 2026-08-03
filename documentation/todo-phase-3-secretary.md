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
- [x] Implement POST `/api/meetings/{id}/generate-summary` (AI hook)

## Action Items — Backend

- [x] Create `ActionItem` entity
- [x] Create `ActionItemController` + service
- [x] Implement action item CRUD
- [x] Implement POST `/api/action-items/{id}/complete`
- [x] Implement POST `/api/action-items/{id}/cancel`
- [x] Implement GET `/api/action-items/overdue`
- [x] Implement GET `/api/action-items/team` (manager)
- [x] Implement overdue detection background job — fixed 2026-08-04, `ActionItemReminderBackgroundService` scans every 30 min, notifies the assignee once per overdue item (tracked via new `ActionItem.OverdueNotifiedAt`)
- [x] Implement due-date reminder notification — same fix; notification fires exactly once when an item first goes overdue, not a recurring reminder before the deadline

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
- [x] Implement PDF generation — fixed 2026-08-04, `LetterDocumentGenerator.GeneratePdf` via **PdfSharpCore** (not QuestPDF as originally planned — QuestPDF's free tier is revenue-capped at $1M/year for commercial use, which is a licensing decision for an "internal company app" of unknown scale; PdfSharpCore is MIT-licensed with no such restriction). `GenerateFinalAsync` previously created a `GeneratedDocument` row pointing at a file that was never written; `DownloadDocumentAsync` served raw UTF-8 text mislabeled as `application/pdf`. Both fixed — real PDF bytes are now generated, written to disk, and served back
- [x] Implement DOCX generation (OpenXML) — fixed 2026-08-04, `LetterDocumentGenerator.GenerateDocx` via `DocumentFormat.OpenXml`. `GenerateFinalAsync` now produces both PDF and DOCX per request; `GET /api/documents/{id}/download?format=docx` selects which one
- [ ] Integrate AI draft generation — `GenerateDraftAsync` still does plain template variable substitution ({employee_name}, {date}), no AI call despite Phase 4's `IAIService` being available and already wired into Phase 5's ticket suggestions

## Database

- [x] Create migration (Phase3_SecretaryModule — all entities in one migration)
- [ ] Seed default document templates (5 templates)

## Frontend — Meeting Pages

- [x] Create MeetingListPage with filters (date range, status)
- [ ] Create MeetingCalendar component (weekly/monthly view)
- [x] Create MeetingCreatePage with form (date, time, location, participants) — inline dialog in MeetingsPage
- [x] Create MeetingDetailPage with tabs (info, participants, notes, action items)
- [x] Create MeetingEditPage — inline dialog in MeetingsPage
- [x] Create ParticipantSelector component (searchable employee multi-select)
- [x] Create MeetingNotesPage with rich text editor — Notes tab in MeetingDetailPage, plain textarea (not rich text)
- [x] Create AISummaryButton component
- [ ] Meeting form validation (end > start, required fields) — required-field validation only, no end>start check
- [x] Create MeetingNotesEditor component — inline in MeetingDetailPage Notes tab

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

- [x] Create Secretary dashboard page (today's agenda, pending reviews, upcoming meetings, overdue action items) — **correction:** this was already fully built in `DashboardPage.tsx` (role-gated cards for today's meetings, pending document reviews, overdue action items, upcoming meetings); the earlier audit missed it since it lives inside the shared `DashboardPage` rather than a separate route
- [x] Extend manager dashboard (approval queue, team action items) — approval queue already existed ("Needs Your Action"); added a "Team Action Items" card (fixed 2026-08-04) using the existing `GET /api/action-items/team` endpoint, which was built but never surfaced in the UI

## Backend Tests

> **Correction:** this section previously read as not started — wrong. `tests/AIHelpdesk.Tests/Services/` has `MeetingServiceTests` (15), `ActionItemServiceTests` (11), `DocumentServiceTests` (18) = 44 real tests. They run against an in-memory DbContext through the service layer, so they satisfy the "Unit" items below but not the "Integration" items (no `WebApplicationFactory`/real HTTP pipeline exists for this module).

- [x] Unit: Meeting CRUD
- [x] Unit: Meeting participant management (add/remove)
- [ ] Unit: Meeting date conflict detection — only start/end time ordering is tested, not double-booking conflicts
- [x] Unit: Action item status transitions (complete/cancel)
- [x] Unit: Overdue detection
- [x] Unit: Document workflow states (submit → review → approve → generate final)
- [x] Unit: Letter number generation (format, yearly reset)
- [x] Unit: PDF generation — `LetterDocumentGeneratorTests` (6 tests) + `DocumentServiceTests.GenerateFinalAsync_ShouldProduceRealPdfAndDocxFiles`, verifies real `%PDF` magic bytes
- [x] Unit: DOCX generation — same test files, verifies real zip (`PK`) magic bytes
- [ ] Integration: Meeting with participants full CRUD — covered at unit level only, no true integration test
- [ ] Integration: Full document workflow (request → AI draft → review → approve → download) — covered at unit level only
- [ ] Integration: Action item lifecycle (create → complete) — covered at unit level only

## Frontend Tests

- [ ] Meeting form validation
- [ ] Participant selector search/filter
- [ ] Action item complete/cancel flow
- [ ] Document request form submission
- [ ] Template editor renders placeholders
- [ ] AI summary button trigger
- [ ] Document preview renders correctly
