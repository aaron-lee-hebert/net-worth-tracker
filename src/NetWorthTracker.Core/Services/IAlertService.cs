using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Services;

public interface IAlertService
{
    /// <summary>
    /// Gets or creates alert configuration for a user
    /// </summary>
    Task<AlertConfiguration> GetOrCreateConfigurationAsync(Guid userId);

    /// <summary>
    /// Updates alert configuration for a user
    /// </summary>
    Task UpdateConfigurationAsync(AlertConfiguration config);

    /// <summary>
    /// Generates a monthly snapshot for a user
    /// </summary>
    Task<MonthlySnapshot?> GenerateMonthlySnapshotAsync(Guid userId, DateTime month);

    /// <summary>
    /// Checks and sends alerts for all users (called by background job)
    /// </summary>
    Task ProcessAlertsAsync();

    /// <summary>
    /// Sends pending monthly snapshot emails (called by background job)
    /// </summary>
    Task SendPendingSnapshotEmailsAsync();
}
