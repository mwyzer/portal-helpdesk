using System.Collections.Concurrent;
using AIHelpdesk.Application.Options;
using Microsoft.Extensions.Options;

namespace AIHelpdesk.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, UserRateTracker> _trackers = new();

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<AIOptions> options)
    {
        // Only rate-limit AI endpoints
        if (!context.Request.Path.StartsWithSegments("/api/ai", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var rateLimitOptions = options.Value.RateLimit;
        if (!rateLimitOptions.Enabled)
        {
            await _next(context);
            return;
        }

        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        var tracker = _trackers.GetOrAdd(userId, _ => new UserRateTracker());
        var now = DateTime.UtcNow;

        lock (tracker)
        {
            // Purge old entries
            tracker.Timestamps.RemoveAll(t => t < now.AddMinutes(-1));

            if (tracker.Timestamps.Count >= rateLimitOptions.MaxRequestsPerMinute)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = "60";
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync(
                    $"{{\"error\":\"Rate limit exceeded. Max {rateLimitOptions.MaxRequestsPerMinute} requests per minute.\"}}");
                return;
            }

            tracker.Timestamps.Add(now);
        }

        await _next(context);
    }

    private class UserRateTracker
    {
        public List<DateTime> Timestamps { get; } = new();
    }
}
