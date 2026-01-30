using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IDashboardService dashboardService,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(bool firstAccount = false)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var summary = await _dashboardService.GetDashboardSummaryAsync(userId);

        var viewModel = new DashboardViewModel
        {
            TotalAssets = summary.TotalAssets,
            TotalLiabilities = summary.TotalLiabilities,
            TotalNetWorth = summary.TotalNetWorth,
            TotalsByCategory = summary.TotalsByCategory,
            RecentAccounts = summary.RecentAccounts.Select(a => new AccountSummaryViewModel
            {
                Id = a.Id,
                Name = a.Name,
                AccountType = a.AccountType,
                CurrentBalance = a.CurrentBalance,
                Institution = a.Institution
            }).ToList(),
            IsFirstTimeUser = !summary.HasAccounts,
            ShowFirstAccountSuccess = firstAccount && summary.RecentAccounts.Count == 1
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountsForBulkUpdate()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = await _dashboardService.GetAccountsForBulkUpdateAsync(userId);

        var result = accounts.Select(a => new AccountForBulkUpdateViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Institution = a.Institution,
            CurrentBalance = a.CurrentBalance,
            Category = a.Category,
            CategoryOrder = a.CategoryOrder,
            IsLiability = a.IsLiability
        }).ToList();

        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("bulk-update")]
    public async Task<IActionResult> BulkUpdateBalances([FromBody] BulkBalanceUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new BulkBalanceUpdateResponse
            {
                Success = false,
                Message = string.Join("; ", errors)
            });
        }

        if (model?.Accounts == null || model.Accounts.Count == 0)
        {
            return Json(new BulkBalanceUpdateResponse
            {
                Success = false,
                Message = "No accounts to update"
            });
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        var request = new BulkUpdateRequest
        {
            RecordedAt = model.RecordedAt,
            Notes = model.Notes,
            Accounts = model.Accounts.Select(a => new AccountBalanceUpdate
            {
                AccountId = a.AccountId,
                NewBalance = a.NewBalance
            }).ToList()
        };

        var result = await _dashboardService.BulkUpdateBalancesAsync(userId, request);

        return Json(new BulkBalanceUpdateResponse
        {
            Success = result.Success,
            UpdatedCount = result.UpdatedCount,
            Message = result.Message
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var summary = await _dashboardService.GetDashboardSummaryAsync(userId);

        var totalsByCategory = summary.TotalsByCategory.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value);

        var recentAccounts = summary.RecentAccounts.Select(a => new
        {
            id = a.Id,
            name = a.Name,
            accountType = a.AccountType.ToString(),
            accountTypeCategory = a.AccountType.ToString(),
            currentBalance = a.CurrentBalance,
            institution = a.Institution,
            isLiability = false
        }).ToList();

        return Json(new
        {
            totalNetWorth = summary.TotalNetWorth,
            totalAssets = summary.TotalAssets,
            totalLiabilities = summary.TotalLiabilities,
            totalsByCategory,
            recentAccounts
        });
    }
}
