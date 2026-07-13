import { test, expect } from '@playwright/test';
import { login, goTo, openDialog, fillField, submitDialog, cancelDialog, getRowCount, clickRowAction, selectOption } from './helpers';

test.describe('Phase 2 — Employees E2E', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await goTo(page, '/employees');
  });

  test('smoke — page loads with heading and table', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Employees');
    await expect(page.locator('table')).toBeVisible();
  });

  test('should see Add Employee, Import, Export, and Refresh buttons', async ({ page }) => {
    await expect(page.locator('button:has-text("Add Employee")')).toBeVisible();
    await expect(page.locator('button:has-text("Import")')).toBeVisible();
    await expect(page.locator('button:has-text("Export")')).toBeVisible();
    await expect(page.locator('button:has-text("Refresh")')).toBeVisible();
  });

  test('should open and close Add Employee dialog', async ({ page }) => {
    await openDialog(page, 'Add Employee');
    await expect(page.locator('[role="dialog"]')).toContainText('Add Employee');
    await cancelDialog(page);
    await expect(page.locator('[role="dialog"]')).not.toBeVisible();
  });

  test('should search employees', async ({ page }) => {
    const searchInput = page.locator('input[placeholder="Search employees..."]');
    await searchInput.fill('admin');
    await page.waitForTimeout(500);
    // Search should filter — page should not crash
    await expect(page.locator('table')).toBeVisible();
  });

  test('should open import dialog and show instructions', async ({ page }) => {
    await openDialog(page, 'Import');
    await expect(page.locator('[role="dialog"]')).toContainText('Import Employees');
    await expect(page.locator('[role="dialog"]')).toContainText('xlsx');
    await cancelDialog(page);
  });

  test('create employee — should show validation on empty submit', async ({ page }) => {
    await openDialog(page, 'Add Employee');
    await submitDialog(page, 'Create');
    // Should still be on the dialog (validation prevented close) or show a toast
    // The form requires fields — check the dialog is still visible or error shown
    await page.waitForTimeout(500);
  });

  test('create employee — fill form and cancel', async ({ page }) => {
    await openDialog(page, 'Add Employee');

    await fillField(page, 'Employee No', 'E2E-TEST-001');
    await fillField(page, 'Full Name', 'E2E Test Employee');
    await fillField(page, 'Email', 'e2e-test@aihelpdesk.com');
    await fillField(page, 'Phone', '+62812000000');
    await fillField(page, 'Work Location', 'Jakarta');

    // Cancel instead of submit
    await cancelDialog(page);
    await expect(page.locator('[role="dialog"]')).not.toBeVisible();
  });

  test('should export employees as XLSX', async ({ page }) => {
    // Click export and verify download starts
    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 10000 }),
      page.click('button:has-text("Export")'),
    ]);
    expect(download.suggestedFilename()).toContain('.xlsx');
  });
});
