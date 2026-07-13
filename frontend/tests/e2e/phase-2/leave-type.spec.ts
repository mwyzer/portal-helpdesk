import { test, expect } from '@playwright/test';
import { login, goTo, openDialog, fillField, submitDialog, cancelDialog, getRowCount, clickRowAction } from './helpers';

test.describe('Phase 2 — Leave Types E2E', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await goTo(page, '/leave-types');
  });

  test('smoke — page loads with heading and table', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Leave Types');
    await expect(page.locator('table')).toBeVisible();
  });

  test('should see Add Type and Refresh buttons', async ({ page }) => {
    await expect(page.locator('button:has-text("Add Type")')).toBeVisible();
    await expect(page.locator('button:has-text("Refresh")')).toBeVisible();
  });

  test('should open and close Add Leave Type dialog', async ({ page }) => {
    await openDialog(page, 'Add Type');
    await expect(page.locator('[role="dialog"]')).toContainText('Add Leave Type');

    // Check key form fields are visible
    await expect(page.locator('[role="dialog"] label:has-text("Name")')).toBeVisible();
    await expect(page.locator('[role="dialog"] label:has-text("Code")')).toBeVisible();
    await expect(page.locator('[role="dialog"] label:has-text("Days / Year")')).toBeVisible();
    await expect(page.locator('[role="dialog"] label:has-text("Paid Leave")')).toBeVisible();

    await cancelDialog(page);
  });

  test('should fill create form and cancel', async ({ page }) => {
    await openDialog(page, 'Add Type');

    await fillField(page, 'Name', 'E2E Test Leave');
    await fillField(page, 'Code', 'E2E');
    await fillField(page, 'Days / Year', '5');

    await cancelDialog(page);
    await expect(page.locator('[role="dialog"]')).not.toBeVisible();
  });

  test('should show edit dialog for existing leave type', async ({ page }) => {
    const rowCount = await getRowCount(page);
    if (rowCount === 0) {
      test.skip(true, 'No leave types to edit — seed database first');
      return;
    }

    await clickRowAction(page, 1, 'Edit');

    await expect(page.locator('[role="dialog"]')).toContainText('Edit Leave Type');
    await cancelDialog(page);
  });

  test('should refresh the page', async ({ page }) => {
    await page.click('button:has-text("Refresh")');
    await page.waitForTimeout(500);
    await expect(page.locator('table')).toBeVisible();
  });
});
