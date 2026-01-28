using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Web.Middleware;

public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionMiddleware> _logger;

    // Paths that don't require subscription check
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/Home",
        "/Home/Index",
        "/Home/Privacy",
        "/Home/Error",
        "/Account/Login",
        "/Account/Register",
        "/Account/Logout",
        "/Account/ForgotPassword",
        "/Account/ForgotPasswordConfirmation",
        "/Account/ResetPassword",
        "/Account/ResetPasswordConfirmation",
        "/Account/ConfirmEmail",
        "/Account/RegisterConfirmation",
        "/Account/AccessDenied",
        "/Account/LoginWith2fa",
        "/Account/LoginWithRecoveryCode",
        "/Subscription",
        "/Subscription/Index",
        "/Subscription/Subscribe",
        "/Subscription/Success",
        "/Subscription/ManageSubscription",
        "/api/webhook/stripe",
        "/health"
    };

    // Paths that are read-only operations (allowed for expired subscriptions)
    private static readonly HashSet<string> ReadOnlyPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Dashboard",
        "/Dashboard/Index",
        "/Accounts",
        "/Accounts/Index",
        "/Accounts/Details",
        "/Reports",
        "/Reports/Index",
        "/Reports/Quarterly",
        "/Reports/DownloadCsv",
        "/Forecasts",
        "/Forecasts/Index",
        "/Settings",
        "/Settings/Index"
    };

    public SubscriptionMiddleware(RequestDelegate next, ILogger<SubscriptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IStripeService stripeService,
        ISubscriptionRepository subscriptionRepository,
        UserManager<ApplicationUser> userManager)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip middleware for excluded paths
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Skip for static files
        if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib") ||
            path.StartsWith("/images") || path.StartsWith("/_framework") || path.Contains('.'))
        {
            await _next(context);
            return;
        }

        // Skip if user is not authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Skip if Stripe is not configured (free mode / self-hosted)
        if (!stripeService.IsConfigured)
        {
            await _next(context);
            return;
        }

        // Get user and subscription
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            await _next(context);
            return;
        }

        var subscription = await subscriptionRepository.GetByUserIdAsync(userGuid);

        // If no subscription exists, create a trial
        if (subscription == null)
        {
            subscription = await CreateTrialSubscription(subscriptionRepository, userGuid);
        }

        // Store subscription info in HttpContext for views
        context.Items["Subscription"] = subscription;
        context.Items["HasActiveAccess"] = subscription.HasActiveAccess;
        context.Items["IsInTrial"] = subscription.IsInTrial;
        context.Items["TrialDaysRemaining"] = subscription.TrialDaysRemaining;

        // If user has active access, proceed
        if (subscription.HasActiveAccess)
        {
            await _next(context);
            return;
        }

        // User doesn't have active access - check if this is a write operation
        var isReadOnly = IsReadOnlyPath(path) && context.Request.Method == "GET";

        if (isReadOnly)
        {
            // Allow read-only access but mark context
            context.Items["ReadOnlyMode"] = true;
            await _next(context);
            return;
        }

        // Block write operations - redirect to subscription page
        _logger.LogInformation("Blocking access for user {UserId} - subscription expired", userGuid);
        context.Response.Redirect("/Subscription?expired=true");
    }

    private static bool IsExcludedPath(string path)
    {
        // Check exact match
        if (ExcludedPaths.Contains(path))
            return true;

        // Check if path starts with any excluded prefix
        return ExcludedPaths.Any(excluded => path.StartsWith(excluded + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsReadOnlyPath(string path)
    {
        // Check exact match
        if (ReadOnlyPaths.Contains(path))
            return true;

        // Check if path starts with any read-only prefix
        return ReadOnlyPaths.Any(readOnly => path.StartsWith(readOnly + "/", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Subscription> CreateTrialSubscription(ISubscriptionRepository repository, Guid userId)
    {
        var trialDays = 14; // Default trial period
        var now = DateTime.UtcNow;

        var subscription = new Subscription
        {
            UserId = userId,
            Status = SubscriptionStatus.Trialing,
            TrialStartedAt = now,
            TrialEndsAt = now.AddDays(trialDays)
        };

        await repository.CreateAsync(subscription);
        _logger.LogInformation("Created trial subscription for user {UserId}, expires {TrialEnd}", userId, subscription.TrialEndsAt);

        return subscription;
    }
}

public static class SubscriptionMiddlewareExtensions
{
    public static IApplicationBuilder UseSubscriptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubscriptionMiddleware>();
    }
}
