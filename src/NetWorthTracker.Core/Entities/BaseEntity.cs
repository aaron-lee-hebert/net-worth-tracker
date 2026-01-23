namespace NetWorthTracker.Core.Entities;

public abstract class BaseEntity
{
    public virtual Guid Id { get; set; } = Guid.NewGuid();
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual DateTime? UpdatedAt { get; set; }
}
