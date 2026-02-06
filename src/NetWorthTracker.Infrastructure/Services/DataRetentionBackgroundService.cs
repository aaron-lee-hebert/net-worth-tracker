using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Constants;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class DataRetentionSettings
{
    public int GracePeriodDays { get; set; } = 30;
    public int CleanupHour { get; set; } = 3;
    public bool Enabled { get; set; } = true;
}

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionBackgroundService> _logger;
    private readonly DataRetentionSettings _settings;

    public DataRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataRetentionBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = configuration.GetSection("DataRetention").Get<DataRetentionSettings>() ?? new DataRetentionSettings();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Data retention background service is disabled");
            return;
        }

        _logger.LogInformation("Data retention background service started (cleanup hour: {Hour}, grace period: {Days} days)",
            _settings.CleanupHour, _settings.GracePeriodDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate delay until next cleanup time
                var nextRunTime = GetNextRunTime();
                var delay = nextRunTime - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug("Data retention cleanup scheduled for {NextRun} (in {Delay})",
                        nextRunTime, delay);

                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data retention background service");

                // Wait an hour before retrying after an error
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Data retention background service stopped");
    }

    private DateTime GetNextRunTime()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var runTime = today.AddHours(_settings.CleanupHour);

        // If we've already passed the cleanup time today, schedule for tomorrow
        if (now >= runTime)
        {
            runTime = runTime.AddDays(1);
        }

        return runTime;
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var softDeleteService = scope.ServiceProvider.GetRequiredService<ISoftDeleteService>();
        var processedJobRepository = scope.ServiceProvider.GetRequiredService<IProcessedJobRepository>();

        // Generate idempotency key for this cleanup run
        var jobKey = $"{DateTime.UtcNow:yyyy-MM-dd}";

        // Check if we already ran cleanup today
        if (await processedJobRepository.ExistsAsync(JobTypes.DataRetention, jobKey))
        {
            _logger.LogDebug("Data retention cleanup already ran today");
            return;
        }

        _logger.LogInformation("Starting data retention cleanup (grace period: {Days} days)", _settings.GracePeriodDays);

        var totalPurged = 0;
        string? errorMessage = null;

        try
        {
            totalPurged = await softDeleteService.PurgeDeletedAsync(_settings.GracePeriodDays);

            if (totalPurged > 0)
            {
                _logger.LogInformation("Data retention cleanup completed: purged {Count} records", totalPurged);
            }
            else
            {
                _logger.LogDebug("Data retention cleanup completed: no records to purge");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data retention cleanup");
            errorMessage = ex.Message;
        }

        // Record job completion
        var processedJob = new ProcessedJob
        {
            JobType = JobTypes.DataRetention,
            JobKey = jobKey,
            ProcessedAt = DateTime.UtcNow,
            Success = errorMessage == null,
            ErrorMessage = errorMessage,
            Metadata = $"{{\"purged\":{totalPurged},\"gracePeriodDays\":{_settings.GracePeriodDays}}}"
        };
        await processedJobRepository.AddAsync(processedJob);
    }
}
