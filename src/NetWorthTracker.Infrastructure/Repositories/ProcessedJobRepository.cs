using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class ProcessedJobRepository : RepositoryBase<ProcessedJob>, IProcessedJobRepository
{
    public ProcessedJobRepository(ISession session) : base(session)
    {
    }

    public async Task<bool> ExistsAsync(string jobType, string jobKey)
    {
        return await Session.Query<ProcessedJob>()
            .AnyAsync(j => j.JobType == jobType && j.JobKey == jobKey);
    }

    public async Task<ProcessedJob?> GetByKeyAsync(string jobType, string jobKey)
    {
        return await Session.Query<ProcessedJob>()
            .FirstOrDefaultAsync(j => j.JobType == jobType && j.JobKey == jobKey);
    }

    public async Task<IEnumerable<ProcessedJob>> GetRecentAsync(string jobType, int limit = 100)
    {
        return await Session.Query<ProcessedJob>()
            .Where(j => j.JobType == jobType)
            .OrderByDescending(j => j.ProcessedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ProcessedJob?> GetLastSuccessfulAsync(string jobType)
    {
        return await Session.Query<ProcessedJob>()
            .Where(j => j.JobType == jobType && j.Success)
            .OrderByDescending(j => j.ProcessedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CleanupOldJobsAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldJobs = await Session.Query<ProcessedJob>()
            .Where(j => j.ProcessedAt < cutoffDate)
            .ToListAsync();

        foreach (var job in oldJobs)
        {
            await Session.DeleteAsync(job);
        }
        await Session.FlushAsync();
    }
}
