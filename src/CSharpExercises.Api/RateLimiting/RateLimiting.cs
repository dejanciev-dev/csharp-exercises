using System.Collections.Concurrent;
using System.Globalization;
using CSharpExercises.Testing;
using Microsoft.Extensions.Options;

namespace CSharpExercises.Api.RateLimiting;

/// <summary>Bound from the "RateLimit" section of appsettings.json.</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int MaxRequests { get; set; } = 60;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Gives each client its own <see cref="SlidingWindowRateLimiter"/> from the testing exercises.
/// For learning: idle clients are never evicted, and behind a proxy every request would share the proxy's IP.
/// Production apps should use ASP.NET Core's built-in rate limiting (AddRateLimiter).
/// </summary>
public sealed class ClientRateLimiter(IOptions<RateLimitOptions> options, TimeProvider time)
{
    private readonly ConcurrentDictionary<string, SlidingWindowRateLimiter> _limiters = new();

    public bool TryAcquire(string clientId, out TimeSpan retryAfter)
    {
        var limiter = _limiters.GetOrAdd(
            clientId,
            _ => new SlidingWindowRateLimiter(options.Value.MaxRequests, options.Value.Window, time));

        // A single limiter isn't thread-safe, and one client can send requests in parallel.
        lock (limiter)
        {
            if (limiter.TryAcquire())
            {
                retryAfter = TimeSpan.Zero;
                return true;
            }

            retryAfter = limiter.RetryAfter;
            return false;
        }
    }
}

/// <summary>Answers 429 Too Many Requests, with a Retry-After header, when a client goes over the limit on /api.</summary>
public sealed class RateLimitMiddleware(RequestDelegate next, ClientRateLimiter limiter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (limiter.TryAcquire(client, out var retryAfter))
        {
            await next(context);
            return;
        }

        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        await Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too many requests",
                detail: $"Try again in {seconds} seconds.")
            .ExecuteAsync(context);
    }
}
