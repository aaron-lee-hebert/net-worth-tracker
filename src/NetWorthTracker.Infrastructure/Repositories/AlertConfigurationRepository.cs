using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class AlertConfigurationRepository : IAlertConfigurationRepository
{
    private readonly ISession _session;

    public AlertConfigurationRepository(ISession session)
    {
        _session = session;
    }

    public async Task<AlertConfiguration?> GetByUserIdAsync(Guid userId)
    {
        return await _session.Query<AlertConfiguration>()
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<AlertConfiguration> CreateAsync(AlertConfiguration config)
    {
        await _session.SaveAsync(config);
        await _session.FlushAsync();
        return config;
    }

    public async Task UpdateAsync(AlertConfiguration config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(config);
        await _session.FlushAsync();
    }

    public async Task<IEnumerable<AlertConfiguration>> GetAllEnabledAsync()
    {
        return await _session.Query<AlertConfiguration>()
            .Where(a => a.AlertsEnabled)
            .ToListAsync();
    }
}
