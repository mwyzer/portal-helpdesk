import { test, expect } from '@playwright/test';
import { login, goTo, openDialog, fillField, submitDialog, cancelDialog, getRowCount, clickRowAction } from './helpers';

test.describe('Phase 2 — Leave Requests E2E', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await goTo(page, '/leave-requests');
  });

  test('smoke — page loads with heading and leave history', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Leave Requests');
    await expect(page.locator('text=Leave History')).toBeVisible();
  });

  test('should see Apply Leave and Refresh buttons', async ({ page }) => {
    await expect(page.locator('button:has-text("Apply Leave")')).toBeVisible();
    await expect(page.locator('button:has-text("Refresh")')).toBeVisible();
  });

  test('should show leave balance cards', async ({ page }) => {
    // Leave balance cards show days per year for each leave type
    await expect(page.locator('text=days per year').first()).toBeVisible({ timeout: 5000 });
  });

  test('should open and close Apply for Leave dialog', async ({ page }) => {
    await openDialog(page, 'Apply Leave');
    await expect(page.locator('[role="dialog"]')).toContainText('Apply for Leave');

    // Check key form fields
    await expect(page.locator('[role="dialog"] label:has-text("Leave Type")')).toBeVisible();
    await expect(page.locator('[role="dialog"] label:has-text("Start Date")')).toBeVisible();
    await expect(page.locator('[role="dialog"] label:has-text("End Date")')).toBeVisible();
    await expect(page.locator('[role="dialog"] label:has-text("Reason")')).toBeVisible();

    await cancelDialog(page);
  });

  test('should fill apply form and cancel (draft not persisted via UI)', async ({ page }) => {
    await openDialog(page, 'Apply Leave');

    // Select leave type if available
    const selectTrigger = page.locator('[role="dialog"] button[role="combobox"]').first();
    if (await selectTrigger.isVisible()) {
      await selectTrigger.click();
      await page.waitForTimeout(300);
      const firstOption = page.locator('[role="option"]').first();
      if (await firstOption.isVisible()) {
        await firstOption.click();
      }
    }

    // Fill dates
    const today = new Date();
    const nextWeek = new Date(today);
    nextWeek.setDate(nextWeek.getDate() + 7);

    await fillField(page, 'Start Date', today.toISOString().split('T')[0]);
    await fillField(page, 'End Date', nextWeek.toISOString().split('T')[0]);
    await fillField(page, 'Reason', 'E2E test leave request');

    await cancelDialog(page);
    await expect(page.locator('[role="dialog"]')).not.toBeVisible();
  });

  test('should view leave request detail if any exist', async ({ page }) => {
    const rowCount = await getRowCount(page);
    if (rowCount === 0) {
      // "No leave requests yet" message
      await expect(page.locator('text=No leave requests yet')).toBeVisible();
      return;
    }

    // Click view detail on first row
    await clickRowAction(page, 1, 'View detail');
    await expect(page.locator('[role="dialog"]')).toContainText('Leave Request Detail');
    await cancelDialog(page);
  });

  test('should refresh leave requests', async ({ page }) => {
    await page.click('button:has-text("Refresh")');
    await page.waitForTimeout(500);
    await expect(page.locator('text=Leave History')).toBeVisible();
  });
});
