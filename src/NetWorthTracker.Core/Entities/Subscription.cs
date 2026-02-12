using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.Entities;

public class Subscription : BaseEntity
{
    public virtual Guid UserId { get; set; }
    public virtual string StripeCustomerId { get; set; } = string.Empty;
    public virtual string StripeSubscriptionId { get; set; } = string.Empty;
    public virtual string StripePriceId { get; set; } = string.Empty;
    public virtual SubscriptionStatus Status { get; set; }
    public virtual DateTime CurrentPeriodStart { get; set; }
    public virtual DateTime CurrentPeriodEnd { get; set; }
}
