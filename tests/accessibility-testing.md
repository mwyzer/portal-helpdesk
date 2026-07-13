# ♿ Accessibility Testing

> **Status:** 📋 Planned — not yet implemented  
> **Standard:** WCAG 2.1 AA  
> **Tool:** axe-core (via Playwright or @axe-core/react)

## Overview

Accessibility testing ensures the helpdesk is usable by all employees, including those with disabilities. This is both a legal requirement and a good practice for internal enterprise tools.

## WCAG 2.1 AA Checklist

### Perceivable
- [ ] All images have `alt` text
- [ ] Color is not the only way to convey information
- [ ] Text contrast ratio ≥ 4.5:1 (normal), ≥ 3:1 (large)
- [ ] Form inputs have visible labels

### Operable
- [ ] All functionality available via keyboard (Tab, Enter, Escape)
- [ ] Focus order is logical
- [ ] Focus indicators are visible
- [ ] No keyboard traps
- [ ] Page has a descriptive `<title>`

### Understandable
- [ ] Language is declared (`<html lang="en">`)
- [ ] Form errors are described in text (not just color)
- [ ] Labels describe the purpose of inputs
- [ ] Navigation is consistent across pages

### Robust
- [ ] Valid HTML (no unclosed tags)
- [ ] ARIA roles, states, and properties are correct
- [ ] Components work with screen readers

## Automated Testing with axe-core

### Via Playwright (recommended)

```typescript
// tests/accessibility/a11y.spec.ts
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const PAGES = [
  { name: 'login', url: '/login' },
  { name: 'dashboard', url: '/dashboard', requiresAuth: true },
  { name: 'users', url: '/users', requiresAuth: true },
  { name: 'employees', url: '/employees', requiresAuth: true },
  { name: 'leave-requests', url: '/leave-requests', requiresAuth: true },
];

for (const page of PAGES) {
  test(`${page.name} — should have no critical a11y violations`, async ({ browser }) => {
    const context = await browser.newContext();
    const pageObj = await context.newPage();

    if (page.requiresAuth) {
      await login(pageObj);
    }

    await pageObj.goto(page.url);
    await pageObj.waitForLoadState('networkidle');

    const results = await new AxeBuilder({ page: pageObj })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    // Zero critical or serious violations
    const violations = results.violations.filter(
      v => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(violations).toEqual([]);

    await context.close();
  });
}
```

Setup:
```bash
cd frontend
npm install -D @axe-core/playwright
```

### Via React (component-level)

```tsx
// tests/accessibility/LoginPage.a11y.test.tsx
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import LoginPage from '../../pages/LoginPage';

expect.extend(toHaveNoViolations);

test('Login form should have no accessibility violations', async () => {
  const { container } = render(<LoginPage />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

## shadcn/ui Accessibility

shadcn/ui components are built on Radix UI, which has good accessibility fundamentals. Verify:

| Component | A11y Concern |
|-----------|-------------|
| `Dialog` | Focus trap, Escape to close, aria-labelledby |
| `DropdownMenu` | Arrow key navigation, aria-expanded |
| `Table` | Proper `<th>` scope, caption if needed |
| `Button` | Accessible name (text or aria-label) |
| `Input` | Associated `<label>`, error message linked via aria-describedby |
| `Tabs` | aria-selected, keyboard navigation |
| `Toast` | role="alert", polite/assertive |

## Color Contrast Check

```bash
# Check Tailwind color pairs
npx color-contrast-checker --bg "#ffffff" --fg "#6b7280"  # Tailwind gray-500
npx color-contrast-checker --bg "#ffffff" --fg "#3b82f6"  # Tailwind blue-500
npx color-contrast-checker --bg "#ef4444" --fg "#ffffff"  # Red bg + white text
```

## Keyboard Navigation Test (Manual)

1. Tab through every page — can you reach all interactive elements?
2. Enter/Space activates buttons and links
3. Escape closes modals, dropdowns
4. Arrow keys navigate tables, selects, tabs
5. Focus never disappears or gets trapped in loops

## Screen Reader Test (Manual)

| Screen Reader | OS | Shortcut |
|---------------|----|---------|
| NVDA | Windows | Free download |
| VoiceOver | macOS | Cmd+F5 |
| JAWS | Windows | Licensed |

Test: Close your eyes, navigate the app using only a screen reader. Can you:
- Log in?
- Navigate to Users page?
- Search for a user?
- Submit a leave request?

## Common Violations to Fix

| Violation | Fix |
|-----------|-----|
| "Buttons must have discernible text" | Add `aria-label` to icon-only buttons |
| "Form elements must have labels" | Add `<label>` or `aria-label` to inputs |
| "Links must have discernible text" | Add text or `aria-label` to icon links |
| "id attribute value must be unique" | Fix duplicate IDs |
| "Ensures every form element has a label" | Wire up React Hook Form's `id` + `<label htmlFor>` |
| "color-contrast" | Darken text or lighten background |

## CI Integration

```yaml
accessibility:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - run: docker compose up -d
    - run: cd frontend && npx playwright test tests/accessibility/ --reporter=dot
    - name: Fail on violations
      if: failure()
      run: echo "A11y violations found — check report"
```

## Related Files

- `frontend/src/components/ui/` — shadcn/ui components (Radix-based)
- `frontend/tailwind.config.js` — Color palette
- `frontend/index.html` — `<html lang="en">` declaration
- `documentation/e2e-testing.md` — Playwright E2E test framework
