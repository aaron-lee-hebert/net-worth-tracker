using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class AlertConfigurationRepository : RepositoryBase<AlertConfiguration>, IAlertConfigurationRepository
{
    public AlertConfigurationRepository(ISession session) : base(session)
    {
    }

    public async Task<AlertConfiguration?> GetByUserIdAsync(Guid userId)
    {
        return await Session.Query<AlertConfiguration>()
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<IEnumerable<AlertConfiguration>> GetAllEnabledAsync()
    {
        return await Session.Query<AlertConfiguration>()
            .Where(a => a.AlertsEnabled)
            .ToListAsync();
    }
}
