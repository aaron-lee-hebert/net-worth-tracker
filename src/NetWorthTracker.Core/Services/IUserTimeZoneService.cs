namespace NetWorthTracker.Core.Services;

/// <summary>
/// Service for converting UTC timestamps to the current user's timezone.
/// </summary>
public interface IUserTimeZoneService
{
    /// <summary>
    /// Gets the current user's timezone identifier.
    /// </summary>
    string GetUserTimeZone();

    /// <summary>
    /// Converts a UTC DateTime to the current user's timezone.
    /// </summary>
    DateTime ToUserTime(DateTime utcDateTime);

    /// <summary>
    /// Converts a nullable UTC DateTime to the current user's timezone.
    /// </summary>
    DateTime? ToUserTime(DateTime? utcDateTime);

    /// <summary>
    /// Formats a UTC DateTime in the user's timezone with the specified format.
    /// </summary>
    string FormatInUserTime(DateTime utcDateTime, string format);

    /// <summary>
    /// Formats a nullable UTC DateTime in the user's timezone with the specified format.
    /// </summary>
    string? FormatInUserTime(DateTime? utcDateTime, string format);
}
