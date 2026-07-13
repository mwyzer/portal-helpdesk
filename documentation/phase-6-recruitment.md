# Phase 6 — Recruitment Assistant

**Tech Stack:** React (TypeScript) + ASP.NET Core Web API + PostgreSQL

**Prerequisite:** Phases 1 (Foundation) + 2 (HR Admin) + 4 (AI Chat) must be complete.

---

## 1. Overview

Phase 6 delivers the recruitment assistant: job vacancy posting, candidate pipeline management, CV upload & AI summarization, AI interview question generation, interview scheduling & notes, and status tracking across the hiring pipeline.

**Goal:** HR can manage the full recruitment lifecycle from job posting to hiring decision.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Job vacancy management | Create, publish, close job postings |
| 2 | Candidate pipeline | Track candidates through hiring stages |
| 3 | CV upload & storage | Upload CV files, link to candidates |
| 4 | AI CV summarization | Extract skills, experience, education from CV |
| 5 | AI interview questions | Generate role-specific interview questions |
| 6 | Interview scheduling | Schedule interviews, record notes/feedback |
| 7 | Candidate comparison | Compare CVs against job requirements |
| 8 | Notification for candidates | Interview invitation, status updates |

---

## 3. New Database Tables

| Table | Key Columns | Description |
|-------|-------------|-------------|
| `JobVacancies` | Id, Title, DepartmentId, EmploymentType, Location, Description, Requirements, MinSalary, MaxSalary, MaxCandidates, Status (Draft/Published/Closed/Filled), PublishedAt, ClosedAt | Job postings |
| `Candidates` | Id, FirstName, LastName, Email, Phone, Position, CurrentCompany, ExpectedSalary, CvFilePath, CvSummary, AISummaryJson, Status, Source | Candidate records |
| `CandidateStages` | Id, CandidateId, Stage (Applied/Screening/Test/Interview/Offering/Hired/Rejected), ChangedById, Notes, ChangedAt | Stage history |
| `Interviews` | Id, CandidateId, JobVacancyId, InterviewerIds (JSON), ScheduledAt, DurationMinutes, Location, MeetingLink, Type (Online/Offline), Status (Scheduled/Completed/Cancelled), Notes, Feedback, Rating | Interview sessions |
| `InterviewQuestions` | Id, CandidateId, JobVacancyId, Question, Category, GeneratedById, UsedInInterview, Feedback | AI-generated questions |
| `CandidateDocuments` | Id, CandidateId, FileName, FilePath, FileType (CV/Portfolio/Certificate/Other), UploadedAt | Additional documents |

All tables include: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

### Candidate Pipeline Stages
```
Applied → Screening → Test → Interview → Offering → Hired
                               ↓                           ↓
                            Rejected                   Rejected
```

### JobVacancy Status
```
Draft → Published → Closed
              ↓
            Filled
```

---

## 4. API Endpoints

### Job Vacancies

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/job-vacancies` | All | List (filter by status, department) |
| GET | `/api/job-vacancies/{id}` | All | Detail with candidate count |
| POST | `/api/job-vacancies` | HR Admin, Recruiter | Create vacancy |
| PUT | `/api/job-vacancies/{id}` | HR Admin, Recruiter | Update vacancy |
| POST | `/api/job-vacancies/{id}/publish` | HR Admin | Publish vacancy |
| POST | `/api/job-vacancies/{id}/close` | HR Admin | Close vacancy |

### Candidates

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/candidates` | HR Admin, Recruiter | List (filter by stage, vacancy) |
| GET | `/api/candidates/{id}` | HR Admin, Recruiter | Detail with stage history, interviews |
| POST | `/api/candidates` | HR Admin, Recruiter | Create candidate |
| PUT | `/api/candidates/{id}` | HR Admin, Recruiter | Update candidate info |
| POST | `/api/candidates/{id}/cv` | HR Admin, Recruiter | Upload CV |
| GET | `/api/candidates/{id}/cv` | HR Admin, Recruiter | Download CV |
| POST | `/api/candidates/{id}/advance-stage` | HR Admin, Recruiter | Move to next stage |
| POST | `/api/candidates/{id}/reject` | HR Admin, Recruiter | Reject candidate (with reason) |
| POST | `/api/candidates/{id}/ai-summarize` | HR Admin, Recruiter | Generate AI CV summary |

### Interviews

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/interviews` | HR Admin, Recruiter | List (filter by date, candidate, vacancy) |
| GET | `/api/interviews/{id}` | HR Admin, Recruiter | Detail |
| POST | `/api/interviews` | HR Admin, Recruiter | Schedule interview |
| PUT | `/api/interviews/{id}` | HR Admin, Recruiter | Update interview |
| POST | `/api/interviews/{id}/complete` | HR Admin, Recruiter | Mark complete + add feedback |
| POST | `/api/interviews/{id}/cancel` | HR Admin, Recruiter | Cancel interview |
| GET | `/api/interviews/upcoming` | HR Admin, Recruiter | Upcoming interviews |

### AI Features

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/candidates/{id}/ai-questions` | HR Admin, Recruiter | Generate interview questions |
| POST | `/api/job-vacancies/{id}/ai-match` | HR Admin, Recruiter | Compare candidate(s) to requirements |
| GET | `/api/candidates/{id}/ai-match/{vacancyId}` | HR Admin, Recruiter | Get match score for specific vacancy |

### Reports

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/recruitment/stats` | HR Admin | Pipeline stats (candidates per stage, time-in-stage) |
| GET | `/api/recruitment/export` | HR Admin | Export to Excel |

---

## 5. Frontend Pages

| Route | Page | Access |
|-------|------|--------|
| `/recruitment/vacancies` | VacancyListPage | HR Admin, Recruiter |
| `/recruitment/vacancies/new` | VacancyCreatePage | HR Admin, Recruiter |
| `/recruitment/vacancies/:id` | VacancyDetailPage | HR Admin, Recruiter |
| `/recruitment/vacancies/:id/edit` | VacancyEditPage | HR Admin, Recruiter |
| `/recruitment/candidates` | CandidateListPage | HR Admin, Recruiter |
| `/recruitment/candidates/new` | CandidateCreatePage | HR Admin, Recruiter |
| `/recruitment/candidates/:id` | CandidateDetailPage | HR Admin, Recruiter |
| `/recruitment/interviews` | InterviewListPage | HR Admin, Recruiter |
| `/recruitment/interviews/schedule` | InterviewSchedulePage | HR Admin, Recruiter |
| `/recruitment/interviews/:id` | InterviewDetailPage | HR Admin, Recruiter |
| `/recruitment/reports` | RecruitmentReportsPage | HR Admin |

### New Components

| Component | Description |
|-----------|-------------|
| `VacancyForm` | Create/edit form with rich text editor for requirements |
| `VacancyCard` | Published vacancy summary card |
| `PipelineBoard` | Kanban-style pipeline view (stages as columns) |
| `CandidateCard` | Draggable candidate card in pipeline |
| `CandidateDetailDrawer` | Slide-out drawer with full candidate info |
| `CvUploadZone` | Drag-drop CV upload with progress |
| `CvSummaryPanel` | AI-generated CV summary display |
| `InterviewForm` | Schedule form with date/time, type, interviewers |
| `InterviewFeedbackForm` | Rating, strengths, weaknesses, recommendation |
| `AISuggestedQuestions` | AI-generated question list with accept/reject |
| `MatchScoreBadge` | % match score between CV and job requirements |
| `StageTimeline` | Visual timeline of candidate stages |
| `InterviewCalendar` | Calendar view of scheduled interviews |

---

## 6. Business Logic

### Candidate Pipeline
- Each stage transition logged in `CandidateStages`
- Cannot skip stages (must go Screening → Test → Interview — cannot skip from Screening to Interview)
- Rejected can happen from any stage
- Hired only from Offering
- When rejected, cannot be reactivated (new candidate entry required)

### AI CV Summarization (with Phase 4 AI service)
1. Extract text from CV (PDF/DOCX)
2. Send to LLM with structured prompt: extract Name, Skills, Experience (years), Education, Previous Companies, Certifications
3. Store structured result in `AISummaryJson`
4. Display as formatted summary in UI

### AI Interview Questions
1. Send job title, requirements, and candidate CV summary to LLM
2. Generate 5-10 role-specific questions (Technical, Behavioral, General)
3. Store in `InterviewQuestions` table
4. Interviewer selects which questions to use

### Candidate-Job Matching
1. Extract structured data from CV
2. Compare against job requirements using LLM
3. Return match score (%), matched skills, missing skills, overall assessment
4. Used in candidate list for ranking

### Interview Scheduling
- Validate interviewer availability (no double-booking)
- Send notification to interviewer(s) + candidate (email)
- Send meeting reminder 1 hour before
- Track status: Scheduled → Completed → Feedback Added

---

## 7. Implementation Steps

### Step 1: Backend — Database
- Create migration: JobVacancies, Candidates, CandidateStages, Interviews, InterviewQuestions, CandidateDocuments
- Seed sample job vacancy templates

### Step 2: Backend — Job Vacancy Module
- Create `JobVacancyController` + service
- Implement CRUD with status transitions (Draft → Published → Closed/Filled)
- Implement filtered listing

### Step 3: Backend — Candidate Module
- Create `CandidateController` + service
- Implement CRUD with CV upload (validation: PDF/DOCX, max 5MB)
- Implement stage advancement with validation
- Implement pipeline stats (count per stage)

### Step 4: Backend — Interview Module
- Create `InterviewController` + service
- Implement scheduling with conflict detection
- Implement feedback submission
- Implement upcoming list
- Implement cancellation

### Step 5: Backend — AI Integration
- Create `RecruitmentAIService` (hook into Phase 4)
- Implement CV summarization
- Implement interview question generation
- Implement candidate-job matching

### Step 6: Backend — Reports
- Pipeline statistics endpoint
- Excel export

### Step 7: Frontend — Vacancy Pages
- Vacancy list with status filters
- Create/edit form with rich text editor
- Vacancy detail with candidate pipeline embed

### Step 8: Frontend — Candidate Pages
- Candidate list with pipeline stage filter
- Candidate detail with Timeline, CV preview, AI Summary, Interview history
- CV upload with drag-drop
- Pipeline Board (Kanban) for visual stage management
- AI summary trigger button
- Stage advance/reject actions

### Step 9: Frontend — Interview Pages
- Interview list with filters (date range, status)
- Schedule interview form (candidate, vacancy, date, interviewers, type)
- Interview detail with feedback form
- Interview calendar

### Step 10: Frontend — Reports
- Pipeline stats charts (candidates per stage)
- Time-in-stage analysis
- Export button

### Step 11: Backend — Tests
- Unit: Stage transition validation (no skipping, reject from any)
- Unit: Interview conflict detection
- Unit: CV file validation (type, size)
- Unit: Job vacancy status transitions
- Integration: Full candidate lifecycle (create → upload CV → advance stages → interview → hire/reject)
- Integration: AI CV summarization flow

### Step 12: Frontend — Tests
- Pipeline board drag-and-drop
- CV upload validation
- Stage transition button visibility
- Interview form validation
- AI summary display
- Match score rendering

---

## 8. Seed Data

### Employment Types
- Full Time, Part Time, Contract, Internship

### Source Options
- LinkedIn, Jobstreet, Glints, Company Website, Employee Referral, Walk-in, Other

### Sample Vacancy Template

| Field | Example |
|-------|---------|
| Title | Software Engineer |
| Department | IT |
| Employment Type | Full Time |
| Min Candidates | 3 |
| Max Candidates | 10 |

---

## 9. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | HR can create and publish job vacancies |
| 2 | Candidate records can be created with CV upload |
| 3 | CV text is extracted and AI generates structured summary |
| 4 | AI can suggest interview questions based on vacancy + CV |
| 5 | Candidate can be advanced through pipeline stages |
| 6 | Stage transitions are logged with who and when |
| 7 | Interviews can be scheduled with interviewer assignment |
| 8 | Interview feedback can be recorded |
| 9 | AI can match candidate CV against job requirements |
| 10 | Pipeline board shows candidates per stage visually |
| 11 | Pipeline statistics and reports are available |
| 12 | Notifications sent for interview invitations |

---

## 10. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| Job vacancy module (backend) | 2 days |
| Candidate module (backend) | 3 days |
| Interview module (backend) | 3 days |
| AI integration (backend) | 2 days |
| Reports (backend) | 1 day |
| Vacancy + candidate pages (frontend) | 4 days |
| Pipeline board (frontend) | 2 days |
| Interview pages (frontend) | 2 days |
| Reports page (frontend) | 1 day |
| Backend tests | 3 days |
| Frontend tests | 2 days |
| **Total** | **~25 days** |

---

## 11. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| CV parsing quality varies | AI summary is assistant only — HR can edit manually |
| Interview scheduling conflicts | Check existing interviews for same interviewers |
| Pipeline stage skipping | Strict server-side validation; cannot skip stages |
| Sensitive candidate data | Encrypt CV storage; access restricted to HR role |
| AI match score misleading | Show detailed breakdown (skills matched, skills missing) |
