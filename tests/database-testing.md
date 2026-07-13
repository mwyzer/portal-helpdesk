# 🗄️ Database Testing

> **Status:** 🟡 Partial — schema via EF Core migrations, unit tests use InMemory  
> **Database:** PostgreSQL 17 + pgvector extension

## Overview

Database testing validates the data layer — schema correctness, migration integrity, seed data, query performance, and EF Core mapping. Must use real PostgreSQL (not InMemory) for pgvector, JSONB, and PG-specific queries.

## Schema Testing

### EF Core Migrations

Every schema change goes through EF Core migrations. Test that migrations apply cleanly:

```bash
# Verify pending migrations
dotnet ef migrations list --project src/AIHelpdesk.Api

# Generate SQL script (review before applying)
dotnet ef migrations script --idempotent -o migrations/script.sql
```

```csharp
[Fact]
public async Task AllMigrations_ApplyWithoutError()
{
    await using var db = new ApplicationDbContext(_options);
    await db.Database.MigrateAsync();
    // No exception = success
}
```

### Seed Data Verification

```csharp
[Fact]
public async Task SeedData_HasRequiredRoles()
{
    // After migrations + seeding
    var roles = await _context.Roles.ToListAsync();
    roles.Should().Contain(r => r.Name == "Super Admin");
    roles.Should().Contain(r => r.Name == "Employee");
}
```

```csharp
[Fact]
public async Task SeedData_HasLeaveTypes()
{
    var types = await _context.LeaveTypes.ToListAsync();
    types.Should().HaveCount(8);
    types.Should().Contain(t => t.Name == "Annual Leave" && t.DefaultDays == 12);
    types.Should().Contain(t => t.Name == "Sick Leave" && t.DefaultDays == 14);
}
```

## Data Integrity Tests

### Foreign Key Constraints

```csharp
[Fact]
public async Task DeleteDepartment_WithPositions_ThrowsForeignKeyViolation()
{
    var dept = TestDataFactory.CreateDepartment();
    var pos = TestDataFactory.CreatePosition(dept.Id);
    _context.AddRange(dept, pos);
    await _context.SaveChangesAsync();

    _context.Departments.Remove(dept);
    var act = () => _context.SaveChangesAsync();
    await act.Should().ThrowAsync<DbUpdateException>();
}
```

### Unique Constraints

```csharp
[Fact]
public async Task CreateUser_DuplicateEmail_ThrowsUniqueConstraintViolation()
{
    var user1 = TestDataFactory.CreateUser("same@email.com");
    var user2 = TestDataFactory.CreateUser("same@email.com");
    await _context.Users.AddRangeAsync(user1, user2);

    var act = () => _context.SaveChangesAsync();
    await act.Should().ThrowAsync<DbUpdateException>();
}
```

### Soft Delete

```csharp
[Fact]
public async Task SoftDeletedUsers_AreExcludedFromQueries()
{
    var active = TestDataFactory.CreateUser(isActive: true);
    var deleted = TestDataFactory.CreateUser("deleted@test.com");
    deleted.IsDeleted = true;
    _context.Users.AddRange(active, deleted);
    await _context.SaveChangesAsync();

    var visible = await _context.Users
        .Where(u => !u.IsDeleted)
        .ToListAsync();
    visible.Should().HaveCount(1);
}
```

## pgvector Testing (AI/RAG)

```csharp
[Fact]
public async Task VectorSearch_ReturnsMostRelevantDocuments()
{
    // Seed embeddings
    var docs = new[]
    {
        new KnowledgeDocument { Content = "Annual leave is 12 days per year",
            Embedding = new Vector(GenerateEmbedding("vacation days policy")) },
        new KnowledgeDocument { Content = "Office hours are 9 AM to 6 PM",
            Embedding = new Vector(GenerateEmbedding("working hours schedule")) },
    };
    _context.KnowledgeDocuments.AddRange(docs);
    await _context.SaveChangesAsync();

    // Cosine similarity search
    var query = new Vector(GenerateEmbedding("How many vacation days?"));
    var results = await _context.KnowledgeDocuments
        .OrderBy(d => d.Embedding!.CosineDistance(query))
        .Take(3)
        .ToListAsync();

    results.First().Content.Should().Contain("Annual leave");
}
```

## Performance Testing

### Query Plan Analysis

```sql
-- Check query plans in PostgreSQL
EXPLAIN ANALYZE
SELECT * FROM "Users"
WHERE "IsDeleted" = false AND "Email" ILIKE '%search%'
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

### Index Verification

```sql
-- List indexes on key tables
SELECT tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename;
```

Expected indexes:
- `Users.Email` (unique)
- `Users.IsDeleted` (filter for active queries)
- `RefreshTokens.UserId` (foreign key lookup)
- `Employees.DepartmentId` (foreign key lookup)

## Test Database Setup

### Option A: Separate Test Database

```json
// appsettings.Test.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aihelpdesk_test;Username=helpdesk;Password=helpdesk123"
  }
}
```

```bash
# Create test database
createdb -h localhost -U helpdesk aihelpdesk_test
```

### Option B: Testcontainers (CI)

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("aihelpdesk_test")
        .WithUsername("helpdesk")
        .WithPassword("helpdesk123")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Apply migrations
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

## Related Files

- `src/AIHelpdesk.Infrastructure/Data/ApplicationDbContext.cs` — EF Core context
- `src/AIHelpdesk.Infrastructure/Data/Migrations/` — EF migrations
- `docker/postgres/Dockerfile` — PostgreSQL image with pgvector
- `documentation/unit-testing.md` — InMemory unit test patterns
