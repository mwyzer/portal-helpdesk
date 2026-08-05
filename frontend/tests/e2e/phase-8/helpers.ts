import { APIRequestContext, Page } from '@playwright/test';

export const STAFF_CREDENTIALS = {
  email: 'admin@aihelpdesk.com',
  password: 'Admin@123',
};

async function assertOk(res: { ok(): boolean; status(): number; text(): Promise<string> }, label: string) {
  if (!res.ok()) {
    throw new Error(`${label} failed: ${res.status()} ${await res.text()}`);
  }
}

/** Log in as staff via the API and return the access token + user id (used as interviewer). */
export async function staffLogin(request: APIRequestContext): Promise<{ token: string; userId: string }> {
  const res = await request.post('/api/auth/login', { data: STAFF_CREDENTIALS });
  await assertOk(res, 'staffLogin');
  const body = await res.json();
  return { token: body.accessToken, userId: body.user.id };
}

/** Create a job vacancy via the API (candidate + slot setup needs one to attach to). */
export async function createVacancy(request: APIRequestContext, staffToken: string): Promise<string> {
  const res = await request.post('/api/job-vacancies', {
    headers: { Authorization: `Bearer ${staffToken}` },
    data: {
      title: `E2E Portal Role ${Date.now()}`,
      description: 'Created by candidate-portal E2E test',
      requirements: 'None',
      departmentId: null,
      positionId: null,
      openingsCount: 1,
    },
  });
  await assertOk(res, 'createVacancy');
  return (await res.json()).id;
}

/** Create a candidate (auto-provisions a CandidateAccount + setup token) and return its id + email. */
export async function createCandidate(
  request: APIRequestContext,
  staffToken: string,
  vacancyId: string,
): Promise<{ candidateId: string; email: string }> {
  const email = `portal-e2e-${Date.now()}-${Math.floor(Math.random() * 10000)}@example.com`;
  const res = await request.post('/api/candidates', {
    headers: { Authorization: `Bearer ${staffToken}` },
    data: {
      jobVacancyId: vacancyId,
      fullName: 'Portal E2E Candidate',
      email,
      phone: null,
      source: 'Other',
    },
  });
  await assertOk(res, 'createCandidate');
  const body = await res.json();
  return { candidateId: body.id, email };
}

/** Fetch a fresh portal setup token for a candidate (regenerates/consumes any prior one). */
export async function getPortalInviteToken(
  request: APIRequestContext,
  staffToken: string,
  candidateId: string,
): Promise<string> {
  const res = await request.post(`/api/candidates/${candidateId}/portal-invite`, {
    headers: { Authorization: `Bearer ${staffToken}` },
  });
  await assertOk(res, 'getPortalInviteToken');
  return (await res.json()).setupToken;
}

/** Open an interview slot for a vacancy, scheduled at a widely randomized future offset.
 *  The offset is randomized (30–330 days out) rather than fixed, because the interviewer
 *  conflict check compares against real committed Interview rows left behind by earlier test
 *  runs using the same staff user as interviewer — a fixed "+2 days" collides with itself
 *  whenever the suite is re-run within about half an hour. */
export async function createInterviewSlot(
  request: APIRequestContext,
  staffToken: string,
  vacancyId: string,
  interviewerId: string,
): Promise<string> {
  const daysOut = 30 + Math.floor(Math.random() * 300);
  const scheduledAt = new Date(Date.now() + daysOut * 24 * 60 * 60 * 1000).toISOString();
  const res = await request.post('/api/interviews/slots', {
    headers: { Authorization: `Bearer ${staffToken}` },
    data: {
      interviewerId,
      jobVacancyId: vacancyId,
      scheduledAt,
      durationMinutes: 30,
      type: 'Video',
    },
  });
  await assertOk(res, 'createInterviewSlot');
  return (await res.json()).id;
}

/** Full setup: vacancy + candidate + fresh invite token, ready for the /portal/activate UI flow. */
export async function setUpCandidate(request: APIRequestContext) {
  const { token: staffToken, userId: staffUserId } = await staffLogin(request);
  const vacancyId = await createVacancy(request, staffToken);
  const { candidateId, email } = await createCandidate(request, staffToken, vacancyId);
  const setupToken = await getPortalInviteToken(request, staffToken, candidateId);
  return { staffToken, staffUserId, vacancyId, candidateId, email, setupToken };
}

/** Drive the actual /portal/activate UI form to set a password and land on /portal/status. */
export async function activateViaUi(page: Page, setupToken: string, password = 'PortalTest@123') {
  await page.goto(`/portal/activate?token=${setupToken}`);
  await page.fill('#newPassword', password);
  await page.fill('#confirmPassword', password);
  await page.click('button:has-text("Activate Account")');
  await page.waitForURL('/portal/status');
}
