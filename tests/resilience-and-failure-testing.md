# 🛟 Resilience & Failure Testing

> **Status:** 📋 Planned — not yet implemented  
> **Goal:** Verify the system degrades gracefully, not catastrophically

## Overview

Resilience testing simulates failures — network outages, database crashes, timeouts, overloads — to verify the application handles them gracefully. A helpdesk that crashes when PostgreSQL restarts is a helpdesk that causes frustration.

## Resilience Principles

1. **Fail fast, recover faster** — don't hang, don't cascade
2. **Graceful degradation** — show partial data rather than crash
3. **User-friendly errors** — never expose stack traces or connection strings
4. **No data corruption** — partial failures leave DB consistent
5. **Observability** — every failure is logged with context

## Test Scenarios

### 1. Database Unavailable

```csharp
[Fact]
public async Task GetUsers_Returns503_WhenDatabaseIsDown()
{
    // Simulate: PostgreSQL container stopped
    await _postgresContainer.StopAsync();

    var response = await Client.GetAsync("/api/users");

    response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem.Title.Should().Contain("Service Unavailable");
    problem.Detail.Should().NotContain("connection string");   // no leaks!
    problem.Detail.Should().NotContain("password");            // no leaks!
}

[Fact]
public async Task GetUsers_Recovers_WhenDatabaseComesBack()
{
    await _postgresContainer.StopAsync();
    var downResponse = await Client.GetAsync("/api/users");
    downResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

    await _postgresContainer.StartAsync();
    await Task.Delay(2000); // wait for reconnect

    var upResponse = await Client.GetAsync("/api/users");
    upResponse.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### 2. Slow Responses / Timeouts

```csharp
[Fact]
public async Task SlowEndpoint_ReturnsTimeout_NotHangForever()
{
    // Use Toxiproxy or similar to introduce 30s delay on DB
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    var act = () => Client.GetAsync("/api/users", timeout.Token);
    await act.Should().ThrowAsync<TaskCanceledException>();
}

[Fact]
public async Task Frontend_ShowsErrorMessage_OnApiTimeout()
{
    // Mock API to return 504
    await page.Route('**/api/users', route => route.abort('timedout'));

    await page.goto('/users');
    await expect(page.locator('.error-message')).toBeVisible();
    await expect(page.locator('.error-message')).toContainText('temporarily unavailable');
}
```

### 3. High Load / Rate Limiting

```csharp
[Fact]
public async Task RateLimiting_Returns429_UnderHighLoad()
{
    var tasks = Enumerable.Range(0, 100).Select(_ =>
        Client.GetAsync("/api/auth/login"));

    var responses = await Task.WhenAll(tasks);

    var tooManyRequests = responses.Count(r =>
        r.StatusCode == HttpStatusCode.TooManyRequests);
    tooManyRequests.Should().BeGreaterThan(0);

    // Should include Retry-After header
    var first429 = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    first429.Headers.Should().ContainKey("Retry-After");
}
```

### 4. Invalid Input / Boundary Testing

```csharp
[Theory]
[InlineData("")]
[InlineData("a")]
[InlineData("a".PadRight(257, 'a'))]  // max length + 1
[InlineData(null)]
[InlineData("<script>alert('xss')</script>")]
public async Task CreateUser_RejectsInvalidFullName(string invalidName)
{
    var response = await Client.PostAsJsonAsync("/api/users", new
    {
        Email = "test@test.com",
        Password = "Valid@123",
        FullName = invalidName,
    });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem.Errors.Should().ContainKey("FullName");
}
```

### 5. Concurrent Conflicting Operations

```csharp
[Fact]
public async Task ConcurrentUpdates_MaintainDataConsistency()
{
    // Seed a user
    var user = await CreateUser("user@test.com");

    // Two concurrent updates
    var update1Task = Client.PutAsJsonAsync($"/api/users/{user.Id}", new
    {
        fullName = "Updated Name 1"
    });
    var update2Task = Client.PutAsJsonAsync($"/api/users/{user.Id}", new
    {
        fullName = "Updated Name 2"
    });

    var results = await Task.WhenAll(update1Task, update2Task);

    // One should succeed, one should get a concurrency error (409 or success)
    results.Should().Contain(r =>
        r.StatusCode == HttpStatusCode.OK ||
        r.StatusCode == HttpStatusCode.Conflict);

    // Verify final state is consistent (not corrupted)
    var final = await Client.GetFromJsonAsync<UserResponse>($"/api/users/{user.Id}");
    final.FullName.Should().NotBeNullOrEmpty();
}
```

### 6. Memory / Resource Leaks

```csharp
[Fact]
public async Task RepeatedRequests_DoNotLeakConnections()
{
    var initialMemory = GC.GetTotalMemory(true);

    for (int i = 0; i < 1000; i++)
    {
        using var response = await Client.GetAsync("/api/users?page=1&pageSize=1");
        response.EnsureSuccessStatusCode();
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    var finalMemory = GC.GetTotalMemory(true);

    var growth = finalMemory - initialMemory;
    growth.Should().BeLessThan(10 * 1024 * 1024); // < 10 MB growth after 1000 requests
}
```

## Resilience Patterns to Implement

| Pattern | Current | Target |
|---------|---------|--------|
| **Retry with backoff** | — | Polly: 3 retries, exponential backoff |
| **Circuit breaker** | — | 5 failures in 30s → open circuit for 60s |
| **Timeout** | — | EF Core command timeout: 30s |
| **Bulkhead** | — | Separate thread pools for critical vs. non-critical |
| **Fallback** | — | Cache stale data if DB is down |
| **Health checks** | — | `/api/health` with DB + AI service checks |

### Polly Implementation (Planned)

```csharp
// Program.cs
builder.Services.AddHttpClient("AIService")
    .AddTransientHttpErrorPolicy(p => p
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddCircuitBreakerPolicy(5, TimeSpan.FromSeconds(30));
```

## Chaos Engineering (Future)

Use controlled chaos experiments:

```bash
# Kill PostgreSQL mid-request
docker compose kill postgres
# → Verify API returns 503, not crash
# → Verify API recovers when DB restarts

# Pause container (simulate GC pause / network stall)
docker compose pause api
sleep 10
docker compose unpause api
# → Verify requests time out gracefully
# → Verify no data corruption

# Network latency
docker compose exec api tc qdisc add dev eth0 root netem delay 5000ms
# → Verify frontend shows loading state
# → Verify no duplicate submissions
```

## Frontend Resilience

```typescript
// Error boundary — catch React rendering errors
class ErrorBoundary extends React.Component {
  render() {
    if (this.state.hasError) {
      return <ErrorFallback message="Something went wrong" />;
    }
    return this.props.children;
  }
}

// Retry stale queries
const { data } = useQuery({
  queryKey: ['users'],
  queryFn: fetchUsers,
  retry: 3,
  retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 30000),
  staleTime: 5 * 60 * 1000, // use cache if fetch fails
});

// Offline detection
window.addEventListener('offline', () => {
  toast.error('You are offline. Some features may be unavailable.');
});
```

## Failure Mode Catalog

| Failure | Symptom | Expected Behavior |
|---------|---------|-------------------|
| PostgreSQL down | All API calls fail | 503 + "Try again later" toast |
| AI service down | Chat returns error | "AI assistant is unavailable" |
| Disk full | Writes fail | 507 Insufficient Storage |
| Memory exhaustion | Slow responses | Graceful degradation, not crash |
| Network partition | Partial failures | Read-only mode, cached data |
| High CPU | Slow responses | Rate limiting kicks in |
| JWT signing key rotated | All tokens invalid | Force re-login, clear tokens |

## Related Files

- `src/AIHelpdesk.Api/Program.cs` — Middleware, services registration
- `src/AIHelpdesk.Api/Middleware/` — Error handling middleware
- `docker-compose.yml` — Container healthchecks, restart policies
- `documentation/performance-testing.md` — Load and stress testing
