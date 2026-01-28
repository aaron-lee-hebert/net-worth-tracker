using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ISession _session;

    public SubscriptionRepository(ISession session)
    {
        _session = session;
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId)
    {
        return await _session.Query<Subscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId)
    {
        return await _session.Query<Subscription>()
            .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        return await _session.Query<Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        await _session.SaveAsync(subscription);
        await _session.FlushAsync();
        return subscription;
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(subscription);
        await _session.FlushAsync();
    }
}
