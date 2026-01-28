using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IStripeService stripeService, ILogger<WebhookController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature");
            return BadRequest("Missing Stripe-Signature header");
        }

        try
        {
            await _stripeService.ProcessWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Stripe webhook");
            return BadRequest("Webhook processing failed");
        }
    }
}
