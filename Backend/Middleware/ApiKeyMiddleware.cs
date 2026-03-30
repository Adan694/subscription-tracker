namespace Backend.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key validation for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is missing" });
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        if (!string.Equals(extractedApiKey, apiKey))
        {
            _logger.LogWarning("Invalid API Key attempt from {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        await _next(context);
    }

    private bool IsPublicEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/register") ||
               path.StartsWithSegments("/api/login") ||
               path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/");
    }
}