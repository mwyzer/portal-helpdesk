# 🖼️ Visual Regression Testing

> **Status:** 🟡 Partial — Playwright screenshots captured, no diff comparison yet  
> **Tool:** Playwright (built-in screenshot + `expect(page).toHaveScreenshot()`)

## Overview

Visual regression testing detects unintended UI changes by comparing screenshots pixel-by-pixel against a known baseline. If a CSS change accidentally shifts the sidebar or breaks a table layout, visual diff catches it.

## Current State

The E2E test suite (`all-phases.spec.ts`) captures 17 full-page screenshots to `screenshots/`. These are **snapshots** (not compared against baselines). To upgrade to true visual regression:

```
screenshots/
├── phase1-01-dashboard.png         ← current: loose snapshot
├── phase1-02-users.png
└── ...

screenshots/
├── baseline/                       ← planned: approved reference
│   ├── phase1-01-dashboard.png
│   ├── phase1-02-users.png
│   └── ...
└── actual/                         ← planned: current run
    ├── phase1-01-dashboard.png
    └── diff/                       ← planned: highlighted diffs
```

## Implementing Visual Regression

### Step 1: Capture Baselines

```typescript
// First run — capture baselines
test('01-dashboard — baseline', async ({ page }) => {
  await login(page);
  await page.goto('/dashboard');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);

  await expect(page).toHaveScreenshot('dashboard.png', { fullPage: true });
});
```

This saves to `frontend/tests/e2e/__screenshots__/dashboard.png`.

### Step 2: Compare Against Baselines

On subsequent runs, Playwright compares automatically. If pixels differ:

```
Error: Screenshot comparison failed:
  1453 pixels (ratio 0.08) are different.
  Expected: tests/e2e/__screenshots__/dashboard.png
  Received: test-results/dashboard-actual.png
      Diff: test-results/dashboard-diff.png
```

### Step 3: Update Baselines

```bash
# After intentional UI changes
npx playwright test --update-snapshots
```

## Per-Component Visual Tests

```typescript
test('login button — primary variant', async ({ page }) => {
  await page.goto('/login');

  const button = page.locator('button:has-text("Sign In")');
  await expect(button).toHaveScreenshot('login-button.png');
});

test('sidebar — expanded state', async ({ page }) => {
  await login(page);
  const sidebar = page.locator('aside');
  await expect(sidebar).toHaveScreenshot('sidebar.png');
});

test('user table — with data', async ({ page }) => {
  await login(page);
  await page.goto('/users');
  await page.waitForLoadState('networkidle');

  const table = page.locator('table');
  await expect(table).toHaveScreenshot('users-table.png');
});
```

## Visual Diff Strategy

| Approach | Pros | Cons |
|----------|------|------|
| **Playwright built-in** | Zero extra deps, same config | Limited threshold tuning |
| **Percy / Chromatic** | Great CI dashboard, review workflow | Paid, external service |
| **BackstopJS** | Free, configurable thresholds | Separate config, headless Chrome |
| **lost-pixel** | OSS, GitHub Actions native | Requires setup |

**Recommendation:** Start with Playwright built-in. Evaluate Percy/Chromatic if the team needs a review workflow.

## Threshold Configuration

```typescript
// playwright.config.ts — tune sensitivity
export default defineConfig({
  expect: {
    toHaveScreenshot: {
      threshold: 0.2,              // allow 0.2% pixel difference
      maxDiffPixelRatio: 0.01,     // max 1% of pixels can differ
      animations: 'disabled',       // disable CSS animations during capture
    },
  },
});
```

## Common False Positives

| Cause | Fix |
|-------|-----|
| Animations (fade-in, spinner) | `.toHaveScreenshot({ animations: 'disabled' })` |
| Time-dependent (date, "2 min ago") | Mock `Date.now()` or use fixed dates in test data |
| Random data (avatars, IDs) | Seed deterministic test data |
| Font rendering (OS differences) | Run CI on same OS (Linux) as baselines |
| Anti-aliasing differences | Increase `threshold` slightly |
| Hover states | Capture in `:hover` state explicitly |

## CI Integration

```yaml
visual:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - run: docker compose up -d
    - run: cd frontend && npx playwright test tests/visual/
    - name: Upload diffs on failure
      if: failure()
      uses: actions/upload-artifact@v4
      with:
        name: visual-diffs
        path: frontend/test-results/
```

## Visual Regression Checklist

When building a new page, add visual tests for:
- [ ] Empty state (no data)
- [ ] Loading state (spinner/skeleton)
- [ ] Populated state (5+ rows)
- [ ] Error state (API failure)
- [ ] Responsive breakpoints (mobile, tablet, desktop)
- [ ] Dark mode (if implemented)

## Related Files

- `frontend/playwright.config.ts` — Playwright configuration
- `frontend/tests/e2e/all-phases.spec.ts` — Current E2E suite
- `screenshots/` — Output directory (git-ignored)
- `documentation/e2e-testing.md` — E2E testing guide
- `documentation/component-testing.md` — Component testing
