namespace NetWorthTracker.Core;

public static class SupportedTimeZones
{
    /// <summary>
    /// Common IANA timezone identifiers grouped by region.
    /// These work cross-platform (.NET uses IANA on Linux/macOS and converts on Windows).
    /// </summary>
    public static readonly Dictionary<string, string> TimeZones = new()
    {
        // North America
        { "America/New_York", "Eastern Time (US & Canada)" },
        { "America/Chicago", "Central Time (US & Canada)" },
        { "America/Denver", "Mountain Time (US & Canada)" },
        { "America/Los_Angeles", "Pacific Time (US & Canada)" },
        { "America/Anchorage", "Alaska" },
        { "Pacific/Honolulu", "Hawaii" },
        { "America/Phoenix", "Arizona (No DST)" },
        { "America/Toronto", "Eastern Time (Canada)" },
        { "America/Vancouver", "Pacific Time (Canada)" },

        // Europe
        { "Europe/London", "London, Edinburgh" },
        { "Europe/Paris", "Paris, Berlin, Rome, Madrid" },
        { "Europe/Amsterdam", "Amsterdam, Brussels" },
        { "Europe/Zurich", "Zurich, Vienna" },
        { "Europe/Stockholm", "Stockholm, Oslo, Copenhagen" },
        { "Europe/Helsinki", "Helsinki, Riga, Tallinn" },
        { "Europe/Athens", "Athens, Bucharest" },
        { "Europe/Moscow", "Moscow, St. Petersburg" },

        // Asia Pacific
        { "Asia/Dubai", "Dubai, Abu Dhabi" },
        { "Asia/Kolkata", "Mumbai, New Delhi" },
        { "Asia/Singapore", "Singapore, Kuala Lumpur" },
        { "Asia/Hong_Kong", "Hong Kong" },
        { "Asia/Shanghai", "Beijing, Shanghai" },
        { "Asia/Tokyo", "Tokyo, Osaka" },
        { "Asia/Seoul", "Seoul" },

        // Australia & New Zealand
        { "Australia/Sydney", "Sydney, Melbourne" },
        { "Australia/Brisbane", "Brisbane (No DST)" },
        { "Australia/Perth", "Perth" },
        { "Australia/Adelaide", "Adelaide" },
        { "Pacific/Auckland", "Auckland, Wellington" },

        // South America
        { "America/Sao_Paulo", "Sao Paulo, Rio de Janeiro" },
        { "America/Buenos_Aires", "Buenos Aires" },
        { "America/Santiago", "Santiago" },

        // Africa
        { "Africa/Johannesburg", "Johannesburg, Pretoria" },
        { "Africa/Cairo", "Cairo" },
        { "Africa/Lagos", "Lagos, Accra" },

        // UTC
        { "UTC", "UTC (Coordinated Universal Time)" }
    };

    /// <summary>
    /// Timezone groups for organized dropdown display
    /// </summary>
    public static readonly Dictionary<string, List<string>> TimeZoneGroups = new()
    {
        { "North America", new List<string>
            {
                "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles",
                "America/Anchorage", "Pacific/Honolulu", "America/Phoenix",
                "America/Toronto", "America/Vancouver"
            }
        },
        { "Europe", new List<string>
            {
                "Europe/London", "Europe/Paris", "Europe/Amsterdam", "Europe/Zurich",
                "Europe/Stockholm", "Europe/Helsinki", "Europe/Athens", "Europe/Moscow"
            }
        },
        { "Asia Pacific", new List<string>
            {
                "Asia/Dubai", "Asia/Kolkata", "Asia/Singapore", "Asia/Hong_Kong",
                "Asia/Shanghai", "Asia/Tokyo", "Asia/Seoul"
            }
        },
        { "Australia & Pacific", new List<string>
            {
                "Australia/Sydney", "Australia/Brisbane", "Australia/Perth",
                "Australia/Adelaide", "Pacific/Auckland"
            }
        },
        { "Americas (South)", new List<string>
            {
                "America/Sao_Paulo", "America/Buenos_Aires", "America/Santiago"
            }
        },
        { "Africa", new List<string>
            {
                "Africa/Johannesburg", "Africa/Cairo", "Africa/Lagos"
            }
        },
        { "Other", new List<string> { "UTC" } }
    };

    public static bool IsSupported(string? timeZone)
    {
        return !string.IsNullOrEmpty(timeZone) && TimeZones.ContainsKey(timeZone);
    }

    public static string GetDisplayName(string timeZone)
    {
        return TimeZones.TryGetValue(timeZone, out var name) ? name : timeZone;
    }

    /// <summary>
    /// Converts a UTC DateTime to the specified timezone.
    /// </summary>
    public static DateTime ConvertFromUtc(DateTime utcDateTime, string timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId))
        {
            return utcDateTime;
        }

        if (utcDateTime.Kind == DateTimeKind.Local)
        {
            utcDateTime = utcDateTime.ToUniversalTime();
        }
        else if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        // .NET 6+ TryFindSystemTimeZoneById handles both IANA and Windows IDs
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }

        // Fallback: try to convert IANA ID to Windows ID (for older systems without ICU)
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId) &&
            TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out var windowsTimeZone))
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, windowsTimeZone);
        }

        // If all else fails, return UTC
        return utcDateTime;
    }

    /// <summary>
    /// Converts a nullable UTC DateTime to the specified timezone.
    /// </summary>
    public static DateTime? ConvertFromUtc(DateTime? utcDateTime, string timeZoneId)
    {
        if (!utcDateTime.HasValue) return null;
        return ConvertFromUtc(utcDateTime.Value, timeZoneId);
    }
}
