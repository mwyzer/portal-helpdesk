# 🔐 Authentication & Authorization Testing

> **Status:** 🟡 Partial — manual testing via Swagger + Playwright login  
> **Auth:** JWT (access + refresh tokens), ASP.NET Core Identity, Role-based access

## Overview

Authentication and authorization are the foundation of a secure multi-tenant helpdesk. Every endpoint must be tested for proper auth gating — unauthenticated requests → 401, unauthorized roles → 403.

## Auth Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  Login Page │────▶│ POST /login  │────▶│  Database   │
│  (React)    │     │  (API)       │     │  (Identity) │
└─────────────┘     └──────┬───────┘     └─────────────┘
                           │
                    ┌──────▼───────┐
                    │ JWT Token    │
                    │ + Refresh    │
                    └──────┬───────┘
                           │
              ┌────────────▼────────────┐
              │ Authorization Middleware │
              │ [Authorize(Roles="...")] │
              └─────────────────────────┘
```

## Authentication Tests

### Login — Success Flow

```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsTokens()
{
    var response = await Client.PostAsJsonAsync("/api/auth/login", new
    {
        email = "admin@aihelpdesk.com",
        password = "Admin@123"
    });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
    body.AccessToken.Should().NotBeNullOrEmpty();
    body.RefreshToken.Should().NotBeNullOrEmpty();
    body.UserInfo.FullName.Should().NotBeNullOrEmpty();
}
```

### Login — Failure Scenarios

| Scenario | Expected |
|----------|----------|
| Wrong password | `401 Unauthorized` |
| Wrong email | `401 Unauthorized` |
| Empty body | `400 Bad Request` |
| Deactivated user | `401 Unauthorized` with message "Account deactivated" |
| Missing email field | `400 Bad Request` with validation error |

```csharp
[Theory]
[InlineData("wrong@email.com", "Admin@123")]
[InlineData("admin@aihelpdesk.com", "wrongpassword")]
[InlineData("admin@aihelpdesk.com", "")]
public async Task Login_WithInvalidCredentials_Returns401(string email, string password)
{
    var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### Refresh Token Flow

```csharp
[Fact]
public async Task RefreshToken_RotatesToken_OnEachUse()
{
    // Login to get tokens
    var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", Credentials);
    var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

    // Refresh
    var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh-token", new
    {
        accessToken = loginBody.AccessToken,
        refreshToken = loginBody.RefreshToken,
    });

    refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();

    // Tokens must differ (rotation)
    refreshBody.AccessToken.Should().NotBe(loginBody.AccessToken);
    refreshBody.RefreshToken.Should().NotBe(loginBody.RefreshToken);
}
```

### Password Reset Flow

```csharp
[Fact]
public async Task ForgotPassword_SendsResetEmail_ForValidEmail()
{
    var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new
    {
        email = "admin@aihelpdesk.com"
    });
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task ResetPassword_ResetsPassword_WithValidToken()
{
    // Get reset token (from email / test hook)
    var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
    {
        email = "admin@aihelpdesk.com",
        token = "valid-reset-token",
        newPassword = "NewPass@123",
    });
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify new password works
    var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
    {
        email = "admin@aihelpdesk.com",
        password = "NewPass@123",
    });
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Authorization Tests

### Role-Based Access Matrix

| Endpoint | Anonymous | Employee | Manager | HRD | Secretary | Super Admin |
|----------|-----------|----------|---------|-----|-----------|-------------|
| `GET /api/users` | 401 | 403 | 403 | 403 | 403 | 200 |
| `POST /api/users` | 401 | 403 | 403 | 403 | 403 | 201 |
| `GET /api/departments` | 401 | 200 | 200 | 200 | 200 | 200 |
| `GET /api/leave-types` | 401 | 200 | 200 | 200 | 200 | 200 |
| `POST /api/leave-requests` | 401 | 201 | 201 | 201 | 201 | 201* |
| `POST /api/leave-approvals/{id}/approve` | 401 | 403 | 200 | 200 | 403 | 200 |

\* Admin may need an Employee record first.

```csharp
[Theory]
[InlineData("Employee", "/api/users", HttpStatusCode.Forbidden)]
[InlineData("Manager", "/api/users", HttpStatusCode.Forbidden)]
[InlineData("SuperAdmin", "/api/users", HttpStatusCode.OK)]
public async Task Endpoint_EnforcesRoleAuthorization(string role, string endpoint, HttpStatusCode expected)
{
    var token = await GetTokenForRole(role);
    Client.DefaultRequestHeaders.Authorization = new("Bearer", token);

    var response = await Client.GetAsync(endpoint);
    response.StatusCode.Should().Be(expected);
}
```

### Expired Token → 401

```csharp
[Fact]
public async Task ExpiredToken_Returns401()
{
    Client.DefaultRequestHeaders.Authorization = new("Bearer", CreateExpiredToken());
    var response = await Client.GetAsync("/api/users");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### Tampered Token → 401

```csharp
[Fact]
public async Task TamperedToken_Returns401()
{
    Client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.rTCH8cLoGxAm_xw68z-zBzXGZeQJF8N-EWtJQ1mbzWk");
    var response = await Client.GetAsync("/api/users");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## Logout Behavior

```csharp
[Fact]
public async Task Logout_RevokesRefreshToken()
{
    var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", Credentials);
    var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

    // Logout
    var logoutResponse = await Client.PostAsJsonAsync("/api/auth/logout", new
    {
        refreshToken = loginBody.RefreshToken,
    });
    logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Refresh should fail
    var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh-token", new
    {
        accessToken = loginBody.AccessToken,
        refreshToken = loginBody.RefreshToken,
    });
    refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## Test User Credentials

| Role | Email | Password | Active |
|------|-------|----------|--------|
| Super Admin | `admin@aihelpdesk.com` | `Admin@123` | ✅ |
| HRD | (add via seed) | — | — |
| Secretary | (add via seed) | — | — |
| Manager | (add via seed) | — | — |
| Employee | (add via seed) | — | — |

## Related Files

- `src/AIHelpdesk.Api/Controllers/AuthController.cs` — Auth endpoints
- `src/AIHelpdesk.Infrastructure/Services/JwtService.cs` — Token generation
- `src/AIHelpdesk.Api/Middleware/` — Auth middleware
- `documentation/api-testing.md` — General API testing
