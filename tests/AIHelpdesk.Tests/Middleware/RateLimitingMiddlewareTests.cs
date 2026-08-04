using AIHelpdesk.Api.Middleware;
using AIHelpdesk.Application.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AIHelpdesk.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private static RateLimitingMiddleware CreateMiddleware(
        RequestDelegate next, int generalMax = 100)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:GeneralMaxRequestsPerMinute"] = generalMax.ToString()
            })
            .Build();

        return new RateLimitingMiddleware(next, configuration);
    }

    private static IOptions<AIOptions> CreateAiOptions(int maxRequestsPerMinute = 30, bool enabled = true)
    {
        return Options.Create(new AIOptions
        {
            RateLimit = new RateLimitOptions { MaxRequestsPerMinute = maxRequestsPerMinute, Enabled = enabled }
        });
    }

    private static DefaultHttpContext CreateContext(string path, string? userId = null, string ip = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        if (userId != null)
        {
            var identity = new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)], "test");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowRequest_UnderGeneralLimit()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, generalMax: 5);
        var context = CreateContext("/api/tickets", userId: Guid.NewGuid().ToString());

        await middleware.InvokeAsync(context, CreateAiOptions());

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldBlock_WhenGeneralLimitExceeded()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ => { callCount++; return Task.CompletedTask; }, generalMax: 3);
        var userId = Guid.NewGuid().ToString();

        for (int i = 0; i < 3; i++)
        {
            await middleware.InvokeAsync(CreateContext("/api/tickets", userId), CreateAiOptions());
        }

        var blockedContext = CreateContext("/api/tickets", userId);
        blockedContext.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(blockedContext, CreateAiOptions());

        callCount.Should().Be(3);
        blockedContext.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task InvokeAsync_ShouldTrackAiAndGeneralEndpoints_Independently()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ => { callCount++; return Task.CompletedTask; }, generalMax: 1);
        var userId = Guid.NewGuid().ToString();

        // Exhaust the general limit (max 1) on a non-AI endpoint
        await middleware.InvokeAsync(CreateContext("/api/tickets", userId), CreateAiOptions(maxRequestsPerMinute: 30));
        var blockedGeneral = CreateContext("/api/tickets", userId);
        blockedGeneral.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(blockedGeneral, CreateAiOptions(maxRequestsPerMinute: 30));

        // AI endpoint should still be allowed — separate tracker/limit
        var aiContext = CreateContext("/api/ai/chat", userId);
        await middleware.InvokeAsync(aiContext, CreateAiOptions(maxRequestsPerMinute: 30));

        blockedGeneral.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        aiContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        callCount.Should().Be(2); // 1 general + 1 AI, the blocked general call never reached `next`
    }

    [Fact]
    public async Task InvokeAsync_ShouldBlock_WhenAiLimitExceeded()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ => { callCount++; return Task.CompletedTask; });
        var userId = Guid.NewGuid().ToString();
        var aiOptions = CreateAiOptions(maxRequestsPerMinute: 2);

        await middleware.InvokeAsync(CreateContext("/api/ai/chat", userId), aiOptions);
        await middleware.InvokeAsync(CreateContext("/api/ai/chat", userId), aiOptions);
        var blocked = CreateContext("/api/ai/chat", userId);
        blocked.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(blocked, aiOptions);

        callCount.Should().Be(2);
        blocked.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotRateLimit_WhenAiRateLimitingDisabled()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ => { callCount++; return Task.CompletedTask; });
        var userId = Guid.NewGuid().ToString();
        var aiOptions = CreateAiOptions(maxRequestsPerMinute: 1, enabled: false);

        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(CreateContext("/api/ai/chat", userId), aiOptions);
        }

        callCount.Should().Be(5);
    }

    [Fact]
    public async Task InvokeAsync_ShouldTrackAnonymousRequests_ByIpAddress()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ => { callCount++; return Task.CompletedTask; }, generalMax: 2);

        await middleware.InvokeAsync(CreateContext("/api/auth/login", userId: null, ip: "10.0.0.5"), CreateAiOptions());
        await middleware.InvokeAsync(CreateContext("/api/auth/login", userId: null, ip: "10.0.0.5"), CreateAiOptions());
        var blocked = CreateContext("/api/auth/login", userId: null, ip: "10.0.0.5");
        blocked.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(blocked, CreateAiOptions());

        // A different IP should have its own, unexhausted tracker
        var otherIp = CreateContext("/api/auth/login", userId: null, ip: "10.0.0.6");
        await middleware.InvokeAsync(otherIp, CreateAiOptions());

        blocked.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        otherIp.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        callCount.Should().Be(3);
    }
}
