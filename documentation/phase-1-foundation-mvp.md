# Phase 1 — Foundation (MVP)

**Tech Stack:** React (TypeScript) + ASP.NET Core Web API + PostgreSQL

---

## 1. Overview

Phase 1 establishes the foundational layer: project scaffolding, database, authentication, user/role management, and base layout. Everything else builds on top of this.

**Goal:** A working application with login, role-based access, user management, and a navigable shell layout.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | Backend scaffolding | ASP.NET Core solution with Clean Architecture modules |
| 2 | Frontend scaffolding | React + Vite + TypeScript project with routing |
| 3 | Database schema + migrations | Users, Roles, Permissions, Departments, Positions tables |
| 4 | Authentication API | Login, logout, refresh token, forgot/reset password |
| 5 | User management API + UI | CRUD users, assign roles, activate/deactivate |
| 6 | Role & permission management | RBAC with granular permissions |
| 7 | Profile management | View/edit own profile, change password |
| 8 | Base layout & navigation | Sidebar + topbar, role-based menu |
| 9 | Docker setup | Dockerfile + docker-compose for local dev |
| 10 | API documentation | Swagger/OpenAPI endpoint docs |

---

## 3. Backend Architecture

### 3.1 Solution Structure

```
src/
├── AIHelpdesk.Api/                 # API layer (controllers, middleware)
├── AIHelpdesk.Application/         # Use cases, DTOs, interfaces
├── AIHelpdesk.Domain/              # Entities, enums, value objects
├── AIHelpdesk.Infrastructure/      # EF Core, Identity, JWT, Repositories
└── AIHelpdesk.Contracts/           # Request/response DTOs shared with API
```

### 3.2 Key NuGet Packages

| Package | Purpose |
|---------|---------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Identity & auth |
| Microsoft.EntityFrameworkCore | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | PostgreSQL provider |
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT auth |
| FluentValidation | Input validation |
| Mapster | Object mapping |
| Serilog | Structured logging |
| Swashbuckle | Swagger |
| xUnit | Unit testing framework |
| Moq | Mocking framework |
| FluentAssertions | Readable assertions |
| Microsoft.AspNetCore.Mvc.Testing | Integration testing (WebApplicationFactory) |
| Bogus | Fake data generation for tests |
| Coverlet | Code coverage |

### 3.3 Database Tables (Phase 1)

| Table | Columns (key) |
|-------|---------------|
| `Users` | Id, Email, PasswordHash, FullName, IsActive, CreatedAt |
| `Roles` | Id, Name, Description |
| `Permissions` | Id, Name, Group |
| `RolePermissions` | RoleId, PermissionId |
| `UserRoles` | UserId, RoleId |
| `Departments` | Id, Name, Code, IsActive |
| `Positions` | Id, Name, DepartmentId, IsActive |
| `RefreshTokens` | Id, UserId, Token, ExpiresAt, IsRevoked |

All tables include: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

### 3.4 API Endpoints

#### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login, returns access + refresh token |
| POST | `/api/auth/refresh-token` | Refresh access token |
| POST | `/api/auth/logout` | Revoke refresh token |
| POST | `/api/auth/forgot-password` | Send reset link |
| POST | `/api/auth/reset-password` | Reset password with token |
| GET | `/api/auth/profile` | Get current user profile |
| PUT | `/api/auth/profile` | Update own profile |
| PUT | `/api/auth/change-password` | Change own password |

#### Users (Super Admin only)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List users (paginated, filterable) |
| GET | `/api/users/{id}` | Get user detail |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Soft-delete user |
| POST | `/api/users/{id}/activate` | Activate user |
| POST | `/api/users/{id}/deactivate` | Deactivate user |
| GET | `/api/users/{id}/roles` | Get user roles |
| PUT | `/api/users/{id}/roles` | Assign roles to user |

#### Roles

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/roles` | List roles |
| POST | `/api/roles` | Create role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role |
| GET | `/api/roles/{id}/permissions` | Get role permissions |
| PUT | `/api/roles/{id}/permissions` | Assign permissions |

#### Departments

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/departments` | List departments |
| POST | `/api/departments` | Create department |
| PUT | `/api/departments/{id}` | Update department |

#### Positions

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/positions` | List positions (optional ?departmentId=) |
| POST | `/api/positions` | Create position |
| PUT | `/api/positions/{id}` | Update position |

---

## 4. Frontend Architecture

### 4.1 Tech Stack

| Library | Purpose |
|---------|---------|
| React 18 + TypeScript | UI framework |
| Vite | Build tool |
| React Router v6 | Routing |
| TanStack Query | Server state & caching |
| Axios | HTTP client |
| React Hook Form + Zod | Forms & validation |
| Tailwind CSS | Styling |
| shadcn/ui | Component library |
| Zustand | Client state (auth, theme) |
| Lucide React | Icons |
| Recharts | Dashboard charts (minimal) |

### 4.2 Project Structure

```
src/
├── api/                    # Axios instance, interceptors, API functions
│   ├── client.ts           # Axios with JWT interceptor
│   ├── auth.api.ts
│   ├── users.api.ts
│   └── ...
├── assets/                 # Static assets
├── components/             # Shared UI components
│   └── ui/                 # shadcn/ui components
├── features/               # Feature modules
│   └── auth/
│       ├── components/
│       ├── hooks/
│       └── schemas/
├── hooks/                  # Shared hooks
├── layouts/                # App layouts
│   ├── AuthLayout.tsx
│   └── DashboardLayout.tsx
├── pages/                  # Page components (one per route)
├── routes/                 # Route definitions + guards
├── stores/                 # Zustand stores
├── types/                  # TypeScript types/interfaces
└── utils/                  # Helpers
```

### 4.3 Pages (Phase 1)

| Route | Page | Access |
|-------|------|--------|
| `/login` | LoginPage | Public |
| `/forgot-password` | ForgotPasswordPage | Public |
| `/reset-password` | ResetPasswordPage | Public |
| `/` | Redirect to dashboard | Authenticated |
| `/dashboard` | DashboardPage | All roles |
| `/profile` | ProfilePage | All roles |
| `/admin/users` | UserListPage | Super Admin |
| `/admin/users/new` | UserCreatePage | Super Admin |
| `/admin/users/:id` | UserDetailPage | Super Admin |
| `/admin/users/:id/edit` | UserEditPage | Super Admin |
| `/admin/roles` | RoleListPage | Super Admin |
| `/admin/roles/:id` | RoleDetailPage | Super Admin |
| `/admin/departments` | DepartmentListPage | Super Admin |
| `/admin/positions` | PositionListPage | Super Admin |

### 4.4 Layout Components

**DashboardLayout** (authenticated):
- **Sidebar**: Collapsible, shows menu items based on user role
- **Topbar**: User avatar, notifications bell, logout button
- **Content area**: Renders matched route

**AuthLayout** (public):
- Centered card layout for login/forgot-password/reset-password

### 4.5 Role-Based Menu Structure

| Menu Item | Employee | HRD | Secretary | Manager | Super Admin |
|-----------|:--------:|:---:|:---------:|:-------:|:-----------:|
| Dashboard | ✓ | ✓ | ✓ | ✓ | ✓ |
| Profile | ✓ | ✓ | ✓ | ✓ | ✓ |
| Admin > Users | | | | | ✓ |
| Admin > Roles | | | | | ✓ |
| Admin > Departments | | | | | ✓ |
| Admin > Positions | | | | | ✓ |

---

## 5. Implementation Steps

### Step 1: Repository & Project Setup
- Initialize Git repo
- Create `.gitignore` (VS + Node + .NET + Rider)
- Create `README.md` with project overview

### Step 2: Backend — Solution Scaffolding
```
dotnet new sln -n AIHelpdesk
dotnet new webapi -n AIHelpdesk.Api
dotnet new classlib -n AIHelpdesk.Domain
dotnet new classlib -n AIHelpdesk.Application
dotnet new classlib -n AIHelpdesk.Infrastructure
dotnet new classlib -n AIHelpdesk.Contracts
```
- Add project references
- Install NuGet packages
- Configure Clean Architecture folder structure

### Step 3: Backend — Domain Layer
- Create base entity class (`BaseEntity`)
- Create `User`, `Role`, `Permission`, `Department`, `Position` entities
- Create enums: `UserStatus`, `PermissionGroup`
- Create value objects: `Email`, `Password`

### Step 4: Backend — Infrastructure Layer
- Configure `AppDbContext` with EF Core + Identity
- Create migrations for all Phase 1 tables
- Implement `JwtService` (generate access + refresh tokens)
- Implement `IAuthService`, `IUserService`, `IRoleService`
- Configure Serilog logging
- Add seed data: default roles, Super Admin user, base permissions

### Step 5: Backend — Application Layer
- Create DTOs: `LoginRequest`, `LoginResponse`, `CreateUserRequest`, etc.
- Create validators with FluentValidation
- Create service interfaces
- Implement Mapster mappings

### Step 6: Backend — API Layer
- Create `AuthController`
- Create `UsersController`
- Create `RolesController`
- Create `DepartmentsController`
- Create `PositionsController`
- Add global exception middleware
- Add request logging middleware
- Configure Swagger with JWT support
- Configure CORS
- Add rate limiting middleware
- Add health check endpoint: `GET /api/health`

### Step 7: Frontend — Project Scaffolding
```
npm create vite@latest frontend -- --template react-ts
```
- Install all dependencies
- Configure Tailwind CSS
- Set up shadcn/ui
- Create folder structure

### Step 8: Frontend — API Layer
- Create Axios instance with base URL and interceptors
- Implement token refresh interceptor (automatic on 401)
- Create API functions for auth, users, roles, departments, positions

### Step 9: Frontend — Auth (Zustand Store)
- `useAuthStore`: user, tokens, isAuthenticated, login, logout, refresh
- Persist tokens in localStorage
- Auto-redirect to login on token expiry

### Step 10: Frontend — Layouts
- Build `AuthLayout` (centered card)
- Build `DashboardLayout` (sidebar + topbar + content)
- Implement responsive sidebar (collapsible, mobile drawer)
- Add breadcrumb component

### Step 11: Frontend — Auth Pages
- `LoginPage` with form validation (Zod)
- `ForgotPasswordPage`
- `ResetPasswordPage`

### Step 12: Frontend — Profile Page
- View profile
- Edit profile form
- Change password form

### Step 13: Frontend — Admin Pages
- `UserListPage` (table with search, filter, pagination)
- `UserCreatePage` / `UserEditPage` (form with role assignment)
- `UserDetailPage` (user info + roles + status)
- `RoleListPage` (table + create/edit modal)
- `RoleDetailPage` (assign permissions with checkboxes)
- `DepartmentListPage` / `PositionListPage` (simple CRUD tables)

### Step 14: Frontend — Routing & Guards
- Define all routes with React Router v6
- Create `ProtectedRoute` component (redirect if not authenticated)
- Create `RoleGuard` component (403 if insufficient permissions)

### Step 15: Docker Setup
- Create `Dockerfile` for backend
- Create `Dockerfile` for frontend (Nginx multi-stage)
- Create `docker-compose.yml` with:
  - `ai-helpdesk-db` (PostgreSQL)
  - `ai-helpdesk-backend`
  - `ai-helpdesk-frontend`
- Create `.env.example` with all environment variables
- Create `nginx.conf` for frontend reverse proxy

### Step 16: Backend — Unit Tests
- Create test project: `AIHelpdesk.UnitTests` (xUnit)
- Install Moq, FluentAssertions, Bogus, Coverlet
- Write tests for **Domain Layer**:
  - Entity validation (e.g., `User` requires email, `Role` requires name)
  - Value object equality (e.g., `Email` value object)
  - Enum state transitions
- Write tests for **Application Layer**:
  - `IAuthService` — login success/failure, invalid credentials, locked account
  - `IUserService` — CRUD operations, pagination, duplicate email detection
  - `IRoleService` — assign permissions, duplicate role check
  - FluentValidation — valid/invalid request DTOs
  - Mapster mappings — all DTO ↔ Entity mappings are correct
- Write tests for **Infrastructure Layer**:
  - `JwtService` — token generation, expiration, invalid token rejection
  - `PasswordService` — hashing, verification

### Step 17: Backend — Integration Tests
- Create test project: `AIHelpdesk.IntegrationTests`
- Use `WebApplicationFactory<Program>` for in-memory API testing
- Use test container (Testcontainers.PostgreSQL) or in-memory database
- Write tests for:
  - **Auth endpoints**: Login flow, token refresh, logout revokes token
  - **User endpoints**: CRUD with auth headers, pagination, filtering
  - **Role endpoints**: Assign permissions, role CRUD
  - **Unauthorized access**: 401 without token, 403 without permission
  - **Validation errors**: 400 with invalid input
  - **Health check**: GET `/api/health` returns 200

### Step 18: Frontend — Unit Tests
- Set up Vitest + React Testing Library + MSW (Mock Service Worker)
- Write tests for **Stores**:
  - `useAuthStore` — login sets tokens, logout clears state, token expiry detection
- Write tests for **Components**:
  - `ProtectedRoute` — redirects unauthenticated users
  - `RoleGuard` — shows 403 for insufficient permissions
  - `DashboardLayout` — sidebar renders role-appropriate menu items
- Write tests for **Pages**:
  - `LoginPage` — form validation, submit calls API, shows errors
  - `UserListPage` — renders table, search filters results, pagination works
- Write tests for **API layer**:
  - Axios interceptor attaches JWT
  - Token refresh interceptor retries on 401
- Write tests for **Hooks**:
  - Custom hooks return correct state

### Step 19: Test Automation & Coverage
- Configure `dotnet test` with Coverlet for code coverage reports
- Add `test` script to frontend `package.json` (`vitest run`)
- Set minimum coverage thresholds:
  - Domain: 90%+
  - Application: 80%+
  - Infrastructure: 70%+
  - API Controllers: 80%+
  - Frontend stores/utils: 80%+
  - Frontend components: 70%+
- Add CI test step (GitHub Actions)

### Step 20: CI/CD Pipeline Setup

#### 20.1 GitHub Actions — Continuous Integration

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: aihelpdesk_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Restore dependencies
        run: dotnet restore src/AIHelpdesk.sln

      - name: Build
        run: dotnet build src/AIHelpdesk.sln --no-restore --configuration Release

      - name: Run unit tests with coverage
        run: |
          dotnet test tests/AIHelpdesk.UnitTests/AIHelpdesk.UnitTests.csproj \
            --no-build \
            --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage-backend

      - name: Run integration tests
        run: |
          dotnet test tests/AIHelpdesk.IntegrationTests/AIHelpdesk.IntegrationTests.csproj \
            --no-build \
            --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage-integration
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=aihelpdesk_test;Username=postgres;Password=postgres"

      - name: Upload backend coverage
        uses: actions/upload-artifact@v4
        with:
          name: backend-coverage
          path: ./coverage-backend

      - name: Generate coverage report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: "coverage-backend/**/coverage.cobertura.xml"
          targetdir: "coverage-report-backend"

  frontend:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20.x
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: frontend

      - name: Lint
        run: npm run lint
        working-directory: frontend

      - name: Run unit tests with coverage
        run: npm run test:coverage
        working-directory: frontend

      - name: Build
        run: npm run build
        working-directory: frontend

      - name: Upload frontend coverage
        uses: actions/upload-artifact@v4
        with:
          name: frontend-coverage
          path: frontend/coverage

  docker-build:
    runs-on: ubuntu-latest
    needs: [backend, frontend]

    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Build backend image
        uses: docker/build-push-action@v5
        with:
          context: .
          file: src/AIHelpdesk.Api/Dockerfile
          push: false
          tags: aihelpdesk-backend:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max

      - name: Build frontend image
        uses: docker/build-push-action@v5
        with:
          context: frontend
          file: frontend/Dockerfile
          push: false
          tags: aihelpdesk-frontend:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

#### 20.2 GitHub Actions — Continuous Deployment

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Log in to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}

      - name: Build & push backend
        uses: docker/build-push-action@v5
        with:
          context: .
          file: src/AIHelpdesk.Api/Dockerfile
          push: true
          tags: |
            ${{ secrets.DOCKER_USERNAME }}/aihelpdesk-backend:latest
            ${{ secrets.DOCKER_USERNAME }}/aihelpdesk-backend:${{ github.sha }}

      - name: Build & push frontend
        uses: docker/build-push-action@v5
        with:
          context: frontend
          file: frontend/Dockerfile
          push: true
          tags: |
            ${{ secrets.DOCKER_USERNAME }}/aihelpdesk-frontend:latest
            ${{ secrets.DOCKER_USERNAME }}/aihelpdesk-frontend:${{ github.sha }}

      - name: Deploy to VPS
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          script: |
            cd /opt/aihelpdesk
            docker compose pull
            docker compose up -d --remove-orphans
            docker image prune -f
```

#### 20.3 Environment-Specific Configurations

| Environment | File | Purpose |
|-------------|------|---------|
| Local Dev | `.env` / `appsettings.Development.json` | Hot reload, debug mode, localhost DB |
| CI | CI environment variables | Ephemeral test DB, no secrets |
| Staging | `.env.staging` | Pre-production validation |
| Production | `.env.production` / Docker secrets | Hardened config, real secrets |

Create `.env.example`:
```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=aihelpdesk
DB_USER=postgres
DB_PASSWORD=postgres

# JWT
JWT_SECRET=your-256-bit-secret-key-here-change-in-production
JWT_ISSUER=AIHelpdesk
JWT_AUDIENCE=AIHelpdesk
JWT_EXPIRY_MINUTES=15
REFRESH_TOKEN_EXPIRY_DAYS=7

# Docker
ASPNETCORE_ENVIRONMENT=Development

# CORS
CORS_ORIGINS=http://localhost:5173
```

#### 20.4 Docker Multi-Stage Builds

**Backend Dockerfile** (`src/AIHelpdesk.Api/Dockerfile`):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore AIHelpdesk.Api/AIHelpdesk.Api.csproj
RUN dotnet publish AIHelpdesk.Api/AIHelpdesk.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "AIHelpdesk.Api.dll"]
```

**Frontend Dockerfile** (`frontend/Dockerfile`):
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**Nginx config** (`frontend/nginx.conf`):
```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /hubs/ {
        proxy_pass http://backend:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

#### 20.5 Health Checks & Readiness

- Backend health endpoint: `GET /api/health` returns `{ status: "Healthy", timestamp, version }`
- Docker Compose health check for PostgreSQL before backend starts
- Docker Compose health check for backend before frontend proxies
- Frontend health check via nginx `/health` passthrough

---

## 7. Seed Data

### Default Roles
| Role | Description |
|------|-------------|
| Super Admin | Full system access |
| Manager | Approves leave, views reports |
| HRD | Manages employees, processes leave |
| Secretary | Manages agenda, meetings, documents |
| Employee | Submits requests, uses AI chat |

### Default Permissions (examples)
| Permission | Group |
|------------|-------|
| `users.read` | User Management |
| `users.create` | User Management |
| `users.update` | User Management |
| `users.delete` | User Management |
| `roles.read` | Role Management |
| `roles.create` | Role Management |
| `roles.update` | Role Management |
| `roles.delete` | Role Management |
| `departments.read` | Organization |
| `departments.create` | Organization |
| `departments.update` | Organization |

### Default Super Admin
- Email: `admin@company.com`
- Password: `Admin@123` (must change on first login)

---

## 8. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | User can login with valid credentials and receive JWT |
| 2 | Expired/invalid token returns 401 and redirects to login |
| 3 | Refresh token works seamlessly (automatic) |
| 4 | Super Admin can create, edit, activate/deactivate users |
| 5 | Super Admin can assign roles to users |
| 6 | Super Admin can manage permissions per role |
| 7 | Super Admin can manage departments and positions |
| 8 | Users can view and edit their own profile |
| 9 | Users can change their password |
| 10 | Sidebar menu adapts to user role |
| 11 | Unauthorized users cannot access admin pages |
| 12 | All API endpoints return proper validation errors |
| 13 | Application runs with a single `docker-compose up` |
| 14 | Swagger UI is accessible at `/swagger` |
| 15 | Backend unit tests pass with ≥80% code coverage |
| 16 | Frontend unit tests pass with ≥70% coverage |
| 17 | Integration tests cover all auth and user CRUD flows |

---

## 9. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| Backend scaffolding & domain | 2 days |
| Infrastructure (EF, Identity, JWT) | 3 days |
| Application layer (services, DTOs) | 2 days |
| API controllers & middleware | 2 days |
| Frontend scaffolding & layouts | 2 days |
| Auth pages & stores | 2 days |
| Admin pages (users, roles) | 3 days |
| Admin pages (departments, positions) | 1 day |
| Backend unit + integration tests | 2 days |
| Frontend unit tests | 1 day |
| CI/CD pipeline (GitHub Actions, Docker, deploy) | 2 days |
| Documentation & polish | 1 day |
| **Total** | **~26 days** |

---

## 10. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| JWT refresh token complexity | Use built-in Identity + simple rotation pattern |
| Role-permission matrix too large | Start with 10 core permissions, expand later |
| Frontend bundle size | Lazy-load admin routes |
| PostgreSQL connection issues | Use Docker Compose with health checks |
| CI pipeline flakiness | Use test containers + retry mechanism |
| Docker image size | Multi-stage builds + .dockerignore |
| Secret leak in CI | Use GitHub Encrypted Secrets, never commit .env |
| Deployment downtime | Rolling update with Docker Compose + health checks |
