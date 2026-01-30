using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Services;

public class ForecastService : IForecastService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IForecastAssumptionsRepository _assumptionsRepository;

    private const int DefaultForecastMonths = 60;
    private const int MinHistoricalMonths = 3;

    private static class DefaultAssumptions
    {
        public const decimal InvestmentAnnualGrowth = 0.07m;
        public const decimal RealEstateAnnualGrowth = 0.02m;
        public const decimal VehicleAnnualDepreciation = 0.15m;
        public const decimal VehicleFloorPercent = 0.10m;
        public const decimal SavingsAnnualGrowth = 0.005m;
        public const decimal BusinessAnnualGrowth = 0.03m;
        public const decimal DebtMinimumMonthlyPayment = 0.01m;
        public const decimal LongTermDecayPerYear = 0.95m;
    }

    private ForecastAssumptions? _userAssumptions;

    public ForecastService(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IForecastAssumptionsRepository assumptionsRepository)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _assumptionsRepository = assumptionsRepository;
    }

    public async Task<ForecastViewModel> GetForecastDataAsync(Guid userId, int forecastMonths = DefaultForecastMonths)
    {
        _userAssumptions = await _assumptionsRepository.GetByUserIdAsync(userId);

        var accounts = (await _accountRepository.GetActiveAccountsByUserIdAsync(userId)).ToList();

        if (!accounts.Any())
        {
            return new ForecastViewModel();
        }

        var allHistory = (await _balanceHistoryRepository.GetByUserIdAndDateRangeAsync(
            userId, DateTime.UtcNow.AddYears(-5), DateTime.UtcNow)).ToList();

        if (!allHistory.Any())
        {
            return new ForecastViewModel();
        }

        var forecastQuarters = forecastMonths / 3;
        var viewModel = BuildForecast(accounts, allHistory, forecastQuarters);
        viewModel.ForecastMonths = forecastMonths;
        return viewModel;
    }

    public async Task<ForecastAssumptionsViewModel> GetAssumptionsAsync(Guid userId)
    {
        var assumptions = await _assumptionsRepository.GetByUserIdAsync(userId);

        return new ForecastAssumptionsViewModel
        {
            InvestmentGrowthRate = (assumptions?.GetInvestmentRate() ?? ForecastAssumptions.Defaults.InvestmentGrowthRate) * 100,
            RealEstateGrowthRate = (assumptions?.GetRealEstateRate() ?? ForecastAssumptions.Defaults.RealEstateGrowthRate) * 100,
            BankingGrowthRate = (assumptions?.GetBankingRate() ?? ForecastAssumptions.Defaults.BankingGrowthRate) * 100,
            BusinessGrowthRate = (assumptions?.GetBusinessRate() ?? ForecastAssumptions.Defaults.BusinessGrowthRate) * 100,
            VehicleDepreciationRate = (assumptions?.GetVehicleRate() ?? ForecastAssumptions.Defaults.VehicleDepreciationRate) * 100,
            HasCustomOverrides = assumptions?.HasCustomOverrides() ?? false
        };
    }

    public async Task SaveAssumptionsAsync(Guid userId, ForecastAssumptionsViewModel model)
    {
        var assumptions = await _assumptionsRepository.GetOrCreateAsync(userId);

        assumptions.InvestmentGrowthRate = model.InvestmentGrowthRate / 100;
        assumptions.RealEstateGrowthRate = model.RealEstateGrowthRate / 100;
        assumptions.BankingGrowthRate = model.BankingGrowthRate / 100;
        assumptions.BusinessGrowthRate = model.BusinessGrowthRate / 100;
        assumptions.VehicleDepreciationRate = model.VehicleDepreciationRate / 100;

        await _assumptionsRepository.UpdateAsync(assumptions);
    }

    public async Task ResetAssumptionsAsync(Guid userId)
    {
        var assumptions = await _assumptionsRepository.GetByUserIdAsync(userId);

        if (assumptions != null)
        {
            assumptions.ResetToDefaults();
            await _assumptionsRepository.UpdateAsync(assumptions);
        }
    }

    private ForecastViewModel BuildForecast(List<Account> accounts, List<BalanceHistory> allHistory, int forecastQuarters)
    {
        var now = DateTime.UtcNow;
        var historyByAccount = allHistory
            .GroupBy(h => h.AccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.RecordedAt).ToList());

        var earliestDate = allHistory.Min(h => h.RecordedAt);
        var historicalQuarters = (int)Math.Ceiling((now - earliestDate).TotalDays / 91.25);
        historicalQuarters = Math.Max(historicalQuarters, 4);

        var labels = new List<string>();
        var startDate = GetQuarterStart(now.AddMonths(-historicalQuarters * 3));
        for (int i = 0; i < historicalQuarters + forecastQuarters; i++)
        {
            var date = startDate.AddMonths(i * 3);
            var quarter = (date.Month - 1) / 3 + 1;
            labels.Add($"Q{quarter} {date.Year}");
        }

        var viewModel = new ForecastViewModel
        {
            Labels = labels,
            HistoricalMonths = historicalQuarters,
            ForecastMonths = forecastQuarters * 3
        };

        foreach (var account in accounts.OrderBy(a => a.AccountType.IsLiability()).ThenBy(a => a.Name))
        {
            var accountHistory = historyByAccount.GetValueOrDefault(account.Id) ?? new List<BalanceHistory>();
            var forecast = BuildAccountForecast(account, accountHistory, historicalQuarters, forecastQuarters, startDate);
            viewModel.Accounts.Add(forecast);
        }

        for (int i = 0; i < historicalQuarters + forecastQuarters; i++)
        {
            if (i < historicalQuarters)
            {
                decimal assets = 0, liabilities = 0;
                foreach (var account in viewModel.Accounts)
                {
                    var balance = account.HistoricalData.Count > i ? account.HistoricalData[i] : 0;
                    if (account.IsLiability)
                        liabilities += balance;
                    else
                        assets += balance;
                }
                viewModel.HistoricalNetWorth.Add(assets - liabilities);
                viewModel.ForecastedNetWorth.Add(null);
            }
            else
            {
                decimal assets = 0, liabilities = 0;
                var forecastIndex = i - historicalQuarters;
                foreach (var account in viewModel.Accounts)
                {
                    var balance = account.ForecastData.Count > forecastIndex
                        ? account.ForecastData[forecastIndex] ?? 0
                        : 0;
                    if (account.IsLiability)
                        liabilities += balance;
                    else
                        assets += balance;
                }
                viewModel.ForecastedNetWorth.Add(assets - liabilities);
            }
        }

        var currentAssets = viewModel.Accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var currentLiabilities = viewModel.Accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);
        var projectedAssets = viewModel.Accounts.Where(a => !a.IsLiability).Sum(a => a.ProjectedBalance);
        var projectedLiabilities = viewModel.Accounts.Where(a => a.IsLiability).Sum(a => a.ProjectedBalance);

        viewModel.Summary = new ForecastSummary
        {
            CurrentNetWorth = currentAssets - currentLiabilities,
            ProjectedNetWorth = projectedAssets - projectedLiabilities,
            ProjectedChange = (projectedAssets - projectedLiabilities) - (currentAssets - currentLiabilities),
            ProjectedChangePercent = currentAssets - currentLiabilities != 0
                ? ((projectedAssets - projectedLiabilities) - (currentAssets - currentLiabilities)) / Math.Abs(currentAssets - currentLiabilities) * 100
                : 0,
            CurrentAssets = currentAssets,
            ProjectedAssets = projectedAssets,
            CurrentLiabilities = currentLiabilities,
            ProjectedLiabilities = projectedLiabilities,
            ProjectionDate = now.AddMonths(forecastQuarters * 3)
        };

        return viewModel;
    }

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarter = (date.Month - 1) / 3;
        return new DateTime(date.Year, quarter * 3 + 1, 1);
    }

    private AccountForecast BuildAccountForecast(
        Account account,
        List<BalanceHistory> history,
        int historicalQuarters,
        int forecastQuarters,
        DateTime startDate)
    {
        var forecast = new AccountForecast
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.AccountType.GetDisplayName(),
            Category = account.AccountType.GetCategory().GetDisplayName(),
            IsLiability = account.AccountType.IsLiability(),
            CurrentBalance = account.CurrentBalance
        };

        var historyByQuarter = history
            .GroupBy(h => GetQuarterStart(h.RecordedAt))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.RecordedAt).First().Balance);

        decimal lastKnownBalance = 0;
        var quarterlyBalances = new List<decimal>();

        for (int i = 0; i < historicalQuarters; i++)
        {
            var quarterStart = GetQuarterStart(startDate.AddMonths(i * 3));
            if (historyByQuarter.TryGetValue(quarterStart, out var balance))
            {
                lastKnownBalance = balance;
            }
            quarterlyBalances.Add(lastKnownBalance);
            forecast.HistoricalData.Add(lastKnownBalance);
        }

        var (quarterlyChange, annualGrowthRate, trendDirection) = CalculateTrend(quarterlyBalances, account.AccountType);
        forecast.MonthlyChange = quarterlyChange / 3;
        forecast.AnnualGrowthRate = annualGrowthRate;
        forecast.TrendDirection = trendDirection;

        var currentBalance = account.CurrentBalance;
        var projectedBalances = GenerateForecast(account.AccountType, currentBalance, quarterlyChange, forecastQuarters);

        forecast.ForecastData = projectedBalances;
        forecast.ProjectedBalance = projectedBalances.LastOrDefault() ?? currentBalance;

        if (account.AccountType.IsLiability() && quarterlyChange < 0 && currentBalance > 0)
        {
            var quartersToPayoff = (int)Math.Ceiling(currentBalance / Math.Abs(quarterlyChange));
            if (quartersToPayoff > 0 && quartersToPayoff < 120)
            {
                forecast.PayoffDate = DateTime.UtcNow.AddMonths(quartersToPayoff * 3);
            }
        }

        return forecast;
    }

    private (decimal quarterlyChange, decimal annualGrowthRate, string direction) CalculateTrend(
        List<decimal> quarterlyBalances,
        AccountType accountType)
    {
        if (quarterlyBalances.Count < 2)
        {
            return (0, 0, "stable");
        }

        var recentData = quarterlyBalances.TakeLast(Math.Min(8, quarterlyBalances.Count)).ToList();

        if (recentData.Count < 2)
        {
            return (0, 0, "stable");
        }

        var firstBalance = recentData.First();
        var lastBalance = recentData.Last();
        var totalChange = lastBalance - firstBalance;
        var quarterlyChange = totalChange / (recentData.Count - 1);

        var annualGrowthRate = firstBalance != 0
            ? (totalChange / Math.Abs(firstBalance)) * (4m / (recentData.Count - 1)) * 100
            : 0;

        var direction = "stable";
        var threshold = Math.Abs(firstBalance) * 0.02m;
        if (quarterlyChange > threshold)
            direction = "up";
        else if (quarterlyChange < -threshold)
            direction = "down";

        return (Math.Round(quarterlyChange, 2), Math.Round(annualGrowthRate, 2), direction);
    }

    private List<decimal?> GenerateForecast(AccountType accountType, decimal currentBalance, decimal quarterlyChange, int quarters)
    {
        var forecast = new List<decimal?>();

        for (int i = 1; i <= quarters; i++)
        {
            var projectedBalance = ProjectBalance(accountType, currentBalance, quarterlyChange, i);
            forecast.Add(Math.Round(projectedBalance, 2));
        }

        return forecast;
    }

    private decimal ProjectBalance(AccountType accountType, decimal currentBalance, decimal quarterlyChange, int quartersAhead)
    {
        var category = accountType.GetCategory();
        var yearsAhead = (double)quartersAhead / 4.0;

        var investmentRate = _userAssumptions?.GetInvestmentRate() ?? DefaultAssumptions.InvestmentAnnualGrowth;
        var realEstateRate = _userAssumptions?.GetRealEstateRate() ?? DefaultAssumptions.RealEstateAnnualGrowth;
        var bankingRate = _userAssumptions?.GetBankingRate() ?? DefaultAssumptions.SavingsAnnualGrowth;
        var businessRate = _userAssumptions?.GetBusinessRate() ?? DefaultAssumptions.BusinessAnnualGrowth;
        var vehicleRate = _userAssumptions?.GetVehicleRate() ?? DefaultAssumptions.VehicleAnnualDepreciation;

        switch (category)
        {
            case AccountCategory.SecuredDebt:
            case AccountCategory.UnsecuredDebt:
            case AccountCategory.OtherLiabilities:
                decimal debtBalance = currentBalance;
                decimal quarterlyPaydown = quarterlyChange < 0 ? quarterlyChange : -currentBalance * 0.03m;
                for (int q = 0; q < quartersAhead; q++)
                {
                    if (debtBalance <= 0) break;
                    var payment = quarterlyChange < 0 ? quarterlyChange : -debtBalance * 0.03m;
                    debtBalance += payment;
                }
                return Math.Max(0, debtBalance);

            case AccountCategory.Investment:
                var investmentFactor = Math.Pow(1.0 + (double)investmentRate, yearsAhead);
                return currentBalance * (decimal)investmentFactor;

            case AccountCategory.RealEstate:
                var realEstateFactor = Math.Pow(1.0 + (double)realEstateRate, yearsAhead);
                return currentBalance * (decimal)realEstateFactor;

            case AccountCategory.VehiclesAndProperty:
                var vehicleFactor = Math.Pow(1.0 - (double)vehicleRate, yearsAhead);
                var vehicleBalance = currentBalance * (decimal)vehicleFactor;
                return Math.Max(currentBalance * DefaultAssumptions.VehicleFloorPercent, vehicleBalance);

            case AccountCategory.Banking:
                var bankingFactor = Math.Pow(1.0 + (double)bankingRate, yearsAhead);
                return Math.Max(0, currentBalance * (decimal)bankingFactor);

            case AccountCategory.Business:
                var businessFactor = Math.Pow(1.0 + (double)businessRate, yearsAhead);
                return Math.Max(0, currentBalance * (decimal)businessFactor);

            default:
                return currentBalance;
        }
    }
}
