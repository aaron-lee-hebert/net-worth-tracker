using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class EmailQueueRepository : RepositoryBase<EmailQueue>, IEmailQueueRepository
{
    public EmailQueueRepository(ISession session) : base(session)
    {
    }

    public async Task<IEnumerable<EmailQueue>> GetPendingEmailsAsync(int batchSize = 10)
    {
        var now = DateTime.UtcNow;
        return await Session.Query<EmailQueue>()
            .Where(e => e.Status == EmailQueueStatus.Pending
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now)
                && e.AttemptCount < e.MaxAttempts)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<EmailQueue?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await Session.Query<EmailQueue>()
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey);
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await Session.Query<EmailQueue>()
            .CountAsync(e => e.Status == EmailQueueStatus.Pending);
    }

    public async Task<int> GetFailedCountAsync()
    {
        return await Session.Query<EmailQueue>()
            .CountAsync(e => e.Status == EmailQueueStatus.Failed);
    }

    public async Task CleanupOldEmailsAsync(int daysToKeep = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldEmails = await Session.Query<EmailQueue>()
            .Where(e => e.CreatedAt < cutoffDate
                && (e.Status == EmailQueueStatus.Sent || e.Status == EmailQueueStatus.Cancelled))
            .ToListAsync();

        foreach (var email in oldEmails)
        {
            await Session.DeleteAsync(email);
        }
        await Session.FlushAsync();
    }
}
