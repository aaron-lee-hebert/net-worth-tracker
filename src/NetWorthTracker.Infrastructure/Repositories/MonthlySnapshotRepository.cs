using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class MonthlySnapshotRepository : RepositoryBase<MonthlySnapshot>, IMonthlySnapshotRepository
{
    public MonthlySnapshotRepository(ISession session) : base(session)
    {
    }

    public async Task<MonthlySnapshot?> GetByUserIdAndMonthAsync(Guid userId, DateTime month)
    {
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        return await Session.Query<MonthlySnapshot>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Month == startOfMonth);
    }

    public async Task<MonthlySnapshot?> GetLatestByUserIdAsync(Guid userId)
    {
        return await Session.Query<MonthlySnapshot>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Month)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MonthlySnapshot>> GetByUserIdAsync(Guid userId, int limit = 12)
    {
        return await Session.Query<MonthlySnapshot>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Month)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<MonthlySnapshot>> GetUnsentSnapshotsAsync()
    {
        return await Session.Query<MonthlySnapshot>()
            .Where(s => !s.EmailSent)
            .ToListAsync();
    }
}
