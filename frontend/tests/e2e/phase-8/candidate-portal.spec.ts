import { test, expect } from '@playwright/test';
import { login as staffLoginUi } from '../phase-2/helpers';
import { setUpCandidate, activateViaUi, createInterviewSlot } from './helpers';

// Phase 8 — Candidate Portal. Each test provisions its own throwaway job vacancy + candidate
// via the API (mirroring how staff would create them), then drives the actual /portal/* UI —
// login, activation, status, documents, interviews — the same way a real candidate would.
// Scoping every fixture to a fresh vacancy/candidate per test keeps tests independent without
// needing to clean up afterwards.

test.describe('Phase 8 — Candidate Portal', () => {
  test('unauthenticated visit to a portal page redirects to /portal/login', async ({ page }) => {
    await page.goto('/portal/status');
    await page.waitForURL('/portal/login');
    await expect(page.locator('text=Candidate Portal')).toBeVisible();
  });

  test('candidate can activate their account and see application status', async ({ page, request }) => {
    const { setupToken } = await setUpCandidate(request);

    await activateViaUi(page, setupToken);

    await expect(page.locator('h1')).toHaveText('Application Status');
    await expect(page.getByText('Applied', { exact: true })).toBeVisible();
  });

  test('activation rejects a mismatched password confirmation', async ({ page, request }) => {
    const { setupToken } = await setUpCandidate(request);

    await page.goto(`/portal/activate?token=${setupToken}`);
    await page.fill('#newPassword', 'PortalTest@123');
    await page.fill('#confirmPassword', 'SomethingElse@123');
    await page.click('button:has-text("Activate Account")');

    await expect(page.getByText('Passwords do not match')).toBeVisible();
    // Should not have navigated away
    await expect(page).toHaveURL(/\/portal\/activate/);
  });

  test('candidate can log out and log back in with their new password', async ({ page, request }) => {
    const { email, setupToken } = await setUpCandidate(request);
    const password = 'PortalTest@123';
    await activateViaUi(page, setupToken, password);

    await page.click('button[aria-label="Log out"]');
    await page.waitForURL('/portal/login');

    await page.fill('#email', email);
    await page.fill('#password', password);
    await page.click('button:has-text("Sign In")');
    await page.waitForURL('/portal/status');
    await expect(page.locator('h1')).toHaveText('Application Status');
  });

  test('candidate can upload a document and see it listed', async ({ page, request }) => {
    const { setupToken } = await setUpCandidate(request);
    await activateViaUi(page, setupToken);

    await page.goto('/portal/documents');
    await page.locator('input[type="file"]').setInputFiles({
      name: 'test-cv.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('%PDF-1.4 minimal test file for E2E upload'),
    });

    await expect(page.getByText('test-cv.pdf')).toBeVisible({ timeout: 10000 });
  });

  test('candidate can book an available interview slot', async ({ page, request }) => {
    const { staffToken, staffUserId, vacancyId, setupToken } = await setUpCandidate(request);
    await createInterviewSlot(request, staffToken, vacancyId, staffUserId);

    await activateViaUi(page, setupToken);
    await page.goto('/portal/interviews');

    await expect(page.getByText('Available Times')).toBeVisible();
    await page.click('button:has-text("Book")');

    // Booking a slot flips hasBookedInterview, which hides the Available Times card entirely
    // and lists the new interview with a "Scheduled" badge under Your Interviews.
    await expect(page.getByText('Scheduled', { exact: true })).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Available Times')).not.toBeVisible();
  });

  test('staff can generate a portal invite link from the candidate detail page', async ({ page, request }) => {
    const { candidateId } = await setUpCandidate(request);

    await staffLoginUi(page);
    await page.goto(`/recruitment/candidates/${candidateId}`);

    await page.click('button:has-text("Copy Portal Invite Link")');
    await expect(page.getByText(/Portal invite link (copied to clipboard|generated)/)).toBeVisible({ timeout: 10000 });
  });
});
