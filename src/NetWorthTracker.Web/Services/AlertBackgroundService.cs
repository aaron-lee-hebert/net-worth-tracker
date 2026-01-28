using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Web.Services;

public class AlertBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public AlertBackgroundService(IServiceProvider serviceProvider, ILogger<AlertBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAlertsAndSnapshotsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alert background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessAlertsAndSnapshotsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Only process if email is configured
        if (!emailService.IsConfigured)
        {
            return;
        }

        _logger.LogDebug("Processing alerts and snapshots");

        // Process alerts
        await alertService.ProcessAlertsAsync();

        // Send pending snapshot emails
        await alertService.SendPendingSnapshotEmailsAsync();

        // Generate monthly snapshots on the 1st of each month
        if (DateTime.UtcNow.Day == 1 && DateTime.UtcNow.Hour < 2)
        {
            await GenerateMonthlySnapshotsAsync(alertService);
        }
    }

    private async Task GenerateMonthlySnapshotsAsync(IAlertService alertService)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepository = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAlertConfigurationRepository>();

        var configs = await configRepository.GetAllEnabledAsync();
        var previousMonth = DateTime.UtcNow.AddMonths(-1);

        foreach (var config in configs)
        {
            if (config.MonthlySnapshotEnabled)
            {
                try
                {
                    await alertService.GenerateMonthlySnapshotAsync(config.UserId, previousMonth);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating monthly snapshot for user {UserId}", config.UserId);
                }
            }
        }
    }
}
