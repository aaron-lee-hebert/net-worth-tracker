using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Infrastructure.Services;

namespace NetWorthTracker.Web.Controllers.Api;

[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ISubscriptionService subscriptionService,
        IOptions<StripeSettings> stripeSettings,
        ILogger<StripeWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        string json;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            json = await reader.ReadToEndAsync();
        }

        // TODO: Verify webhook signature using _stripeSettings.WebhookSecret
        // var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _stripeSettings.WebhookSecret);

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var eventType = root.GetProperty("type").GetString();

            _logger.LogInformation("Received Stripe webhook: {EventType}", eventType);

            switch (eventType)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(root);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaid(root);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(root);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(root);
                    break;

                default:
                    _logger.LogDebug("Unhandled Stripe event type: {EventType}", eventType);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest();
        }
    }

    private Task HandleCheckoutSessionCompleted(JsonElement root)
    {
        // TODO: Extract session data and create/update subscription
        // var dataObject = root.GetProperty("data").GetProperty("object");
        // var customerId = dataObject.GetProperty("customer").GetString();
        // var subscriptionId = dataObject.GetProperty("subscription").GetString();
        // var userId = extract from metadata
        // await _subscriptionService.CreateOrUpdateFromStripeAsync(...)

        _logger.LogInformation("Checkout session completed - stub handler");
        return Task.CompletedTask;
    }

    private Task HandleInvoicePaid(JsonElement root)
    {
        // TODO: Update subscription period dates
        // var dataObject = root.GetProperty("data").GetProperty("object");
        // var subscriptionId = dataObject.GetProperty("subscription").GetString();

        _logger.LogInformation("Invoice paid - stub handler");
        return Task.CompletedTask;
    }

    private async Task HandleSubscriptionUpdated(JsonElement root)
    {
        // TODO: Parse full subscription object from Stripe SDK
        // For now, extract status and update
        try
        {
            var dataObject = root.GetProperty("data").GetProperty("object");
            var subscriptionId = dataObject.GetProperty("id").GetString();
            var statusString = dataObject.GetProperty("status").GetString();

            if (subscriptionId != null && statusString != null)
            {
                var status = MapStripeStatus(statusString);
                await _subscriptionService.UpdateStatusAsync(subscriptionId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription updated webhook");
        }
    }

    private async Task HandleSubscriptionDeleted(JsonElement root)
    {
        try
        {
            var dataObject = root.GetProperty("data").GetProperty("object");
            var subscriptionId = dataObject.GetProperty("id").GetString();

            if (subscriptionId != null)
            {
                await _subscriptionService.UpdateStatusAsync(subscriptionId, SubscriptionStatus.Canceled);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription deleted webhook");
        }
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.Expired,
            "unpaid" => SubscriptionStatus.Unpaid,
            _ => SubscriptionStatus.Canceled
        };
    }
}
