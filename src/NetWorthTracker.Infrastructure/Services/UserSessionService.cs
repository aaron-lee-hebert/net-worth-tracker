using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserSessionService> _logger;

    // Configuration
    public int MaxSessionsPerUser => 5;
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ActivityThrottleInterval = TimeSpan.FromMinutes(1);

    public UserSessionService(
        IUserSessionRepository sessionRepository,
        IAuditService auditService,
        ILogger<UserSessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, string? userAgent, string? ipAddress)
    {
        // Check if we need to evict the oldest session
        var sessionCount = await _sessionRepository.GetActiveSessionCountAsync(userId);
        if (sessionCount >= MaxSessionsPerUser)
        {
            var oldestSession = await _sessionRepository.GetOldestActiveSessionAsync(userId);
            if (oldestSession != null)
            {
                oldestSession.IsRevoked = true;
                oldestSession.RevocationReason = "Session limit exceeded - oldest session evicted";
                oldestSession.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(oldestSession);

                _logger.LogInformation("Evicted oldest session {SessionId} for user {UserId} due to session limit",
                    oldestSession.Id, userId);
            }
        }

        var session = new UserSession
        {
            UserId = userId,
            SessionToken = GenerateSecureToken(),
            UserAgent = TruncateString(userAgent, 500),
            IpAddress = TruncateString(ipAddress, 50),
            DeviceName = ParseDeviceName(userAgent),
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(SessionDuration),
            IsRevoked = false
        };

        await _sessionRepository.AddAsync(session);

        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.SessionCreated,
            AuditEntityType.Session,
            session.Id,
            oldValue: null,
            newValue: new { session.DeviceName, session.IpAddress },
            description: $"Session created from {session.IpAddress ?? "unknown IP"} ({session.DeviceName ?? "unknown device"})");

        _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, userId);

        return session;
    }

    public async Task<UserSession?> ValidateSessionAsync(string sessionToken)
    {
        var session = await _sessionRepository.GetByTokenAsync(sessionToken);
        if (session == null)
        {
            return null;
        }

        // Check if revoked
        if (session.IsRevoked)
        {
            _logger.LogDebug("Session {SessionId} is revoked", session.Id);
            return null;
        }

        // Check if expired
        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogDebug("Session {SessionId} has expired", session.Id);
            session.IsRevoked = true;
            session.RevocationReason = "Session expired";
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            return null;
        }

        return session;
    }

    public async Task UpdateActivityAsync(string sessionToken)
    {
        var session = await _sessionRepository.GetByTokenAsync(sessionToken);
        if (session == null || session.IsRevoked)
        {
            return;
        }

        // Throttle updates to once per minute
        if (DateTime.UtcNow - session.LastActivityAt < ActivityThrottleInterval)
        {
            return;
        }

        session.LastActivityAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.IsRevoked)
        {
            return;
        }

        session.IsRevoked = true;
        session.RevocationReason = reason;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

        await _auditService.LogEntityActionAsync(
            session.UserId,
            AuditAction.SessionRevoked,
            AuditEntityType.Session,
            session.Id,
            oldValue: null,
            newValue: new { Reason = reason },
            description: $"Session revoked: {reason}");

        _logger.LogInformation("Revoked session {SessionId} for user {UserId}: {Reason}",
            sessionId, session.UserId, reason);
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, string reason)
    {
        await _sessionRepository.RevokeAllSessionsAsync(userId, reason);

        await _auditService.LogAsync(
            userId,
            AuditAction.SessionsRevoked,
            $"All sessions revoked: {reason}");

        _logger.LogInformation("Revoked all sessions for user {UserId}: {Reason}", userId, reason);
    }

    public async Task RevokeAllUserSessionsExceptAsync(Guid userId, Guid exceptSessionId, string reason)
    {
        await _sessionRepository.RevokeAllSessionsExceptAsync(userId, exceptSessionId, reason);

        await _auditService.LogAsync(
            userId,
            AuditAction.SessionsRevoked,
            $"All other sessions revoked: {reason}");

        _logger.LogInformation("Revoked all sessions except {SessionId} for user {UserId}: {Reason}",
            exceptSessionId, userId, reason);
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(Guid userId)
    {
        return await _sessionRepository.GetActiveSessionsAsync(userId);
    }

    public async Task<UserSession?> GetSessionByIdAsync(Guid sessionId)
    {
        return await _sessionRepository.GetByIdAsync(sessionId);
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        await _sessionRepository.CleanupExpiredSessionsAsync();
        _logger.LogInformation("Cleaned up expired sessions");
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    private static string? ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return null;
        }

        // Simple device/browser detection
        if (userAgent.Contains("Windows"))
        {
            if (userAgent.Contains("Edge"))
                return "Windows (Edge)";
            if (userAgent.Contains("Chrome"))
                return "Windows (Chrome)";
            if (userAgent.Contains("Firefox"))
                return "Windows (Firefox)";
            return "Windows";
        }

        if (userAgent.Contains("Mac OS") || userAgent.Contains("Macintosh"))
        {
            if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
                return "macOS (Safari)";
            if (userAgent.Contains("Chrome"))
                return "macOS (Chrome)";
            if (userAgent.Contains("Firefox"))
                return "macOS (Firefox)";
            return "macOS";
        }

        if (userAgent.Contains("iPhone"))
            return "iPhone";
        if (userAgent.Contains("iPad"))
            return "iPad";
        if (userAgent.Contains("Android"))
            return "Android";
        if (userAgent.Contains("Linux"))
            return "Linux";

        return "Unknown Device";
    }
}
