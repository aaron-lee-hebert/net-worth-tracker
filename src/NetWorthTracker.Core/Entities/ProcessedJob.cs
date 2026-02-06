namespace NetWorthTracker.Core.Entities;

public class ProcessedJob : BaseEntity
{
    public virtual string JobType { get; set; } = string.Empty;
    public virtual string JobKey { get; set; } = string.Empty;
    public virtual DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public virtual bool Success { get; set; }
    public virtual string? ErrorMessage { get; set; }
    public virtual string? Metadata { get; set; }
}
