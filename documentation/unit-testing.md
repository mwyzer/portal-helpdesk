# 🧪 Unit Testing (Backend)

> **Framework:** xUnit 2.9.2 + FluentAssertions 8.10.0 + Moq 4.20.72 + Bogus 35.6.5  
> **Test Project:** `tests/AIHelpdesk.Tests/`  
> **Target:** .NET 9.0, Clean Architecture (Domain → Contracts → Services)

---

## Overview

Unit tests validate business logic in isolation using in-memory EF Core databases (no real PostgreSQL needed). The test project follows the same Clean Architecture layering as the main codebase:

```
tests/AIHelpdesk.Tests/
├── Contracts/            # DTO / record type tests
│   └── AuthContractsTests.cs
├── Domain/               # Entity / value object tests
│   ├── DepartmentTests.cs
│   └── RefreshTokenTests.cs
├── Services/             # Application / Infrastructure service tests
│   ├── UserServiceTests.cs
│   ├── RoleServiceTests.cs
│   └── DepartmentServiceTests.cs
├── TestDataFactory.cs    # Shared test data builder (Bogus)
└── UnitTest1.cs          # Placeholder (can be deleted)
```

## Prerequisites

```bash
# .NET SDK 9.0
dotnet --version   # should be 9.x
```

No external services (PostgreSQL, Docker, API) needed — tests use `Microsoft.EntityFrameworkCore.InMemory`.

## Tech Stack

| Library | Version | Purpose |
|---------|---------|---------|
| **xUnit** | 2.9.2 | Test framework — `[Fact]`, `[Theory]`, assertions |
| **FluentAssertions** | 8.10.0 | Readable `.Should().Be(...)` assertion syntax |
| **Moq** | 4.20.72 | Mock dependencies (interfaces, services) |
| **Bogus** | 35.6.5 | Fake data generation in `TestDataFactory` |
| **EF Core InMemory** | 9.0.x | In-memory database for service tests |
| **Coverlet** | 10.0.1 | Code coverage collection |
| **Microsoft.AspNetCore.Mvc.Testing** | 9.0.x | Integration test host (available for future use) |

## Running Tests

```bash
# From repo root
dotnet test

# Or from the test project directory
cd tests/AIHelpdesk.Tests
dotnet test

# With verbosity
dotnet test -v n

# Run a specific test
dotnet test --filter "FullyQualifiedName~UserServiceTests"

# Run a specific test class
dotnet test --filter "FullyQualifiedName=AIHelpdesk.Tests.Services.UserServiceTests"

# With code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Test Patterns

### 1. Domain Entity Tests

Test entity creation, property defaults, and computed properties. Pure unit tests — no DI, no database.

```csharp
// From: Domain/DepartmentTests.cs

[Fact]
public void CreateDepartment_ShouldSetProperties()
{
    var dept = TestDataFactory.CreateDepartment("Human Resources", "HR");

    dept.Name.Should().Be("Human Resources");
    dept.Code.Should().Be("HR");
    dept.IsActive.Should().BeTrue();
    dept.IsDeleted.Should().BeFalse();
    dept.Id.Should().NotBeEmpty();
}
```

### 2. Domain Logic Tests

Test business rules and computed properties on entities.

```csharp
// From: Domain/RefreshTokenTests.cs

[Fact]
public void IsExpired_ShouldReturnTrue_WhenTokenIsExpired()
{
    var token = new RefreshToken
    {
        UserId = Guid.NewGuid(),
        Token = "test-token",
        ExpiresAt = DateTime.UtcNow.AddDays(-1),
        IsRevoked = false,
    };

    token.IsExpired.Should().BeTrue();
    token.IsActive.Should().BeFalse();
}
```

### 3. Contract / DTO Tests

Verify request/response records deserialize correctly. Simple data-in, data-out.

```csharp
// From: Contracts/AuthContractsTests.cs

[Fact]
public void LoginRequest_ShouldBeRecordType()
{
    var request = new LoginRequest("test@example.com", "Password123!");

    request.Email.Should().Be("test@example.com");
    request.Password.Should().Be("Password123!");
}
```

### 4. Service Tests with In-Memory DB

The most common pattern. Each test gets a **fresh, isolated** in-memory database via `$"TestDb_{Guid.NewGuid()}"`, so tests never share state.

```csharp
// Pattern from: Services/DepartmentServiceTests.cs

private async Task<(DepartmentService, ApplicationDbContext)> CreateServiceAsync()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")   // unique per test
        .Options;

    var context = new ApplicationDbContext(options);
    var service = new DepartmentService(context);
    return (service, context);
}

[Fact]
public async Task CreateDepartmentAsync_ShouldCreateDepartment()
{
    var (service, _) = await CreateServiceAsync();

    var result = await service.CreateDepartmentAsync(new("Finance", "FIN"));

    result.Name.Should().Be("Finance");
    result.Code.Should().Be("FIN");
    result.IsActive.Should().BeTrue();
}
```

For Identity-dependent services, `UserManager<TUser>` and `RoleManager<TRole>` are constructed manually with in-memory stores:

```csharp
// Pattern from: Services/UserServiceTests.cs

private async Task<(UserService, ApplicationDbContext)> CreateServiceAsync()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
        .Options;

    var context = new ApplicationDbContext(options);
    var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(context);
    var userManager = new UserManager<ApplicationUser>(
        store, null!, new PasswordHasher<ApplicationUser>(),
        Array.Empty<IUserValidator<ApplicationUser>>(),
        Array.Empty<IPasswordValidator<ApplicationUser>>(),
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(), null!, null!);

    var service = new UserService(userManager, context);
    return (service, context);
}
```

### 5. TestDataFactory

Shared factory methods provide pre-configured entities with sensible defaults. All `Guid.NewGuid()` for unique IDs.

| Method | Returns | Defaults |
|--------|---------|----------|
| `CreateUser(email, fullName, nik, isActive)` | `ApplicationUser` | `test@example.com`, active, not deleted |
| `CreateRole(name, description)` | `ApplicationRole` | "Test Role", active |
| `CreateDepartment(name, code)` | `Department` | "IT" / "IT", active |
| `CreatePosition(deptId, name)` | `Position` | "Developer", active |
| `CreatePermission(name, group)` | `Permission` | "users.read" / "Users" |
| `CreateRefreshToken(userId, isRevoked)` | `RefreshToken` | 7-day expiry, not revoked |

## Current Test Coverage

### Services (3 files)

| File | Tests | Focus |
|------|-------|-------|
| `UserServiceTests.cs` | 5 | List, create, get, activate/deactivate, pagination |
| `RoleServiceTests.cs` | 4 | Create, list, delete, permissions |
| `DepartmentServiceTests.cs` | 6 | CRUD, positions, department filtering |

### Domain (2 files)

| File | Tests | Focus |
|------|-------|-------|
| `DepartmentTests.cs` | 2 | Entity creation, position relationship |
| `RefreshTokenTests.cs` | 3 | IsActive, IsRevoked, IsExpired |

### Contracts (1 file)

| File | Tests | Focus |
|------|-------|-------|
| `AuthContractsTests.cs` | 3 | LoginRequest, UserInfo, AuthResponse |

**Total: 23 tests across 6 files**

## Writing New Tests

### Adding a Service Test

1. Create `tests/AIHelpdesk.Tests/Services/{ServiceName}Tests.cs`
2. Follow the existing pattern with `CreateServiceAsync()` factory method
3. Use `TestDataFactory` for seed entities where possible
4. Each test gets its own database (`$"TestDb_{Guid.NewGuid()}"`)

### Adding a Domain Test

1. Create `tests/AIHelpdesk.Tests/Domain/{EntityName}Tests.cs`
2. Test creation, property assignment, computed properties, edge cases
3. Use `FluentAssertions` for readable assertions
4. No database — these are pure unit tests

### Adding a Contract Test

1. Create `tests/AIHelpdesk.Tests/Contracts/{Feature}ContractsTests.cs`
2. Test that records/DTOs are creatable and all fields are set
3. Useful as a smoke test for serialization

### Naming Convention

```
MethodName_ShouldExpectedBehavior_WhenCondition
```

Examples:
- `GetUsersAsync_ShouldReturnEmptyList_WhenNoUsersExist`
- `CreateDepartment_ShouldSetProperties`
- `IsExpired_ShouldReturnTrue_WhenTokenIsExpired`

## Best Practices

| Practice | Description |
|----------|-------------|
| **AAA Pattern** | Arrange → Act → Assert — clearly separate setup, execution, and verification |
| **Isolated Databases** | `Guid.NewGuid()` database name per test — no shared state |
| **FluentAssertions** | Use `.Should().Be(...)`, `.Should().ThrowAsync<T>()`, `.Should().BeEmpty()` |
| **Async all the way** | All service tests are `async Task` — real async/await |
| **Factory helpers** | `CreateServiceAsync()` creates fresh service + context per test |
| **Factory data** | `TestDataFactory` builds entities with sensible defaults |
| **One assert group** | Each test verifies one logical behavior (can have multiple `.Should()` assertions) |

## Troubleshooting

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| `dotnet test` not found | .NET SDK not installed | Install .NET 9 SDK |
| Tests won't build | Missing project reference | Check `.csproj` includes all 5 layers |
| Test hangs indefinitely | Async deadlock | Ensure all tests are `async Task`, not `async void` |
| Duplicate key exception | Two tests sharing DB name | Use `Guid.NewGuid()` in database name |
| `KeyNotFoundException` | Entity not seeded before test | Seed required entities in Arrange phase |
| `NullReferenceException` | Identity managers not set up | Follow `UserServiceTests` pattern for `UserManager`/`RoleManager` |

## Related Files

| File | Purpose |
|------|---------|
| `tests/AIHelpdesk.Tests/AIHelpdesk.Tests.csproj` | Test project references & NuGet packages |
| `tests/AIHelpdesk.Tests/TestDataFactory.cs` | Shared test entity builders |
| `tests/AIHelpdesk.Tests/Services/` | Service-layer tests |
| `tests/AIHelpdesk.Tests/Domain/` | Domain entity tests |
| `tests/AIHelpdesk.Tests/Contracts/` | DTO/contract tests |
| `documentation/e2e-testing.md` | E2E testing guide (Playwright) |
