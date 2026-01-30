using System.Security.Claims;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Web.Middleware;

/// <summary>
/// Middleware that updates session activity timestamp on authenticated requests.
/// Activity updates are throttled to once per minute per session.
/// </summary>
public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionActivityMiddleware> _logger;

    public const string SessionTokenClaimType = "SessionToken";

    public SessionActivityMiddleware(RequestDelegate next, ILogger<SessionActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserSessionService sessionService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionToken = context.User.FindFirstValue(SessionTokenClaimType);
            if (!string.IsNullOrEmpty(sessionToken))
            {
                try
                {
                    await sessionService.UpdateActivityAsync(sessionToken);
                }
                catch (Exception ex)
                {
                    // Don't fail the request if activity update fails
                    _logger.LogWarning(ex, "Failed to update session activity");
                }
            }
        }

        await _next(context);
    }
}

public static class SessionActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionActivity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionActivityMiddleware>();
    }
}
