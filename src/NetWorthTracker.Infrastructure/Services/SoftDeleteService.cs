using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class SoftDeleteService : ISoftDeleteService
{
    private readonly ISession _session;
    private readonly ILogger<SoftDeleteService> _logger;

    public SoftDeleteService(ISession session, ILogger<SoftDeleteService> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task SoftDeleteAsync<T>(Guid id) where T : BaseEntity
    {
        var entity = await _session.GetAsync<T>(id);
        if (entity == null)
        {
            _logger.LogWarning("Attempted to soft delete non-existent entity {EntityType} with id {Id}", typeof(T).Name, id);
            return;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _session.UpdateAsync(entity);
        await _session.FlushAsync();

        _logger.LogInformation("Soft deleted {EntityType} with id {Id}", typeof(T).Name, id);
    }

    public async Task RestoreAsync<T>(Guid id) where T : BaseEntity
    {
        var entity = await _session.GetAsync<T>(id);
        if (entity == null)
        {
            _logger.LogWarning("Attempted to restore non-existent entity {EntityType} with id {Id}", typeof(T).Name, id);
            return;
        }

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;

        await _session.UpdateAsync(entity);
        await _session.FlushAsync();

        _logger.LogInformation("Restored {EntityType} with id {Id}", typeof(T).Name, id);
    }

    public async Task<int> PurgeDeletedAsync(int gracePeriodDays = 30)
    {
        var totalPurged = 0;

        // Purge each entity type
        totalPurged += await PurgeDeletedAsync<Account>(gracePeriodDays);
        totalPurged += await PurgeDeletedAsync<BalanceHistory>(gracePeriodDays);
        totalPurged += await PurgeDeletedAsync<AlertConfiguration>(gracePeriodDays);
        totalPurged += await PurgeDeletedAsync<MonthlySnapshot>(gracePeriodDays);
        totalPurged += await PurgeDeletedAsync<ForecastAssumptions>(gracePeriodDays);

        return totalPurged;
    }

    public async Task<int> PurgeDeletedAsync<T>(int gracePeriodDays = 30) where T : BaseEntity
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-gracePeriodDays);

        var entitiesToPurge = await _session.Query<T>()
            .Where(e => e.IsDeleted && e.DeletedAt != null && e.DeletedAt < cutoffDate)
            .ToListAsync();

        if (entitiesToPurge.Count == 0)
        {
            return 0;
        }

        foreach (var entity in entitiesToPurge)
        {
            await _session.DeleteAsync(entity);
        }

        await _session.FlushAsync();

        _logger.LogInformation("Purged {Count} {EntityType} records older than {Days} days",
            entitiesToPurge.Count, typeof(T).Name, gracePeriodDays);

        return entitiesToPurge.Count;
    }
}
