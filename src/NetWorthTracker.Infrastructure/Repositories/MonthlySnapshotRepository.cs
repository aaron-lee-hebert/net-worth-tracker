using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class MonthlySnapshotRepository : IMonthlySnapshotRepository
{
    private readonly ISession _session;

    public MonthlySnapshotRepository(ISession session)
    {
        _session = session;
    }

    public async Task<MonthlySnapshot?> GetByUserIdAndMonthAsync(Guid userId, DateTime month)
    {
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        return await _session.Query<MonthlySnapshot>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Month == startOfMonth);
    }

    public async Task<MonthlySnapshot?> GetLatestByUserIdAsync(Guid userId)
    {
        return await _session.Query<MonthlySnapshot>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Month)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MonthlySnapshot>> GetByUserIdAsync(Guid userId, int limit = 12)
    {
        return await _session.Query<MonthlySnapshot>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Month)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<MonthlySnapshot> CreateAsync(MonthlySnapshot snapshot)
    {
        await _session.SaveAsync(snapshot);
        await _session.FlushAsync();
        return snapshot;
    }

    public async Task UpdateAsync(MonthlySnapshot snapshot)
    {
        snapshot.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(snapshot);
        await _session.FlushAsync();
    }

    public async Task<IEnumerable<MonthlySnapshot>> GetUnsentSnapshotsAsync()
    {
        return await _session.Query<MonthlySnapshot>()
            .Where(s => !s.EmailSent)
            .ToListAsync();
    }
}
