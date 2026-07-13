# 📄 Contract Testing

> **Status:** 📋 Planned — not yet implemented  
> **Related:** `src/AIHelpdesk.Contracts/` (DTOs), `http://localhost:5192/swagger/v1/swagger.json`

## Overview

Contract testing ensures that the **API provider** (backend) and **API consumer** (frontend) agree on request/response shapes. If the backend changes a field name or type, contract tests catch it before the frontend breaks.

## Contract Testing Levels

### Level 1: DTO Shape Tests (✅ Implemented)

The existing `Contracts/AuthContractsTests.cs` validates that DTO records can be created with expected values:

```csharp
[Fact]
public void LoginRequest_ShouldBeRecordType()
{
    var request = new LoginRequest("test@example.com", "Password123!");
    request.Email.Should().Be("test@example.com");
}
```

**Coverage needed:** Extend to all contracts — Users, Roles, Departments, LeaveRequests, etc.

### Level 2: JSON Schema Validation (📋 Planned)

Define JSON schemas and validate API responses against them:

```csharp
[Fact]
public async Task GetUsers_Response_MatchesSchema()
{
    var response = await Client.GetAsync("/api/users");
    var json = await response.Content.ReadAsStringAsync();

    var schema = JSchema.Parse(File.ReadAllText("schemas/users-list.json"));
    var document = JToken.Parse(json);

    document.IsValid(schema, out IList<string> errors).Should().BeTrue(
        string.Join(", ", errors));
}
```

Schema example (`schemas/users-list.json`):
```json
{
  "type": "object",
  "required": ["items", "totalCount", "page", "pageSize"],
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["id", "email", "fullName", "isActive"],
        "properties": {
          "id": { "type": "string", "format": "uuid" },
          "email": { "type": "string", "format": "email" },
          "fullName": { "type": "string", "minLength": 1 }
        }
      }
    },
    "totalCount": { "type": "integer", "minimum": 0 }
  }
}
```

### Level 3: Consumer-Driven Contracts (Future)

Use **Pact** for bidirectional contract testing:

```
                    publishes expectations
    Frontend ──────────────────────────────→ Pact Broker
                                               │
    Backend  ←────────────────────────────── pulls & verifies
```

```bash
npm install -D @pact-foundation/pact
```

## Contract Checklist

| Contract | Provider (API) | Consumer (Frontend) |
|----------|---------------|---------------------|
| `LoginRequest` / `AuthResponse` | ✅ Tested | Login page |
| `CreateUserRequest` / `UserResponse` | — | Users page |
| `CreateRoleRequest` / `RoleResponse` | — | Roles page |
| `DepartmentResponse` / `PositionResponse` | — | Departments page |
| `CreateLeaveRequestRequest` / `LeaveRequestResponse` | — | Leave Requests page |
| `ErrorResponse` / `ProblemDetails` | — | All pages (error handling) |

## OpenAPI as Source of Truth

The Swagger doc at `http://localhost:5192/swagger/v1/swagger.json` is the authoritative contract. All contract tests should derive from it:

```bash
# Download latest schema
curl http://localhost:5192/swagger/v1/swagger.json -o tests/schemas/openapi.json

# Validate against it
npx spectral lint tests/schemas/openapi.json
```

## Related Files

- `src/AIHelpdesk.Contracts/` — All DTOs (request/response)
- `tests/AIHelpdesk.Tests/Contracts/` — Existing DTO shape tests
- `tests/schemas/` — JSON schemas (to be created)
- `http://localhost:5192/swagger/v1/swagger.json` — OpenAPI spec
- `documentation/api-testing.md` — API endpoint testing
