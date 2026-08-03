# ✅ Phase 1 — Foundation: Task Tracker

**Status Legend:** `[ ]` Not Started · `[/]` In Progress · `[x]` Done · `[!]` Blocked

---

## 1. Repository & Project Setup

- [x] Initialize Git repository (`git init`)
- [x] Create `.gitignore` (Visual Studio + Node + .NET + Rider)
- [x] Create `README.md` with project overview
- [ ] Create `LICENSE` file
- [x] Set up GitHub repository (remote origin)
- [ ] Create branch strategy: `main`, `develop`, `feature/*`

---

## 2. Backend — Solution Scaffolding

- [x] Create .NET solution: `AIHelpdesk.sln`
- [x] Create `AIHelpdesk.Api` (Web API project)
- [x] Create `AIHelpdesk.Domain` (class library)
- [x] Create `AIHelpdesk.Application` (class library)
- [x] Create `AIHelpdesk.Infrastructure` (class library)
- [x] Create `AIHelpdesk.Contracts` (class library)
- [x] Add project references between layers
- [x] Install NuGet packages (EF Core, JWT, FluentValidation, Mapster, Serilog, Swashbuckle)
- [x] Configure Clean Architecture folder structure in each project

---

## 3. Backend — Domain Layer

- [x] Create `BaseEntity` abstract class (Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
- [x] Create `User` entity
- [x] Create `Role` entity
- [x] Create `Permission` entity
- [x] Create `RolePermissions` join entity
- [x] Create `UserRoles` join entity
- [x] Create `Department` entity
- [x] Create `Position` entity
- [x] Create `RefreshToken` entity
- [x] Create enums: `UserStatus`, `PermissionGroup`
- [ ] Create value objects: `Email`, `Password` — inline validation used

---

## 4. Backend — Infrastructure Layer

- [x] Configure `AppDbContext` with EF Core + Identity
- [x] Register Identity services (`AddIdentity<ApplicationUser, IdentityRole>`)
- [x] Configure PostgreSQL connection string
- [x] Create initial migration for all Phase 1 tables
- [x] Apply migration to database
- [x] Implement `JwtService` (generate access token + refresh token)
- [x] Implement `IAuthService` (login, logout, refresh, forgot/reset password) — `ForgotPasswordAsync` no-op stub fixed 2026-08-04 (now generates a real Identity reset token, logged until SMTP is wired up)
- [x] Implement `IUserService` (CRUD, pagination, search, activate/deactivate)
- [x] Implement `IRoleService` (CRUD, assign permissions)
- [x] Implement `IDepartmentService` (CRUD)
- [x] Implement `IPositionService` (CRUD)
- [x] Configure Serilog (file + console sinks)
- [x] Seed default data: 5 roles (Super Admin, Manager, HRD, Secretary, Employee)
- [x] Seed default permissions (users.read, roles.create, etc.)
- [x] Seed Super Admin user (`admin@aihelpdesk.com` / `Admin@123`)

---

## 5. Backend — Application Layer

- [x] Create DTOs: `LoginRequest`, `LoginResponse`, `RegisterRequest`
- [x] Create DTOs: `CreateUserRequest`, `UpdateUserRequest`, `UserResponse`, `UserListResponse`
- [x] Create DTOs: `CreateRoleRequest`, `UpdateRoleRequest`, `RoleResponse`
- [x] Create DTOs: `DepartmentRequest`, `DepartmentResponse`, `PositionRequest`, `PositionResponse`
- [x] Create DTOs: `RefreshTokenRequest`, `ChangePasswordRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`
- [ ] Create FluentValidation validators for all request DTOs
- [ ] Create Mapster mapping profiles (Entity → DTO, DTO → Entity) — manual mapping used
- [x] Create service interfaces (`IAuthService`, `IUserService`, `IRoleService`, etc.)

---

## 6. Backend — API Layer

- [x] Create `AuthController` (login, refresh, logout, forgot/reset password) — endpoints exist and route correctly; see note above re: `ForgotPasswordAsync` being a stub
- [x] Create `UsersController` (CRUD, activate/deactivate, assign roles)
- [x] Create `RolesController` (CRUD, assign permissions)
- [x] Create `DepartmentsController` (CRUD)
- [x] Create `PositionsController` (CRUD)
- [x] Add global exception middleware (`ExceptionMiddleware`)
- [x] Add request logging middleware
- [x] Add rate limiting middleware — `RateLimitingMiddleware` registered in `Program.cs`
- [x] Configure Swagger with JWT Bearer token support
- [x] Configure CORS (allow frontend origin)
- [x] Add health check endpoint: `GET /api/health` — fixed 2026-08-04, minimal API checking DB connectivity in `Program.cs`
- [x] Add `Program.cs` service registration and middleware pipeline

---

## 7. Frontend — Project Scaffolding

- [x] Create Vite project: `npm create vite@latest frontend -- --template react-ts`
- [x] Install dependencies (React Router, TanStack Query, Axios, Zustand, etc.)
- [x] Configure Tailwind CSS (`tailwind.config.js`, `postcss.config.js`)
- [x] Initialize shadcn/ui (`npx shadcn@latest init`)
- [x] Create folder structure (api, components, features, hooks, layouts, pages, routes, stores, types, utils)
- [x] Set up path aliases in `vite.config.ts` (`@/` → `src/`)

---

## 8. Frontend — API Layer

- [x] Create Axios instance with base URL configuration
- [x] Implement JWT interceptor (attach token to every request)
- [x] Implement 401 interceptor (auto-refresh token on 401, retry)
- [x] Create `auth.api.ts` (login, logout, refresh, forgot/reset password, profile)
- [x] Create `users.api.ts` (list, get, create, update, delete, activate, deactivate)
- [x] Create `roles.api.ts` (list, get, create, update, delete, assign permissions)
- [x] Create `departments.api.ts` (list, create, update)
- [x] Create `positions.api.ts` (list, create, update)

---

## 9. Frontend — Auth (Zustand Store)

- [x] Create `useAuthStore` (user, tokens, isAuthenticated, isLoading)
- [x] Implement `login` action (call API, store tokens in localStorage)
- [x] Implement `logout` action (clear tokens, redirect to login)
- [x] Implement `refreshToken` action (auto-refresh on page load)
- [x] Implement token expiry detection (redirect to login if expired)
- [x] Persist auth state (zustand/middleware persist with localStorage)

---

## 10. Frontend — Layouts

- [x] Build `AuthLayout` (centered card container, app logo)
- [x] Build `DashboardLayout` (sidebar + topbar + content area)
- [x] Build `Sidebar` component (collapsible, role-based menu items)
- [x] Build `Topbar` component (user avatar, notifications bell, logout button)
- [x] Build `Breadcrumb` component
- [ ] Implement responsive sidebar (collapsible on desktop, drawer on mobile)
- [ ] Add loading skeleton for layout content area

---

## 11. Frontend — Auth Pages

- [x] Build `LoginPage` with Zod validation schema
- [x] Build `LoginPage` form (email, password, remember me, submit button)
- [x] Build `LoginPage` error handling (invalid credentials, account locked)
- [x] Build `ForgotPasswordPage` (email input, submit, success message)
- [x] Build `ResetPasswordPage` (token from URL, new password, confirm password)

---

## 12. Frontend — Profile Page

- [x] Build profile view (user info, roles, join date) — DashboardPage shows user info
- [ ] Build edit profile form (name, email, phone) — via API only
- [ ] Build change password form (current, new, confirm) — via API only (AuthController)
- [x] Add loading states and success/error toast notifications

---

## 13. Frontend — Admin Pages

### User Management
- [x] Build `UserListPage` (table, search input, filter by status/role)
- [x] Build `UserListPage` pagination component
- [x] Build `UserCreatePage` form (name, email, password, role assignment) — inline dialog
- [x] Build `UserEditPage` form (pre-filled, update fields, role assignment) — inline dialog
- [ ] Build `UserDetailPage` (user info card, roles list, status badge, activity log)

### Role Management
- [x] Build `RoleListPage` (table, create/edit modal)
- [ ] Build `RoleDetailPage` (role info, permission checkboxes grouped by category)

### Organization
- [x] Build `DepartmentListPage` (table, create/edit inline modal)
- [x] Build `PositionListPage` (table with department filter, create/edit inline modal)

---

## 14. Frontend — Routing & Guards

- [x] Define all public routes (login, forgot-password, reset-password)
- [x] Define all authenticated routes (dashboard, profile, admin/*)
- [x] Create `ProtectedRoute` component (redirect to `/login` if not authenticated)
- [x] Create `RoleGuard` component — implemented and wired across ~15 routes in `App.tsx`; redirects to `/dashboard` on insufficient role rather than showing a 403 page
- [ ] Define route lazy-loading with `React.lazy()` and `Suspense`

---

## 15. Docker Setup

- [x] Create backend `Dockerfile` (multi-stage: build → runtime)
- [x] Create frontend `Dockerfile` (multi-stage: node build → nginx)
- [x] Create `nginx.conf` (SPA fallback, API proxy, WebSocket upgrade)
- [x] Create `docker-compose.yml` (db, backend, frontend services)
- [x] Add PostgreSQL service with health check
- [ ] Add backend service with depends_on + health check — **correction:** `depends_on: postgres (condition: service_healthy)` exists, but the backend service itself has no `healthcheck:` block (nothing to check against, since no `/api/health` endpoint exists)
- [x] Add frontend service (nginx, depends_on backend)
- [ ] Create `.env.example` with all environment variables
- [x] Create `.dockerignore` files (backend + frontend)

---

## 16. Backend — Unit Tests

> **Correction:** this section previously read as "not started," but a real test project exists at `tests/AIHelpdesk.Tests` with 158 passing `[Fact]`/`[Theory]` tests across all phases (not just Phase 1) — see `test-coverage-report.md` (itself dated 2026-07-14 and now stale: it's missing `AIServiceTests`/`ChatServiceTests`/`KnowledgeBaseServiceTests`, 45 more tests covering Phase 4). Phase-1-relevant coverage: `UserServiceTests` (5), `RoleServiceTests` (4), `DepartmentServiceTests` (5), `DepartmentTests` domain (2), `RefreshTokenTests` (3), `AuthContractsTests` (3), `UnitTest1` (1, placeholder) = 23 tests. The granular scenario checkboxes below aren't individually re-verified against actual test bodies — left unchecked rather than guessed.

- [x] Create test project: `AIHelpdesk.Tests` (xUnit)
- [x] Install Moq, FluentAssertions, Bogus, Coverlet
- [ ] **Domain Tests:** Entity validation (User requires email, Role requires name)
- [ ] **Domain Tests:** Value object equality (Email, Password)
- [ ] **Domain Tests:** Enum state transitions
- [ ] **Auth Tests:** Login success returns tokens
- [ ] **Auth Tests:** Invalid credentials return failure
- [ ] **Auth Tests:** Locked account returns locked message
- [ ] **Auth Tests:** Refresh token with valid token returns new tokens
- [ ] **Auth Tests:** Refresh token with revoked token returns error
- [ ] **User Tests:** Create user success, duplicate email returns error
- [ ] **User Tests:** Update user, activate/deactivate
- [ ] **User Tests:** Paginated list with search/filter
- [ ] **Role Tests:** Create role, duplicate name check
- [ ] **Role Tests:** Assign permissions to role
- [ ] **FluentValidation Tests:** Valid/invalid request DTOs for all endpoints
- [ ] **Mapster Tests:** All DTO ↔ Entity mappings resolve correctly
- [ ] **JWT Tests:** Token generation with correct claims
- [ ] **JWT Tests:** Expired token rejected, valid token accepted

---

## 17. Backend — Integration Tests

- [ ] Create test project: `AIHelpdesk.IntegrationTests`
- [ ] Set up `WebApplicationFactory<Program>` with test database
- [ ] Set up Testcontainers.PostgreSQL for integration testing
- [ ] **Auth Integration:** Full login → use token → refresh → logout flow
- [ ] **Auth Integration:** Login with invalid password returns 401
- [ ] **User Integration:** CRUD endpoints with auth headers
- [ ] **User Integration:** Pagination and filtering work
- [ ] **User Integration:** 403 when non-admin tries to access
- [ ] **Role Integration:** Create role, assign permissions
- [ ] **Health Check:** `GET /api/health` returns 200
- [ ] **Validation:** POST with invalid body returns 400 with errors
- [ ] **Unauthorized:** Endpoint without token returns 401

---

## 18. Frontend — Unit Tests

- [ ] Set up Vitest + React Testing Library + MSW
- [ ] **Store Tests:** `useAuthStore` login sets tokens correctly
- [ ] **Store Tests:** `useAuthStore` logout clears all state
- [ ] **Store Tests:** Token expiry detection works
- [ ] **Component Tests:** `ProtectedRoute` redirects unauthenticated users
- [ ] **Component Tests:** `RoleGuard` shows 403 for insufficient role
- [ ] **Component Tests:** `DashboardLayout` renders role-appropriate menu
- [ ] **Page Tests:** `LoginPage` form validation shows errors
- [ ] **Page Tests:** `LoginPage` submit calls API, shows success/error
- [ ] **Page Tests:** `UserListPage` renders table, search filters results
- [ ] **Page Tests:** `UserListPage` pagination navigates correctly
- [ ] **API Tests:** Axios interceptor attaches Bearer token
- [ ] **API Tests:** 401 interceptor triggers token refresh and retries

---

## 19. Test Automation & Coverage

- [ ] Configure `dotnet test` with Coverlet (XPlat Code Coverage)
- [ ] Add `npm run test:coverage` script to frontend
- [ ] Set coverage thresholds in `coverlet.runsettings`
- [ ] Set coverage thresholds in frontend `vitest.config.ts`
- [ ] Verify Domain coverage ≥ 90%
- [ ] Verify Application coverage ≥ 80%
- [ ] Verify Infrastructure coverage ≥ 70%
- [ ] Verify API Controllers coverage ≥ 80%
- [ ] Verify Frontend stores/utils coverage ≥ 80%
- [ ] Verify Frontend components coverage ≥ 70%

---

## 20. CI/CD Pipeline

### 20.1 Continuous Integration
- [ ] Create `.github/workflows/ci.yml` with backend job
- [ ] CI: Setup .NET 8, restore, build
- [ ] CI: Run unit tests with PostgreSQL service container
- [ ] CI: Run integration tests with PostgreSQL
- [ ] CI: Upload backend coverage artifacts
- [ ] CI: Setup Node.js 20, npm ci
- [ ] CI: Run frontend lint
- [ ] CI: Run frontend tests with coverage
- [ ] CI: Build frontend
- [ ] CI: Docker Buildx with layer caching (backend + frontend images)

### 20.2 Continuous Deployment
- [ ] Create `.github/workflows/deploy.yml`
- [ ] CD: Docker Hub login with secrets
- [ ] CD: Build & push backend image (latest + git SHA tags)
- [ ] CD: Build & push frontend image (latest + git SHA tags)
- [ ] CD: SSH into VPS, pull images, docker compose up
- [ ] CD: Prune old Docker images

### 20.3 Environment Setup
- [ ] Create `.env.example` with all variables
- [ ] Configure GitHub secrets: `DOCKER_USERNAME`, `DOCKER_PASSWORD`
- [ ] Configure GitHub secrets: `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`
- [ ] Document environment tiers: Dev, CI, Staging, Production

### 20.4 Health Checks
- [ ] Backend health endpoint returns `{ status: "Healthy" }`
- [ ] Docker Compose health check for PostgreSQL
- [ ] Docker Compose health check for backend service

---

## Summary

| Category | Total Tasks | Done |
|----------|:-----------:|:----:|
| Setup & Scaffolding | 15 | 13 |
| Backend Domain | 11 | 10 |
| Backend Infrastructure | 15 | 15 |
| Backend Application | 8 | 6 |
| Backend API | 12 | 12 |
| Frontend Scaffolding | 6 | 6 |
| Frontend API | 8 | 8 |
| Frontend Auth Store | 6 | 6 |
| Frontend Layouts | 7 | 5 |
| Frontend Auth Pages | 5 | 5 |
| Frontend Profile | 4 | 2 |
| Frontend Admin Pages | 9 | 7 |
| Frontend Routing | 5 | 4 |
| Docker | 9 | 7 |
| Backend Unit Tests | 19 | 2 (infra only — 158 real tests exist project-wide, not itemized here; see note above) |
| Backend Integration Tests | 12 | 0 |
| Frontend Unit Tests (Vitest) | 13 | 0 — note: Playwright E2E tests exist separately (`frontend/tests/e2e/`), just not Vitest unit tests |
| Test Automation | 10 | 0 |
| CI/CD Pipeline | 23 | 0 — no `.github/workflows` directory exists |
| **TOTAL** | **197 tasks** | **108 (55%)** |

Core backend + frontend for auth, users, roles, departments/positions, and layout/routing are done. The gap is almost entirely: LICENSE/env docs, a few polish items (responsive sidebar, detail pages), and the whole test-automation/CI-CD layer (no integration tests, no frontend unit tests, no CI/CD pipeline at all despite 158 real backend unit tests existing).
