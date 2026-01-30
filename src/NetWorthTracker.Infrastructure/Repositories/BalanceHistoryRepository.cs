using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class BalanceHistoryRepository : RepositoryBase<BalanceHistory>, IBalanceHistoryRepository
{
    public BalanceHistoryRepository(ISession session) : base(session)
    {
    }

    public async Task<IEnumerable<BalanceHistory>> GetByAccountIdAsync(Guid accountId)
    {
        return await Session.Query<BalanceHistory>()
            .Where(b => b.AccountId == accountId && !b.IsDeleted)
            .OrderByDescending(b => b.RecordedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BalanceHistory>> GetByAccountIdAndDateRangeAsync(
        Guid accountId,
        DateTime startDate,
        DateTime endDate)
    {
        return await Session.Query<BalanceHistory>()
            .Where(b => b.AccountId == accountId &&
                        b.RecordedAt >= startDate &&
                        b.RecordedAt <= endDate &&
                        !b.IsDeleted)
            .OrderByDescending(b => b.RecordedAt)
            .ToListAsync();
    }

    public async Task<BalanceHistory?> GetLatestByAccountIdAsync(Guid accountId)
    {
        return await Session.Query<BalanceHistory>()
            .Where(b => b.AccountId == accountId && !b.IsDeleted)
            .OrderByDescending(b => b.RecordedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BalanceHistory>> GetByUserIdAndDateRangeAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate)
    {
        return await Session.Query<BalanceHistory>()
            .Where(b => b.Account!.UserId == userId &&
                        b.RecordedAt >= startDate &&
                        b.RecordedAt <= endDate &&
                        !b.IsDeleted)
            .OrderBy(b => b.RecordedAt)
            .ToListAsync();
    }

    public async Task<BalanceHistory?> GetByAccountIdAndDateAsync(Guid accountId, DateTime date)
    {
        // Match records on the same calendar date (within UTC day boundaries)
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await Session.Query<BalanceHistory>()
            .Where(b => b.AccountId == accountId &&
                        b.RecordedAt >= startOfDay &&
                        b.RecordedAt < endOfDay &&
                        !b.IsDeleted)
            .FirstOrDefaultAsync();
    }
}
