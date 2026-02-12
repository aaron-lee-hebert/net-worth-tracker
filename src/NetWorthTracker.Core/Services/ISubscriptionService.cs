using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.Services;

public interface ISubscriptionService
{
    Task<Subscription?> GetByUserIdAsync(Guid userId);
    Task<bool> HasActiveSubscriptionAsync(Guid userId);
    Task<Subscription> CreateOrUpdateFromStripeAsync(Guid userId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, SubscriptionStatus status, DateTime currentPeriodStart, DateTime currentPeriodEnd);
    Task UpdateStatusAsync(string stripeSubscriptionId, SubscriptionStatus status);
    Task CancelByUserIdAsync(Guid userId);
}
