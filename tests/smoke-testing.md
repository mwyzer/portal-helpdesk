# 💨 Smoke Testing

> **Status:** 📋 Planned — partially covered by Playwright E2E login  
> **Goal:** Quick health check after deployment — did we break anything critical?

## Overview

Smoke tests are a minimal, fast subset of tests that verify the application is "alive" after a deployment. If smoke tests fail, the deployment is rejected immediately. They should run in **under 60 seconds**.

## Critical Paths (Must Not Break)

```
1. Login  ──▶  2. Dashboard  ──▶  3. Users Page  ──▶  4. Create User
                                      │
                                      └──▶ 5. Leave Types
                                      └──▶ 6. Logout
```

## Smoke Test Script

### Playwright (current approach)

```typescript
// tests/smoke/smoke.spec.ts
import { test, expect } from '@playwright/test';

const CREDENTIALS = {
  email: 'admin@aihelpdesk.com',
  password: 'Admin@123',
};

test.describe('Smoke Tests', () => {
  test('login — should authenticate and reach dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[placeholder="you@company.com"]', CREDENTIALS.email);
    await page.fill('input[placeholder="••••••••"]', CREDENTIALS.password);
    await page.click('button:has-text("Sign In")');
    await page.waitForURL('/dashboard');
    await expect(page.locator('h1')).toContainText('Dashboard');
  });

  test('users — should load user list', async ({ page }) => {
    // Already logged in from previous test
    await page.goto('/users');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('h1')).toContainText('Users');
  });

  test('api health — backend responds', async ({ request }) => {
    const response = await request.get('http://localhost:5192/api/health');
    expect(response.status()).toBe(200);
  });
});
```

### Bash (for Docker deployments)

```bash
#!/bin/bash
# tests/smoke/health-check.sh
set -e

BASE_API="http://localhost:5192"

echo "=== Smoke Test $(date) ==="

# 1. API Health
echo "1. API Health..."
curl -f -s -o /dev/null $BASE_API/api/health || { echo "❌ API DOWN"; exit 1; }
echo "   ✅ API healthy"

# 2. Login
echo "2. Login..."
TOKEN=$(curl -s -X POST $BASE_API/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@aihelpdesk.com","password":"Admin@123"}' \
  | jq -r '.accessToken')
[ -z "$TOKEN" ] || [ "$TOKEN" = "null" ] && { echo "❌ Login failed"; exit 1; }
echo "   ✅ Login successful"

# 3. Users endpoint
echo "3. Users..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  $BASE_API/api/users?page=1&pageSize=1)
[ "$STATUS" != "200" ] && { echo "❌ Users returned $STATUS"; exit 1; }
echo "   ✅ Users ($STATUS)"

# 4. Database connectivity (indirect)
echo "4. Database..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  $BASE_API/api/departments)
[ "$STATUS" != "200" ] && { echo "❌ DB check returned $STATUS"; exit 1; }
echo "   ✅ DB connected"

echo "=== All smoke tests passed ✅ ==="
```

## Health Check Endpoint

A dedicated health endpoint should verify all dependencies:

```csharp
// GET /api/health
[AllowAnonymous]
[HttpGet("health")]
public async Task<IActionResult> Health()
{
    var dbOk = await _context.Database.CanConnectAsync();
    return Ok(new
    {
        status = dbOk ? "Healthy" : "Degraded",
        timestamp = DateTime.UtcNow,
        checks = new
        {
            database = dbOk ? "OK" : "FAIL",
        }
    });
}
```

Docker healthcheck in `docker-compose.yml`:
```yaml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5192/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Smoke Test Rules

| Rule | Description |
|------|-------------|
| ⏱️ 60 seconds max | If it takes longer, it's not a smoke test |
| 🔴 All or nothing | Any failure = reject deployment |
| 📦 Self-contained | No external dependencies beyond the app itself |
| 🔄 Idempotent | Can run multiple times without side effects |
| 🧹 Clean up | Don't leave test data in production |
| 📊 Read-only preferred | Avoid writes where possible |

## CI Integration

```yaml
# Run after every deployment
deploy:
  steps:
    - run: docker compose up -d
    - run: bash tests/smoke/health-check.sh
    - run: npx playwright test tests/smoke/ --reporter=dot
```

## Related Files

- `frontend/tests/e2e/all-phases.spec.ts` — Full E2E suite (superset of smoke)
- `documentation/e2e-testing.md` — E2E testing guide
- `docker-compose.yml` — Docker healthchecks
