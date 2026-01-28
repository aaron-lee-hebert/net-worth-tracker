using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using Stripe;
using Stripe.Checkout;

namespace NetWorthTracker.Infrastructure.Services;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PriceId { get; set; } = string.Empty; // The Stripe Price ID for the $20/year subscription
    public int TrialDays { get; set; } = 14;
}

public class StripeService : IStripeService
{
    private readonly StripeSettings _settings;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IOptions<StripeSettings> settings,
        ISubscriptionRepository subscriptionRepository,
        ILogger<StripeService> logger)
    {
        _settings = settings.Value;
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;

        if (IsConfigured)
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;
        }
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.SecretKey) &&
                                !string.IsNullOrEmpty(_settings.PriceId);

    public async Task<string> CreateCheckoutSessionAsync(Guid userId, string email, string successUrl, string cancelUrl)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured");
        }

        var options = new SessionCreateOptions
        {
            CustomerEmail = email,
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = _settings.PriceId,
                    Quantity = 1
                }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() }
                }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId}", session.Id, userId);

        return session.Url!;
    }

    public async Task<string> CreateCustomerPortalSessionAsync(string stripeCustomerId, string returnUrl)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured");
        }

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    public async Task ProcessWebhookAsync(string json, string signature)
    {
        if (string.IsNullOrEmpty(_settings.WebhookSecret))
        {
            _logger.LogWarning("Webhook secret not configured, skipping webhook processing");
            return;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _settings.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to validate Stripe webhook signature");
            throw;
        }

        _logger.LogInformation("Processing Stripe webhook event {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompleted(stripeEvent);
                break;

            case "invoice.payment_failed":
                await HandlePaymentFailed(stripeEvent);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(stripeEvent);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdated(stripeEvent);
                break;

            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogWarning("checkout.session.completed event has no session data");
            return;
        }

        if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("checkout.session.completed event missing userId metadata");
            return;
        }

        var subscription = await _subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for user {UserId} during checkout completion", userId);
            return;
        }

        // Get the Stripe subscription to get period end
        var subscriptionService = new Stripe.SubscriptionService();
        var stripeSubscription = await subscriptionService.GetAsync(session.SubscriptionId);

        subscription.StripeCustomerId = session.CustomerId;
        subscription.StripeSubscriptionId = session.SubscriptionId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;

        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Activated subscription for user {UserId}, customer {CustomerId}", userId, session.CustomerId);
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.SubscriptionId == null)
        {
            return;
        }

        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for Stripe subscription {SubscriptionId}", invoice.SubscriptionId);
            return;
        }

        subscription.Status = SubscriptionStatus.PastDue;
        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogWarning("Payment failed for subscription {SubscriptionId}, user {UserId}", invoice.SubscriptionId, subscription.UserId);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null)
        {
            return;
        }

        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscription.Id);
        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for Stripe subscription {SubscriptionId}", stripeSubscription.Id);
            return;
        }

        subscription.Status = SubscriptionStatus.Expired;
        subscription.CurrentPeriodEnd = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Subscription {SubscriptionId} deleted for user {UserId}", stripeSubscription.Id, subscription.UserId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null)
        {
            return;
        }

        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscription.Id);
        if (subscription == null)
        {
            _logger.LogDebug("No subscription found for Stripe subscription {SubscriptionId}", stripeSubscription.Id);
            return;
        }

        // Update status based on Stripe status
        subscription.Status = stripeSubscription.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Expired,
            _ => subscription.Status
        };

        subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Updated subscription {SubscriptionId} status to {Status}", stripeSubscription.Id, subscription.Status);
    }
}
