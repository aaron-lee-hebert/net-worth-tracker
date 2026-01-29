using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IBalanceHistoryRepository : IRepository<BalanceHistory>
{
    Task<IEnumerable<BalanceHistory>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<BalanceHistory>> GetByAccountIdAndDateRangeAsync(Guid accountId, DateTime startDate, DateTime endDate);
    Task<BalanceHistory?> GetLatestByAccountIdAsync(Guid accountId);
    Task<IEnumerable<BalanceHistory>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<BalanceHistory?> GetByAccountIdAndDateAsync(Guid accountId, DateTime date);
}
