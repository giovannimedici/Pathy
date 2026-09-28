using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Pathy.API.Configurations;

/// <summary>
/// Rate limiting configuration using ASP.NET Core native rate limiter.
/// </summary>
public static class RateLimitingConfiguration
{
    /// <summary>
    /// Policy name for anonymous (unauthenticated) requests.
    /// More restrictive: 10 requests per minute.
    /// </summary>
    public const string AnonymousPolicy = "anonymous";

    /// <summary>
    /// Policy name for authenticated requests.
    /// More permissive: 100 requests per minute.
    /// </summary>
    public const string AuthenticatedPolicy = "authenticated";

    /// <summary>
    /// Adds rate limiting services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment? environment = null)
    {
        // Disable rate limiting in testing environment
        var isTesting = environment?.EnvironmentName == "Testing" ||
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing";

        // Allow configuration override for testing environments
        var anonymousLimit = isTesting ? 10000 : configuration.GetValue("RateLimiting:AnonymousPermitLimit", 10);
        var authenticatedLimit = isTesting ? 10000 : configuration.GetValue("RateLimiting:AuthenticatedPermitLimit", 100);

        services.AddRateLimiter(options =>
        {
            // Configure rejection response as Problem Details (429 Too Many Requests)
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var retryAfter = GetRetryAfterSeconds(context.Lease);

                if (retryAfter.HasValue)
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.Value.ToString();
                }

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                    Instance = context.HttpContext.Request.Path
                };

                if (retryAfter.HasValue)
                {
                    problemDetails.Extensions["retryAfterSeconds"] = retryAfter.Value;
                }

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };

            // Anonymous policy: configurable requests per minute per IP (default: 10)
            options.AddPolicy(AnonymousPolicy, httpContext =>
            {
                var clientIp = GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"anonymous_{clientIp}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = anonymousLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queuing - reject immediately
                    });
            });

            // Authenticated policy: configurable requests per minute per User ID (default: 100)
            options.AddPolicy(AuthenticatedPolicy, httpContext =>
            {
                var userId = GetUserId(httpContext);

                // Fallback to anonymous policy if user is not authenticated
                if (string.IsNullOrEmpty(userId))
                {
                    var clientIp = GetClientIpAddress(httpContext);
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"anonymous_{clientIp}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = anonymousLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"authenticated_{userId}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authenticatedLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiting middleware to the application pipeline.
    /// </summary>
    public static WebApplication UseRateLimitingMiddleware(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }

    /// <summary>
    /// Gets the client IP address from the HTTP context.
    /// Considers X-Forwarded-For header for proxy scenarios.
    /// </summary>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Check X-Forwarded-For header first (for reverse proxy scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Fallback to remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Gets the user ID from the authenticated user's claims.
    /// </summary>
    private static string? GetUserId(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Try common claim types for user ID
        return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue("user_id");
    }

    /// <summary>
    /// Gets the retry-after value in seconds from the rate limit lease metadata.
    /// </summary>
    private static int? GetRetryAfterSeconds(RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return (int)retryAfter.TotalSeconds;
        }

        // Default to 60 seconds (window size) if metadata is not available
        return 60;
    }
}
