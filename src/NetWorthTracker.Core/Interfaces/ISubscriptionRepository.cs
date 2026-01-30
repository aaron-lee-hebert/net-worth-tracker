using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByUserIdAsync(Guid userId);
    Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);

    // Analytics methods for admin
    Task<int> GetCountByStatusAsync(SubscriptionStatus status);
    Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, int limit = 100);
    Task<IEnumerable<Subscription>> GetAllWithUsersAsync();
    Task<int> GetTotalCountAsync();
}
