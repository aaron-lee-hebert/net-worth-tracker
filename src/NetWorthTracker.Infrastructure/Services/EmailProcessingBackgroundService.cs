using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Constants;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class EmailProcessingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailProcessingBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1);

    public EmailProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailProcessingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email processing background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEmailQueueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email processing background service");
            }

            try
            {
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("Email processing background service stopped");
    }

    private async Task ProcessEmailQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
        var processedJobRepository = scope.ServiceProvider.GetRequiredService<IProcessedJobRepository>();

        // Generate idempotency key for this processing run
        var jobKey = $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm}";

        // Check if we already processed this minute
        if (await processedJobRepository.ExistsAsync(JobTypes.EmailQueue, jobKey))
        {
            return;
        }

        var stats = await emailQueueService.GetQueueStatsAsync();
        if (stats.Pending == 0)
        {
            return;
        }

        _logger.LogDebug("Processing email queue: {Pending} pending, {Failed} failed",
            stats.Pending, stats.Failed);

        var processedCount = 0;
        var errorCount = 0;

        try
        {
            // Process in batches to avoid holding connections too long
            const int batchSize = 10;
            var processed = 0;

            do
            {
                processed = await emailQueueService.ProcessQueueAsync(batchSize, stoppingToken);
                processedCount += processed;

                // Small delay between batches
                if (processed == batchSize && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
            while (processed == batchSize && !stoppingToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email queue batch");
            errorCount++;
        }

        // Record job completion
        var processedJob = new ProcessedJob
        {
            JobType = JobTypes.EmailQueue,
            JobKey = jobKey,
            ProcessedAt = DateTime.UtcNow,
            Success = errorCount == 0,
            ErrorMessage = errorCount > 0 ? "Batch processing error occurred" : null,
            Metadata = $"{{\"processed\":{processedCount},\"errors\":{errorCount}}}"
        };
        await processedJobRepository.AddAsync(processedJob);

        if (processedCount > 0)
        {
            _logger.LogInformation("Email queue processed: {Count} emails sent", processedCount);
        }
    }
}
