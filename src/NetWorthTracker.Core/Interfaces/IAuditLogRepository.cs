using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Repository for audit log persistence
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int limit = 100, int offset = 0);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, int limit = 100);

    /// <summary>
    /// Get recent audit logs
    /// </summary>
    Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100, int offset = 0);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? since = null, int limit = 100);

    /// <summary>
    /// Get login attempts for an email address (for security monitoring)
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLoginAttemptsAsync(string email, DateTime since, int limit = 100);
}
