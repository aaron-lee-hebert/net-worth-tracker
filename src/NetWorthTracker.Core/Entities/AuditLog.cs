namespace NetWorthTracker.Core.Entities;

/// <summary>
/// Audit log entry for tracking user actions and data changes
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// The user who performed the action (null for system actions or failed logins)
    /// </summary>
    public virtual Guid? UserId { get; set; }

    /// <summary>
    /// The action performed (e.g., Create, Update, Delete, Login, Export)
    /// </summary>
    public virtual string Action { get; set; } = string.Empty;

    /// <summary>
    /// The type of entity affected (e.g., Account, BalanceHistory, User)
    /// </summary>
    public virtual string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the affected entity (if applicable)
    /// </summary>
    public virtual Guid? EntityId { get; set; }

    /// <summary>
    /// JSON-serialized previous state (for updates/deletes)
    /// </summary>
    public virtual string? OldValue { get; set; }

    /// <summary>
    /// JSON-serialized new state (for creates/updates)
    /// </summary>
    public virtual string? NewValue { get; set; }

    /// <summary>
    /// Additional context or description of the action
    /// </summary>
    public virtual string? Description { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    public virtual string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the client
    /// </summary>
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// When the action occurred
    /// </summary>
    public virtual DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public virtual bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    public virtual string? ErrorMessage { get; set; }
}

/// <summary>
/// Standard audit action types
/// </summary>
public static class AuditAction
{
    // Account operations
    public const string AccountCreated = "Account.Created";
    public const string AccountUpdated = "Account.Updated";
    public const string AccountDeleted = "Account.Deleted";

    // Balance operations
    public const string BalanceUpdated = "Balance.Updated";
    public const string BalanceBulkUpdated = "Balance.BulkUpdated";
    public const string BalanceRecordDeleted = "Balance.RecordDeleted";

    // Authentication
    public const string LoginSuccess = "Auth.LoginSuccess";
    public const string LoginFailed = "Auth.LoginFailed";
    public const string Logout = "Auth.Logout";
    public const string PasswordChanged = "Auth.PasswordChanged";
    public const string PasswordResetRequested = "Auth.PasswordResetRequested";
    public const string PasswordResetCompleted = "Auth.PasswordResetCompleted";

    // Two-factor authentication
    public const string TwoFactorEnabled = "Auth.2FA.Enabled";
    public const string TwoFactorDisabled = "Auth.2FA.Disabled";
    public const string TwoFactorRecoveryCodesGenerated = "Auth.2FA.RecoveryCodesGenerated";
    public const string TwoFactorLoginSuccess = "Auth.2FA.LoginSuccess";
    public const string TwoFactorLoginFailed = "Auth.2FA.LoginFailed";
    public const string RecoveryCodeUsed = "Auth.2FA.RecoveryCodeUsed";

    // User management
    public const string UserRegistered = "User.Registered";
    public const string UserDeleted = "User.Deleted";
    public const string EmailVerified = "User.EmailVerified";
    public const string ProfileUpdated = "User.ProfileUpdated";
    public const string AllDataDeleted = "User.AllDataDeleted";

    // Data export
    public const string DataExported = "Data.Exported";

    // Settings
    public const string SettingsUpdated = "Settings.Updated";
    public const string AlertSettingsUpdated = "Settings.Alerts.Updated";
    public const string ForecastAssumptionsUpdated = "Settings.Forecast.Updated";
}

/// <summary>
/// Entity types for audit logging
/// </summary>
public static class AuditEntityType
{
    public const string Account = "Account";
    public const string BalanceHistory = "BalanceHistory";
    public const string User = "User";
    public const string Subscription = "Subscription";
    public const string AlertConfiguration = "AlertConfiguration";
    public const string ForecastAssumptions = "ForecastAssumptions";
}
