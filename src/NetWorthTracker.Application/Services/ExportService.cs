using System.Text;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Application.Services;

public class ExportService : IExportService
{
    private readonly IReportService _reportService;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IAuditService _auditService;

    public ExportService(
        IReportService reportService,
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAuditService auditService)
    {
        _reportService = reportService;
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _auditService = auditService;
    }

    public async Task<ExportResult> ExportQuarterlyReportCsvAsync(Guid userId)
    {
        var report = await _reportService.BuildQuarterlyReportAsync(userId);

        if (!report.Accounts.Any())
        {
            return ExportResult.NoData();
        }

        var sb = new StringBuilder();

        sb.Append("Account,Type");
        foreach (var quarter in report.Quarters)
        {
            sb.Append($",{quarter}");
        }
        sb.AppendLine();

        foreach (var account in report.Accounts)
        {
            sb.Append($"\"{EscapeCsv(account.Name)}\",\"{EscapeCsv(account.Type)}\"");
            foreach (var balance in account.Balances)
            {
                sb.Append($",{balance?.ToString("F2") ?? ""}");
            }
            sb.AppendLine();
        }

        sb.AppendLine();

        sb.Append("Net Worth,");
        foreach (var netWorth in report.Totals.NetWorth)
        {
            sb.Append($",{netWorth:F2}");
        }
        sb.AppendLine();

        sb.Append("% Change,");
        foreach (var change in report.Totals.PercentChange)
        {
            sb.Append($",{change?.ToString("F2") ?? ""}");
        }
        sb.AppendLine();

        var fileName = $"net-worth-quarterly-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        // Audit log - quarterly report export
        await _auditService.LogExportAsync(userId, "QuarterlyReport",
            $"Exported quarterly report with {report.Accounts.Count} accounts across {report.Quarters.Count} quarters");

        return ExportResult.Ok(sb.ToString(), fileName);
    }

    public async Task<ExportResult> ExportNetWorthHistoryCsvAsync(Guid userId)
    {
        var history = await _reportService.GetNetWorthHistoryAsync(userId);

        if (!history.HasData)
        {
            return ExportResult.NoData();
        }

        var sb = new StringBuilder();
        sb.AppendLine("Date,Total Assets,Total Liabilities,Net Worth,Change,% Change");

        foreach (var month in history.Months)
        {
            sb.AppendLine($"{month.Month:yyyy-MM},{month.TotalAssets:F2},{month.TotalLiabilities:F2},{month.NetWorth:F2},{month.Change?.ToString("F2") ?? ""},{month.PercentChange?.ToString("F2") ?? ""}");
        }

        var fileName = $"net-worth-history-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        // Audit log - net worth history export
        await _auditService.LogExportAsync(userId, "NetWorthHistory",
            $"Exported net worth history with {history.Months.Count} months of data");

        return ExportResult.Ok(sb.ToString(), fileName);
    }

    public async Task<ExportResult> ExportAccountsCsvAsync(Guid userId, AccountCategory? category = null)
    {
        var accounts = category.HasValue
            ? await _accountRepository.GetByUserIdAndCategoryAsync(userId, category.Value)
            : await _accountRepository.GetByUserIdAsync(userId);

        var accountList = accounts.ToList();
        if (!accountList.Any())
        {
            return ExportResult.NoData();
        }

        var csv = GenerateAccountsCsv(accountList);
        var categoryName = category.HasValue ? $"-{category.Value.ToString().ToLower()}" : "";
        var fileName = $"accounts{categoryName}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        // Audit log - accounts export
        var categoryDesc = category.HasValue ? $" ({category.Value})" : "";
        await _auditService.LogExportAsync(userId, "Accounts",
            $"Exported {accountList.Count} accounts{categoryDesc}");

        return ExportResult.Ok(csv, fileName);
    }

    public async Task<ExportResult> ExportAccountHistoryCsvAsync(Guid userId, Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account == null || account.UserId != userId)
        {
            return ExportResult.NoData("Account not found");
        }

        var history = await _balanceHistoryRepository.GetByAccountIdAsync(accountId);
        var historyList = history.ToList();

        if (!historyList.Any())
        {
            return ExportResult.NoData("No balance history found");
        }

        var csv = GenerateAccountHistoryCsv(account, historyList);
        var safeName = account.Name.Replace(" ", "-").ToLower();
        var fileName = $"account-history-{safeName}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        // Audit log - account history export
        await _auditService.LogExportAsync(userId, "AccountHistory",
            $"Exported history for account '{account.Name}' with {historyList.Count} records");

        return ExportResult.Ok(csv, fileName);
    }

    private static string GenerateAccountsCsv(List<Account> accounts)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Name,Type,Category,Institution,Account Number,Balance,Status");

        var orderedAccounts = accounts
            .OrderBy(a => a.AccountType.IsLiability())
            .ThenBy(a => a.AccountType.GetCategory())
            .ThenBy(a => a.Name);

        decimal totalAssets = 0;
        decimal totalLiabilities = 0;

        foreach (var account in orderedAccounts)
        {
            var maskedAccountNum = string.IsNullOrEmpty(account.AccountNumber)
                ? ""
                : "****" + account.AccountNumber[^Math.Min(4, account.AccountNumber.Length)..];

            sb.AppendLine($"\"{EscapeCsv(account.Name)}\",\"{account.AccountType.GetDisplayName()}\",\"{account.AccountType.GetCategory().GetDisplayName()}\",\"{EscapeCsv(account.Institution ?? "")}\",\"{maskedAccountNum}\",{account.CurrentBalance:F2},{(account.IsActive ? "Active" : "Inactive")}");

            if (account.IsActive)
            {
                if (account.AccountType.IsLiability())
                    totalLiabilities += account.CurrentBalance;
                else
                    totalAssets += account.CurrentBalance;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Total Assets,,,,,{totalAssets:F2},");
        sb.AppendLine($"Total Liabilities,,,,,{totalLiabilities:F2},");
        sb.AppendLine($"Net Worth,,,,,{totalAssets - totalLiabilities:F2},");

        return sb.ToString();
    }

    private static string GenerateAccountHistoryCsv(Account account, List<BalanceHistory> history)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Account: {account.Name}");
        sb.AppendLine($"Type: {account.AccountType.GetDisplayName()}");
        sb.AppendLine($"Institution: {account.Institution ?? "N/A"}");
        sb.AppendLine();

        sb.AppendLine("Date,Balance,Change,% Change,Notes");

        var orderedHistory = history.OrderBy(h => h.RecordedAt).ToList();
        decimal? previousBalance = null;

        foreach (var entry in orderedHistory)
        {
            var change = previousBalance.HasValue ? entry.Balance - previousBalance.Value : (decimal?)null;
            var percentChange = previousBalance.HasValue && previousBalance.Value != 0
                ? (change / Math.Abs(previousBalance.Value)) * 100
                : (decimal?)null;

            sb.AppendLine($"{entry.RecordedAt:yyyy-MM-dd},{entry.Balance:F2},{change?.ToString("F2") ?? ""},{percentChange?.ToString("F2") ?? ""},{EscapeCsv(entry.Notes ?? "")}");

            previousBalance = entry.Balance;
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}
