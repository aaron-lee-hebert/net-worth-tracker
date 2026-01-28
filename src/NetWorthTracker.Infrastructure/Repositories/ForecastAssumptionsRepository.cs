using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class ForecastAssumptionsRepository : IForecastAssumptionsRepository
{
    private readonly NHibernate.ISession _session;

    public ForecastAssumptionsRepository(NHibernate.ISession session)
    {
        _session = session;
    }

    public async Task<ForecastAssumptions?> GetByUserIdAsync(Guid userId)
    {
        return await _session.Query<ForecastAssumptions>()
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<ForecastAssumptions> CreateAsync(ForecastAssumptions assumptions)
    {
        assumptions.CreatedAt = DateTime.UtcNow;
        assumptions.ModifiedAt = DateTime.UtcNow;
        await _session.SaveAsync(assumptions);
        await _session.FlushAsync();
        return assumptions;
    }

    public async Task UpdateAsync(ForecastAssumptions assumptions)
    {
        assumptions.ModifiedAt = DateTime.UtcNow;
        await _session.UpdateAsync(assumptions);
        await _session.FlushAsync();
    }

    public async Task<ForecastAssumptions> GetOrCreateAsync(Guid userId)
    {
        var existing = await GetByUserIdAsync(userId);
        if (existing != null)
        {
            return existing;
        }

        var assumptions = new ForecastAssumptions
        {
            UserId = userId
        };

        return await CreateAsync(assumptions);
    }
}
