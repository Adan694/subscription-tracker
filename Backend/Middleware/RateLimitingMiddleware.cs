using System.Collections.Concurrent;

namespace Backend.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestCount> _clientRequests = new();
    private readonly int _maxRequests = 100; // Max requests per window
    private readonly int _windowMinutes = 1; // Time window in minutes

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{context.Request.Path}";

        if (IsRateLimited(key))
        {
            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." });
            return;
        }

        await _next(context);
    }

    private bool IsRateLimited(string key)
    {
        var now = DateTime.UtcNow;
        var requestCount = _clientRequests.GetOrAdd(key, new ClientRequestCount());

        lock (requestCount)
        {
            if (requestCount.WindowStart.AddMinutes(_windowMinutes) < now)
            {
                requestCount.WindowStart = now;
                requestCount.Count = 1;
                return false;
            }

            if (requestCount.Count >= _maxRequests)
            {
                return true;
            }

            requestCount.Count++;
            return false;
        }
    }

    private class ClientRequestCount
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int Count { get; set; } = 1;
    }
}