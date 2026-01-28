using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        UserManager<ApplicationUser> userManager,
        ISubscriptionRepository subscriptionRepository,
        IStripeService stripeService,
        ILogger<SubscriptionController> logger)
    {
        _userManager = userManager;
        _subscriptionRepository = subscriptionRepository;
        _stripeService = stripeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var subscription = await _subscriptionRepository.GetByUserIdAsync(user.Id);

        ViewBag.StripeConfigured = _stripeService.IsConfigured;
        return View(subscription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe()
    {
        if (!_stripeService.IsConfigured)
        {
            TempData["Error"] = "Payment processing is not configured.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var successUrl = Url.Action(nameof(Success), "Subscription", null, Request.Scheme)!;
            var cancelUrl = Url.Action(nameof(Index), "Subscription", null, Request.Scheme)!;

            var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(
                user.Id,
                user.Email!,
                successUrl,
                cancelUrl);

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for user {UserId}", user.Id);
            TempData["Error"] = "Unable to start checkout. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Success()
    {
        TempData["Success"] = "Thank you for subscribing! Your subscription is now active.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageSubscription()
    {
        if (!_stripeService.IsConfigured)
        {
            TempData["Error"] = "Payment processing is not configured.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var subscription = await _subscriptionRepository.GetByUserIdAsync(user.Id);
        if (subscription?.StripeCustomerId == null)
        {
            TempData["Error"] = "No active subscription found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var returnUrl = Url.Action(nameof(Index), "Subscription", null, Request.Scheme)!;
            var portalUrl = await _stripeService.CreateCustomerPortalSessionAsync(
                subscription.StripeCustomerId,
                returnUrl);

            return Redirect(portalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer portal session for user {UserId}", user.Id);
            TempData["Error"] = "Unable to open subscription management. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
}
