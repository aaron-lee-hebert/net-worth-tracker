using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface IEmailQueueRepository : IRepository<EmailQueue>
{
    Task<IEnumerable<EmailQueue>> GetPendingEmailsAsync(int batchSize = 10);
    Task<EmailQueue?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<int> GetPendingCountAsync();
    Task<int> GetFailedCountAsync();
    Task CleanupOldEmailsAsync(int daysToKeep = 30);
}
