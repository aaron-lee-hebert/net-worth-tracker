using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Service for recording audit logs of user actions and data changes
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an action with full details
    /// </summary>
    Task LogAsync(AuditLogEntry entry);

    /// <summary>
    /// Log a simple action without entity changes
    /// </summary>
    Task LogAsync(Guid? userId, string action, string? description = null);

    /// <summary>
    /// Log an action on a specific entity
    /// </summary>
    Task LogEntityActionAsync(Guid? userId, string action, string entityType, Guid entityId,
        object? oldValue = null, object? newValue = null, string? description = null);

    /// <summary>
    /// Log a login attempt
    /// </summary>
    Task LogLoginAttemptAsync(string email, bool success, Guid? userId = null, string? ipAddress = null,
        string? userAgent = null, string? failureReason = null);

    /// <summary>
    /// Log a data export action
    /// </summary>
    Task LogExportAsync(Guid userId, string exportType, string? description = null);

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int limit = 100, int offset = 0);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, int limit = 100);

    /// <summary>
    /// Get recent audit logs (for admin view)
    /// </summary>
    Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100, int offset = 0);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? since = null, int limit = 100);
}

/// <summary>
/// Entry for creating an audit log record
/// </summary>
public class AuditLogEntry
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
