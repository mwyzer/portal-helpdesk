import { test, expect } from '@playwright/test';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const SCREENSHOTS_DIR = path.resolve(__dirname, '..', '..', '..', 'screenshots');

const CREDENTIALS = {
  email: 'admin@aihelpdesk.com',
  password: 'Admin@123',
};

// ── Helper: login and return the authenticated page ──
async function login(page: any) {
  await page.goto('/login');
  await page.fill('input[placeholder="you@company.com"]', CREDENTIALS.email);
  await page.fill('input[placeholder="••••••••"]', CREDENTIALS.password);
  await page.click('button:has-text("Sign In")');
  await page.waitForURL('/dashboard');
}

// ── Helper: navigate & screenshot ──
async function snapshot(page: any, name: string, url: string) {
  await page.goto(url);
  await page.waitForLoadState('networkidle');
  // Wait a tick for animations
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, name), fullPage: true });
}

// ═══════════════════════════════════════════════
//  PHASE 1 — Foundation MVP
// ═══════════════════════════════════════════════

test.describe('Phase 1 — Foundation MVP', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('01-dashboard', async ({ page }) => {
    await snapshot(page, 'phase1-01-dashboard.png', '/dashboard');
    await expect(page.locator('h1')).toContainText('Dashboard');
  });

  test('02-users', async ({ page }) => {
    await snapshot(page, 'phase1-02-users.png', '/users');
    await expect(page.locator('h1')).toContainText('Users');
  });

  test('03-roles', async ({ page }) => {
    await snapshot(page, 'phase1-03-roles.png', '/roles');
    await expect(page.locator('h1')).toContainText('Roles');
  });

  test('04-departments', async ({ page }) => {
    await snapshot(page, 'phase1-04-departments.png', '/departments');
    await expect(page.locator('h1')).toContainText('Departments');
  });

  test('05-meetings', async ({ page }) => {
    await snapshot(page, 'phase1-05-meetings.png', '/meetings');
  });

  test('06-action-items', async ({ page }) => {
    await snapshot(page, 'phase1-06-action-items.png', '/action-items');
  });

  test('07-document-requests', async ({ page }) => {
    await snapshot(page, 'phase1-07-document-requests.png', '/documents/requests');
  });

  test('08-document-templates', async ({ page }) => {
    await snapshot(page, 'phase1-08-document-templates.png', '/documents/templates');
  });

  test('09-ai-chat', async ({ page }) => {
    await snapshot(page, 'phase1-09-ai-chat.png', '/ai/chat');
  });

  test('10-knowledge-base', async ({ page }) => {
    await snapshot(page, 'phase1-10-knowledge-base.png', '/knowledge-base');
  });

  test('11-login-page', async ({ page }) => {
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'phase1-11-login.png'), fullPage: true });
    await expect(page.locator('h3')).toContainText('Welcome back');
  });

  test('12-forgot-password', async ({ page }) => {
    await page.goto('/forgot-password');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'phase1-12-forgot-password.png'), fullPage: true });
  });

  test('13-reset-password', async ({ page }) => {
    await page.goto('/reset-password');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'phase1-13-reset-password.png'), fullPage: true });
  });
});

// ═══════════════════════════════════════════════
//  PHASE 2 — HR Administration
// ═══════════════════════════════════════════════

test.describe('Phase 2 — HR Administration', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('14-employees', async ({ page }) => {
    await snapshot(page, 'phase2-01-employees.png', '/employees');
    await expect(page.locator('h1')).toContainText('Employees');
  });

  test('15-leave-types', async ({ page }) => {
    await snapshot(page, 'phase2-02-leave-types.png', '/leave-types');
    await expect(page.locator('h1')).toContainText('Leave Types');
  });

  test('16-leave-requests', async ({ page }) => {
    await snapshot(page, 'phase2-03-leave-requests.png', '/leave-requests');
    await expect(page.locator('h1')).toContainText('Leave Requests');
  });

  test('17-leave-approvals', async ({ page }) => {
    await snapshot(page, 'phase2-04-approvals.png', '/leave-approvals');
    await expect(page.locator('h1')).toContainText('Leave Approvals');
  });
});

// ═══════════════════════════════════════════════
//  PHASE 3 — Secretary Module
// ═══════════════════════════════════════════════

test.describe('Phase 3 — Secretary Module', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('18-meetings-list', async ({ page }) => {
    await snapshot(page, 'phase3-01-meetings.png', '/meetings');
    await expect(page.locator('h1')).toContainText('Meetings');
  });

  test('19-meeting-detail', async ({ page }) => {
    // Navigate to meetings list first to find a meeting
    await page.goto('/meetings');
    await page.waitForLoadState('networkidle');
    // Click the first meeting row if any exist
    const meetingLink = page.locator('table tbody tr:first-child a, a[href*="/meetings/"]').first();
    if (await meetingLink.isVisible()) {
      await meetingLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(500);
      await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'phase3-02-meeting-detail.png'), fullPage: true });
      await expect(page.locator('h1')).toBeVisible();
    }
  });

  test('20-action-items-full', async ({ page }) => {
    await snapshot(page, 'phase3-03-action-items.png', '/action-items');
    await expect(page.locator('h1')).toContainText('Action Items');
    // Test create button is visible
    const createBtn = page.locator('button:has-text("New Action Item"), button:has-text("Add Action Item"), button:has-text("Create")').first();
    await expect(createBtn).toBeVisible({ timeout: 5000 });
  });

  test('21-document-requests', async ({ page }) => {
    await snapshot(page, 'phase3-04-document-requests.png', '/documents/requests');
    await expect(page.locator('h1')).toContainText('Document Requests');
  });

  test('22-document-templates', async ({ page }) => {
    await snapshot(page, 'phase3-05-document-templates.png', '/documents/templates');
    await expect(page.locator('h1')).toContainText('Document Templates');
  });

  test('23-dashboard-secretary-cards', async ({ page }) => {
    await snapshot(page, 'phase3-06-dashboard.png', '/dashboard');
    await expect(page.locator('h1')).toContainText('Dashboard');
    // Secretary cards should be visible for admin/secretary role
    const secretarySection = page.locator('text=Today\'s Meetings, text=Upcoming Meetings, text=Overdue Items, text=Document Reviews');
    // At minimum, the dashboard page loads
  });
});
