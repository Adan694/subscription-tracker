using System.Text;

namespace Backend.Middleware;

public class SimpleAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SimpleAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        // Public endpoints - no auth required
        if (path == "/" ||
            path == "/health" ||
            path.StartsWith("/swagger") ||
            path == "/api/Auth/register" ||
            path == "/api/Auth/login")
        {
            await _next(context);
            return;
        }

        // Check for token in Authorization header
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid token" });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            // Decode Base64 token to get userId
            var decodedBytes = Convert.FromBase64String(token);
            var userIdString = Encoding.UTF8.GetString(decodedBytes);

            if (Guid.TryParse(userIdString, out var userId))
            {
                // Store userId in context for controllers to use
                context.Items["UserId"] = userId;
                await _next(context);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token decode error: {ex.Message}");
        }

        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
    }
}