using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.Entities;

public class Account : BaseEntity
{
    public virtual string Name { get; set; } = string.Empty;
    public virtual string? Description { get; set; }
    public virtual AccountType AccountType { get; set; }
    public virtual decimal CurrentBalance { get; set; }
    public virtual string? Institution { get; set; }
    public virtual string? AccountNumber { get; set; }
    public virtual bool IsActive { get; set; } = true;

    public virtual Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public virtual IList<BalanceHistory> BalanceHistories { get; set; } = new List<BalanceHistory>();
}
