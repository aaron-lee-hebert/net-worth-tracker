using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChartDataController : ControllerBase
{
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChartDataController(
        IBalanceHistoryRepository balanceHistoryRepository,
        IAccountRepository accountRepository,
        UserManager<ApplicationUser> userManager)
    {
        _balanceHistoryRepository = balanceHistoryRepository;
        _accountRepository = accountRepository;
        _userManager = userManager;
    }

    [HttpGet("history")]
    public async Task<ActionResult<ChartDataViewModel>> GetHistory(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        var effectiveEndDate = endDate ?? DateTime.UtcNow;
        var effectiveStartDate = startDate ?? effectiveEndDate.AddMonths(-1);

        var accounts = (await _accountRepository.GetActiveAccountsByUserIdAsync(userId)).ToList();
        var balanceHistory = (await _balanceHistoryRepository.GetByUserIdAndDateRangeAsync(
            userId, effectiveStartDate, effectiveEndDate)).ToList();

        var allDates = balanceHistory
            .Select(b => b.RecordedAt.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var viewModel = new ChartDataViewModel
        {
            Labels = allDates.Select(d => d.ToString("yyyy-MM-dd")).ToList()
        };

        var historyByAccount = balanceHistory
            .GroupBy(b => b.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var account in accounts)
        {
            var category = account.AccountType.GetCategory();
            var accountData = new AccountChartData
            {
                Id = account.Id,
                Name = account.Name,
                Type = account.AccountType.GetDisplayName(),
                Category = category.GetDisplayName(),
                IsAsset = account.AccountType.IsAsset()
            };

            if (historyByAccount.TryGetValue(account.Id, out var accountHistory))
            {
                var historyByDate = accountHistory.ToDictionary(h => h.RecordedAt.Date, h => h.Balance);
                decimal lastKnownBalance = 0;

                foreach (var date in allDates)
                {
                    if (historyByDate.TryGetValue(date, out var balance))
                    {
                        lastKnownBalance = balance;
                    }
                    accountData.Data.Add(lastKnownBalance);
                }
            }
            else
            {
                accountData.Data = allDates.Select(_ => 0m).ToList();
            }

            viewModel.Accounts.Add(accountData);
        }

        // Group by actual categories
        var allCategories = Enum.GetValues<AccountCategory>()
            .Select(c => c.GetDisplayName())
            .ToList();

        foreach (var categoryName in allCategories)
        {
            viewModel.ByType[categoryName] = new List<decimal>();
        }

        for (int i = 0; i < allDates.Count; i++)
        {
            foreach (var categoryName in allCategories)
            {
                var categoryAccounts = viewModel.Accounts
                    .Where(a => a.Category == categoryName)
                    .ToList();

                var sum = categoryAccounts.Sum(a => i < a.Data.Count ? a.Data[i] : 0);
                viewModel.ByType[categoryName].Add(sum);
            }
        }

        // Calculate net worth: assets - liabilities
        for (int i = 0; i < allDates.Count; i++)
        {
            var totalAssets = viewModel.Accounts
                .Where(a => a.IsAsset)
                .Sum(a => i < a.Data.Count ? a.Data[i] : 0);

            var totalLiabilities = viewModel.Accounts
                .Where(a => !a.IsAsset)
                .Sum(a => i < a.Data.Count ? a.Data[i] : 0);

            viewModel.NetWorth.Add(totalAssets - totalLiabilities);
        }

        return Ok(viewModel);
    }
}
