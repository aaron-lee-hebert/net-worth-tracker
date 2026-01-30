using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ISession session) : base(session)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int limit = 100, int offset = 0)
    {
        return await Session.Query<AuditLog>()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, int limit = 100)
    {
        return await Session.Query<AuditLog>()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100, int offset = 0)
    {
        return await Session.Query<AuditLog>()
            .OrderByDescending(a => a.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? since = null, int limit = 100)
    {
        var query = Session.Query<AuditLog>()
            .Where(a => a.Action == action);

        if (since.HasValue)
        {
            query = query.Where(a => a.Timestamp >= since.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLoginAttemptsAsync(string email, DateTime since, int limit = 100)
    {
        return await Session.Query<AuditLog>()
            .Where(a => (a.Action == AuditAction.LoginSuccess || a.Action == AuditAction.LoginFailed)
                && a.Description != null && a.Description.Contains(email)
                && a.Timestamp >= since)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
