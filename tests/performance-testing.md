# ⚡ Performance Testing

> **Status:** 📋 Planned — not yet implemented  
> **Tool:** k6 (Grafana) or NBomber (.NET)  
> **Target:** Identify bottlenecks before production

## Overview

Performance testing measures how the system behaves under load — response times, throughput, and resource usage. The goal is to find the breaking point and ensure acceptable response times under normal and peak loads.

## Performance Targets

| Metric | Target | Critical |
|--------|--------|----------|
| **P50 response time** | < 200ms | < 500ms |
| **P95 response time** | < 500ms | < 1s |
| **P99 response time** | < 1s | < 2s |
| **Throughput** | 100 req/s | 500 req/s |
| **Error rate** | < 0.1% | < 1% |
| **Concurrent users** | 50 | 200 |

## Load Test Scenarios

### Scenario 1: Login Spike (Monday morning)

```javascript
// k6 script — tests/login-load.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // ramp up to 20 users
    { duration: '1m', target: 50 },   // spike to 50
    { duration: '30s', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const res = http.post('http://localhost:5192/api/auth/login', JSON.stringify({
    email: 'admin@aihelpdesk.com',
    password: 'Admin@123',
  }), { headers: { 'Content-Type': 'application/json' } });

  check(res, {
    'status 200': (r) => r.status === 200,
    'has token': (r) => r.json('accessToken') !== '',
  });

  sleep(1);
}
```

### Scenario 2: User List (high read)

```javascript
// tests/user-list-load.js
export const options = {
  stages: [
    { duration: '2m', target: 50 },
    { duration: '3m', target: 50 },
    { duration: '1m', target: 0 },
  ],
};

export default function () {
  const token = getToken(); // helper
  const res = http.get('http://localhost:5192/api/users?page=1&pageSize=50', {
    headers: { Authorization: `Bearer ${token}` },
  });
  check(res, { 'status 200': (r) => r.status === 200 });
}
```

### Scenario 3: Leave Request Submission (write-heavy)

```javascript
export default function () {
  const token = getToken();
  const res = http.post('http://localhost:5192/api/leave-requests', JSON.stringify({
    leaveTypeId: '...',
    startDate: '2026-07-20',
    endDate: '2026-07-21',
    reason: 'Performance test leave',
  }), {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });
  check(res, { 'created': (r) => r.status === 201 });
}
```

### Scenario 4: Endurance Test (24-hour soak)

```javascript
export const options = {
  stages: [
    { duration: '5m', target: 20 },
    { duration: '23h50m', target: 20 },
    { duration: '5m', target: 0 },
  ],
};
```

## Running with k6

```bash
# Install k6
winget install k6

# Run load test
k6 run tests/performance/login-load.js

# With real-time dashboard
k6 run --out web-dashboard tests/performance/login-load.js

# Output JSON for analysis
k6 run --out json=results.json tests/performance/login-load.js
```

## .NET Alternative: NBomber

```csharp
// Preferred if team is .NET-focused
var scenario = Scenario.Create("login_load", async context =>
{
    var response = await http.Post("http://localhost:5192/api/auth/login")
        .WithBody(new StringContent(JsonSerializer.Serialize(new
        {
            email = "admin@aihelpdesk.com",
            password = "Admin@123"
        })))
        .SendAsync(context.CancellationToken);

    return response.StatusCode == HttpStatusCode.OK
        ? Response.Ok()
        : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
);

NBomberRunner.RegisterScenarios(scenario).Run();
```

## Database Performance

### Identify Slow Queries

```sql
-- Enable query logging temporarily
SET log_min_duration_statement = 100; -- log queries > 100ms

-- Check for missing indexes
SELECT schemaname, tablename, seq_scan, seq_tup_read,
       idx_scan, seq_tup_read / seq_scan AS avg_seq_tup
FROM pg_stat_user_tables
WHERE seq_scan > 0
ORDER BY seq_tup_read DESC;
```

### Connection Pooling

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aihelpdesk;Username=helpdesk;Password=helpdesk123;Maximum Pool Size=100;Minimum Pool Size=10"
  }
}
```

## Monitoring During Tests

| What | Tool |
|------|------|
| API metrics | `dotnet-counters monitor -p <pid>` |
| Database queries | `SELECT * FROM pg_stat_activity;` |
| Container resources | `docker stats` |
| Memory leaks | `dotnet-dump`, `dotnet-gcdump` |

```bash
# .NET runtime metrics during load test
dotnet-counters monitor --process-id $(pgrep dotnet) \
  --counters System.Runtime,Microsoft.AspNetCore.Hosting
```

## CI Integration

```yaml
# GitHub Actions — run quick smoke performance test
performance:
  runs-on: ubuntu-latest
  services:
    postgres:
      image: pgvector/pgvector:pg17
  steps:
    - run: docker compose up -d
    - run: k6 run tests/performance/smoke.js
```

## Related Files

- `docker-compose.yml` — Container resource limits
- `src/AIHelpdesk.Api/appsettings.json` — Connection pool config
- `src/AIHelpdesk.Api/Program.cs` — Middleware pipeline
- `documentation/resilience-and-failure-testing.md` — Resilience testing
