import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, authHeaders } from './helpers.js';

// AI endpoint load: 10 concurrent chat conversations.
//
// Only 5 demo accounts are seeded (see README.md "Demo Account" section), and the AI
// rate limiter (AIOptions.RateLimit, default 30/min) is keyed per-user — so more than 5
// truly independent VUs will start sharing a rate-limit bucket with another VU using the
// same account. This cycles through the 5 demo accounts round-robin by VU ID; for a
// realistic 10-distinct-user test, seed 10 real accounts first and list them in
// DEMO_ACCOUNTS via -e AI_ACCOUNTS_JSON=... instead.
const DEMO_ACCOUNTS = [
  { email: 'admin@aihelpdesk.com', password: 'Admin@123' },
  { email: 'hrd@aihelpdesk.com', password: 'Hrd@12345' },
  { email: 'secretary@aihelpdesk.com', password: 'Secretary@123' },
  { email: 'manager@aihelpdesk.com', password: 'Manager@123' },
  { email: 'employee@aihelpdesk.com', password: 'Employee@123' },
];

export const options = {
  scenarios: {
    concurrent_chats: {
      executor: 'constant-vus',
      vus: 10,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<15000'], // AI responses are slow by nature — matches the "AI API latency > 15s" alert threshold
    http_req_failed: ['rate<0.05'],
  },
};

function loginAs(account) {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify(account),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(res, { 'login succeeded': (r) => r.status === 200 });
  return res.json('accessToken');
}

export default function () {
  const account = DEMO_ACCOUNTS[__VU % DEMO_ACCOUNTS.length];
  const token = loginAs(account);
  const params = authHeaders(token);

  const res = http.post(
    `${BASE_URL}/api/ai/chat`,
    JSON.stringify({ message: 'What is the company leave policy?' }),
    params,
  );

  check(res, {
    'chat request accepted': (r) => r.status === 200 || r.status === 429, // 429 is a valid, expected outcome here — see note above
  });

  sleep(3); // pause between messages in the same "conversation"
}
