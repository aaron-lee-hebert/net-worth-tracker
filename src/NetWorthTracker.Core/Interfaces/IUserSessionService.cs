using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Service for managing user sessions
/// </summary>
public interface IUserSessionService
{
    /// <summary>
    /// Maximum number of concurrent sessions allowed per user
    /// </summary>
    int MaxSessionsPerUser { get; }

    /// <summary>
    /// Create a new session for a user. Evicts oldest session if limit exceeded.
    /// </summary>
    Task<UserSession> CreateSessionAsync(Guid userId, string? userAgent, string? ipAddress);

    /// <summary>
    /// Validate a session token. Returns null if session is invalid, expired, or revoked.
    /// Updates LastActivityAt if valid.
    /// </summary>
    Task<UserSession?> ValidateSessionAsync(string sessionToken);

    /// <summary>
    /// Update the last activity timestamp for a session (throttled to once per minute)
    /// </summary>
    Task UpdateActivityAsync(string sessionToken);

    /// <summary>
    /// Revoke a single session
    /// </summary>
    Task RevokeSessionAsync(Guid sessionId, string reason);

    /// <summary>
    /// Revoke all sessions for a user
    /// </summary>
    Task RevokeAllUserSessionsAsync(Guid userId, string reason);

    /// <summary>
    /// Revoke all sessions for a user except the specified session
    /// </summary>
    Task RevokeAllUserSessionsExceptAsync(Guid userId, Guid exceptSessionId, string reason);

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(Guid userId);

    /// <summary>
    /// Get a session by ID
    /// </summary>
    Task<UserSession?> GetSessionByIdAsync(Guid sessionId);

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}
