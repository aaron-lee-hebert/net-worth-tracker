using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Services;

public class ReportService : IReportService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;

    public ReportService(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
    }

    public async Task<QuarterlyReportViewModel> BuildQuarterlyReportAsync(Guid userId)
    {
        var accounts = (await _accountRepository.GetActiveAccountsByUserIdAsync(userId)).ToList();

        if (!accounts.Any())
        {
            return new QuarterlyReportViewModel();
        }

        var earliestDate = new DateTime(2000, 1, 1);
        var allHistory = (await _balanceHistoryRepository.GetByUserIdAndDateRangeAsync(
            userId, earliestDate, DateTime.UtcNow)).ToList();

        if (!allHistory.Any())
        {
            return new QuarterlyReportViewModel();
        }

        var firstRecord = allHistory.Min(h => h.RecordedAt);
        var quarters = GenerateQuarters(firstRecord, DateTime.UtcNow);

        var viewModel = new QuarterlyReportViewModel
        {
            Quarters = quarters.Select(q => FormatQuarter(q)).ToList()
        };

        var historyByAccount = allHistory
            .GroupBy(h => h.AccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.RecordedAt).ToList());

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

    public async Task<NetWorthHistoryData> GetNetWorthHistoryAsync(Guid userId)
    {
        var accounts = (await _accountRepository.GetActiveAccountsByUserIdAsync(userId)).ToList();

        if (!accounts.Any())
        {
            return new NetWorthHistoryData();
        }

        var earliestDate = new DateTime(2000, 1, 1);
        var allHistory = (await _balanceHistoryRepository.GetByUserIdAndDateRangeAsync(
            userId, earliestDate, DateTime.UtcNow)).ToList();

        if (!allHistory.Any())
        {
            return new NetWorthHistoryData();
        }

        var historyByAccount = allHistory
            .GroupBy(h => h.AccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.RecordedAt).ToList());

        var firstDate = allHistory.Min(h => h.RecordedAt);
        var months = new List<DateTime>();
        var currentMonth = new DateTime(firstDate.Year, firstDate.Month, 1);

        while (currentMonth <= DateTime.UtcNow)
        {
            months.Add(currentMonth);
            currentMonth = currentMonth.AddMonths(1);
        }

        var result = new List<MonthlyNetWorth>();
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

            result.Add(new MonthlyNetWorth
            {
                Month = month,
                TotalAssets = totalAssets,
                TotalLiabilities = totalLiabilities,
                NetWorth = netWorth,
                Change = change,
                PercentChange = percentChange
            });

            previousNetWorth = netWorth;
        }

        return new NetWorthHistoryData { Months = result };
    }

    private static List<DateTime> GenerateQuarters(DateTime start, DateTime end)
    {
        var quarters = new List<DateTime>();

        var currentQuarter = GetQuarterEnd(start);

        while (currentQuarter <= end)
        {
            quarters.Add(currentQuarter);
            currentQuarter = currentQuarter.AddMonths(3);
            currentQuarter = GetQuarterEnd(currentQuarter);
        }

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
}
