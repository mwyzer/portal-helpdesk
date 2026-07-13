# 🛡️ Security Testing

> **Status:** 📋 Planned — not yet implemented  
> **Standards:** OWASP Top 10, CIS Benchmarks

## Overview

Security testing identifies vulnerabilities before attackers do. For a helpdesk handling employee data, leave records, and internal documents, security is non-negotiable.

## OWASP Top 10 Coverage

| # | Vulnerability | Mitigation in AIHelpdesk | Test |
|---|--------------|--------------------------|------|
| 1 | Broken Access Control | JWT + role-based `[Authorize]` | Auth test suite |
| 2 | Cryptographic Failures | BCrypt passwords, HTTPS | Penetration test |
| 3 | Injection | EF Core parameterized queries, input validation (FluentValidation) | Fuzz test |
| 4 | Insecure Design | Clean Architecture, DTOs, rate limiting (planned) | Threat model |
| 5 | Security Misconfiguration | App secrets, CORS policy, HSTS | Config audit |
| 6 | Vulnerable Components | Dependabot / `dotnet list package --vulnerable` | CI scan |
| 7 | Auth Failures | Password complexity, lockout, MFA (planned) | Auth test suite |
| 8 | Software & Data Integrity | NuGet package signing, artifact verification | Supply chain |
| 9 | Logging & Monitoring Failures | Serilog structured logging | Log audit |
| 10 | SSRF | No external URL fetching (currently) | N/A |

## Automated Scans

### Dependency Vulnerabilities

```bash
# .NET — check NuGet packages
dotnet list package --vulnerable

# Frontend — npm audit
cd frontend && npm audit

# Docker images
docker scout cves aihelpdesk-api
```

### Secret Detection

```bash
# GitLeaks — find secrets in git history
gitleaks detect --source .

# TruffleHog — scan for high-entropy strings
trufflehog git file://. --only-verified
```

### SAST (Static Analysis)

```bash
# .NET Roslyn analyzers
dotnet build /p:EnforceCodeStyleInBuild=true

# SonarQube (planned)
dotnet sonarscanner begin /k:"aihelpdesk" /d:sonar.host.url="http://localhost:9000"
dotnet build
dotnet sonarscanner end
```

## Manual Penetration Test Checklist

### Authentication

- [ ] Brute force protection (account lockout after N attempts)
- [ ] Password policy enforced (length, complexity)
- [ ] Passwords not returned in API responses
- [ ] JWT secret is strong (≥256 bits)
- [ ] Token expiration is reasonable (≤15 min access, ≤7 days refresh)
- [ ] Refresh token rotation prevents replay attacks

### Authorization

- [ ] Every `[Authorize]` endpoint requires valid JWT
- [ ] Role checks are server-side (not just client-side UI hiding)
- [ ] User cannot access another user's data (IDOR)
- [ ] `PUT /api/users/{id}` — cannot modify other users
- [ ] `DELETE` operations require appropriate roles

### Input Validation

- [ ] SQL injection — `' OR '1'='1` in search fields
- [ ] XSS — `<script>alert(1)</script>` in text inputs
- [ ] Path traversal — `../../../etc/passwd` in file uploads (future)
- [ ] Mass assignment — extra JSON fields are ignored by DTO binding

### API Security

- [ ] CORS allows only `http://localhost:5173` (dev) / production domain
- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-Frame-Options: DENY`
- [ ] `Content-Security-Policy` header
- [ ] Rate limiting on `/api/auth/login`
- [ ] No stack traces in error responses (use `ProblemDetails`)
- [ ] HTTPS enforced (HSTS in production)

### Data Protection

- [ ] PII (NIK, email) masked in logs
- [ ] Passwords hashed (BCrypt via ASP.NET Core Identity)
- [ ] Refresh tokens stored as hash, not plaintext
- [ ] Database connection encrypted (TLS in production)
- [ ] Backups encrypted at rest

## Security Headers Test

```csharp
[Fact]
public async Task Api_ReturnsSecurityHeaders()
{
    var response = await Client.GetAsync("/api/health");
    response.Headers.Should().ContainKey("X-Content-Type-Options");
    response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
}
```

```bash
# Test with curl
curl -I http://localhost:5192/api/health
```

## JWT Security Tests

```csharp
[Fact]
public async Task Jwt_ContainsMinimalClaims()
{
    var token = await GetToken();
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    // Should NOT contain sensitive data
    jwt.Claims.Should().NotContain(c => c.Type == "password");
    jwt.Claims.Should().NotContain(c => c.Type == "nik");

    // Should contain role claims
    jwt.Claims.Should().Contain(c => c.Type == "role");
}
```

## CI/CD Integration

```yaml
# GitHub Actions (planned)
security:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: .NET dependency scan
      run: dotnet list package --vulnerable
    - name: npm audit
      working-directory: frontend
      run: npm audit --audit-level=high
    - name: Secret scan
      uses: gitleaks/gitleaks-action@v2
    - name: Docker scan
      run: docker scout cves aihelpdesk-api
```

## Related Files

- `documentation/authentication-and-authorization-testing.md` — Auth-specific testing
- `src/AIHelpdesk.Api/Program.cs` — Security middleware configuration
- `src/AIHelpdesk.Api/appsettings.json` — Security settings (JWT, CORS)
- `src/AIHelpdesk.Infrastructure/Services/JwtService.cs` — Token service
