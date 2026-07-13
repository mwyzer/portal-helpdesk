# 🧩 Component Testing (Frontend)

> **Status:** 📋 Planned — not yet implemented  
> **Target:** `frontend/src/**/*.test.tsx` (co-located) or `frontend/tests/components/`

## Overview

Component tests verify React components in isolation — rendering, user interaction, props, and state changes. Unlike E2E tests (full browser), component tests run in a Node.js environment with a simulated DOM (jsdom).

## Recommended Stack

| Tool | Purpose |
|------|---------|
| **Vitest** | Test runner (fast, Vite-native, compatible with Jest API) |
| **React Testing Library** | Render components, query DOM, simulate user events |
| **jsdom** | Simulated browser DOM in Node.js |
| **@testing-library/user-event** | Realistic user interactions (type, click, select) |
| **MSW (Mock Service Worker)** | Mock API responses at the network level |

## Setup

```bash
cd frontend
npm install -D vitest @testing-library/react @testing-library/user-event @testing-library/jest-dom jsdom
```

`vitest.config.ts`:
```ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './tests/setup.ts',
  },
});
```

`tests/setup.ts`:
```ts
import '@testing-library/jest-dom';
```

## Test Patterns

### 1. Page Component — renders without crashing

```tsx
// UsersPage.test.tsx
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import UsersPage from '../pages/UsersPage';

const queryClient = new QueryClient();

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
}

test('renders users page heading', () => {
  renderWithProviders(<UsersPage />);
  expect(screen.getByRole('heading', { name: /users/i })).toBeInTheDocument();
});
```

### 2. Form — validation & submission

```tsx
test('shows validation errors on empty submit', async () => {
  const user = userEvent.setup();
  renderWithProviders(<LoginPage />);

  await user.click(screen.getByRole('button', { name: /sign in/i }));

  expect(await screen.findByText(/email is required/i)).toBeInTheDocument();
});
```

### 3. UI Component — interaction & callbacks

```tsx
// Button.test.tsx
test('calls onClick handler', async () => {
  const handleClick = vi.fn();
  render(<Button onClick={handleClick}>Click Me</Button>);

  await userEvent.click(screen.getByRole('button'));
  expect(handleClick).toHaveBeenCalledTimes(1);
});
```

### 4. With MSW — mocking API

```tsx
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

const server = setupServer(
  http.get('/api/users', () =>
    HttpResponse.json({
      items: [{ id: '1', email: 'test@test.com', fullName: 'Test' }],
      totalCount: 1,
    })
  )
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

test('renders user table with data', async () => {
  renderWithProviders(<UsersPage />);
  expect(await screen.findByText(/test@test.com/i)).toBeInTheDocument();
});
```

## What to Test (Priority Order)

| Priority | Component Type | Tests |
|----------|---------------|-------|
| 🔴 Critical | Login form | Validation, submit, error states |
| 🔴 Critical | Auth guards / ProtectedRoute | Redirect when unauthenticated |
| 🟡 High | Forms (create user, leave request) | Validation, submit, success/error |
| 🟡 High | Data tables | Empty state, loading, populated, pagination |
| 🟢 Medium | Sidebar / navigation | Links, active state, role-based visibility |
| 🟢 Medium | Modal dialogs | Open, close, confirm/cancel |
| ⚪ Low | UI components (Button, Input, Badge) | Props, variants, disabled state |

## Component Coverage Target

| Phase | Components to Test |
|-------|-------------------|
| Phase 1 | `LoginPage`, `UsersPage`, `RolesPage`, `DepartmentsPage` |
| Phase 2 | `EmployeesPage`, `LeaveRequestsPage`, `LeaveApprovalsPage` |
| Phase 3+ | Added per phase |

## npm Scripts

```json
{
  "scripts": {
    "test": "vitest",
    "test:ui": "vitest --ui",
    "test:coverage": "vitest --coverage"
  }
}
```

## Related Files

- `frontend/vitest.config.ts` — Vitest config (to be created)
- `frontend/tests/setup.ts` — Test setup with jest-dom matchers
- `frontend/src/**/*.test.tsx` — Co-located component tests
- `documentation/e2e-testing.md` — E2E browser testing
