using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        UserManager<ApplicationUser> userManager)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(bool firstAccount = false)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);

        var accountList = accounts.ToList();
        var isFirstTimeUser = accountList.Count == 0;

        var totalsByCategory = accountList
            .GroupBy(a => a.AccountType.GetCategory())
            .ToDictionary(g => g.Key, g => g.Sum(a => a.CurrentBalance));

        var totalAssets = accountList
            .Where(a => a.AccountType.IsAsset())
            .Sum(a => a.CurrentBalance);

        var totalLiabilities = accountList
            .Where(a => a.AccountType.IsLiability())
            .Sum(a => a.CurrentBalance);

        var viewModel = new DashboardViewModel
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalNetWorth = totalAssets - totalLiabilities,
            TotalsByCategory = totalsByCategory,
            RecentAccounts = accountList
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .Take(5)
                .Select(a => new AccountSummaryViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    AccountType = a.AccountType,
                    CurrentBalance = a.CurrentBalance,
                    Institution = a.Institution
                })
                .ToList(),
            IsFirstTimeUser = isFirstTimeUser,
            ShowFirstAccountSuccess = firstAccount && accountList.Count == 1
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountsForBulkUpdate()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);

        var categoryOrder = new Dictionary<AccountCategory, int>
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

        var result = accounts
            .OrderBy(a => categoryOrder.GetValueOrDefault(a.AccountType.GetCategory(), 99))
            .ThenBy(a => a.Name)
            .Select(a => new AccountForBulkUpdateViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Institution = a.Institution,
                CurrentBalance = a.CurrentBalance,
                Category = a.AccountType.GetCategory().GetDisplayName(),
                CategoryOrder = categoryOrder.GetValueOrDefault(a.AccountType.GetCategory(), 99),
                IsLiability = a.AccountType.IsLiability()
            })
            .ToList();

        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateBalances([FromBody] BulkBalanceUpdateViewModel model)
    {
        if (model?.Accounts == null || model.Accounts.Count == 0)
        {
            return Json(new BulkBalanceUpdateResponse
            {
                Success = false,
                Message = "No accounts to update"
            });
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var userAccounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);
        var userAccountIds = userAccounts.ToDictionary(a => a.Id);

        // Use UTC timestamp
        var recordedAt = model.RecordedAt;

        var updatedCount = 0;

        foreach (var item in model.Accounts)
        {
            // Verify account belongs to user
            if (!userAccountIds.TryGetValue(item.AccountId, out var account))
            {
                continue;
            }

            // Skip if balance hasn't changed
            if (account.CurrentBalance == item.NewBalance)
            {
                continue;
            }

            // Check if a record already exists for this account and date (upsert)
            var existingRecord = await _balanceHistoryRepository.GetByAccountIdAndDateAsync(item.AccountId, recordedAt);

            if (existingRecord != null)
            {
                // Update existing record
                existingRecord.Balance = item.NewBalance;
                existingRecord.Notes = model.Notes;
                await _balanceHistoryRepository.UpdateAsync(existingRecord);
            }
            else
            {
                // Create new balance history record
                var balanceHistory = new BalanceHistory
                {
                    AccountId = item.AccountId,
                    Balance = item.NewBalance,
                    RecordedAt = recordedAt,
                    Notes = model.Notes
                };

                await _balanceHistoryRepository.AddAsync(balanceHistory);
            }

            // Update account current balance
            await UpdateAccountCurrentBalanceAsync(account);

            updatedCount++;
        }

        return Json(new BulkBalanceUpdateResponse
        {
            Success = true,
            UpdatedCount = updatedCount,
            Message = $"Successfully updated {updatedCount} account(s)"
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = await _accountRepository.GetActiveAccountsByUserIdAsync(userId);

        var accountList = accounts.ToList();

        var totalsByCategory = accountList
            .GroupBy(a => a.AccountType.GetCategory())
            .ToDictionary(
                g => g.Key.GetDisplayName(),
                g => g.Sum(a => a.CurrentBalance));

        var totalAssets = accountList
            .Where(a => a.AccountType.IsAsset())
            .Sum(a => a.CurrentBalance);

        var totalLiabilities = accountList
            .Where(a => a.AccountType.IsLiability())
            .Sum(a => a.CurrentBalance);

        var recentAccounts = accountList
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .Take(5)
            .Select(a => new
            {
                id = a.Id,
                name = a.Name,
                accountType = a.AccountType.GetDisplayName(),
                accountTypeCategory = a.AccountType.GetCategory().ToString(),
                currentBalance = a.CurrentBalance,
                institution = a.Institution,
                isLiability = a.AccountType.IsLiability()
            })
            .ToList();

        return Json(new
        {
            totalNetWorth = totalAssets - totalLiabilities,
            totalAssets,
            totalLiabilities,
            totalsByCategory,
            recentAccounts
        });
    }

    private async Task UpdateAccountCurrentBalanceAsync(Account account)
    {
        var allHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
        var latestHistory = allHistory.OrderByDescending(h => h.RecordedAt).FirstOrDefault();

        account.CurrentBalance = latestHistory?.Balance ?? 0;
        await _accountRepository.UpdateAsync(account);
    }
}
