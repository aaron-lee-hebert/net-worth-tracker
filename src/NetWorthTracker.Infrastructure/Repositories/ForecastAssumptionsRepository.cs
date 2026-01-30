using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class ForecastAssumptionsRepository : RepositoryBase<ForecastAssumptions>, IForecastAssumptionsRepository
{
    public ForecastAssumptionsRepository(ISession session) : base(session)
    {
    }

    public async Task<ForecastAssumptions?> GetByUserIdAsync(Guid userId)
    {
        return await Session.Query<ForecastAssumptions>()
            .FirstOrDefaultAsync(a => a.UserId == userId && !a.IsDeleted);
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

        return await AddAsync(assumptions);
    }
}
