using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.ViewModels;

public class BillingViewModel
{
    public bool HasSubscription { get; set; }
    public SubscriptionStatus? Status { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public string? StripePriceId { get; set; }
}
