import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';

// Base URL of the API under test. Override with: k6 run -e BASE_URL=https://staging.example.com ...
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5192';

// The general rate limiter buckets by authenticated user id (see RateLimitingMiddleware.cs),
// so every VU sharing one login means the whole run shares one 300 req/min bucket regardless
// of VU count -- that's not "N concurrent users", it's one user hammering the API N times as
// fast. DbSeeder seeds exactly 50 accounts (10 each across Super Admin/HRD/Secretary/Manager/
// Employee), so for the default 50-VU scenarios each VU can get its own real identity and its
// own rate-limit bucket. Override with -e LOGIN_EMAIL=... -e LOGIN_PASSWORD=... to pin every VU
// to a single non-seeded account instead (e.g. against a staging environment).
const PINNED_EMAIL = __ENV.LOGIN_EMAIL;
const PINNED_PASSWORD = __ENV.LOGIN_PASSWORD;

const ROLE_PREFIXES = [
  { prefix: 'admin', domainLocal: 'admin', password: 'Admin' },
  { prefix: 'hrd', domainLocal: 'hrd', password: 'Hrd' },
  { prefix: 'secretary', domainLocal: 'secretary', password: 'Secretary' },
  { prefix: 'manager', domainLocal: 'manager', password: 'Manager' },
  { prefix: 'employee', domainLocal: 'employee', password: 'Employee' },
];

// Reproduces DbSeeder's 50 accounts: <role>@aihelpdesk.com / <Role>@123 for #1, then
// <role>{i}@aihelpdesk.com / <Role>@{i}123 for #2-10 (HRD's password has no "@" before 12345).
function seededAccount(index) {
  const role = ROLE_PREFIXES[Math.floor(index / 10)];
  const n = (index % 10) + 1;
  if (role.prefix === 'hrd') {
    return n === 1
      ? { email: 'hrd@aihelpdesk.com', password: 'Hrd@12345' }
      : { email: `hrd${n}@aihelpdesk.com`, password: `Hrd@${n}12345` };
  }
  return n === 1
    ? { email: `${role.domainLocal}@aihelpdesk.com`, password: `${role.password}@123` }
    : { email: `${role.domainLocal}${n}@aihelpdesk.com`, password: `${role.password}@${n}123` };
}

/** One of the 50 seeded accounts, deterministically assigned per VU so each VU keeps the same
 *  identity (and rate-limit bucket) across its own iterations, cycling if there are more than
 *  50 VUs (e.g. stress-test.js's 500-VU stage — buckets get shared 10-ways there, which is a
 *  meaningful thing to observe, not a bug). */
export function credentialsForVU() {
  if (PINNED_EMAIL) return { email: PINNED_EMAIL, password: PINNED_PASSWORD };
  return seededAccount((exec.vu.idInTest - 1) % 50);
}

/** Logs in and returns an access token. Call once per VU (cache the result in a module-level
 *  variable in the calling script -- k6 gives each VU an isolated JS runtime, so a plain
 *  module-level variable is already per-VU, not shared across VUs) rather than per-iteration,
 *  so the login endpoint itself doesn't become the bottleneck under load. */
export function login(credentials = credentialsForVU()) {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify(credentials),
    { headers: { 'Content-Type': 'application/json' } },
  );

  check(res, {
    'login succeeded': (r) => r.status === 200,
    'login returned an access token': (r) => !!r.json('accessToken'),
  });

  return res.json('accessToken');
}

export function authHeaders(token) {
  return { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };
}
