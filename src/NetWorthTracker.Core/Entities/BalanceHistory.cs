namespace NetWorthTracker.Core.Entities;

public class BalanceHistory : BaseEntity
{
    public virtual decimal Balance { get; set; }
    public virtual DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public virtual string? Notes { get; set; }

    public virtual Guid AccountId { get; set; }
    public virtual Account? Account { get; set; }
}
