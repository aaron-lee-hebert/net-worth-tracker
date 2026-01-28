using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IAlertConfigurationRepository
{
    Task<AlertConfiguration?> GetByUserIdAsync(Guid userId);
    Task<AlertConfiguration> CreateAsync(AlertConfiguration config);
    Task UpdateAsync(AlertConfiguration config);
    Task<IEnumerable<AlertConfiguration>> GetAllEnabledAsync();
}
