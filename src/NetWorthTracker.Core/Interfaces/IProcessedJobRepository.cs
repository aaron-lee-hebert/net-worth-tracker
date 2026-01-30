using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IProcessedJobRepository : IRepository<ProcessedJob>
{
    Task<bool> ExistsAsync(string jobType, string jobKey);
    Task<ProcessedJob?> GetByKeyAsync(string jobType, string jobKey);
    Task<IEnumerable<ProcessedJob>> GetRecentAsync(string jobType, int limit = 100);
    Task<ProcessedJob?> GetLastSuccessfulAsync(string jobType);
    Task CleanupOldJobsAsync(int daysToKeep = 90);
}
