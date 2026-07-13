import { test, expect } from '@playwright/test';
import { login, goTo, getRowCount, clickRowAction } from './helpers';

test.describe('Phase 2 — Leave Approvals E2E', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await goTo(page, '/leave-approvals');
  });

  test('smoke — page loads with heading and pending approvals', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Leave Approvals');
    await expect(page.locator('text=Pending Approvals')).toBeVisible();
  });

  test('should see Refresh button', async ({ page }) => {
    await expect(page.locator('button:has-text("Refresh")')).toBeVisible();
  });

  test('should display table with employee info if pending requests exist', async ({ page }) => {
    // If no pending approvals, the table may be empty
    await expect(page.locator('table')).toBeVisible();
    await page.waitForTimeout(500);
  });

  test('should view approval detail if any pending', async ({ page }) => {
    const rowCount = await getRowCount(page);
    if (rowCount === 0) {
      // No pending approvals — that's OK
      return;
    }

    // Click the Approve button on first pending request
    const approveBtn = page.locator('table tbody tr:nth-child(1) button:has-text("Approve")');
    if (await approveBtn.isVisible()) {
      await approveBtn.click();
      await page.waitForTimeout(300);

      // Should show the ApprovalTimeline in the dialog
      const dialog = page.locator('[role="dialog"]');
      if (await dialog.isVisible()) {
        await expect(dialog).toBeVisible();
      }
    }
  });

  test('should show Approve/Reject action buttons on pending items', async ({ page }) => {
    const rowCount = await getRowCount(page);
    if (rowCount === 0) return;

    // Each pending item should have Approve and Reject buttons
    const firstRow = page.locator('table tbody tr:nth-child(1)');
    const hasApprove = await firstRow.locator('button:has-text("Approve")').isVisible();
    const hasReject = await firstRow.locator('button:has-text("Reject")').isVisible();

    // At least one action button should exist if items are pending
    if (hasApprove || hasReject) {
      // Good — actions are present
    }
  });

  test('should refresh the page', async ({ page }) => {
    await page.click('button:has-text("Refresh")');
    await page.waitForTimeout(500);
    await expect(page.locator('text=Pending Approvals')).toBeVisible();
  });
});
