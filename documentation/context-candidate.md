# Candidate Self-Service Portal

## Context

The recruitment module (Phase 6) currently only has a staff-facing pipeline: HRD/Manager
manually create `Candidate` records, upload their CV, advance their stage, and schedule
interviews. Candidates themselves have no way to check their application status, add
supporting documents, or pick an interview time — everything is relayed by staff.

We're adding a small, separate self-service portal so candidates can log in, see their
application status, upload documents, and book an interview from slots staff make available.

The key constraint driving the design (confirmed by the user): this must be a **genuinely
separate, more restricted auth path** — not the same five internal roles (Super Admin/HRD/
Secretary/Manager/Employee) — since a candidate must never be able to see HR/leave/ticket data.
An audit of the API confirmed **16 controllers use bare `[Authorize]`** (any authenticated
user, no role check) — reusing the existing `ApplicationUser`/Identity system for candidates
would mean a candidate's JWT satisfies all 16 of those unless every one is individually
re-audited. Instead, candidates get a fully separate identity, JWT audience, and API surface,
so a candidate token cannot satisfy `[Authorize]` on any internal endpoint by construction, not
by convention.

**Scheduling model** (user-selected): staff opens interview time slots (interviewer + time +
duration); the candidate portal lists open slots and lets the candidate book one, which
converts it into a real `Interview` row. This is a new "open slot" concept layered on top of
the existing `Interview` entity, not a redesign of it.

## Architecture

### 1. New identity: `CandidateAccount` (not `ApplicationUser`)

New entity `CandidateAccount : BaseEntity` in `src/AIHelpdesk.Domain/Entities/`:
- `CandidateId` (Guid, FK → `Candidate`, unique index — 1:1)
- `PasswordHash` (string, nullable until activated)
- `IsActive` (bool)
- `InvitedAt`, `ActivatedAt`, `LastLoginAt` (DateTime?)
- `SetupToken` (string?, single-use token for the initial "set your password" link)
- `SetupTokenExpiresAt` (DateTime?)

Password hashing reuses ASP.NET Identity's `PasswordHasher<CandidateAccount>` directly (it's a
generic, standalone class — doesn't require `UserManager`/`IdentityDbContext` machinery), so no
new hashing code is needed.

Add `DbSet<CandidateAccount> CandidateAccounts` to `ApplicationDbContext`, with
`HasOne(e => e.Candidate).WithOne().HasForeignKey<CandidateAccount>(e => e.CandidateId)`,
`OnDelete(DeleteBehavior.Cascade)` (deleting a candidate removes their portal account), plus a
unique index on `CandidateId`. New migration: `Phase8_CandidatePortal`.

### 2. New interview-slot concept

New entity `InterviewSlot : BaseEntity`:
- `InterviewerId` (Guid, FK → `ApplicationUser`)
- `JobVacancyId` (Guid, FK → `JobVacancy` — slots are scoped to a vacancy so a candidate only
  sees slots relevant to the role they applied for)
- `ScheduledAt` (DateTime), `DurationMinutes` (int)
- `Status` (enum: `Open`, `Booked`, `Cancelled`)
- `BookedByCandidateId` (Guid?, FK → `Candidate`, set when booked)
- `InterviewId` (Guid?, FK → `Interview`, set when booked — links the slot to the resulting
  interview row)

Booking a slot: wrap in a transaction, re-check `Status == Open` (race guard against two
candidates booking simultaneously), reuse `InterviewService`'s existing
`EnsureNoConflictAsync(interviewerId, scheduledAt, durationMinutes)` for the interviewer
double-booking check, create the `Interview` row (`Status = Scheduled`), then mark the slot
`Booked` with `BookedByCandidateId`/`InterviewId` set.

### 3. `CandidateDocument.UploadedById` becomes nullable

Currently `UploadedById` is a non-nullable FK to `ApplicationUser` — every document assumes a
staff uploader. For candidate self-uploads there is no `ApplicationUser`, so:
- Change `CandidateDocument.UploadedById` / `UploadedBy` to nullable (`Guid?` / `ApplicationUser?`).
- `null` means "uploaded by the candidate via the portal" — no new FK needed, since
  `CandidateDocument.CandidateId` already identifies which candidate. Add this as a one-line
  comment on the property so it's not mysterious later.
- Migration updates the column nullability; existing rows keep their staff `UploadedById`.

### 4. Backend auth: second JWT scheme, not a second secret

`Program.cs` currently registers one `AddJwtBearer()` scheme (default). Add a **named** second
scheme, same signing key, **different `ValidAudience`**:

```csharp
.AddJwtBearer(options => { /* existing, staff scheme, default */ })
.AddJwtBearer("CandidatePortal", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = <same key>,
        ValidateIssuer = true,
        ValidIssuer = <same issuer>,
        ValidateAudience = true,
        ValidAudience = "AIHelpdesk-CandidatePortal",   // different from staff audience
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
```

Because the audience differs, a staff-issued token is rejected by the `CandidatePortal` scheme
and vice versa — this is enforced by the token validation itself, not by remembering to add a
role check everywhere. New candidate controllers use
`[Authorize(AuthenticationSchemes = "CandidatePortal")]`; existing controllers are untouched
(they keep using the default scheme).

`ITokenService` gets one new method: `GenerateCandidatePortalToken(CandidateAccount account)`,
parallel to the existing `GenerateAccessToken`, using the same `JwtSecurityToken` construction
but with `audience: "AIHelpdesk-CandidatePortal"` and a `candidateId` claim instead of roles.
Refresh tokens for the candidate scheme reuse the existing `RefreshToken` entity (already just
`UserId`/`Token`/`ExpiresAt` — works fine with a `CandidateAccount.Id` in `UserId` since it's
not FK-constrained to `ApplicationUser`).

### 5. New backend module: `src/AIHelpdesk.Infrastructure/Services/CandidatePortalService.cs`

New interface `ICandidatePortalService` (Application layer) with:
- `LoginAsync(email, password)` → issues candidate JWT via `ITokenService`
- `ActivateAccountAsync(setupToken, newPassword)` — consumes the invite token, sets password
- `GetMyStatusAsync(candidateId)` → stage, vacancy title, rejection reason if rejected
- `GetMyDocumentsAsync` / `UploadMyDocumentAsync` (reuses `CandidateService`'s existing
  extension/size validation constants — pull `AllowedCvExtensions`/`MaxCvSizeBytes` up to a
  shared location, e.g. a small `static class RecruitmentFileValidation`, used by both
  `CandidateService` and this new service, instead of duplicating the constants)
- `GetAvailableSlotsAsync(candidateId)` → open `InterviewSlot`s for the candidate's vacancy
- `BookSlotAsync(candidateId, slotId)` → the booking transaction described above
- `GetMyInterviewsAsync(candidateId)` → booked interviews (reuses `InterviewResponse` shape)

Staff-side additions (existing `IInterviewService`/`InterviewsController`):
- `CreateSlotAsync` / `CancelSlotAsync` / `GetSlotsAsync` (filtered by vacancy/interviewer) —
  new endpoints on `InterviewsController`, staff-only (existing role attribute), for HRD/Manager
  to open slots.

New `CandidatePortalController` at `api/candidate-portal/*`, `[Authorize(AuthenticationSchemes
= "CandidatePortal")]` on everything except `POST /login` and `POST /activate` (anonymous).

**Provisioning**: when staff creates a `Candidate` (existing `CandidateService.CreateAsync`),
also create a `CandidateAccount` with a generated `SetupToken` (mirrors
`AuthService.ForgotPasswordAsync`'s existing pattern: no SMTP is configured in this
environment, so the token is logged via `ILogger`, same documented limitation already accepted
for password resets — not solving email delivery as part of this feature). Staff can see/copy
the activation link from the candidate detail page.

### 6. Frontend: fully separate route subtree, not reusing `AppLayout`/`RoleGuard`

- New `frontend/src/lib/candidatePortalApi.ts` — a second axios instance, same shape as
  `lib/axios.ts` but reading/writing `candidatePortalAccessToken` / `candidatePortalRefreshToken`
  from `localStorage` (distinct keys so a staff session and a candidate session can never
  collide in the same browser), refreshing against
  `/api/candidate-portal/auth/refresh-token`, redirecting to `/portal/login` on failure.
- New `frontend/src/store/candidatePortalAuthStore.ts` — same shape as `authStore.ts` (and
  applies the same fix: read the persisted account from `localStorage` synchronously in the
  initial state, learned from today's `authStore.user` bug — don't repeat it here).
- New routes mounted in `App.tsx` under `/portal/*`, **outside** the existing
  `<ProtectedRoute><AppLayout /></ProtectedRoute>` tree entirely:
  - `/portal/login`, `/portal/activate` (public)
  - `/portal/status`, `/portal/documents`, `/portal/interviews` behind a new lightweight
    `CandidatePortalRoute` guard (checks `candidatePortalAuthStore.isAuthenticated` only — no
    role concept needed, there's only one "role" in this portal)
- New minimal `CandidatePortalLayout` (own header, no sidebar reuse — the internal sidebar's
  nav items are all staff-only pages that don't exist for a candidate).

## Files

**Backend (new):**
- `src/AIHelpdesk.Domain/Entities/CandidateAccount.cs`, `InterviewSlot.cs`
- `src/AIHelpdesk.Domain/Common/InterviewSlotStatus.cs` (enum, next to existing recruitment enums)
- `src/AIHelpdesk.Application/Interfaces/ICandidatePortalService.cs`
- `src/AIHelpdesk.Infrastructure/Services/CandidatePortalService.cs`
- `src/AIHelpdesk.Infrastructure/Services/RecruitmentFileValidation.cs` (shared constants,
  extracted from `CandidateService`)
- `src/AIHelpdesk.Api/Controllers/CandidatePortalController.cs`
- Migration: `Phase8_CandidatePortal`

**Backend (edit):**
- `ApplicationDbContext.cs` — 2 new `DbSet`s + `OnModelCreating` config
- `Program.cs` — second `AddJwtBearer("CandidatePortal", ...)` scheme
- `TokenService.cs` / `ITokenService` — `GenerateCandidatePortalToken`
- `CandidateDocument.cs` — nullable `UploadedById`/`UploadedBy`
- `CandidateService.cs` — use extracted `RecruitmentFileValidation`; call account provisioning
  in `CreateAsync`
- `InterviewService.cs` / `IInterviewService.cs` / `InterviewsController.cs` — slot CRUD for staff
- `DependencyInjection.cs` — register `ICandidatePortalService`

**Frontend (new):**
- `frontend/src/lib/candidatePortalApi.ts`
- `frontend/src/store/candidatePortalAuthStore.ts`
- `frontend/src/components/layout/CandidatePortalLayout.tsx`, `CandidatePortalRoute.tsx`
- `frontend/src/pages/portal/PortalLoginPage.tsx`, `PortalActivatePage.tsx`,
  `PortalStatusPage.tsx`, `PortalDocumentsPage.tsx`, `PortalInterviewsPage.tsx`

**Frontend (edit):**
- `App.tsx` — new `/portal/*` route subtree
- `InterviewsPage.tsx` (staff) — add a small "Open Slots" management section

**Tests (new, mirroring existing `tests/AIHelpdesk.Tests/Services/*ServiceTests.cs` pattern —
in-memory EF Core, `SeedUserAsync`/candidate-seeding helpers):**
- `CandidatePortalServiceTests.cs` — login, activation, status, document upload/validation,
  slot listing, slot booking (incl. the race-guard and interviewer-conflict cases)
- Extend `InterviewServiceTests.cs` for slot CRUD

## Verification

1. `dotnet build` + `dotnet ef migrations add Phase8_CandidatePortal` + `dotnet test` (full
   suite, confirm no regressions in the existing 279 plus new candidate-portal tests passing).
2. `docker compose up -d --build` (reuses the already-working local Docker stack from this
   session), then manually: create a vacancy + candidate as HRD, confirm the setup-token is
   logged, hit `/api/candidate-portal/activate` with it via curl/Swagger, log in as the
   candidate, confirm `/api/candidate-portal/status` works and that the same candidate JWT gets
   a 401 on an internal endpoint like `/api/tickets` (proving the audience isolation actually
   works, not just in theory).
3. Frontend: `npm run build` (type-check), then manually walk `/portal/login` →
   `/portal/status` → `/portal/documents` (upload) → `/portal/interviews` (book an
   open slot staff created) in the browser.
4. Re-run the Playwright E2E suite to confirm the new `/portal/*` routes don't interfere with
   existing routes (react-router path collision check) — no new E2E tests written for the
   portal itself in this pass, noted as a follow-up.
