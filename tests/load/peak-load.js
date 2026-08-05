import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, login, authHeaders } from './helpers.js';

// Peak load: 200 concurrent users for 5 minutes.
export const options = {
  stages: [
    { duration: '1m', target: 200 },
    { duration: '4m', target: 200 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // more lenient than normal load — this is a stress scenario
    http_req_failed: ['rate<0.05'],
  },
};

let token; // module-level = isolated per VU in k6, cached across this VU's iterations

export default function () {
  if (!token) token = login();
  const params = authHeaders(token);

  const tickets = http.get(`${BASE_URL}/api/tickets?page=1&pageSize=20`, params);
  check(tickets, { 'tickets: 200': (r) => r.status === 200 });

  // /api/employees is Super Admin/HRD/Manager only -- 20 of the 50 seeded accounts (Secretary,
  // Employee) correctly get 403 here. Without expectedStatuses, k6's http_req_failed metric
  // counts any non-2xx as a failure regardless of what the check considers a pass, which
  // inflated the threshold-tracked failure rate to ~21% even though the app was behaving
  // exactly as designed (checks_succeeded was 99.78%). expectedStatuses tells k6 both codes
  // are legitimate outcomes for this specific request.
  const employees = http.get(`${BASE_URL}/api/employees?page=1&pageSize=20`, {
    ...params,
    responseCallback: http.expectedStatuses(200, 403),
  });
  check(employees, { 'employees: 200 or 403': (r) => r.status === 200 || r.status === 403 });

  sleep(Math.random() + 0.5); // shorter think time to simulate a busier peak
}
