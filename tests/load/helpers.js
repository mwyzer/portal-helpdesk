import http from 'k6/http';
import { check } from 'k6';

// Base URL of the API under test. Override with: k6 run -e BASE_URL=https://staging.example.com ...
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5192';

// Demo credentials seeded by DbSeeder (see README.md "Demo Account" section).
// Override with -e LOGIN_EMAIL=... -e LOGIN_PASSWORD=... for a non-seeded environment.
const LOGIN_EMAIL = __ENV.LOGIN_EMAIL || 'employee@aihelpdesk.com';
const LOGIN_PASSWORD = __ENV.LOGIN_PASSWORD || 'Employee@123';

/**
 * Logs in with the demo employee account and returns an access token.
 * Call once per VU (e.g. in the `setup()` function) rather than per-iteration,
 * to avoid the login endpoint itself becoming the bottleneck under load.
 */
export function login() {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email: LOGIN_EMAIL, password: LOGIN_PASSWORD }),
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
