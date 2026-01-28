using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByUserIdAsync(Guid userId);
    Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
    Task<Subscription> CreateAsync(Subscription subscription);
    Task UpdateAsync(Subscription subscription);
}
