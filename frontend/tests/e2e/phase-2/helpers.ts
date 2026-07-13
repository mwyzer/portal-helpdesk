import { test, expect, Page } from '@playwright/test';

export const CREDENTIALS = {
  email: 'admin@aihelpdesk.com',
  password: 'Admin@123',
};

/** Login and wait for redirect to /dashboard */
export async function login(page: Page) {
  await page.goto('/login');
  await page.fill('input[placeholder="you@company.com"]', CREDENTIALS.email);
  await page.fill('input[placeholder="••••••••"]', CREDENTIALS.password);
  await page.click('button:has-text("Sign In")');
  await page.waitForURL('/dashboard');
}

/** Navigate to a page and wait for network idle */
export async function goTo(page: Page, url: string) {
  await page.goto(url);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(300);
}

/** Open a dialog by clicking a button with exact text */
export async function openDialog(page: Page, buttonText: string) {
  await page.click(`button:has-text("${buttonText}")`);
  await page.waitForSelector('[role="dialog"]');
  await page.waitForTimeout(200);
}

/** Fill a form field by its label text */
export async function fillField(page: Page, label: string, value: string) {
  // Try label + input pattern from shadcn/ui
  const input = page.locator(`label:has-text("${label}")`).locator('..').locator('input, textarea, select');
  if ((await input.count()) > 0) {
    await input.fill(value);
    return;
  }
  // Fallback: find by placeholder containing the label
  await page.fill(`input[placeholder*="${label}"]`, value);
}

/** Select an option in a shadcn/ui select by label */
export async function selectOption(page: Page, label: string, optionText: string) {
  // Click the select trigger
  const selectTrigger = page.locator(`[role="dialog"] label:has-text("${label}")`).locator('..').locator('button[role="combobox"]');
  if ((await selectTrigger.count()) > 0) {
    await selectTrigger.click();
    await page.waitForTimeout(300);
    // Click the option
    await page.locator('[role="option"]').filter({ hasText: optionText }).first().click();
    await page.waitForTimeout(200);
  }
}

/** Submit a dialog form */
export async function submitDialog(page: Page, submitText = 'Create') {
  await page.click(`[role="dialog"] button:has-text("${submitText}")`);
  // Wait for dialog to close or toast
  await page.waitForTimeout(500);
}

/** Cancel a dialog */
export async function cancelDialog(page: Page) {
  await page.click('[role="dialog"] button:has-text("Cancel")');
  await page.waitForTimeout(300);
}

/** Assert a toast notification appeared */
export async function assertToast(page: Page, text: string) {
  // Toast container is fixed at bottom-right
  await expect(page.locator('#toast-container, [role="status"]').filter({ hasText: text })).toBeVisible({ timeout: 5000 });
}

/** Get a table row count */
export async function getRowCount(page: Page): Promise<number> {
  return await page.locator('table tbody tr').count();
}

/** Click the nth row's action button */
export async function clickRowAction(page: Page, rowIndex: number, title: string) {
  await page.locator(`table tbody tr:nth-child(${rowIndex}) button[title="${title}"]`).click();
  await page.waitForTimeout(300);
}
