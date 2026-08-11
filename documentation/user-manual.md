# User Manual

For all employees using the AI Helpdesk portal. What's visible to you depends on your role —
Employee, Secretary, Manager, HRD, or Super Admin (see `admin-manual.md` for the
admin-specific screens). This manual covers the features every employee has access to, plus
the extra screens Managers and Secretaries see.

---

## 1. Dashboard

`/dashboard` — your landing page after login. Shows a summary relevant to your role: pending
leave requests, ticket counts, and (for Managers/Secretaries) a "Team Action Items" card
tracking outstanding action items assigned to your team.

## 2. Leave Requests

- **Submit Leave** (`/leave-requests`) — request time off by selecting a leave type, date
  range, and reason. Your request goes to your reporting manager for approval.
- **Leave Approvals** (`/leave-approvals`, Managers only) — approve or reject leave requests
  from your direct reports, with an optional reason on rejection.

## 3. Tickets (Helpdesk)

`/tickets` — file a support ticket ("New Ticket") for IT, HR, or facilities issues, choosing
a category (each has a target response/resolution time — its SLA). Track your ticket's
status and add comments as it's worked. Attachments are limited to common document/image
types and 10MB per file.

If you're acting as a ticket agent (Manager/HRD/Super Admin), open a ticket to add internal
notes, change status, or request an AI-suggested response before replying to the requester.

## 4. AI Assistant

`/ai/chat` — ask questions in natural language; the assistant answers using your
organization's Knowledge Base documents where relevant. If the assistant can't help, use
**Escalate to Human** to convert the conversation into a ticket without losing context.

## 5. Knowledge Base

`/knowledge-base` — browse and search uploaded reference documents (policies, guides, FAQs).
Some documents are department-specific and only visible to staff in that department.

## 6. Meetings & Action Items

*(Visible to Managers and Secretaries.)*

- **Meetings** (`/meetings`) — schedule a meeting, then from the meeting detail page add
  participants, minutes/notes, and action items assigned to specific people with due dates.
- **Action Items** (`/action-items`) — a combined view of all action items, whether created
  standalone or from a meeting. Overdue items trigger an automatic reminder notification to
  the assignee (checked every 30 minutes).

## 7. Documents & Letters

*(Request access: everyone. Approve/generate: Managers/Secretaries.)*

- **New Request** (`/documents/requests`) — request an official document (e.g. an employment
  or reference letter) by choosing a template and filling in any required details.
- Approvers review pending requests, and on approval the system generates a final PDF and
  DOCX using the selected template with your details automatically filled in — download both
  formats from the request's detail view.

## 8. Notifications

`/notifications` — a full history of your notifications (leave decisions, ticket updates,
action item reminders, etc.). A bell icon in the top bar shows live updates in real time when
connected; if real-time delivery isn't working (e.g. a restrictive network), notifications
still arrive via a 30-second polling fallback, just with a short delay.

## 9. Recruitment

*(Visible to Managers, HRD, and Super Admin.)*

- **Vacancies** (`/recruitment/vacancies`) — create a job opening, then **Publish** it when
  ready to accept candidates. A vacancy auto-marks itself **Filled** once enough candidates
  have been hired to fill all its openings, and can be **Closed** manually otherwise.
- **Candidates** (`/recruitment/candidates`) — a Kanban-style board of candidates by pipeline
  stage (Applied → Screening → Test → Interview → Offering → Hired, or Rejected from any
  non-final stage). Add a candidate with their CV (PDF or DOCX, 5MB max); the AI assistant can
  summarize the CV and suggest interview questions — if the file has no readable text (e.g. a
  scanned image), it says so plainly instead of guessing at a summary. A CV can be removed from
  the candidate's detail page if it was uploaded by mistake. Candidates can only be advanced one
  stage forward at a time, never skipped or moved backward, except into Rejected.
- **Interviews** (`/recruitment/interviews`) — schedule an interview for a candidate with an
  interviewer and time slot; the system prevents double-booking the same interviewer against
  an overlapping time. Record a recommendation after the interview (Strong Hire / Hire / No
  Hire / Strong No Hire).

## 10. Getting Help

If something in the app isn't working as described here, file a ticket (§3) under the IT
category — that's the same support channel the team behind this manual monitors.
