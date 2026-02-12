using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly ISubscriptionService _subscriptionService;

    public BillingController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var subscription = await _subscriptionService.GetByUserIdAsync(userId);

        var model = new BillingViewModel
        {
            HasSubscription = subscription != null,
            Status = subscription?.Status,
            CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
            StripePriceId = subscription?.StripePriceId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateCheckoutSession()
    {
        // TODO: Integrate with Stripe SDK to create a Checkout Session
        // var session = await stripeClient.CreateCheckoutSession(...)
        // return Redirect(session.Url);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePortalSession()
    {
        // TODO: Integrate with Stripe SDK to create a Billing Portal Session
        // var session = await stripeClient.CreatePortalSession(...)
        // return Redirect(session.Url);

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Success()
    {
        return View();
    }

    public IActionResult Cancel()
    {
        return View();
    }
}
