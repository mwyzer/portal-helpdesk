# 🎭 E2E Testing — Playwright

> **Framework:** Playwright 1.61.1  
> **Test File:** `frontend/tests/e2e/all-phases.spec.ts`  
> **Config:** `frontend/playwright.config.ts`  
> **Output:** `/screenshots/` (21 screenshots, git-ignored)

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
└── all-phases.spec.ts    # 17 tests covering Phase 1 (13) + Phase 2 (4)
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

## Adding Tests for a New Phase

When Phase 3 (or later) is built, add tests by:

1. Open `frontend/tests/e2e/all-phases.spec.ts`
2. Add a new `test.describe` block at the bottom:

```ts
test.describe('Phase 3 — Secretary Module', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('18-meeting-minutes', async ({ page }) => {
    await snapshot(page, 'phase3-01-meeting-minutes.png', '/meeting-minutes');
    await expect(page.locator('h1')).toContainText('Meeting Minutes');
  });

  // ... more tests
});
```

3. Follow the naming convention: `phase{N}-{NN}-{slug}.png`
4. Run `npx playwright test` to verify

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
| `frontend/tests/e2e/all-phases.spec.ts` | All E2E tests |
| `frontend/package.json` | npm scripts (`test:e2e`, etc.) |
| `screenshots/` | Output directory (git-ignored) |
| `documentation/screenshots.md` | Screenshot gallery & feature checklists |
