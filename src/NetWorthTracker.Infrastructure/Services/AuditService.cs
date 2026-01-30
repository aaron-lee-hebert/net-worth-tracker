using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public AuditService(IAuditLogRepository repository, ILogger<AuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = entry.UserId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                OldValue = SerializeValue(entry.OldValue),
                NewValue = SerializeValue(entry.NewValue),
                Description = entry.Description,
                IpAddress = entry.IpAddress,
                UserAgent = TruncateUserAgent(entry.UserAgent),
                Timestamp = DateTime.UtcNow,
                Success = entry.Success,
                ErrorMessage = entry.ErrorMessage
            };

            await _repository.AddAsync(auditLog);

            _logger.LogDebug("Audit log created: {Action} by user {UserId} on {EntityType}:{EntityId}",
                entry.Action, entry.UserId, entry.EntityType, entry.EntityId);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break the application
            _logger.LogError(ex, "Failed to create audit log entry: {Action}", entry.Action);
        }
    }

    public async Task LogAsync(Guid? userId, string action, string? description = null)
    {
        await LogAsync(new AuditLogEntry
        {
            UserId = userId,
            Action = action,
            Description = description
        });
    }

    public async Task LogEntityActionAsync(Guid? userId, string action, string entityType, Guid entityId,
        object? oldValue = null, object? newValue = null, string? description = null)
    {
        await LogAsync(new AuditLogEntry
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Description = description
        });
    }

    public async Task LogLoginAttemptAsync(string email, bool success, Guid? userId = null, string? ipAddress = null,
        string? userAgent = null, string? failureReason = null)
    {
        await LogAsync(new AuditLogEntry
        {
            UserId = userId, // User ID known for successful logins
            Action = success ? AuditAction.LoginSuccess : AuditAction.LoginFailed,
            EntityType = AuditEntityType.User,
            Description = success ? $"Login successful for {email}" : $"Login failed for {email}: {failureReason}",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            ErrorMessage = success ? null : failureReason
        });
    }

    public async Task LogExportAsync(Guid userId, string exportType, string? description = null)
    {
        await LogAsync(new AuditLogEntry
        {
            UserId = userId,
            Action = AuditAction.DataExported,
            Description = description ?? $"Exported {exportType}"
        });
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int limit = 100, int offset = 0)
    {
        return await _repository.GetByUserIdAsync(userId, limit, offset);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, int limit = 100)
    {
        return await _repository.GetByEntityAsync(entityType, entityId, limit);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100, int offset = 0)
    {
        return await _repository.GetRecentAsync(limit, offset);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? since = null, int limit = 100)
    {
        return await _repository.GetByActionAsync(action, since, limit);
    }

    private static string? SerializeValue(object? value)
    {
        if (value == null)
            return null;

        try
        {
            // For simple types, just convert to string
            if (value is string s)
                return s;

            // For complex objects, serialize to JSON
            var json = JsonSerializer.Serialize(value, JsonOptions);

            // Truncate if too long (max 10000 chars in database)
            if (json.Length > 9900)
            {
                return json.Substring(0, 9900) + "...(truncated)";
            }

            return json;
        }
        catch
        {
            return value.ToString();
        }
    }

    private static string? TruncateUserAgent(string? userAgent)
    {
        if (userAgent == null)
            return null;

        // Limit user agent to 500 chars (database limit)
        return userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent;
    }
}
