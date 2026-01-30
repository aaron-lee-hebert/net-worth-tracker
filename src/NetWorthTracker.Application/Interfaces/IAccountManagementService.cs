using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Interfaces;

/// <summary>
/// Service for managing accounts and balance history.
/// </summary>
public interface IAccountManagementService
{
    /// <summary>
    /// Gets all accounts for a user, optionally filtered by category.
    /// </summary>
    Task<IReadOnlyList<AccountViewModel>> GetAccountsAsync(Guid userId, AccountCategory? category = null);

    /// <summary>
    /// Gets detailed account information including balance history.
    /// </summary>
    Task<AccountDetailsResult?> GetAccountDetailsAsync(Guid userId, Guid accountId);

    /// <summary>
    /// Creates a new account with initial balance history record.
    /// </summary>
    Task<AccountCreateResult> CreateAccountAsync(Guid userId, AccountCreateViewModel model);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    Task<ServiceResult> UpdateAccountAsync(Guid userId, Guid accountId, AccountEditViewModel model);

    /// <summary>
    /// Deletes an account.
    /// </summary>
    Task<ServiceResult> DeleteAccountAsync(Guid userId, Guid accountId);

    /// <summary>
    /// Adds a new balance history record for an account.
    /// </summary>
    Task<ServiceResult> AddBalanceRecordAsync(Guid userId, Guid accountId, decimal newBalance, string? notes, DateTime? recordedAt);

    /// <summary>
    /// Gets a specific balance history record.
    /// </summary>
    Task<BalanceHistoryEditViewModel?> GetBalanceRecordAsync(Guid userId, Guid historyId);

    /// <summary>
    /// Updates an existing balance history record.
    /// </summary>
    Task<ServiceResult> UpdateBalanceRecordAsync(Guid userId, BalanceHistoryEditViewModel model);

    /// <summary>
    /// Deletes a balance history record.
    /// </summary>
    Task<ServiceResult> DeleteBalanceRecordAsync(Guid userId, Guid historyId);
}

/// <summary>
/// Result of getting account details.
/// </summary>
public class AccountDetailsResult
{
    public AccountViewModel Account { get; init; } = null!;
    public IReadOnlyList<BalanceHistoryViewModel> BalanceHistory { get; init; } = [];
}

/// <summary>
/// Result of creating an account.
/// </summary>
public class AccountCreateResult
{
    public bool Success { get; init; }
    public Guid AccountId { get; init; }
    public bool IsFirstAccount { get; init; }
    public string? ErrorMessage { get; init; }

    public static AccountCreateResult Ok(Guid accountId, bool isFirstAccount) => new()
    {
        Success = true,
        AccountId = accountId,
        IsFirstAccount = isFirstAccount
    };

    public static AccountCreateResult Failure(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

/// <summary>
/// Generic service operation result.
/// </summary>
public class ServiceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RelatedId { get; init; }

    public static ServiceResult Ok(Guid? relatedId = null) => new() { Success = true, RelatedId = relatedId };
    public static ServiceResult NotFound(string message = "Not found") => new() { Success = false, ErrorMessage = message };
    public static ServiceResult Failure(string message) => new() { Success = false, ErrorMessage = message };
}
