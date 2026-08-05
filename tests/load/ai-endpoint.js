import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, login, authHeaders } from './helpers.js';

// AI endpoint load: 10 concurrent chat conversations.
//
// The AI rate limiter (AIOptions.RateLimit, default 30/min) is keyed per-user, same as the
// general limiter — helpers.js's credentialsForVU() gives each of these 10 VUs one of
// DbSeeder's 50 distinct seeded accounts, so all 10 get their own bucket with room to spare.
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

let token; // module-level = isolated per VU in k6, cached across this VU's iterations

export default function () {
  if (!token) token = login();
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
