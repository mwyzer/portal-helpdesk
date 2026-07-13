# AI Helpdesk — Digital Secretary & HR Assistant

An internal company application that serves as a **digital secretary** and **HR assistant**, powered by AI.

## Overview

AI Helpdesk centralizes administrative and HR services into a single application, reducing repetitive paperwork, accelerating employee request responses, and improving communication between employees, HR, and management.

### Key Capabilities

- **Digital Secretary** — Manage agendas, record & summarize meetings, generate letters & documents, handle internal requests, send work reminders
- **HR Assistant** — Manage employee data, process leave & permits, assist recruitment, answer policy questions, generate HR documents
- **AI-Powered Chat** — Conversational interface for employees to ask questions, submit requests, and search internal knowledge

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core Web API (.NET) |
| **Frontend** | React 18 + TypeScript + Vite |
| **Database** | PostgreSQL 17 with pgvector |
| **AI / LLM** | OpenAI / Azure OpenAI (pluggable) |
| **Auth** | JWT (access + refresh tokens), ASP.NET Core Identity |
| **CSS** | Tailwind CSS + shadcn/ui |
| **State** | Zustand (client), TanStack Query (server) |
| **Forms** | React Hook Form + Zod |
| **Charts** | Recharts |
| **ORM** | Entity Framework Core |
| **Mapping** | Mapster |
| **Validation** | FluentValidation |
| **Logging** | Serilog |
| **Testing** | xUnit, Moq, FluentAssertions, Bogus, Coverlet |
| **Containerization** | Docker + Docker Compose |

## Architecture

### Backend — Clean Architecture

```
src/
├── AIHelpdesk.Api/            # ASP.NET Core Web API (controllers, middleware, Program.cs)
├── AIHelpdesk.Application/    # Use cases, service interfaces, DTOs
├── AIHelpdesk.Contracts/      # Request/response DTOs shared across layers
├── AIHelpdesk.Domain/         # Entities, enums, value objects, domain logic
└── AIHelpdesk.Infrastructure/ # EF Core, Identity, JWT, repositories, external services
```

### Frontend

```
frontend/src/
├── components/
│   ├── layout/                # AppShell, Sidebar, Topbar
│   └── ui/                    # shadcn/ui components
├── lib/                       # Axios instance, utilities
├── pages/                     # Page components (one per route)
└── store/                     # Zustand stores (auth, etc.)
```

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)
- [Node.js 18+](https://nodejs.org/) (for local frontend development)

### Quick Start (Docker)

```bash
# Clone the repository
git clone <repository-url>
cd portal-helpdesk

# Start all services (PostgreSQL, API, Frontend)
docker compose up -d --build
```

| Service | URL |
|---------|-----|
| **Frontend** | http://localhost:5173 |
| **Backend API** | http://localhost:5192 |
| **Swagger UI** | http://localhost:5192/swagger |
| **PostgreSQL** | `localhost:5432` (user: `helpdesk`, password: `helpdesk123`, db: `aihelpdesk`) |

### Local Development

#### Backend

```bash
cd src/AIHelpdesk.Api
dotnet restore
dotnet run
```

#### Frontend

```bash
cd frontend
npm install
npm run dev
```

#### Database

```bash
# Start only PostgreSQL
docker compose up -d postgres

# Apply EF Core migrations
cd src/AIHelpdesk.Api
dotnet ef database update
```

### Running Tests

```bash
cd tests/AIHelpdesk.Tests
dotnet test
```

## API Endpoints (Phase 1 — Foundation MVP)

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login, returns access + refresh token |
| POST | `/api/auth/refresh-token` | Refresh access token |
| POST | `/api/auth/logout` | Revoke refresh token |
| POST | `/api/auth/forgot-password` | Send password reset link |
| POST | `/api/auth/reset-password` | Reset password with token |
| GET | `/api/auth/profile` | Get current user profile |
| PUT | `/api/auth/profile` | Update own profile |
| PUT | `/api/auth/change-password` | Change password |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List users (paginated, filterable) |
| GET | `/api/users/{id}` | Get user detail |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Soft-delete user |
| POST | `/api/users/{id}/activate` | Activate user |
| POST | `/api/users/{id}/deactivate` | Deactivate user |

### Roles & Permissions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/roles` | List roles |
| POST | `/api/roles` | Create role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role |
| GET | `/api/roles/{id}/permissions` | Get role permissions |
| PUT | `/api/roles/{id}/permissions` | Assign permissions |

### Departments & Positions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/departments` | List departments |
| POST | `/api/departments` | Create department |
| PUT | `/api/departments/{id}` | Update department |
| GET | `/api/positions` | List positions |
| POST | `/api/positions` | Create position |
| PUT | `/api/positions/{id}` | Update position |

## User Roles

| Role | Description |
|------|-------------|
| **Super Admin** | Full system access — manage users, roles, departments, and all settings |
| **HRD** | Manage employee data, process leave/permits, create HR documents, upload policies |
| **Secretary / Admin** | Manage agendas, meeting minutes, incoming/outgoing letters, announcements |
| **Manager** | View dashboards, approve leave & documents, view reports |
| **Employee** | Submit leave & permit requests, ask policy questions, view announcements |

## Project Phases

| Phase | Focus | Status |
|-------|-------|--------|
| **Phase 1** | Foundation MVP — Auth, users, roles, departments, base layout | 🚧 In Progress |
| **Phase 2** | HR Administration — Employee data, leave management | 📋 Planned |
| **Phase 3** | Secretary Module — Meetings, agendas, documents | 📋 Planned |
| **Phase 4** | AI Helpdesk Chat — AI-powered conversational interface | 📋 Planned |
| **Phase 5** | Ticketing System — Request tracking & workflows | 📋 Planned |
| **Phase 6** | Recruitment — Job postings, candidate management | 📋 Planned |
| **Phase 7** | Hardening & Deployment — Security, performance, CI/CD | 📋 Planned |

Detailed documentation for each phase is available in the [`documentation/`](documentation/) directory.

## License

*[Add license information here]*


## Demo Account **IMPORTANT MUST READ**

http://localhost:5173/login

**Super Admin:**
-   Email: `admin@aihelpdesk.com`
-   Password: `Admin@123`

**HRD:**
-   Email: `hrd@aihelpdesk.com`
-   Password: `Hrd@12345`

**Secretary:**
-   Email: `secretary@aihelpdesk.com`
-   Password: `Secretary@123`

**Manager:**
-   Email: `manager@aihelpdesk.com`
-   Password: `Manager@123`

**Employee:**
-   Email: `employee@aihelpdesk.com`
-   Password: `Employee@123`
