using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IMonthlySnapshotRepository
{
    Task<MonthlySnapshot?> GetByUserIdAndMonthAsync(Guid userId, DateTime month);
    Task<MonthlySnapshot?> GetLatestByUserIdAsync(Guid userId);
    Task<IEnumerable<MonthlySnapshot>> GetByUserIdAsync(Guid userId, int limit = 12);
    Task<MonthlySnapshot> CreateAsync(MonthlySnapshot snapshot);
    Task UpdateAsync(MonthlySnapshot snapshot);
    Task<IEnumerable<MonthlySnapshot>> GetUnsentSnapshotsAsync();
}
