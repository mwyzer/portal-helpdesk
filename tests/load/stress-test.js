import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, login, authHeaders } from './helpers.js';

// Stress test: ramp from 0 to 500 concurrent users to find the breaking point.
// Thresholds are deliberately loose — the goal is to observe *where* things degrade
// (check the k6 summary + your APM/logs), not to pass/fail a specific SLA.
export const options = {
  stages: [
    { duration: '2m', target: 100 },
    { duration: '2m', target: 250 },
    { duration: '2m', target: 500 },
    { duration: '3m', target: 500 },
    { duration: '2m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.20'], // generous — we expect degradation, not necessarily zero errors
  },
};

let token; // module-level = isolated per VU in k6, cached across this VU's iterations

export default function () {
  if (!token) token = login();
  const params = authHeaders(token);

  const health = http.get(`${BASE_URL}/api/health`);
  check(health, { 'health check reachable': (r) => r.status === 200 || r.status === 503 });

  const tickets = http.get(`${BASE_URL}/api/tickets?page=1&pageSize=10`, params);
  check(tickets, { 'tickets reachable': (r) => r.status !== 0 });

  sleep(0.5);
}
