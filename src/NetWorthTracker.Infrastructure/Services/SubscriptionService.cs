using Microsoft.Extensions.Logging;
using NHibernate;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISession _session;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(ISession session, ILogger<SubscriptionService> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId)
    {
        return await _session.QueryOver<Subscription>()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .SingleOrDefaultAsync();
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId)
    {
        var subscription = await GetByUserIdAsync(userId);
        if (subscription == null)
            return false;

        return subscription.Status == SubscriptionStatus.Active
            || subscription.Status == SubscriptionStatus.Trialing;
    }

    public async Task<Subscription> CreateOrUpdateFromStripeAsync(
        Guid userId,
        string stripeCustomerId,
        string stripeSubscriptionId,
        string stripePriceId,
        SubscriptionStatus status,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd)
    {
        var existing = await GetByUserIdAsync(userId);

        using var transaction = _session.BeginTransaction();

        if (existing != null)
        {
            existing.StripeCustomerId = stripeCustomerId;
            existing.StripeSubscriptionId = stripeSubscriptionId;
            existing.StripePriceId = stripePriceId;
            existing.Status = status;
            existing.CurrentPeriodStart = currentPeriodStart;
            existing.CurrentPeriodEnd = currentPeriodEnd;
            existing.UpdatedAt = DateTime.UtcNow;

            await _session.UpdateAsync(existing);
            await transaction.CommitAsync();

            _logger.LogInformation("Updated subscription for user {UserId}", userId);
            return existing;
        }

        var subscription = new Subscription
        {
            UserId = userId,
            StripeCustomerId = stripeCustomerId,
            StripeSubscriptionId = stripeSubscriptionId,
            StripePriceId = stripePriceId,
            Status = status,
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd
        };

        await _session.SaveAsync(subscription);
        await transaction.CommitAsync();

        _logger.LogInformation("Created subscription for user {UserId}", userId);
        return subscription;
    }

    public async Task UpdateStatusAsync(string stripeSubscriptionId, SubscriptionStatus status)
    {
        var subscription = await _session.QueryOver<Subscription>()
            .Where(s => s.StripeSubscriptionId == stripeSubscriptionId && !s.IsDeleted)
            .SingleOrDefaultAsync();

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found for Stripe ID {StripeSubscriptionId}", stripeSubscriptionId);
            return;
        }

        using var transaction = _session.BeginTransaction();

        subscription.Status = status;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _session.UpdateAsync(subscription);
        await transaction.CommitAsync();

        _logger.LogInformation("Updated subscription {StripeSubscriptionId} to status {Status}",
            stripeSubscriptionId, status);
    }

    public async Task CancelByUserIdAsync(Guid userId)
    {
        var subscription = await GetByUserIdAsync(userId);
        if (subscription == null)
        {
            _logger.LogDebug("No subscription to cancel for user {UserId}", userId);
            return;
        }

        using var transaction = _session.BeginTransaction();

        // TODO: Call Stripe API to cancel the subscription
        // await stripeClient.CancelSubscriptionAsync(subscription.StripeSubscriptionId);

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _session.UpdateAsync(subscription);
        await transaction.CommitAsync();

        _logger.LogInformation("Canceled subscription for user {UserId} (Stripe ID: {StripeSubscriptionId})",
            userId, subscription.StripeSubscriptionId);
    }
}
