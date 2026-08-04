# 🎭 E2E Testing — Playwright

> **Framework:** Playwright 1.61.1  
> **Test Files:** `frontend/tests/e2e/all-phases.spec.ts` (23 smoke) + `frontend/tests/e2e/phase-2/*.spec.ts` (27 interaction) — 50 tests total  
> **Config:** `frontend/playwright.config.ts`  
> **Output:** `/screenshots/` (23 screenshots, git-ignored)  
> **Last actually run:** 2026-08-04, against a live Docker Compose stack rebuilt from current
> code — **49/50 passing** (final state, after fixing two real app bugs, raising the general
> rate limit default from 100 to 300 req/min, and fixing three test/UI-copy mismatches; one
> unrelated pre-existing test-data race remains). See `test-coverage-report.md` for the full
> writeup, including the 18/50 → 42/50 → 49/50 progression and what each fix addressed.

---

## Overview

End-to-end (E2E) tests use **Playwright** to automate a real Chromium browser against the running app (`http://localhost:5173`). Each test:

1. Logs in as Super Admin (`admin@aihelpdesk.com` / `Admin@123`)
2. Navigates to a page
3. Waits for all network requests to settle
4. Captures a full-page screenshot
5. Asserts the page title (h1) is visible

Tests are **serial** (1 worker) to avoid login race conditions and keep screenshots consistent.

## Prerequisites

- Frontend running at `http://localhost:5173` (via `docker compose up -d` or `npm run dev`)
- Backend API running at `http://localhost:5192`
- PostgreSQL running at `localhost:5432`
- Node.js dependencies installed (`npm install` in `frontend/`)
- Playwright Chromium browser installed (`npx playwright install chromium`)

## Configuration

`frontend/playwright.config.ts`:
```ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'off',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
});
```

Key settings:
| Setting | Value | Rationale |
|---------|-------|-----------|
| `workers: 1` | Serial | Prevents multiple logins racing against each other |
| `fullyParallel: false` | Serialized suites | Each `test.describe` block runs sequentially |
| `timeout: 30_000` | 30 seconds | Enough for slow page loads + networkidle |
| `viewport` | 1440×900 | Desktop-optimized screenshots |
| `baseURL` | `http://localhost:5173` | Vite dev server |

## Test Structure

```
frontend/tests/e2e/
├── all-phases.spec.ts             # 23 smoke tests: Phase 1 (13) + Phase 2 (4) + Phase 3 (6)
└── phase-2/
    ├── employee.spec.ts           # 8 interaction tests
    ├── leave-type.spec.ts         # 6 interaction tests
    ├── leave-request.spec.ts      # 7 interaction tests
    ├── leave-approvals.spec.ts    # 6 interaction tests
    └── helpers.ts                 # Shared login/dialog/form helpers
```

### Helpers

```ts
// Login and wait for redirect to /dashboard
async function login(page: Page) {
  await page.goto('/login');
  await page.fill('input[placeholder="you@company.com"]', 'admin@aihelpdesk.com');
  await page.fill('input[placeholder="••••••••"]', 'Admin@123');
  await page.click('button:has-text("Sign In")');
  await page.waitForURL('/dashboard');
}

// Navigate, wait for idle, take full-page screenshot
async function snapshot(page: Page, name: string, url: string) {
  await page.goto(url);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);        // allow animations to settle
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, name), fullPage: true });
}
```

### ES Module Compatibility

The project uses `"type": "module"` in `package.json`. `__dirname` is not available — use the `import.meta.url` pattern instead:

```ts
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
```

## Running Tests

```bash
cd frontend

# Run all E2E tests (headless)
npx playwright test

# Run with list reporter
npx playwright test --reporter=list

# Run a single test by name
npx playwright test -g "01-dashboard"

# Interactive UI mode (browser visible, step debugging)
npx playwright test --ui

# Generate & open HTML report
npx playwright show-report
```

### npm Scripts

These are defined in `frontend/package.json`:

```json
{
  "scripts": {
    "test:e2e": "npx playwright test",
    "test:e2e:ui": "npx playwright test --ui",
    "test:e2e:report": "npx playwright show-report"
  }
}
```

Usage:
```bash
npm run test:e2e         # headless run
npm run test:e2e:ui      # interactive debugger
npm run test:e2e:report  # view last report
```

## Test Coverage

### Phase 1 — Foundation MVP (13 tests)

| # | Test | URL | Assertion |
|---|------|-----|-----------|
| 01 | Dashboard | `/dashboard` | h1 contains "Dashboard" |
| 02 | Users | `/users` | h1 contains "Users" |
| 03 | Roles | `/roles` | h1 contains "Roles" |
| 04 | Departments | `/departments` | h1 contains "Departments" |
| 05 | Meetings | `/meetings` | — |
| 06 | Action Items | `/action-items` | — |
| 07 | Document Requests | `/documents/requests` | — |
| 08 | Document Templates | `/documents/templates` | — |
| 09 | AI Chat | `/ai/chat` | — |
| 10 | Knowledge Base | `/knowledge-base` | — |
| 11 | Login | `/login` | h3 contains "Welcome back" |
| 12 | Forgot Password | `/forgot-password` | — |
| 13 | Reset Password | `/reset-password` | — |

### Phase 2 — HR Administration (4 tests)

| # | Test | URL | Assertion |
|---|------|-----|-----------|
| 14 | Employees | `/employees` | h1 contains "Employees" |
| 15 | Leave Types | `/leave-types` | h1 contains "Leave Types" |
| 16 | Leave Requests | `/leave-requests` | h1 contains "Leave Requests" |
| 17 | Leave Approvals | `/leave-approvals` | h1 contains "Leave Approvals" |

### Phase 2 — HR Administration — Interaction Tests (27 tests)

In addition to the smoke tests above, `frontend/tests/e2e/phase-2/` covers the Phase 2 pages
with dialog/form/search/CRUD interaction tests:

| Spec File | Tests | Covers |
|-----------|-------|--------|
| `employee.spec.ts` | 8 | Page load, toolbar buttons, add dialog, search, import, validation, form + cancel, export |
| `leave-type.spec.ts` | 6 | Page load, buttons, add dialog, form fields, edit, refresh |
| `leave-request.spec.ts` | 7 | Page load, buttons, balance cards, apply dialog, form fill, detail, refresh |
| `leave-approvals.spec.ts` | 6 | Page load, refresh, table, approve detail, action buttons |

### Phase 3 — Secretary Module (6 tests)

| # | Test | URL | Assertion |
|---|------|-----|-----------|
| 18 | Meetings List | `/meetings` | h1 contains "Meetings" |
| 19 | Meeting Detail | `/meetings` → first row | h1 visible |
| 20 | Action Items | `/action-items` | h1 contains "Action Items" + create button |
| 21 | Document Requests | `/documents/requests` | h1 contains "Document Requests" |
| 22 | Document Templates | `/documents/templates` | h1 contains "Document Templates" |
| 23 | Dashboard (Secretary) | `/dashboard` | h1 contains "Dashboard" |

> Phases 4–6 have no E2E coverage yet. Add smoke tests to `all-phases.spec.ts` and interaction
> tests under a new `frontend/tests/e2e/phase-{N}/` folder following the `phase-2/` pattern below.

## Adding Tests for a New Phase

When a new phase (e.g. Phase 4 — AI Helpdesk Chat) is built, add tests by:

1. Open `frontend/tests/e2e/all-phases.spec.ts`
2. Add a new `test.describe` block at the bottom:

```ts
test.describe('Phase 4 — AI Helpdesk Chat', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('24-ai-chat', async ({ page }) => {
    await snapshot(page, 'phase4-01-ai-chat.png', '/ai/chat');
    await expect(page.locator('h1')).toContainText('AI Chat');
  });

  // ... more tests
});
```

3. Follow the naming convention: `phase{N}-{NN}-{slug}.png`
4. Run `npx playwright test` to verify
5. For interaction-heavy pages (dialogs, forms, CRUD), add a spec under a new
   `frontend/tests/e2e/phase-{N}/` folder that imports the shared helpers from
   `frontend/tests/e2e/phase-2/helpers.ts`.

## Troubleshooting

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| `ReferenceError: __dirname is not defined` | ES module scope | Use `fileURLToPath(import.meta.url)` pattern |
| Test hangs on login | Backend API not running | `docker compose up -d` |
| `page.fill` timeout | Wrong selector | Inspect input placeholder in DevTools |
| Screenshot shows empty table | No seed data for that entity | Seed via API or check DB |
| `waitForURL('/dashboard')` times out | Wrong credentials | Verify `admin@aihelpdesk.com` / `Admin@123` |
| Leave pages show empty state | Admin has no Employee record | Expected — create employee record for full flow |

## Related Files

| File | Purpose |
|------|---------|
| `frontend/playwright.config.ts` | Playwright configuration |
| `frontend/tests/e2e/all-phases.spec.ts` | 23 smoke tests (Phase 1–3) |
| `frontend/tests/e2e/phase-2/*.spec.ts` | 27 interaction tests (Phase 2) |
| `frontend/tests/e2e/phase-2/helpers.ts` | Shared login/dialog/form helpers |
| `frontend/package.json` | npm scripts (`test:e2e`, etc.) |
| `screenshots/` | Output directory (git-ignored) |
| `documentation/screenshots.md` | Screenshot gallery & feature checklists |
