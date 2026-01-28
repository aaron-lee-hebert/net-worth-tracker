namespace NetWorthTracker.Core.Entities;

public enum SubscriptionStatus
{
    /// <summary>
    /// User is in trial period (no payment required yet)
    /// </summary>
    Trialing,

    /// <summary>
    /// Active paid subscription
    /// </summary>
    Active,

    /// <summary>
    /// Payment failed, in grace period
    /// </summary>
    PastDue,

    /// <summary>
    /// Subscription canceled by user (access until period end)
    /// </summary>
    Canceled,

    /// <summary>
    /// Subscription expired or fully canceled
    /// </summary>
    Expired
}

public class Subscription : BaseEntity
{
    public virtual Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Stripe customer ID (cus_xxx)
    /// </summary>
    public virtual string? StripeCustomerId { get; set; }

    /// <summary>
    /// Stripe subscription ID (sub_xxx)
    /// </summary>
    public virtual string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Current subscription status
    /// </summary>
    public virtual SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    /// <summary>
    /// When the current billing period ends (for active subscriptions)
    /// or when the trial ends (for trialing)
    /// </summary>
    public virtual DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>
    /// When the trial started
    /// </summary>
    public virtual DateTime TrialStartedAt { get; set; }

    /// <summary>
    /// When the trial ends
    /// </summary>
    public virtual DateTime TrialEndsAt { get; set; }

    /// <summary>
    /// Returns true if the user has active access (trial, active, or canceled but not yet expired)
    /// </summary>
    public virtual bool HasActiveAccess
    {
        get
        {
            return Status switch
            {
                SubscriptionStatus.Trialing => DateTime.UtcNow < TrialEndsAt,
                SubscriptionStatus.Active => true,
                SubscriptionStatus.PastDue => true, // Grace period - still allow access
                SubscriptionStatus.Canceled => CurrentPeriodEnd.HasValue && DateTime.UtcNow < CurrentPeriodEnd.Value,
                SubscriptionStatus.Expired => false,
                _ => false
            };
        }
    }

    /// <summary>
    /// Returns true if the user is in trial period
    /// </summary>
    public virtual bool IsInTrial => Status == SubscriptionStatus.Trialing && DateTime.UtcNow < TrialEndsAt;

    /// <summary>
    /// Returns the number of days remaining in trial
    /// </summary>
    public virtual int TrialDaysRemaining
    {
        get
        {
            if (!IsInTrial) return 0;
            return Math.Max(0, (int)(TrialEndsAt - DateTime.UtcNow).TotalDays);
        }
    }
}
