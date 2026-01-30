namespace NetWorthTracker.Core.Entities;

public class UserSession : BaseEntity
{
    public virtual Guid UserId { get; set; }
    public virtual string SessionToken { get; set; } = string.Empty;
    public virtual string? UserAgent { get; set; }
    public virtual string? IpAddress { get; set; }
    public virtual string? DeviceName { get; set; }
    public virtual DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public virtual DateTime ExpiresAt { get; set; }
    public virtual bool IsRevoked { get; set; }
    public virtual string? RevocationReason { get; set; }
}
