using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Constants;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Health;

public class BackgroundJobHealthCheck : IHealthCheck
{
    private readonly IProcessedJobRepository _processedJobRepository;
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<BackgroundJobHealthCheck> _logger;

    // Maximum allowed time since last successful job run
    private readonly TimeSpan _alertJobMaxAge = TimeSpan.FromHours(25);
    private readonly TimeSpan _emailQueueMaxAge = TimeSpan.FromMinutes(5);

    public BackgroundJobHealthCheck(
        IProcessedJobRepository processedJobRepository,
        IEmailQueueService emailQueueService,
        ILogger<BackgroundJobHealthCheck> logger)
    {
        _processedJobRepository = processedJobRepository;
        _emailQueueService = emailQueueService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var data = new Dictionary<string, object>();

        try
        {
            // Check alert processing job
            var lastAlertJob = await _processedJobRepository.GetLastSuccessfulAsync(JobTypes.AlertProcessing);
            if (lastAlertJob != null)
            {
                data["LastAlertProcessing"] = lastAlertJob.ProcessedAt.ToString("O");
                var age = DateTime.UtcNow - lastAlertJob.ProcessedAt;
                if (age > _alertJobMaxAge)
                {
                    issues.Add($"Alert processing job hasn't run successfully in {age.TotalHours:F1} hours");
                }
            }
            else
            {
                data["LastAlertProcessing"] = "Never";
            }

            // Check email queue processing
            var lastEmailJob = await _processedJobRepository.GetLastSuccessfulAsync(JobTypes.EmailQueue);
            if (lastEmailJob != null)
            {
                data["LastEmailQueueProcessing"] = lastEmailJob.ProcessedAt.ToString("O");
            }

            // Check email queue stats
            var queueStats = await _emailQueueService.GetQueueStatsAsync();
            data["EmailQueuePending"] = queueStats.Pending;
            data["EmailQueueFailed"] = queueStats.Failed;
            data["EmailQueueTotal"] = queueStats.Total;

            // Alert if too many failed emails
            if (queueStats.Failed > 10)
            {
                issues.Add($"Email queue has {queueStats.Failed} failed emails");
            }

            // Alert if pending emails are stacking up
            if (queueStats.Pending > 100)
            {
                issues.Add($"Email queue has {queueStats.Pending} pending emails");
            }

            // Check session cleanup job
            var lastSessionCleanup = await _processedJobRepository.GetLastSuccessfulAsync(JobTypes.SessionCleanup);
            if (lastSessionCleanup != null)
            {
                data["LastSessionCleanup"] = lastSessionCleanup.ProcessedAt.ToString("O");
            }
            else
            {
                data["LastSessionCleanup"] = "Never";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking background job health");
            return HealthCheckResult.Unhealthy(
                "Failed to check background job health",
                ex,
                data);
        }

        if (issues.Count > 0)
        {
            return HealthCheckResult.Degraded(
                string.Join("; ", issues),
                data: data);
        }

        return HealthCheckResult.Healthy("All background jobs are running normally", data);
    }
}
