# ✅ Phase 1 — Foundation: Task Tracker

**Status Legend:** `[ ]` Not Started · `[/]` In Progress · `[x]` Done · `[!]` Blocked

---

## 1. Repository & Project Setup

- [ ] Initialize Git repository (`git init`)
- [ ] Create `.gitignore` (Visual Studio + Node + .NET + Rider)
- [ ] Create `README.md` with project overview
- [ ] Create `LICENSE` file
- [ ] Set up GitHub repository (remote origin)
- [ ] Create branch strategy: `main`, `develop`, `feature/*`

---

## 2. Backend — Solution Scaffolding

- [ ] Create .NET solution: `AIHelpdesk.sln`
- [ ] Create `AIHelpdesk.Api` (Web API project)
- [ ] Create `AIHelpdesk.Domain` (class library)
- [ ] Create `AIHelpdesk.Application` (class library)
- [ ] Create `AIHelpdesk.Infrastructure` (class library)
- [ ] Create `AIHelpdesk.Contracts` (class library)
- [ ] Add project references between layers
- [ ] Install NuGet packages (EF Core, JWT, FluentValidation, Mapster, Serilog, Swashbuckle)
- [ ] Configure Clean Architecture folder structure in each project

---

## 3. Backend — Domain Layer

- [ ] Create `BaseEntity` abstract class (Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
- [ ] Create `User` entity
- [ ] Create `Role` entity
- [ ] Create `Permission` entity
- [ ] Create `RolePermissions` join entity
- [ ] Create `UserRoles` join entity
- [ ] Create `Department` entity
- [ ] Create `Position` entity
- [ ] Create `RefreshToken` entity
- [ ] Create enums: `UserStatus`, `PermissionGroup`
- [ ] Create value objects: `Email`, `Password`

---

## 4. Backend — Infrastructure Layer

- [ ] Configure `AppDbContext` with EF Core + Identity
- [ ] Register Identity services (`AddIdentity<ApplicationUser, IdentityRole>`)
- [ ] Configure PostgreSQL connection string
- [ ] Create initial migration for all Phase 1 tables
- [ ] Apply migration to database
- [ ] Implement `JwtService` (generate access token + refresh token)
- [ ] Implement `IAuthService` (login, logout, refresh, forgot/reset password)
- [ ] Implement `IUserService` (CRUD, pagination, search, activate/deactivate)
- [ ] Implement `IRoleService` (CRUD, assign permissions)
- [ ] Implement `IDepartmentService` (CRUD)
- [ ] Implement `IPositionService` (CRUD)
- [ ] Configure Serilog (file + console sinks)
- [ ] Seed default data: 5 roles (Super Admin, Manager, HRD, Secretary, Employee)
- [ ] Seed default permissions (users.read, roles.create, etc.)
- [ ] Seed Super Admin user (`admin@company.com` / `Admin@123`)

---

## 5. Backend — Application Layer

- [ ] Create DTOs: `LoginRequest`, `LoginResponse`, `RegisterRequest`
- [ ] Create DTOs: `CreateUserRequest`, `UpdateUserRequest`, `UserResponse`, `UserListResponse`
- [ ] Create DTOs: `CreateRoleRequest`, `UpdateRoleRequest`, `RoleResponse`
- [ ] Create DTOs: `DepartmentRequest`, `DepartmentResponse`, `PositionRequest`, `PositionResponse`
- [ ] Create DTOs: `RefreshTokenRequest`, `ChangePasswordRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`
- [ ] Create FluentValidation validators for all request DTOs
- [ ] Create Mapster mapping profiles (Entity → DTO, DTO → Entity)
- [ ] Create service interfaces (`IAuthService`, `IUserService`, `IRoleService`, etc.)

---

## 6. Backend — API Layer

- [ ] Create `AuthController` (login, refresh, logout, forgot/reset password)
- [ ] Create `UsersController` (CRUD, activate/deactivate, assign roles)
- [ ] Create `RolesController` (CRUD, assign permissions)
- [ ] Create `DepartmentsController` (CRUD)
- [ ] Create `PositionsController` (CRUD)
- [ ] Add global exception middleware (`ExceptionMiddleware`)
- [ ] Add request logging middleware
- [ ] Add rate limiting middleware
- [ ] Configure Swagger with JWT Bearer token support
- [ ] Configure CORS (allow frontend origin)
- [ ] Add health check endpoint: `GET /api/health`
- [ ] Add `Program.cs` service registration and middleware pipeline

---

## 7. Frontend — Project Scaffolding

- [ ] Create Vite project: `npm create vite@latest frontend -- --template react-ts`
- [ ] Install dependencies (React Router, TanStack Query, Axios, Zustand, etc.)
- [ ] Configure Tailwind CSS (`tailwind.config.js`, `postcss.config.js`)
- [ ] Initialize shadcn/ui (`npx shadcn@latest init`)
- [ ] Create folder structure (api, components, features, hooks, layouts, pages, routes, stores, types, utils)
- [ ] Set up path aliases in `vite.config.ts` (`@/` → `src/`)

---

## 8. Frontend — API Layer

- [ ] Create Axios instance with base URL configuration
- [ ] Implement JWT interceptor (attach token to every request)
- [ ] Implement 401 interceptor (auto-refresh token on 401, retry)
- [ ] Create `auth.api.ts` (login, logout, refresh, forgot/reset password, profile)
- [ ] Create `users.api.ts` (list, get, create, update, delete, activate, deactivate)
- [ ] Create `roles.api.ts` (list, get, create, update, delete, assign permissions)
- [ ] Create `departments.api.ts` (list, create, update)
- [ ] Create `positions.api.ts` (list, create, update)

---

## 9. Frontend — Auth (Zustand Store)

- [ ] Create `useAuthStore` (user, tokens, isAuthenticated, isLoading)
- [ ] Implement `login` action (call API, store tokens in localStorage)
- [ ] Implement `logout` action (clear tokens, redirect to login)
- [ ] Implement `refreshToken` action (auto-refresh on page load)
- [ ] Implement token expiry detection (redirect to login if expired)
- [ ] Persist auth state (zustand/middleware persist with localStorage)

---

## 10. Frontend — Layouts

- [ ] Build `AuthLayout` (centered card container, app logo)
- [ ] Build `DashboardLayout` (sidebar + topbar + content area)
- [ ] Build `Sidebar` component (collapsible, role-based menu items)
- [ ] Build `Topbar` component (user avatar, notifications bell, logout button)
- [ ] Build `Breadcrumb` component
- [ ] Implement responsive sidebar (collapsible on desktop, drawer on mobile)
- [ ] Add loading skeleton for layout content area

---

## 11. Frontend — Auth Pages

- [ ] Build `LoginPage` with Zod validation schema
- [ ] Build `LoginPage` form (email, password, remember me, submit button)
- [ ] Build `LoginPage` error handling (invalid credentials, account locked)
- [ ] Build `ForgotPasswordPage` (email input, submit, success message)
- [ ] Build `ResetPasswordPage` (token from URL, new password, confirm password)

---

## 12. Frontend — Profile Page

- [ ] Build profile view (user info, roles, join date)
- [ ] Build edit profile form (name, email, phone)
- [ ] Build change password form (current, new, confirm)
- [ ] Add loading states and success/error toast notifications

---

## 13. Frontend — Admin Pages

### User Management
- [ ] Build `UserListPage` (table, search input, filter by status/role)
- [ ] Build `UserListPage` pagination component
- [ ] Build `UserCreatePage` form (name, email, password, role assignment)
- [ ] Build `UserEditPage` form (pre-filled, update fields, role assignment)
- [ ] Build `UserDetailPage` (user info card, roles list, status badge, activity log)

### Role Management
- [ ] Build `RoleListPage` (table, create/edit modal)
- [ ] Build `RoleDetailPage` (role info, permission checkboxes grouped by category)

### Organization
- [ ] Build `DepartmentListPage` (table, create/edit inline modal)
- [ ] Build `PositionListPage` (table with department filter, create/edit inline modal)

---

## 14. Frontend — Routing & Guards

- [ ] Define all public routes (login, forgot-password, reset-password)
- [ ] Define all authenticated routes (dashboard, profile, admin/*)
- [ ] Create `ProtectedRoute` component (redirect to `/login` if not authenticated)
- [ ] Create `RoleGuard` component (show 403 page if insufficient permissions)
- [ ] Define route lazy-loading with `React.lazy()` and `Suspense`

---

## 15. Docker Setup

- [ ] Create backend `Dockerfile` (multi-stage: build → runtime)
- [ ] Create frontend `Dockerfile` (multi-stage: node build → nginx)
- [ ] Create `nginx.conf` (SPA fallback, API proxy, WebSocket upgrade)
- [ ] Create `docker-compose.yml` (db, backend, frontend services)
- [ ] Add PostgreSQL service with health check
- [ ] Add backend service with depends_on + health check
- [ ] Add frontend service (nginx, depends_on backend)
- [ ] Create `.env.example` with all environment variables
- [ ] Create `.dockerignore` files (backend + frontend)

---

## 16. Backend — Unit Tests

- [ ] Create test project: `AIHelpdesk.UnitTests` (xUnit)
- [ ] Install Moq, FluentAssertions, Bogus, Coverlet
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
| Setup & Scaffolding | 12 | `[ ]` |
| Backend Domain | 11 | `[ ]` |
| Backend Infrastructure | 15 | `[ ]` |
| Backend Application | 8 | `[ ]` |
| Backend API | 12 | `[ ]` |
| Frontend Scaffolding | 6 | `[ ]` |
| Frontend API | 7 | `[ ]` |
| Frontend Auth Store | 6 | `[ ]` |
| Frontend Layouts | 7 | `[ ]` |
| Frontend Auth Pages | 5 | `[ ]` |
| Frontend Profile | 4 | `[ ]` |
| Frontend Admin Pages | 10 | `[ ]` |
| Frontend Routing | 5 | `[ ]` |
| Docker | 9 | `[ ]` |
| Backend Unit Tests | 18 | `[ ]` |
| Backend Integration Tests | 10 | `[ ]` |
| Frontend Unit Tests | 13 | `[ ]` |
| Test Automation | 10 | `[ ]` |
| CI/CD Pipeline | 17 | `[ ]` |
| **TOTAL** | **~165 tasks** | |
