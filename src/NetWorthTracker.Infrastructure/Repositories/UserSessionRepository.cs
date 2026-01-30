using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class UserSessionRepository : RepositoryBase<UserSession>, IUserSessionRepository
{
    public UserSessionRepository(ISession session) : base(session)
    {
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await Session.Query<UserSession>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    public async Task<UserSession?> GetByTokenAsync(string token)
    {
        return await Session.Query<UserSession>()
            .FirstOrDefaultAsync(s => s.SessionToken == token);
    }

    public async Task<int> GetActiveSessionCountAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await Session.Query<UserSession>()
            .CountAsync(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, string reason)
    {
        var now = DateTime.UtcNow;
        var sessions = await Session.Query<UserSession>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevocationReason = reason;
            session.UpdatedAt = DateTime.UtcNow;
            await Session.SaveOrUpdateAsync(session);
        }
        await Session.FlushAsync();
    }

    public async Task RevokeAllSessionsExceptAsync(Guid userId, Guid exceptSessionId, string reason)
    {
        var now = DateTime.UtcNow;
        var sessions = await Session.Query<UserSession>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now && s.Id != exceptSessionId)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevocationReason = reason;
            session.UpdatedAt = DateTime.UtcNow;
            await Session.SaveOrUpdateAsync(session);
        }
        await Session.FlushAsync();
    }

    public async Task<UserSession?> GetOldestActiveSessionAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await Session.Query<UserSession>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredSessions = await Session.Query<UserSession>()
            .Where(s => s.ExpiresAt <= now && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in expiredSessions)
        {
            session.IsRevoked = true;
            session.RevocationReason = "Session expired";
            session.UpdatedAt = DateTime.UtcNow;
            await Session.SaveOrUpdateAsync(session);
        }
        await Session.FlushAsync();
    }
}
