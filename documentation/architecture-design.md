# Architecture Design

## AI Helpdesk — Digital Secretary & HR Assistant

| | |
|---|---|
| **Scope** | Technical/system architecture — components, layers, deployment topology, and key runtime flows. For functional behavior see [`FSD.md`](FSD.md); for environment variables and deploy steps see [`deployment-runbook.md`](deployment-runbook.md). |
| **Reflects** | The system as actually deployed: React SPA on Vercel, ASP.NET Core API on Render, PostgreSQL (pgvector) on Render. |

---

## 1. System Context

One React SPA serves both the internal staff app and the external candidate portal (same bundle,
different route prefix and JWT audience). It talks to a single ASP.NET Core Web API, which is the
only component with direct database access.

```mermaid
flowchart TB
    staff["Staff user\n(Super Admin / HRD / Secretary / Manager / Employee)"]
    candidate["Candidate\n(external, invite-only)"]

    subgraph vercel["Vercel"]
        spa["React SPA (Vite)\nfrontend/\nstaff app + /portal/* candidate portal"]
    end

    subgraph render["Render"]
        api["ASP.NET Core Web API\nsrc/AIHelpdesk.Api\nKestrel, Docker runtime"]
        db[("PostgreSQL 16 + pgvector\naihelpdesk-db")]
    end

    subgraph external["External"]
        ai["AI Provider\nOpenAI-compatible\nchat + embeddings"]
    end

    staff -->|HTTPS| spa
    candidate -->|HTTPS| spa
    spa -->|REST /api/*\nJWT bearer| api
    spa -.->|WebSocket /hubs/notifications\nSignalR| api
    api -->|EF Core| db
    api -->|HTTPS| ai

    style vercel fill:#0b1220,stroke:#5b8def,color:#e6edf7
    style render fill:#0b1220,stroke:#4caf7d,color:#e6edf7
    style external fill:#0b1220,stroke:#c9a227,color:#e6edf7
```

---

## 2. Backend — Clean Architecture Layers

`src/` is split into five projects with a strict inward dependency rule: `Api` depends on
`Application` and `Infrastructure`; `Infrastructure` implements `Application`'s interfaces;
`Domain` has no dependencies at all.

```mermaid
flowchart TB
    subgraph api["AIHelpdesk.Api"]
        controllers["Controllers (22)\nAuth, Tickets, Employees,\nAIChat, CandidatePortal, ..."]
        hubs["NotificationHub\n(SignalR)"]
    end

    subgraph app["AIHelpdesk.Application"]
        interfaces["Service interfaces\nIAIService, IRecruitmentAIService, ..."]
        options["Options\nAIOptions, JwtOptions, ..."]
    end

    subgraph contracts["AIHelpdesk.Contracts"]
        dtos["Request / response DTOs"]
    end

    subgraph domain["AIHelpdesk.Domain"]
        entities["Entities\nUser, Ticket, AgentAssignment,\nCandidateAccount, AIUsageLog, ..."]
    end

    subgraph infra["AIHelpdesk.Infrastructure"]
        efcore["ApplicationDbContext (EF Core)"]
        services["Service implementations\nAIService, RecruitmentAIService, ..."]
        background["Background services\nTicketSlaBackgroundService\nActionItemReminderBackgroundService"]
        seeder["DbSeeder\n(demo accounts, best-effort)"]
    end

    api --> app
    api --> infra
    api --> contracts
    infra -.implements.-> interfaces
    app --> domain
    infra --> domain
    infra --> efcore
```

Notes:
- Controllers depend only on `Application` interfaces, never on `Infrastructure` concrete types —
  `Program.cs` wires the DI container.
- `DbSeeder` runs on every startup but failures are caught and logged, not fatal (see
  `Program.cs` lines ~148–155) — seed data is best-effort demo content, not a boot dependency.
- Two background services run inside the same API process (no separate worker deployment):
  SLA breach checks every 5 minutes, and action-item reminders.

---

## 3. Deployment Topology

Two deployment paths exist in the repo. **Cloud (current, live)** is what's actually running;
**self-hosted Docker** is an alternative path for on-prem/VPS deployment.

```mermaid
flowchart LR
    subgraph cloud["Cloud path (current) — render.yaml + frontend/vercel.json"]
        direction TB
        gh["GitHub main branch"]
        ghactions["GitHub Actions (ci.yml)\nbuild + test + deploy"]
        vercelP["Vercel\nfrontend project\nRoot Directory: frontend"]
        renderP["Render\naihelpdesk-api (Docker)"]
        renderDB[("Render Postgres\naihelpdesk-db")]

        gh --> ghactions
        ghactions -->|vercel deploy --prebuilt --prod| vercelP
        gh -.->|Render's own git integration| renderP
        renderP --> renderDB
    end

    subgraph selfhosted["Self-hosted path (alternative) — docker/production/"]
        direction TB
        nginx["nginx\n(reverse proxy + TLS)"]
        certbot["certbot\n(Let's Encrypt)"]
        feC["frontend container"]
        beC["backend container"]
        pgC[("postgres container")]

        nginx --> feC
        nginx --> beC
        beC --> pgC
        certbot -.-> nginx
    end
```

CI pipeline detail (`.github/workflows/ci.yml`):

```mermaid
flowchart LR
    trigger(["push to main\npull_request to main\nworkflow_dispatch"]) --> backend["Backend (.NET)\nrestore, build, test\nagainst pgvector/pgvector:pg17"]
    trigger --> frontend["Frontend (React/Vite)\nnpm ci, type-check, build"]
    backend --> docker["Docker build smoke test\nbuild both images, push:false"]
    frontend --> docker
    frontend --> deploy["Deploy frontend to Vercel\nvercel pull → vercel build → vercel deploy --prebuilt\n(push to main only)"]
```

---

## 4. Auth Flow

Two independent JWT audiences share the same `AuthController` shape but issue tokens scoped to
different claim sets: `AIHelpdesk` for the five staff roles, `AIHelpdesk-CandidatePortal` for
candidates. A candidate token cannot satisfy `[Authorize]` on any staff endpoint, and vice versa.

```mermaid
sequenceDiagram
    participant U as Browser (SPA)
    participant A as AuthController
    participant D as PostgreSQL

    U->>A: POST /api/auth/login {email, password}
    A->>D: look up User by email
    D-->>A: user + password hash + roles
    A->>A: verify password, build claims (roles)
    A-->>U: 200 {accessToken (JWT), refreshToken, expiresAt, user}
    U->>U: store tokens (authStore / candidatePortalAuthStore)

    Note over U,A: subsequent requests
    U->>A: any /api/* request\nAuthorization: Bearer <accessToken>
    A->>A: validate JWT signature + expiry + role claim
    A-->>U: 200 / 401 / 403

    Note over U,A: on 401 (expired access token)
    U->>A: POST /api/auth/refresh-token {refreshToken}
    A-->>U: new accessToken + refreshToken
```

---

## 5. AI Chat & Knowledge Base (RAG)

`pgvector` stores embeddings for knowledge-base documents; chat responses are grounded by
similarity search before calling the chat model.

```mermaid
sequenceDiagram
    participant U as Staff user (SPA)
    participant C as AIChatController
    participant S as AIService
    participant D as PostgreSQL (pgvector)
    participant AI as AI Provider (chat + embeddings)

    U->>C: POST /api/ai/chat {message}
    C->>S: HandleChat(message)
    S->>AI: embeddings request (AI__EmbeddingModel)
    AI-->>S: embedding vector
    S->>D: similarity search over KnowledgeBase embeddings
    D-->>S: top-k relevant chunks
    S->>AI: chat completion (AI__ChatModel)\ncontext = retrieved chunks + message
    AI-->>S: completion
    S->>D: log AIUsageLog (tokens, cost, rate/budget check)
    S-->>C: answer
    C-->>U: 200 {answer, sources}
```

Rate limiting (`AI__RateLimit__*`) and a daily token budget (`AI__Budget__*`) are enforced in
`AIService` before calling out to the provider — see [`FSD.md §5`](FSD.md) for the exact rules.

---

## 6. Ticketing, Agent Assignment & SLA

There is no dedicated "Agent" login role — Manager, HRD, and Super Admin can all act as ticket
agents. Assignment is always a manual action; only SLA breach detection runs automatically.

```mermaid
flowchart TB
    create["User files ticket\nPOST /api/tickets\n(category → SLA target)"]
    assign["Manager/HRD/Super Admin\nassigns agent\nPOST /api/tickets/{id}/assign"]
    load["AgentAssignment.CurrentLoad\nupdated (vs MaxTickets)"]
    sla["TicketSlaBackgroundService\n(every 5 min)"]
    breach{"SLA breached?"}
    flag["Ticket flagged Breached\nassigned agent notified\n(SignalR + NotificationHub)"]
    escalate["Admin/Manager manually escalates\nPOST /api/escalations\n(choose assignee + reason)"]

    create --> assign --> load
    sla --> breach
    breach -->|yes| flag --> escalate
    breach -->|no| sla
```

---

## 7. Role-Based Access Summary

| Area | Super Admin | HRD | Secretary | Manager | Employee |
|---|:---:|:---:|:---:|:---:|:---:|
| Users / Roles / Departments | ✅ | ✅ | – | – | – |
| Employees / Leave admin | ✅ | ✅ | – | view own | view own |
| Meetings / Documents / Action Items | ✅ | ✅ | ✅ | ✅ | limited |
| Ticket Categories | ✅ | ✅ | – | – | – |
| Agent Workload (`/tickets/agents`) | ✅ | ✅ | – | – | – |
| Escalations | ✅ | – | – | ✅ | – |
| Recruitment / Candidates | ✅ | ✅ | – | – | – |
| AI Chat / Knowledge Base | ✅ | ✅ | ✅ | ✅ | ✅ |
| Candidate Portal | *(external, separate JWT audience — not a staff role)* | | | | |

Source of truth: nav/route guards in `frontend/src/components/layout/AppLayout.tsx` and
`frontend/src/App.tsx`, mirrored by `[Authorize(Roles = ...)]` on each controller action.

---

## 8. Key Design Decisions

- **No microservices** — one API process handles HTTP, SignalR, and background jobs. Justified by
  scale (internal tool, Render free-tier hosting); revisit if traffic or team size grows.
- **pgvector over a dedicated vector DB** — keeps infra to a single Postgres instance; acceptable
  at current knowledge-base size.
- **Manual ticket assignment, no auto-routing** — explicit product decision (see `FSD.md`
  "Out of Scope"), not a missing feature.
- **Vercel's own Git integration is disconnected** (`vercel git disconnect`, 2026-08-12) —
  GitHub Actions' `deploy-vercel` job is the sole deploy path, avoiding a duplicate-deploy race.
  See [`deployment-runbook.md`](deployment-runbook.md) for current status of that job.
