using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NetWorthTracker.Core;
using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Web.Middleware;

public class UserLocaleMiddleware
{
    private readonly RequestDelegate _next;

    public UserLocaleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        string locale = "en-US";

        // For authenticated users, load locale from user profile
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user != null && SupportedLocales.IsSupported(user.Locale))
                {
                    locale = user.Locale;
                }
            }
        }
        else
        {
            // For anonymous users, detect from browser Accept-Language header
            var acceptLanguage = context.Request.Headers.AcceptLanguage.FirstOrDefault();
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                // Parse Accept-Language header (e.g., "en-US,en;q=0.9,fr;q=0.8")
                var languages = acceptLanguage.Split(',')
                    .Select(l => l.Split(';')[0].Trim())
                    .ToList();

                foreach (var lang in languages)
                {
                    if (SupportedLocales.IsSupported(lang))
                    {
                        locale = lang;
                        break;
                    }

                    // Try to match base language (e.g., "en" -> "en-US")
                    var baseLang = lang.Split('-')[0];
                    var match = SupportedLocales.Locales.Keys.FirstOrDefault(
                        l => l.StartsWith(baseLang + "-", StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        locale = match;
                        break;
                    }
                }
            }
        }

        // Set culture for the current request
        var culture = new CultureInfo(locale);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        // Store locale info in HttpContext.Items for JavaScript access
        context.Items["UserLocale"] = locale;
        context.Items["UserCurrency"] = SupportedLocales.GetCurrency(locale);

        await _next(context);
    }
}

public static class UserLocaleMiddlewareExtensions
{
    public static IApplicationBuilder UseUserLocale(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserLocaleMiddleware>();
    }
}
