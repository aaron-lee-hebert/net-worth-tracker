using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Application.Interfaces;

/// <summary>
/// Service for dashboard operations including net worth calculations and bulk balance updates.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the complete dashboard summary for a user.
    /// </summary>
    Task<DashboardSummaryResult> GetDashboardSummaryAsync(Guid userId);

    /// <summary>
    /// Gets accounts formatted for bulk update, ordered by category.
    /// </summary>
    Task<IReadOnlyList<AccountForBulkUpdate>> GetAccountsForBulkUpdateAsync(Guid userId);

    /// <summary>
    /// Performs bulk balance updates across multiple accounts.
    /// </summary>
    Task<BulkUpdateResult> BulkUpdateBalancesAsync(Guid userId, BulkUpdateRequest request);
}

/// <summary>
/// Dashboard summary data returned by the service.
/// </summary>
public class DashboardSummaryResult
{
    public decimal TotalNetWorth { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public Dictionary<AccountCategory, decimal> TotalsByCategory { get; init; } = new();
    public IReadOnlyList<AccountSummary> RecentAccounts { get; init; } = [];
    public bool HasAccounts { get; init; }
}

/// <summary>
/// Summary information for a single account.
/// </summary>
public class AccountSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    public decimal CurrentBalance { get; init; }
    public string? Institution { get; init; }
}

/// <summary>
/// Account data formatted for bulk update operations.
/// </summary>
public class AccountForBulkUpdate
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Institution { get; init; }
    public decimal CurrentBalance { get; init; }
    public string Category { get; init; } = string.Empty;
    public int CategoryOrder { get; init; }
    public bool IsLiability { get; init; }
}

/// <summary>
/// Request for bulk balance update operation.
/// </summary>
public class BulkUpdateRequest
{
    public DateTime RecordedAt { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<AccountBalanceUpdate> Accounts { get; init; } = [];
}

/// <summary>
/// Individual account balance update within a bulk operation.
/// </summary>
public class AccountBalanceUpdate
{
    public Guid AccountId { get; init; }
    public decimal NewBalance { get; init; }
}

/// <summary>
/// Result of a bulk balance update operation.
/// </summary>
public class BulkUpdateResult
{
    public bool Success { get; init; }
    public int UpdatedCount { get; init; }
    public string? Message { get; init; }

    public static BulkUpdateResult Failure(string message) => new() { Success = false, Message = message };
    public static BulkUpdateResult Ok(int count, string message) => new() { Success = true, UpdatedCount = count, Message = message };
}
