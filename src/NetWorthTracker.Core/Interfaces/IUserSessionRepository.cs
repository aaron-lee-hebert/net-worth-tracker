using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Repository for user session persistence
/// </summary>
public interface IUserSessionRepository : IRepository<UserSession>
{
    /// <summary>
    /// Get all active (non-revoked, non-expired) sessions for a user
    /// </summary>
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId);

    /// <summary>
    /// Get a session by its token
    /// </summary>
    Task<UserSession?> GetByTokenAsync(string token);

    /// <summary>
    /// Get the count of active sessions for a user
    /// </summary>
    Task<int> GetActiveSessionCountAsync(Guid userId);

    /// <summary>
    /// Revoke all active sessions for a user
    /// </summary>
    Task RevokeAllSessionsAsync(Guid userId, string reason);

    /// <summary>
    /// Revoke all active sessions for a user except the specified session
    /// </summary>
    Task RevokeAllSessionsExceptAsync(Guid userId, Guid exceptSessionId, string reason);

    /// <summary>
    /// Get the oldest active session for a user (for eviction when limit exceeded)
    /// </summary>
    Task<UserSession?> GetOldestActiveSessionAsync(Guid userId);

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}
