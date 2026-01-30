using Microsoft.AspNetCore.Identity;
using NetWorthTracker.Core;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Web.Services;

/// <summary>
/// Service for converting UTC timestamps to the current user's timezone.
/// </summary>
public class UserTimeZoneService : IUserTimeZoneService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private string? _cachedTimeZone;

    public UserTimeZoneService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string GetUserTimeZone()
    {
        if (_cachedTimeZone != null)
            return _cachedTimeZone;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Try to get from claims first (faster)
            var timeZoneClaim = httpContext.User.FindFirst("TimeZone")?.Value;
            if (!string.IsNullOrEmpty(timeZoneClaim) && SupportedTimeZones.IsSupported(timeZoneClaim))
            {
                _cachedTimeZone = timeZoneClaim;
                return _cachedTimeZone;
            }

            // Fall back to getting from user manager (requires async, but we cache it)
            var userId = _userManager.GetUserId(httpContext.User);
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    // Note: This is synchronous for simplicity in views.
                    // The timezone is cached per-request to avoid repeated lookups.
                    var user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
                    if (user != null && !string.IsNullOrEmpty(user.TimeZone) && SupportedTimeZones.IsSupported(user.TimeZone))
                    {
                        _cachedTimeZone = user.TimeZone;
                        return _cachedTimeZone;
                    }
                }
                catch
                {
                    // If database lookup fails, fall through to default
                }
            }
        }

        // Default to Eastern Time
        _cachedTimeZone = "America/New_York";
        return _cachedTimeZone;
    }

    public DateTime ToUserTime(DateTime utcDateTime)
    {
        var timeZone = GetUserTimeZone();
        return SupportedTimeZones.ConvertFromUtc(utcDateTime, timeZone);
    }

    public DateTime? ToUserTime(DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue) return null;
        return ToUserTime(utcDateTime.Value);
    }

    public string FormatInUserTime(DateTime utcDateTime, string format)
    {
        var localTime = ToUserTime(utcDateTime);
        return localTime.ToString(format);
    }

    public string? FormatInUserTime(DateTime? utcDateTime, string format)
    {
        if (!utcDateTime.HasValue) return null;
        return FormatInUserTime(utcDateTime.Value, format);
    }
}
