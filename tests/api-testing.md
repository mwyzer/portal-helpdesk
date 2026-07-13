# 🌐 API Testing

> **Status:** 🟡 Partially available via Swagger + E2E tests  
> **Manual:** Swagger UI at `http://localhost:5192/swagger`

## Overview

API testing validates HTTP endpoints — status codes, response schemas, headers, and business logic through the REST interface. This complements unit tests (which test services directly) and E2E tests (which test through a browser).

## Current Capabilities

### Swagger / OpenAPI

Every endpoint is documented via Swashbuckle. Access at:
- **Swagger UI:** `http://localhost:5192/swagger`
- **OpenAPI JSON:** `http://localhost:5192/swagger/v1/swagger.json`

Use Swagger UI for manual exploration and ad-hoc testing.

### .http File

`src/AIHelpdesk.Api/AIHelpdesk.Api.http` — VS Code REST Client / JetBrains HTTP file. Can be run directly in the editor.

```http
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "admin@aihelpdesk.com",
  "password": "Admin@123"
}
```

### Playwright E2E (indirect coverage)

`frontend/tests/e2e/all-phases.spec.ts` exercises all pages end-to-end, which indirectly hits every API endpoint behind those pages.

## Planned: Automated API Tests

### Option A: xUnit + `HttpClient` + `WebApplicationFactory`

```csharp
public class UsersApiTests : IntegrationTestBase
{
    [Fact]
    public async Task GetUsers_ReturnsPaginatedList()
    {
        var response = await Client.GetAsync("/api/users?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<PaginatedResponse<UserResponse>>(body);
        users.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUsers_RequiresAuthentication()
    {
        var unauthenticatedClient = _factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Option B: Postman / Newman (CI-friendly)

Export a Postman collection and run via Newman:

```bash
npm install -g newman
newman run tests/api/aihelpdesk.postman_collection.json \
  -e tests/api/local.postman_environment.json
```

### Option C: REST Client CLI

```bash
dotnet tool install -g Microsoft.dotnet-httprepl
httprepl http://localhost:5192
```

## API Testing Checklist

| Category | What to Test |
|----------|-------------|
| **Status Codes** | 200, 201, 204, 400, 401, 403, 404, 409, 422 |
| **Auth** | Missing/expired/invalid token → 401 |
| **Authorization** | Wrong role → 403 |
| **Validation** | Missing required fields → 400 with `ProblemDetails` |
| **Pagination** | Default page, edge cases (page 0, page 999) |
| **Rate Limiting** | Too many requests → 429 (future) |
| **CORS** | Proper `Access-Control-Allow-Origin` headers |
| **Content-Type** | `application/json` on all responses |

## Related Files

- `src/AIHelpdesk.Api/AIHelpdesk.Api.http` — HTTP test file
- `http://localhost:5192/swagger` — Swagger UI
- `documentation/integration-testing.md` — Full pipeline integration tests
- `documentation/contract-testing.md` — API contract validation
