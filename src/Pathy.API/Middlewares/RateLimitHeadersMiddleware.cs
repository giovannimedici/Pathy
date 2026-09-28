using System.Collections.Concurrent;
using System.Security.Claims;

namespace Pathy.API.Middlewares;

/// <summary>
/// Middleware that tracks rate limit usage and adds informative headers to responses.
/// </summary>
/// <remarks>
/// Headers added:
/// - X-RateLimit-Limit: Maximum number of requests allowed in the window
/// - X-RateLimit-Remaining: Number of requests remaining in the current window
/// - X-RateLimit-Reset: Unix timestamp when the rate limit window resets
/// </remarks>
public class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // In-memory tracking of request counts per partition key
    // In production, consider using Redis or another distributed cache
    private static readonly ConcurrentDictionary<string, RateLimitTracker> _trackers = new();

    // Rate limit configurations
    private const int AnonymousLimit = 10;
    private const int AuthenticatedLimit = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var partitionKey = GetPartitionKey(context, isAuthenticated);
        var limit = isAuthenticated ? AuthenticatedLimit : AnonymousLimit;

        var tracker = _trackers.GetOrAdd(partitionKey, _ => new RateLimitTracker());
        var (remaining, resetTime) = tracker.GetStatus(limit, Window);

        // Track this request
        tracker.IncrementRequest(Window);

        // Add headers to response
        context.Response.OnStarting(() =>
        {
            // Don't add headers if response is already 429 (handled by rate limiter)
            if (context.Response.StatusCode != StatusCodes.Status429TooManyRequests)
            {
                context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, remaining - 1).ToString();
                context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToUnixTimeSeconds().ToString();
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string GetPartitionKey(HttpContext context, bool isAuthenticated)
    {
        if (isAuthenticated)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue("user_id");

            if (!string.IsNullOrEmpty(userId))
            {
                return $"authenticated_{userId}";
            }
        }

        // Fallback to IP address for anonymous users
        var clientIp = GetClientIpAddress(context);
        return $"anonymous_{clientIp}";
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Tracks rate limit usage for a partition key.
/// Thread-safe implementation using locks.
/// </summary>
internal class RateLimitTracker
{
    private readonly object _lock = new();
    private int _requestCount;
    private DateTimeOffset _windowStart;

    public RateLimitTracker()
    {
        _windowStart = GetCurrentWindowStart(TimeSpan.FromMinutes(1));
        _requestCount = 0;
    }

    public (int Remaining, DateTimeOffset ResetTime) GetStatus(int limit, TimeSpan window)
    {
        lock (_lock)
        {
            var currentWindowStart = GetCurrentWindowStart(window);

            // Reset counter if we're in a new window
            if (currentWindowStart > _windowStart)
            {
                _windowStart = currentWindowStart;
                _requestCount = 0;
            }

            var remaining = limit - _requestCount;
            var resetTime = _windowStart.Add(window);

            return (Math.Max(0, remaining), resetTime);
        }
    }

    public void IncrementRequest(TimeSpan window)
    {
        lock (_lock)
        {
            var currentWindowStart = GetCurrentWindowStart(window);

            // Reset counter if we're in a new window
            if (currentWindowStart > _windowStart)
            {
                _windowStart = currentWindowStart;
                _requestCount = 0;
            }

            _requestCount++;
        }
    }

    private static DateTimeOffset GetCurrentWindowStart(TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var ticks = now.Ticks - (now.Ticks % window.Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}

/// <summary>
/// Extension methods for adding rate limit headers middleware.
/// </summary>
public static class RateLimitHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitHeadersMiddleware>();
    }
}
