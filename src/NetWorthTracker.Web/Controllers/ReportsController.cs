using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        UserManager<ApplicationUser> userManager)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Quarterly()
    {
        var viewModel = await BuildQuarterlyReportAsync();
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCsv()
    {
        var report = await BuildQuarterlyReportAsync();
        var csv = GenerateCsv(report);

        var fileName = $"net-worth-quarterly-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    private async Task<QuarterlyReportViewModel> BuildQuarterlyReportAsync()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = (await _accountRepository.GetActiveAccountsByUserIdAsync(userId)).ToList();

        if (!accounts.Any())
        {
            return new QuarterlyReportViewModel();
        }

        // Get all balance history for the user
        var earliestDate = new DateTime(2000, 1, 1);
        var allHistory = (await _balanceHistoryRepository.GetByUserIdAndDateRangeAsync(
            userId, earliestDate, DateTime.UtcNow)).ToList();

        if (!allHistory.Any())
        {
            return new QuarterlyReportViewModel();
        }

        // Determine quarters from earliest record to now
        var firstRecord = allHistory.Min(h => h.RecordedAt);
        var quarters = GenerateQuarters(firstRecord, DateTime.UtcNow);

        var viewModel = new QuarterlyReportViewModel
        {
            Quarters = quarters.Select(q => FormatQuarter(q)).ToList()
        };

        // Group history by account
        var historyByAccount = allHistory
            .GroupBy(h => h.AccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.RecordedAt).ToList());

        // Build account data
        foreach (var account in accounts.OrderBy(a => a.AccountType.IsLiability()).ThenBy(a => a.Name))
        {
            var accountData = new AccountQuarterlyData
            {
                Id = account.Id,
                Name = account.Name,
                Type = account.AccountType.GetDisplayName(),
                IsLiability = account.AccountType.IsLiability()
            };

            decimal? lastKnownBalance = null;

            foreach (var quarterEnd in quarters)
            {
                if (historyByAccount.TryGetValue(account.Id, out var accountHistory))
                {
                    // Find the balance at the end of this quarter (or the most recent before it)
                    var balanceAtQuarter = accountHistory
                        .Where(h => h.RecordedAt.Date <= quarterEnd)
                        .OrderByDescending(h => h.RecordedAt)
                        .FirstOrDefault();

                    if (balanceAtQuarter != null)
                    {
                        lastKnownBalance = balanceAtQuarter.Balance;
                    }
                }

                accountData.Balances.Add(lastKnownBalance);
            }

            viewModel.Accounts.Add(accountData);
        }

        // Calculate totals (net worth = assets - liabilities)
        for (int i = 0; i < quarters.Count; i++)
        {
            decimal totalAssets = 0;
            decimal totalLiabilities = 0;

            foreach (var account in viewModel.Accounts)
            {
                var balance = account.Balances[i] ?? 0;
                if (account.IsLiability)
                {
                    totalLiabilities += balance;
                }
                else
                {
                    totalAssets += balance;
                }
            }

            var netWorth = totalAssets - totalLiabilities;
            viewModel.Totals.NetWorth.Add(netWorth);

            // Calculate percent change from previous quarter
            if (i > 0)
            {
                var previousNetWorth = viewModel.Totals.NetWorth[i - 1];
                if (previousNetWorth != 0)
                {
                    var percentChange = ((netWorth - previousNetWorth) / Math.Abs(previousNetWorth)) * 100;
                    viewModel.Totals.PercentChange.Add(percentChange);
                }
                else
                {
                    viewModel.Totals.PercentChange.Add(null);
                }
            }
            else
            {
                viewModel.Totals.PercentChange.Add(null);
            }
        }

        return viewModel;
    }

    private static List<DateTime> GenerateQuarters(DateTime start, DateTime end)
    {
        var quarters = new List<DateTime>();

        // Start from the beginning of the quarter containing the start date
        var currentQuarter = GetQuarterEnd(start);

        while (currentQuarter <= end)
        {
            quarters.Add(currentQuarter);
            currentQuarter = currentQuarter.AddMonths(3);
            currentQuarter = GetQuarterEnd(currentQuarter);
        }

        // Add current quarter if we haven't passed it
        if (quarters.Count == 0 || quarters.Last() < GetQuarterEnd(end))
        {
            quarters.Add(GetQuarterEnd(end));
        }

        return quarters.Distinct().OrderBy(q => q).ToList();
    }

    private static DateTime GetQuarterEnd(DateTime date)
    {
        var quarter = (date.Month - 1) / 3;
        var quarterEndMonth = (quarter + 1) * 3;
        return new DateTime(date.Year, quarterEndMonth, 1).AddMonths(1).AddDays(-1);
    }

    private static string FormatQuarter(DateTime quarterEnd)
    {
        var quarter = (quarterEnd.Month - 1) / 3 + 1;
        return $"Q{quarter} {quarterEnd.Year}";
    }

    private static string GenerateCsv(QuarterlyReportViewModel report)
    {
        var sb = new StringBuilder();

        // Header row
        sb.Append("Account,Type");
        foreach (var quarter in report.Quarters)
        {
            sb.Append($",{quarter}");
        }
        sb.AppendLine();

        // Account rows
        foreach (var account in report.Accounts)
        {
            sb.Append($"\"{EscapeCsv(account.Name)}\",\"{EscapeCsv(account.Type)}\"");
            foreach (var balance in account.Balances)
            {
                sb.Append($",{balance?.ToString("F2") ?? ""}");
            }
            sb.AppendLine();
        }

        // Empty row before totals
        sb.AppendLine();

        // Net Worth row
        sb.Append("Net Worth,");
        foreach (var netWorth in report.Totals.NetWorth)
        {
            sb.Append($",{netWorth:F2}");
        }
        sb.AppendLine();

        // Percent Change row
        sb.Append("% Change,");
        foreach (var change in report.Totals.PercentChange)
        {
            sb.Append($",{change?.ToString("F2") ?? ""}");
        }
        sb.AppendLine();

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadNetWorthHistoryCsv()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var accounts = (await _accountRepository.GetActiveAccountsByUserIdAsync(userId)).ToList();

        if (!accounts.Any())
        {
            return RedirectToAction(nameof(Quarterly));
        }

        var earliestDate = new DateTime(2000, 1, 1);
        var allHistory = (await _balanceHistoryRepository.GetByUserIdAndDateRangeAsync(
            userId, earliestDate, DateTime.UtcNow)).ToList();

        if (!allHistory.Any())
        {
            return RedirectToAction(nameof(Quarterly));
        }

        var csv = GenerateNetWorthHistoryCsv(accounts, allHistory);
        var fileName = $"net-worth-history-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    private string GenerateNetWorthHistoryCsv(List<Account> accounts, List<BalanceHistory> allHistory)
    {
        var sb = new StringBuilder();

        // Build monthly snapshots
        var historyByAccount = allHistory
            .GroupBy(h => h.AccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.RecordedAt).ToList());

        // Get date range
        var firstDate = allHistory.Min(h => h.RecordedAt);
        var months = new List<DateTime>();
        var currentMonth = new DateTime(firstDate.Year, firstDate.Month, 1);

        while (currentMonth <= DateTime.UtcNow)
        {
            months.Add(currentMonth);
            currentMonth = currentMonth.AddMonths(1);
        }

        // Header
        sb.AppendLine("Date,Total Assets,Total Liabilities,Net Worth,Change,% Change");

        decimal? previousNetWorth = null;

        foreach (var month in months)
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            decimal totalAssets = 0;
            decimal totalLiabilities = 0;

            foreach (var account in accounts)
            {
                if (historyByAccount.TryGetValue(account.Id, out var accountHistory))
                {
                    var balanceAtMonth = accountHistory
                        .Where(h => h.RecordedAt.Date <= monthEnd)
                        .OrderByDescending(h => h.RecordedAt)
                        .FirstOrDefault();

                    if (balanceAtMonth != null)
                    {
                        if (account.AccountType.IsLiability())
                            totalLiabilities += balanceAtMonth.Balance;
                        else
                            totalAssets += balanceAtMonth.Balance;
                    }
                }
            }

            var netWorth = totalAssets - totalLiabilities;
            var change = previousNetWorth.HasValue ? netWorth - previousNetWorth.Value : (decimal?)null;
            var percentChange = previousNetWorth.HasValue && previousNetWorth.Value != 0
                ? (change / Math.Abs(previousNetWorth.Value)) * 100
                : (decimal?)null;

            sb.AppendLine($"{month:yyyy-MM},{totalAssets:F2},{totalLiabilities:F2},{netWorth:F2},{change?.ToString("F2") ?? ""},{percentChange?.ToString("F2") ?? ""}");

            previousNetWorth = netWorth;
        }

        return sb.ToString();
    }
}
