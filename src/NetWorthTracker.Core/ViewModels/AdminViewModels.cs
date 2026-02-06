using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.ViewModels;

/// <summary>
/// Dashboard metrics for admin home page
/// </summary>
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int NewUsersLastMonth { get; set; }
    public List<SignupTrendPoint> SignupTrend { get; set; } = new();
    public List<AdminUserViewModel> RecentSignups { get; set; } = new();
}

/// <summary>
/// Data point for signup trend chart
/// </summary>
public class SignupTrendPoint
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// User summary for admin user list
/// </summary>
public class AdminUserViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool EmailConfirmed { get; set; }
    public int AccountCount { get; set; }
}

/// <summary>
/// Detailed user information for admin user details page
/// </summary>
public class AdminUserDetailsViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? Locale { get; set; }
    public string? TimeZone { get; set; }

    // Account summary
    public int AccountCount { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }

    // Recent activity
    public List<AuditLogViewModel> RecentActivity { get; set; } = new();
}

/// <summary>
/// Audit log entry for admin view
/// </summary>
public class AuditLogViewModel
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>
/// Filter options for audit log queries
/// </summary>
public class AuditLogFilter
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? UserId { get; set; }
    public string? UserSearch { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? SuccessOnly { get; set; }
}

/// <summary>
/// Generic paginated result
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Health dashboard view model for admin health page
/// </summary>
public class HealthDashboardViewModel
{
    public string OverallStatus { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public List<HealthCheckViewModel> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check result
/// </summary>
public class HealthCheckViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}
