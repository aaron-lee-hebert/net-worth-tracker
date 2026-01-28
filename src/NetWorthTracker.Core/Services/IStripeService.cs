namespace NetWorthTracker.Core.Services;

public interface IStripeService
{
    /// <summary>
    /// Returns true if Stripe is configured with API keys
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Creates a Stripe Checkout session for subscription purchase
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="email">The user's email</param>
    /// <param name="successUrl">URL to redirect to on success</param>
    /// <param name="cancelUrl">URL to redirect to on cancel</param>
    /// <returns>The checkout session URL</returns>
    Task<string> CreateCheckoutSessionAsync(Guid userId, string email, string successUrl, string cancelUrl);

    /// <summary>
    /// Creates a Stripe Customer Portal session for managing subscription
    /// </summary>
    /// <param name="stripeCustomerId">The Stripe customer ID</param>
    /// <param name="returnUrl">URL to return to after portal</param>
    /// <returns>The portal session URL</returns>
    Task<string> CreateCustomerPortalSessionAsync(string stripeCustomerId, string returnUrl);

    /// <summary>
    /// Processes a Stripe webhook event
    /// </summary>
    /// <param name="json">The webhook payload JSON</param>
    /// <param name="signature">The Stripe-Signature header</param>
    Task ProcessWebhookAsync(string json, string signature);
}
