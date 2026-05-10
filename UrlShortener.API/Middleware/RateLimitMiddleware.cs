using UrlShortener.Core.Interfaces;

namespace UrlShortener.API.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimiterService rateLimiter)
    {
        // Only rate limit the shorten endpoint
        if (context.Request.Path.StartsWithSegments("/api/shorten") &&
            context.Request.Method == "POST")
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var isAllowed = await rateLimiter.IsAllowedAsync(clientIp);

            if (!isAllowed)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"error\": \"Too many requests. Please try again after 1 minute.\"}");
                return;
            }
        }

        await _next(context);
    }
}
