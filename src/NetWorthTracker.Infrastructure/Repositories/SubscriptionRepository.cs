using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class SubscriptionRepository : RepositoryBase<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(ISession session) : base(session)
    {
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId)
    {
        return await Session.Query<Subscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId)
    {
        return await Session.Query<Subscription>()
            .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        return await Session.Query<Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
    }

    public async Task<int> GetCountByStatusAsync(SubscriptionStatus status)
    {
        return await Session.Query<Subscription>()
            .CountAsync(s => s.Status == status);
    }

    public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, int limit = 100)
    {
        return await Session.Query<Subscription>()
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetAllWithUsersAsync()
    {
        return await Session.Query<Subscription>()
            .Fetch(s => s.User)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await Session.Query<Subscription>().CountAsync();
    }
}
