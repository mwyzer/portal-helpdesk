# 🔗 Integration Testing

> **Status:** 📋 Planned — not yet implemented  
> **Target:** `tests/AIHelpdesk.IntegrationTests/`

## Overview

Integration tests verify that multiple components work together correctly — controller → service → database, with real DI and a real PostgreSQL instance. Unlike unit tests (in-memory, isolated), integration tests run against the full ASP.NET Core pipeline.

## Planned Approach: `WebApplicationFactory<T>`

Use `Microsoft.AspNetCore.Mvc.Testing` to spin up a test host in-process:

```csharp
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Host=localhost;Database=aihelpdesk_test;Username=helpdesk;Password=helpdesk123");
        }).CreateClient();
    }
}
```

## Test Scenarios

### Auth Flow (end-to-end)
```
POST /api/auth/login          → 200 + access token + refresh token
POST /api/auth/refresh-token  → 200 + new access token
GET  /api/auth/profile        → 200 + user info (authenticated)
POST /api/auth/logout         → 200 + refresh token revoked
```

### User CRUD (authenticated)
```
GET    /api/users              → 200 + paginated list
GET    /api/users/{id}         → 200 / 404
POST   /api/users              → 201 + created user
PUT    /api/users/{id}         → 200 / 404
DELETE /api/users/{id}         → 200 / 404
POST   /api/users/{id}/activate   → 200
POST   /api/users/{id}/deactivate → 200
```

### Leave Workflow (multi-step)
```
POST   /api/leave-requests     → 201 (employee submits)
GET    /api/leave-approvals    → 200 (manager sees pending)
POST   /api/leave-approvals/{id}/approve → 200
GET    /api/leave-requests     → 200 (status = Approved)
```

## Database Strategy

| Option | Pros | Cons |
|--------|------|------|
| **Dedicated test DB** | Real PostgreSQL behavior, pgvector support | Slower, needs Docker |
| **Testcontainers** | Spin up PostgreSQL per run | Adds Testcontainers dependency |
| **InMemory** | Fast | No pgvector, no PG-specific SQL |

**Recommendation:** Use **Testcontainers for .NET** for CI, dedicated local test DB for development.

```bash
# Start test database
docker compose -f docker-compose.test.yml up -d
```

## Related Files

- `documentation/unit-testing.md` — Unit tests with InMemory DB
- `documentation/api-testing.md` — API-focused testing
- `documentation/contract-testing.md` — API contract validation
