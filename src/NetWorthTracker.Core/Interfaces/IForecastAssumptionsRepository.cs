using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IForecastAssumptionsRepository
{
    Task<ForecastAssumptions?> GetByUserIdAsync(Guid userId);
    Task<ForecastAssumptions> CreateAsync(ForecastAssumptions assumptions);
    Task UpdateAsync(ForecastAssumptions assumptions);
    Task<ForecastAssumptions> GetOrCreateAsync(Guid userId);
}
