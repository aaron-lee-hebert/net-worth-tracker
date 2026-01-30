using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Health;

/// <summary>
/// Health check that reports pending database migrations
/// </summary>
public class MigrationHealthCheck : IHealthCheck
{
    private readonly IMigrationRunner _migrationRunner;
    private readonly ILogger<MigrationHealthCheck> _logger;

    public MigrationHealthCheck(
        IMigrationRunner migrationRunner,
        ILogger<MigrationHealthCheck> logger)
    {
        _migrationRunner = migrationRunner;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = (await _migrationRunner.GetPendingMigrationsAsync()).ToList();
            var applied = (await _migrationRunner.GetAppliedMigrationsAsync()).ToList();

            var data = new Dictionary<string, object>
            {
                ["AppliedMigrations"] = applied.Count,
                ["PendingMigrations"] = pending.Count
            };

            if (pending.Any())
            {
                data["PendingVersions"] = pending.Select(m => m.Version).ToList();

                return HealthCheckResult.Degraded(
                    $"{pending.Count} pending migration(s) need to be applied",
                    data: data);
            }

            var lastApplied = applied.OrderByDescending(m => m.Version).FirstOrDefault();
            if (lastApplied != null)
            {
                data["LatestVersion"] = lastApplied.Version;
                data["LatestAppliedAt"] = lastApplied.AppliedAt?.ToString("O") ?? "Unknown";
            }

            return HealthCheckResult.Healthy(
                $"All {applied.Count} migration(s) applied",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check migration health");
            return HealthCheckResult.Unhealthy(
                "Failed to check migration status",
                ex);
        }
    }
}
