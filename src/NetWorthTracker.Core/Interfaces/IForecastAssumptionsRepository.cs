using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IForecastAssumptionsRepository : IRepository<ForecastAssumptions>
{
    Task<ForecastAssumptions?> GetByUserIdAsync(Guid userId);
    Task<ForecastAssumptions> GetOrCreateAsync(Guid userId);
}
