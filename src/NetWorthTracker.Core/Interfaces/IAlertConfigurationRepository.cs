using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IAlertConfigurationRepository : IRepository<AlertConfiguration>
{
    Task<AlertConfiguration?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AlertConfiguration>> GetAllEnabledAsync();
}
