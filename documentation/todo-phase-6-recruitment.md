# Phase 6 — Recruitment Assistant — TODO Checklist

## Database

- [ ] Create migration for JobVacancies
- [ ] Create migration for Candidates
- [ ] Create migration for CandidateStages
- [ ] Create migration for Interviews
- [ ] Create migration for InterviewQuestions
- [ ] Create migration for CandidateDocuments

## Backend — Job Vacancy Module

- [ ] Create `JobVacancy` entity
- [ ] Create `JobVacancyController` + service + repository
- [ ] Implement GET `/api/job-vacancies` (filter by status, department)
- [ ] Implement GET `/api/job-vacancies/{id}` (detail with candidate count)
- [ ] Implement POST `/api/job-vacancies`
- [ ] Implement PUT `/api/job-vacancies/{id}`
- [ ] Implement POST `/api/job-vacancies/{id}/publish`
- [ ] Implement POST `/api/job-vacancies/{id}/close`
- [ ] Implement status transitions (Draft → Published → Closed/Filled)

## Backend — Candidate Module

- [ ] Create `Candidate` entity
- [ ] Create `CandidateStage` entity
- [ ] Create `CandidateController` + service
- [ ] Implement GET `/api/candidates` (filter by stage, vacancy)
- [ ] Implement GET `/api/candidates/{id}` (detail with stages, interviews)
- [ ] Implement POST `/api/candidates`
- [ ] Implement PUT `/api/candidates/{id}`
- [ ] Implement POST `/api/candidates/{id}/cv` (upload)
- [ ] Implement GET `/api/candidates/{id}/cv` (download)
- [ ] Implement POST `/api/candidates/{id}/advance-stage`
- [ ] Implement POST `/api/candidates/{id}/reject`
- [ ] Implement stage transition validation (no skipping)
- [ ] Implement CV file validation (PDF/DOCX, max 5MB)
- [ ] Implement candidate source tracking

## Backend — Interview Module

- [ ] Create `Interview` entity
- [ ] Create `InterviewQuestion` entity
- [ ] Create `InterviewController` + service
- [ ] Implement GET `/api/interviews` (filter by date, candidate, vacancy)
- [ ] Implement GET `/api/interviews/{id}`
- [ ] Implement POST `/api/interviews`
- [ ] Implement PUT `/api/interviews/{id}`
- [ ] Implement POST `/api/interviews/{id}/complete` (add feedback)
- [ ] Implement POST `/api/interviews/{id}/cancel`
- [ ] Implement GET `/api/interviews/upcoming`
- [ ] Implement interviewer conflict detection
- [ ] Implement interview status tracking (Scheduled → Completed → Cancelled)

## Backend — AI Integration

- [ ] Create `RecruitmentAIService`
- [ ] Implement POST `/api/candidates/{id}/ai-summarize` (CV summarization)
- [ ] Implement POST `/api/candidates/{id}/ai-questions` (interview questions)
- [ ] Implement POST `/api/job-vacancies/{id}/ai-match` (candidate-job matching)
- [ ] Implement structured prompt templates for each AI feature
- [ ] Store AI summaries in `AISummaryJson` field

## Backend — Reports

- [ ] Create GET `/api/recruitment/stats` (pipeline stats)
- [ ] Implement Excel export
- [ ] Create candidates-per-stage aggregation
- [ ] Create average time-in-stage calculation

## Frontend — Vacancy Pages

- [ ] Create VacancyListPage with status filters
- [ ] Create VacancyForm component (rich text for requirements)
- [ ] Create VacancyCreatePage
- [ ] Create VacancyDetailPage (with embedded candidate pipeline)
- [ ] Create VacancyCard component

## Frontend — Candidate Pages

- [ ] Create CandidateListPage with stage filter
- [ ] Create CandidateDetailPage (timeline, CV preview, AI summary, interview history)
- [ ] Create PipelineBoard (Kanban-style, stages as columns)
- [ ] Create CandidateCard component (draggable)
- [ ] Create CvUploadZone (drag-drop with progress)
- [ ] Create CvSummaryPanel (AI summary display)
- [ ] Create StageTimeline component
- [ ] Create MatchScoreBadge component
- [ ] Create stage advance/reject buttons
- [ ] Implement AI summary trigger button

## Frontend — Interview Pages

- [ ] Create InterviewListPage with filters (date range, status)
- [ ] Create InterviewForm (candidate, vacancy, date, interviewers, type)
- [ ] Create InterviewSchedulePage
- [ ] Create InterviewDetailPage with feedback form
- [ ] Create InterviewFeedbackForm (rating, strengths, weaknesses, recommendation)
- [ ] Create AISuggestedQuestions component (accept/reject)
- [ ] Create InterviewCalendar component

## Frontend — Reports

- [ ] Create RecruitmentReportsPage
- [ ] Create pipeline stats charts
- [ ] Create time-in-stage analysis
- [ ] Create export button

## Backend Tests

- [ ] Unit: Stage transition validation (no skipping)
- [ ] Unit: Stage transition validation (reject from any stage)
- [ ] Unit: Interview conflict detection
- [ ] Unit: CV file validation (type, size)
- [ ] Unit: Job vacancy status transitions
- [ ] Unit: Candidate CRUD
- [ ] Integration: Full candidate lifecycle (create → upload CV → advance stages → interview → hire/reject)
- [ ] Integration: AI CV summarization flow
- [ ] Integration: Interview scheduling + feedback

## Frontend Tests

- [ ] Pipeline board drag-and-drop
- [ ] CV upload validation
- [ ] Stage transition button visibility
- [ ] Interview form validation
- [ ] AI summary display
- [ ] Match score rendering
- [ ] Interview schedule form submission
