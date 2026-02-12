using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Infrastructure.Services;

namespace NetWorthTracker.Web.Middleware;

public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppMode _appMode;

    public SubscriptionMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appMode = appSettings.Value.AppMode;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
    {
        // Skip entirely in self-hosted mode
        if (_appMode != AppMode.Saas)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Bypass list: static files, auth, billing, webhooks, health
        if (ShouldBypass(path))
        {
            await _next(context);
            return;
        }

        // Only check authenticated users
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        // Fail-closed: if check fails, deny access
        bool hasActive;
        try
        {
            hasActive = await subscriptionService.HasActiveSubscriptionAsync(userId);
        }
        catch
        {
            hasActive = false;
        }

        if (hasActive)
        {
            await _next(context);
            return;
        }

        // Deny: API routes get 403 JSON, UI routes redirect to /Billing
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Active subscription required." }));
            return;
        }

        context.Response.Redirect("/Billing");
    }

    private static bool ShouldBypass(string path)
    {
        // Static files (heuristic: path contains a dot extension)
        if (path.Contains('.'))
            return true;

        // Auth routes (/Account/Login, /Account/Register, etc. but NOT /Accounts)
        if (path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Account", StringComparison.OrdinalIgnoreCase))
            return true;

        // Settings routes (profile, MFA, account deletion)
        if (path.StartsWith("/Settings", StringComparison.OrdinalIgnoreCase))
            return true;

        // Billing routes
        if (path.StartsWith("/Billing", StringComparison.OrdinalIgnoreCase))
            return true;

        // Webhook routes
        if (path.StartsWith("/api/webhooks", StringComparison.OrdinalIgnoreCase))
            return true;

        // Health check
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}

public static class SubscriptionMiddlewareExtensions
{
    public static IApplicationBuilder UseSubscriptionGating(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubscriptionMiddleware>();
    }
}
