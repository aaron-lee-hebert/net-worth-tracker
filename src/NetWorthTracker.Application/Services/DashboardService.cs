using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IAuditService _auditService;

    private static readonly Dictionary<AccountCategory, int> CategoryOrder = new()
    {
        { AccountCategory.Banking, 1 },
        { AccountCategory.Investment, 2 },
        { AccountCategory.RealEstate, 3 },
        { AccountCategory.VehiclesAndProperty, 4 },
        { AccountCategory.Business, 5 },
        { AccountCategory.SecuredDebt, 6 },
        { AccountCategory.UnsecuredDebt, 7 },
        { AccountCategory.OtherLiabilities, 8 }
    };

    public DashboardService(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAuditService auditService)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _auditService = auditService;
    }

    public async Task<DashboardSummaryResult> GetDashboardSummaryAsync(Guid userId)
    {
        var accounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);
        var accountList = accounts.ToList();

        var totalsByCategory = accountList
            .GroupBy(a => a.AccountType.GetCategory())
            .ToDictionary(g => g.Key, g => g.Sum(a => a.CurrentBalance));

        var totalAssets = accountList
            .Where(a => a.AccountType.IsAsset())
            .Sum(a => a.CurrentBalance);

        var totalLiabilities = accountList
            .Where(a => a.AccountType.IsLiability())
            .Sum(a => a.CurrentBalance);

        return new DashboardSummaryResult
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalNetWorth = totalAssets - totalLiabilities,
            TotalsByCategory = totalsByCategory,
            RecentAccounts = accountList
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .Take(5)
                .Select(a => new AccountSummary
                {
                    Id = a.Id,
                    Name = a.Name,
                    AccountType = a.AccountType,
                    CurrentBalance = a.CurrentBalance,
                    Institution = a.Institution
                })
                .ToList(),
            HasAccounts = accountList.Count > 0
        };
    }

    public async Task<IReadOnlyList<AccountForBulkUpdate>> GetAccountsForBulkUpdateAsync(Guid userId)
    {
        var accounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);

        return accounts
            .OrderBy(a => CategoryOrder.GetValueOrDefault(a.AccountType.GetCategory(), 99))
            .ThenBy(a => a.Name)
            .Select(a => new AccountForBulkUpdate
            {
                Id = a.Id,
                Name = a.Name,
                Institution = a.Institution,
                CurrentBalance = a.CurrentBalance,
                Category = a.AccountType.GetCategory().GetDisplayName(),
                CategoryOrder = CategoryOrder.GetValueOrDefault(a.AccountType.GetCategory(), 99),
                IsLiability = a.AccountType.IsLiability()
            })
            .ToList();
    }

    public async Task<BulkUpdateResult> BulkUpdateBalancesAsync(Guid userId, BulkUpdateRequest request)
    {
        if (request.Accounts.Count == 0)
        {
            return BulkUpdateResult.Failure("No accounts to update");
        }

        var userAccounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);
        var userAccountIds = userAccounts.ToDictionary(a => a.Id);

        var updatedCount = 0;

        foreach (var item in request.Accounts)
        {
            if (!userAccountIds.TryGetValue(item.AccountId, out var account))
            {
                continue;
            }

            if (account.CurrentBalance == item.NewBalance)
            {
                continue;
            }

            var existingRecord = await _balanceHistoryRepository.GetByAccountIdAndDateAsync(item.AccountId, request.RecordedAt);

            if (existingRecord != null)
            {
                existingRecord.Balance = item.NewBalance;
                existingRecord.Notes = request.Notes;
                await _balanceHistoryRepository.UpdateAsync(existingRecord);
            }
            else
            {
                var balanceHistory = new Core.Entities.BalanceHistory
                {
                    AccountId = item.AccountId,
                    Balance = item.NewBalance,
                    RecordedAt = request.RecordedAt,
                    Notes = request.Notes
                };

                await _balanceHistoryRepository.AddAsync(balanceHistory);
            }

            await UpdateAccountCurrentBalanceAsync(account);
            updatedCount++;
        }

        // Audit log - bulk balance update
        if (updatedCount > 0)
        {
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = userId,
                Action = AuditAction.BalanceBulkUpdated,
                EntityType = AuditEntityType.BalanceHistory,
                Description = $"Bulk updated {updatedCount} account balance(s) for {request.RecordedAt:d}",
                NewValue = new
                {
                    UpdatedCount = updatedCount,
                    RecordedAt = request.RecordedAt,
                    Notes = request.Notes
                }
            });
        }

        return BulkUpdateResult.Ok(updatedCount, $"Successfully updated {updatedCount} account(s)");
    }

    private async Task UpdateAccountCurrentBalanceAsync(Core.Entities.Account account)
    {
        var allHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
        var latestHistory = allHistory.OrderByDescending(h => h.RecordedAt).FirstOrDefault();

        account.CurrentBalance = latestHistory?.Balance ?? 0;
        await _accountRepository.UpdateAsync(account);
    }
}
