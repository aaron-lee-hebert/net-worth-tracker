namespace NetWorthTracker.Core.Entities;

public class EmailQueue : BaseEntity
{
    public virtual string ToEmail { get; set; } = string.Empty;
    public virtual string Subject { get; set; } = string.Empty;
    public virtual string HtmlBody { get; set; } = string.Empty;
    public virtual EmailQueueStatus Status { get; set; } = EmailQueueStatus.Pending;
    public virtual int AttemptCount { get; set; }
    public virtual int MaxAttempts { get; set; } = 3;
    public virtual DateTime? LastAttemptAt { get; set; }
    public virtual DateTime? NextAttemptAt { get; set; }
    public virtual DateTime? SentAt { get; set; }
    public virtual string? ErrorMessage { get; set; }
    public virtual string? IdempotencyKey { get; set; }
}

public enum EmailQueueStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}
