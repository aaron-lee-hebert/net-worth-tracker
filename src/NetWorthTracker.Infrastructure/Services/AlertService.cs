using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly IAlertConfigurationRepository _configRepository;
    private readonly IMonthlySnapshotRepository _snapshotRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AlertService> _logger;

    // Maximum alerts per day to prevent spam
    private const int MaxAlertsPerDay = 5;

    public AlertService(
        IAlertConfigurationRepository configRepository,
        IMonthlySnapshotRepository snapshotRepository,
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IEmailService emailService,
        ILogger<AlertService> logger)
    {
        _configRepository = configRepository;
        _snapshotRepository = snapshotRepository;
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AlertConfiguration> GetOrCreateConfigurationAsync(Guid userId)
    {
        var config = await _configRepository.GetByUserIdAsync(userId);
        if (config != null)
            return config;

        config = new AlertConfiguration
        {
            UserId = userId,
            AlertsEnabled = true,
            NetWorthChangeThreshold = 5m,
            CashRunwayMonths = 3,
            MonthlySnapshotEnabled = true
        };

        await _configRepository.CreateAsync(config);
        return config;
    }

    public async Task UpdateConfigurationAsync(AlertConfiguration config)
    {
        await _configRepository.UpdateAsync(config);
    }

    public async Task<MonthlySnapshot?> GenerateMonthlySnapshotAsync(Guid userId, DateTime month)
    {
        var startOfMonth = new DateTime(month.Year, month.Month, 1);

        // Check if snapshot already exists
        var existing = await _snapshotRepository.GetByUserIdAndMonthAsync(userId, startOfMonth);
        if (existing != null)
            return existing;

        // Get current financial data
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        if (!accounts.Any())
            return null;

        var totalAssets = accounts
            .Where(a => a.AccountType.IsAsset())
            .Sum(a => a.CurrentBalance);

        var totalLiabilities = accounts
            .Where(a => a.AccountType.IsLiability())
            .Sum(a => a.CurrentBalance);

        var netWorth = totalAssets - totalLiabilities;

        // Get previous month's snapshot for comparison
        var previousMonth = startOfMonth.AddMonths(-1);
        var previousSnapshot = await _snapshotRepository.GetByUserIdAndMonthAsync(userId, previousMonth);

        var netWorthDelta = previousSnapshot != null ? netWorth - previousSnapshot.NetWorth : 0;
        var netWorthDeltaPercent = previousSnapshot != null && previousSnapshot.NetWorth != 0
            ? (netWorthDelta / Math.Abs(previousSnapshot.NetWorth)) * 100
            : 0;

        // Find biggest contributor to change
        var (biggestName, biggestDelta, biggestPositive) = await FindBiggestContributorAsync(userId, startOfMonth);

        // Generate interpretation
        var interpretation = GenerateInterpretation(netWorthDelta, netWorthDeltaPercent, biggestName, biggestDelta);

        var snapshot = new MonthlySnapshot
        {
            UserId = userId,
            Month = startOfMonth,
            NetWorth = netWorth,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorthDelta = netWorthDelta,
            NetWorthDeltaPercent = netWorthDeltaPercent,
            BiggestContributorName = biggestName,
            BiggestContributorDelta = biggestDelta,
            BiggestContributorPositive = biggestPositive,
            Interpretation = interpretation,
            EmailSent = false
        };

        await _snapshotRepository.CreateAsync(snapshot);
        _logger.LogInformation("Generated monthly snapshot for user {UserId} for {Month}", userId, startOfMonth.ToString("yyyy-MM"));

        return snapshot;
    }

    public async Task ProcessAlertsAsync()
    {
        if (!_emailService.IsConfigured)
        {
            _logger.LogDebug("Email not configured, skipping alert processing");
            return;
        }

        var configs = await _configRepository.GetAllEnabledAsync();
        var alertsSentToday = 0;

        foreach (var config in configs)
        {
            if (alertsSentToday >= MaxAlertsPerDay)
            {
                _logger.LogWarning("Maximum alerts per day ({Max}) reached, stopping alert processing", MaxAlertsPerDay);
                break;
            }

            try
            {
                var alertSent = await CheckAndSendAlertsForUserAsync(config);
                if (alertSent)
                    alertsSentToday++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alerts for user {UserId}", config.UserId);
            }
        }
    }

    public async Task SendPendingSnapshotEmailsAsync()
    {
        if (!_emailService.IsConfigured)
        {
            _logger.LogDebug("Email not configured, skipping snapshot emails");
            return;
        }

        var pendingSnapshots = await _snapshotRepository.GetUnsentSnapshotsAsync();

        foreach (var snapshot in pendingSnapshots)
        {
            try
            {
                // Check if user has snapshots enabled
                var config = await _configRepository.GetByUserIdAsync(snapshot.UserId);
                if (config == null || !config.MonthlySnapshotEnabled)
                {
                    // Mark as sent to not try again
                    snapshot.EmailSent = true;
                    await _snapshotRepository.UpdateAsync(snapshot);
                    continue;
                }

                await SendSnapshotEmailAsync(snapshot);

                snapshot.EmailSent = true;
                snapshot.EmailSentAt = DateTime.UtcNow;
                await _snapshotRepository.UpdateAsync(snapshot);

                _logger.LogInformation("Sent monthly snapshot email to user {UserId} for {Month}",
                    snapshot.UserId, snapshot.Month.ToString("yyyy-MM"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending snapshot email for user {UserId}", snapshot.UserId);
            }
        }
    }

    private async Task<bool> CheckAndSendAlertsForUserAsync(AlertConfiguration config)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(config.UserId);
        if (!accounts.Any())
            return false;

        var totalAssets = accounts
            .Where(a => a.AccountType.IsAsset())
            .Sum(a => a.CurrentBalance);

        var totalLiabilities = accounts
            .Where(a => a.AccountType.IsLiability())
            .Sum(a => a.CurrentBalance);

        var netWorth = totalAssets - totalLiabilities;

        // Check net worth change alert
        if (config.NetWorthChangeThreshold > 0 && config.LastAlertedNetWorth.HasValue)
        {
            var change = netWorth - config.LastAlertedNetWorth.Value;
            var changePercent = Math.Abs(change / config.LastAlertedNetWorth.Value) * 100;

            if (changePercent >= config.NetWorthChangeThreshold)
            {
                // Don't send if we already sent one today
                if (config.LastNetWorthAlertSentAt?.Date != DateTime.UtcNow.Date)
                {
                    // Send alert (would need user email - simplified for now)
                    config.LastNetWorthAlertSentAt = DateTime.UtcNow;
                    config.LastAlertedNetWorth = netWorth;
                    await _configRepository.UpdateAsync(config);
                    _logger.LogInformation("Net worth change alert triggered for user {UserId}: {Change:P2}",
                        config.UserId, changePercent / 100);
                    return true;
                }
            }
        }
        else if (config.LastAlertedNetWorth == null)
        {
            // Initialize baseline
            config.LastAlertedNetWorth = netWorth;
            await _configRepository.UpdateAsync(config);
        }

        // Check cash runway alert
        if (config.CashRunwayMonths > 0)
        {
            var cashAccounts = accounts
                .Where(a => a.AccountType == AccountType.Checking || a.AccountType == AccountType.Savings)
                .Sum(a => a.CurrentBalance);

            // Estimate monthly expenses from liability changes (simplified)
            var monthlyExpenses = totalLiabilities > 0 ? totalLiabilities / 12 : 1000; // Default estimate
            var runwayMonths = monthlyExpenses > 0 ? (int)(cashAccounts / monthlyExpenses) : int.MaxValue;

            if (runwayMonths <= config.CashRunwayMonths)
            {
                if (config.LastCashRunwayAlertSentAt?.Date != DateTime.UtcNow.Date)
                {
                    config.LastCashRunwayAlertSentAt = DateTime.UtcNow;
                    await _configRepository.UpdateAsync(config);
                    _logger.LogInformation("Cash runway alert triggered for user {UserId}: {Months} months",
                        config.UserId, runwayMonths);
                    return true;
                }
            }
        }

        return false;
    }

    private async Task<(string? name, decimal delta, bool positive)> FindBiggestContributorAsync(Guid userId, DateTime month)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        var endOfPreviousMonth = startOfMonth.AddDays(-1);

        string? biggestName = null;
        decimal biggestDelta = 0;

        foreach (var account in accounts)
        {
            // Get balance at end of current month vs end of previous month
            var currentBalance = account.CurrentBalance;

            // Try to get historical balance (simplified - using current balance)
            var previousHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
            var previousBalance = previousHistory
                .Where(h => h.RecordedAt <= endOfPreviousMonth)
                .OrderByDescending(h => h.RecordedAt)
                .FirstOrDefault()?.Balance ?? currentBalance;

            var delta = currentBalance - previousBalance;

            if (Math.Abs(delta) > Math.Abs(biggestDelta))
            {
                biggestDelta = delta;
                biggestName = account.Name;
            }
        }

        return (biggestName, Math.Abs(biggestDelta), biggestDelta >= 0);
    }

    private string GenerateInterpretation(decimal netWorthDelta, decimal netWorthDeltaPercent, string? biggestContributor, decimal biggestDelta)
    {
        if (netWorthDelta == 0)
            return "Your net worth remained stable this month.";

        var direction = netWorthDelta > 0 ? "increased" : "decreased";
        var amount = Math.Abs(netWorthDelta);
        var percent = Math.Abs(netWorthDeltaPercent);

        var interpretation = $"Your net worth {direction} by {amount:C0} ({percent:F1}%) this month.";

        if (!string.IsNullOrEmpty(biggestContributor) && biggestDelta > 0)
        {
            interpretation += $" {biggestContributor} was the biggest contributor.";
        }

        return interpretation;
    }

    private async Task SendSnapshotEmailAsync(MonthlySnapshot snapshot)
    {
        // Get user email (would need to inject user manager or pass email)
        // For now, this is a placeholder - the actual implementation would
        // need the user's email address
        var subject = $"Your Monthly Financial Snapshot - {snapshot.Month:MMMM yyyy}";
        var body = GenerateSnapshotEmailBody(snapshot);

        // Note: In a real implementation, we'd need to get the user's email
        // This would require passing it through or looking it up
        _logger.LogInformation("Would send snapshot email for {Month}: {Interpretation}",
            snapshot.Month.ToString("yyyy-MM"), snapshot.Interpretation);
    }

    private string GenerateSnapshotEmailBody(MonthlySnapshot snapshot)
    {
        var changeDirection = snapshot.NetWorthDelta >= 0 ? "up" : "down";
        var changeColor = snapshot.NetWorthDelta >= 0 ? "#28a745" : "#dc3545";
        var arrow = snapshot.NetWorthDelta >= 0 ? "&#8593;" : "&#8595;";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Georgia, 'Times New Roman', serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #1a3c34; color: #d4a843; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background-color: #faf8f5; }}
        .stat {{ text-align: center; margin: 20px 0; }}
        .stat-value {{ font-size: 28px; font-weight: bold; color: #1a3c34; }}
        .stat-label {{ font-size: 14px; color: #666; }}
        .change {{ color: {changeColor}; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Monthly Financial Snapshot</h1>
            <p>{snapshot.Month:MMMM yyyy}</p>
        </div>
        <div class='content'>
            <div class='stat'>
                <div class='stat-value'>{snapshot.NetWorth:C0}</div>
                <div class='stat-label'>Net Worth</div>
            </div>
            <div class='stat'>
                <div class='stat-value change'>{arrow} {Math.Abs(snapshot.NetWorthDelta):C0} ({Math.Abs(snapshot.NetWorthDeltaPercent):F1}%)</div>
                <div class='stat-label'>Change from Last Month</div>
            </div>
            <hr />
            <p><strong>Assets:</strong> {snapshot.TotalAssets:C0}</p>
            <p><strong>Liabilities:</strong> {snapshot.TotalLiabilities:C0}</p>
            {(string.IsNullOrEmpty(snapshot.BiggestContributorName) ? "" : $"<p><strong>Biggest Contributor:</strong> {snapshot.BiggestContributorName} ({(snapshot.BiggestContributorPositive ? "+" : "-")}{snapshot.BiggestContributorDelta:C0})</p>")}
            <hr />
            <p><em>{snapshot.Interpretation}</em></p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Net Worth Tracker.</p>
            <p>You can manage your notification preferences in Settings.</p>
        </div>
    </div>
</body>
</html>";
    }
}
