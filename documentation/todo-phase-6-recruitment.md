# Phase 6 — Recruitment Assistant — TODO Checklist

> For confirmed as-built behavior rather than a task checklist, see [`FSD.md`](FSD.md).
>
> **Status (2026-08-04): built from scratch.** This phase was genuinely 0% before this pass (confirmed directly against the code, not just doc-reading) — unlike Phases 2/3/5, there was no earlier partial implementation to find. Full backend (entities, migration, 3 services, 3 controllers, AI integration) + 4 frontend pages, following the conventions established in the Ticketing and HR modules. 37 new backend tests, 273/273 passing project-wide.

## Database

- [x] Create migration for JobVacancies — single `Phase6_Recruitment` migration covers all entities below
- [x] Create migration for Candidates
- [x] Create migration for CandidateStages — implemented as `CandidateStageHistory` (an append-only log, matching the `TicketHistory` pattern), not a lookup table; the current stage lives directly on `Candidate.Stage`
- [x] Create migration for Interviews
- [x] Create migration for InterviewQuestions
- [x] Create migration for CandidateDocuments

## Backend — Job Vacancy Module

- [x] Create `JobVacancy` entity
- [x] Create `JobVacancyController` + service — no separate repository, service uses `ApplicationDbContext` directly (consistent with the rest of the codebase)
- [x] Implement GET `/api/job-vacancies` (filter by status, department)
- [x] Implement GET `/api/job-vacancies/{id}` (detail with candidate count)
- [x] Implement POST `/api/job-vacancies`
- [x] Implement PUT `/api/job-vacancies/{id}` — blocked once Closed/Filled
- [x] Implement POST `/api/job-vacancies/{id}/publish`
- [x] Implement POST `/api/job-vacancies/{id}/close` — automatically resolves to `Filled` instead of `Closed` if hired-candidate count already meets `OpeningsCount`
- [x] Implement status transitions (Draft → Published → Closed/Filled)

## Backend — Candidate Module

- [x] Create `Candidate` entity
- [x] Create `CandidateStage` entity — implemented as an enum + `CandidateStageHistory` log (see Database note above), not a separate lookup table
- [x] Create `CandidateController` + service — `CandidatesController`/`CandidateService`
- [x] Implement GET `/api/candidates` (filter by stage, vacancy)
- [x] Implement GET `/api/candidates/{id}` (detail with stages, interviews)
- [x] Implement POST `/api/candidates`
- [x] Implement PUT `/api/candidates/{id}`
- [x] Implement POST `/api/candidates/{id}/cv` (upload)
- [x] Implement GET `/api/candidates/{id}/cv` (download) — actual route is `/api/candidates/{id}/cv/{documentId}` since a candidate can have multiple documents
- [x] Implement POST `/api/candidates/{id}/advance-stage`
- [x] Implement POST `/api/candidates/{id}/reject`
- [x] Implement stage transition validation (no skipping) — enforced via the `CandidateStage` enum's declaration order; advance always moves exactly one step forward
- [x] Implement CV file validation (PDF/DOCX, max 5MB)
- [x] Implement candidate source tracking — free-text `Source` field

## Backend — Interview Module

- [x] Create `Interview` entity
- [x] Create `InterviewQuestion` entity
- [x] Create `InterviewController` + service — `InterviewsController`/`InterviewService`
- [x] Implement GET `/api/interviews` (filter by date, candidate) — no vacancy filter (interviews aren't directly vacancy-scoped, only via candidate)
- [x] Implement GET `/api/interviews/{id}`
- [x] Implement POST `/api/interviews`
- [x] Implement PUT `/api/interviews/{id}`
- [x] Implement POST `/api/interviews/{id}/complete` (add feedback)
- [x] Implement POST `/api/interviews/{id}/cancel`
- [x] Implement GET `/api/interviews/upcoming`
- [x] Implement interviewer conflict detection — interval-overlap check against the same interviewer's other `Scheduled` interviews
- [x] Implement interview status tracking (Scheduled → Completed → Cancelled)

## Backend — AI Integration

- [x] Create `RecruitmentAIService` — wraps the existing Phase 4 `IAIService`, with graceful fallback (empty/default result, not a 500) if the AI call fails, matching the Phase 5 ticket-suggestion pattern
- [x] Implement POST `/api/candidates/{id}/ai-summarize` (CV summarization) — text extraction reuses the same lightweight PDF approach as `KnowledgeBaseService` (not a full parser) and real `DocumentFormat.OpenXml`-based DOCX extraction
- [x] Implement POST `/api/interviews/{id}/ai-questions` (interview questions) — endpoint lives on the interview (needs an interview to attach generated questions to), not on the candidate as originally sketched
- [x] Implement POST `/api/job-vacancies/{id}/ai-match` (candidate-job matching)
- [x] Implement structured prompt templates for each AI feature
- [x] Store AI summaries in `AISummaryJson` field

## Backend — Reports

- [x] Create GET `/api/recruitment/stats` (pipeline stats) — implemented as `GET /api/candidates/stats`
- [x] Implement Excel export — `GET /api/candidates/export`, reuses the shared `IExcelService` from Phase 2
- [x] Create candidates-per-stage aggregation
- [x] Create average time-in-stage calculation — approximated as average days-to-resolution (Hired or Rejected candidates only), not per-individual-stage timing

## Frontend — Vacancy Pages

- [x] Create VacancyListPage with status filters — `VacanciesPage.tsx`
- [x] Create VacancyForm component (rich text for requirements) — inline dialog form in `VacanciesPage.tsx`; plain textarea, not rich text
- [x] Create VacancyCreatePage — inline dialog, not a separate page
- [ ] Create VacancyDetailPage (with embedded candidate pipeline) — deliberately not built separately; clicking a vacancy routes to `CandidatesPage` pre-filtered by that vacancy (`/recruitment/candidates?vacancyId=`), which is the same information without a duplicate page
- [x] Create VacancyCard component — inline row in `VacanciesPage.tsx`, not a separate list-of-cards layout

## Frontend — Candidate Pages

- [x] Create CandidateListPage with stage filter — the Kanban board itself doubles as this (all stages visible as columns); no separate flat-list view
- [x] Create CandidateDetailPage (timeline, CV preview, AI summary, interview history) — no inline CV preview (download only), everything else present
- [x] Create PipelineBoard (Kanban-style, stages as columns) — `CandidatesPage.tsx`
- [ ] Create CandidateCard component (draggable) — cards exist but are **not draggable**; no drag-and-drop library was in the project, and advance/reject is done via buttons on each card instead. Adding a DnD dependency for this felt like more risk/scope than the interaction was worth for a first pass
- [ ] Create CvUploadZone (drag-drop with progress) — plain file-picker button, not a drag-drop zone with progress bar
- [x] Create CvSummaryPanel (AI summary display) — inline panel in `CandidateDetailPage.tsx`
- [x] Create StageTimeline component — inline in `CandidateDetailPage.tsx`
- [ ] Create MatchScoreBadge component — `ai-match` endpoint exists and is callable, but no frontend UI surfaces the result yet
- [x] Create stage advance/reject buttons
- [x] Implement AI summary trigger button

## Frontend — Interview Pages

- [x] Create InterviewListPage with filters (date range, status) — `InterviewsPage.tsx`; status filter only, no date-range picker
- [x] Create InterviewForm (candidate, vacancy, date, interviewers, type) — inline dialog; vacancy is implied by the selected candidate, not a separate field
- [x] Create InterviewSchedulePage — same dialog, not a separate page
- [ ] Create InterviewDetailPage with feedback form — feedback is collected via a dialog on the list page, not a dedicated detail page
- [x] Create InterviewFeedbackForm (rating, strengths, weaknesses, recommendation) — rating + free-text feedback + recommendation; no separate strengths/weaknesses fields
- [x] Create AISuggestedQuestions component (accept/reject) — questions are generated and displayed; no accept/reject interaction, they're just informational for the interviewer
- [ ] Create InterviewCalendar component — list view only, no calendar/month-grid view

## Frontend — Reports

- [ ] Create RecruitmentReportsPage — stats are shown as cards on `VacanciesPage.tsx`, not a dedicated reports page
- [ ] Create pipeline stats charts — stats returned by the API (`candidatesPerStage`) aren't charted, only shown as top-line numbers
- [ ] Create time-in-stage analysis — `averageDaysInPipeline` is fetched but not surfaced as its own analysis view
- [x] Create export button — Excel export button on `CandidatesPage.tsx`

## Backend Tests

> **Added 2026-08-04:** 37 tests across `JobVacancyServiceTests` (8), `CandidateServiceTests` (13), `InterviewServiceTests` (8), `RecruitmentAIServiceTests` (8) — all against an in-memory `ApplicationDbContext`, no `WebApplicationFactory`, so "Integration" items below remain unchecked.

- [x] Unit: Stage transition validation (no skipping)
- [x] Unit: Stage transition validation (reject from any stage)
- [x] Unit: Interview conflict detection — covers both the conflicting-overlap and back-to-back no-overlap cases
- [x] Unit: CV file validation (type, size)
- [x] Unit: Job vacancy status transitions
- [x] Unit: Candidate CRUD
- [ ] Integration: Full candidate lifecycle (create → upload CV → advance stages → interview → hire/reject) — covered at unit level only
- [ ] Integration: AI CV summarization flow — covered at unit level only (with a mocked `IAIService`)
- [ ] Integration: Interview scheduling + feedback — covered at unit level only

## Frontend Tests

- [ ] Pipeline board drag-and-drop — not applicable; board uses buttons, not drag-and-drop (see note above)
- [ ] CV upload validation
- [ ] Stage transition button visibility
- [ ] Interview form validation
- [ ] AI summary display
- [ ] Match score rendering — not applicable yet; no frontend UI for match scores exists
- [ ] Interview schedule form submission

No frontend tests exist for this module — consistent with the rest of the app (Vitest/RTL was never set up; see `todo-phase-1-foundation.md`).
