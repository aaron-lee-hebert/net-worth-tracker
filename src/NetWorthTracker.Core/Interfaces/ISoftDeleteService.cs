using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface ISoftDeleteService
{
    Task SoftDeleteAsync<T>(Guid id) where T : BaseEntity;
    Task RestoreAsync<T>(Guid id) where T : BaseEntity;
    Task<int> PurgeDeletedAsync(int gracePeriodDays = 30);
    Task<int> PurgeDeletedAsync<T>(int gracePeriodDays = 30) where T : BaseEntity;
}
