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
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IAccountRepository accountRepository,
        UserManager<ApplicationUser> userManager)
    {
        _accountRepository = accountRepository;
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
}
