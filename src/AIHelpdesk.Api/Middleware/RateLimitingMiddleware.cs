using System.Collections.Concurrent;
using AIHelpdesk.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AIHelpdesk.Api.Middleware;

/// <summary>
/// Two-tier rate limiting: a tighter per-user limit on AI endpoints (configured via
/// <see cref="AIOptions.RateLimit"/>), and a general per-client limit on everything else
/// (configured via "RateLimiting:GeneralMaxRequestsPerMinute", default 100/min).
/// Keyed by authenticated user ID when available, falling back to client IP for anonymous
/// requests (login, forgot-password, health check) so those can't be used to bypass limiting.
/// </summary>
public class RateLimitingMiddleware
{
    private const int DefaultGeneralMaxRequestsPerMinute = 100;

    private readonly RequestDelegate _next;
    private readonly int _generalMaxRequestsPerMinute;
    private static readonly ConcurrentDictionary<string, UserRateTracker> _aiTrackers = new();
    private static readonly ConcurrentDictionary<string, UserRateTracker> _generalTrackers = new();

    public RateLimitingMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _generalMaxRequestsPerMinute = configuration.GetValue<int?>("RateLimiting:GeneralMaxRequestsPerMinute")
            ?? DefaultGeneralMaxRequestsPerMinute;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<AIOptions> aiOptions)
    {
        var isAiEndpoint = context.Request.Path.StartsWithSegments("/api/ai", StringComparison.OrdinalIgnoreCase);
        var key = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        if (isAiEndpoint)
        {
            var aiRateLimit = aiOptions.Value.RateLimit;
            if (aiRateLimit.Enabled && IsRateLimited(_aiTrackers, key, aiRateLimit.MaxRequestsPerMinute))
            {
                await WriteTooManyRequestsAsync(context, aiRateLimit.MaxRequestsPerMinute);
                return;
            }
        }
        else
        {
            if (IsRateLimited(_generalTrackers, key, _generalMaxRequestsPerMinute))
            {
                await WriteTooManyRequestsAsync(context, _generalMaxRequestsPerMinute);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsRateLimited(ConcurrentDictionary<string, UserRateTracker> trackers, string key, int maxRequestsPerMinute)
    {
        var tracker = trackers.GetOrAdd(key, _ => new UserRateTracker());
        var now = DateTime.UtcNow;

        lock (tracker)
        {
            tracker.Timestamps.RemoveAll(t => t < now.AddMinutes(-1));

            if (tracker.Timestamps.Count >= maxRequestsPerMinute)
                return true;

            tracker.Timestamps.Add(now);
            return false;
        }
    }

    private static Task WriteTooManyRequestsAsync(HttpContext context, int maxRequestsPerMinute)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = "60";
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            $"{{\"error\":\"Rate limit exceeded. Max {maxRequestsPerMinute} requests per minute.\"}}");
    }

    private class UserRateTracker
    {
        public List<DateTime> Timestamps { get; } = new();
    }
}
