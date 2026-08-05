import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, login, authHeaders } from './helpers.js';

// Normal load: 50 concurrent users for 10 minutes, ramped up/down gradually.
export const options = {
  stages: [
    { duration: '1m', target: 50 },
    { duration: '8m', target: 50 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests under 1s
    http_req_failed: ['rate<0.01'], // <1% error rate
  },
};

let token; // module-level = isolated per VU in k6, cached across this VU's iterations

export default function () {
  if (!token) token = login();
  const params = authHeaders(token);

  const dashboard = http.get(`${BASE_URL}/api/leave-requests?page=1&pageSize=10`, params);
  check(dashboard, { 'leave requests: 200': (r) => r.status === 200 });

  const tickets = http.get(`${BASE_URL}/api/tickets?page=1&pageSize=10`, params);
  check(tickets, { 'tickets: 200': (r) => r.status === 200 });

  const notifications = http.get(`${BASE_URL}/api/notifications/unread-count`, params);
  check(notifications, { 'unread count: 200': (r) => r.status === 200 });

  sleep(Math.random() * 2 + 1); // 1-3s think time between iterations
}
