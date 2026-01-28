using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Web.Models;

namespace NetWorthTracker.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IStripeService _stripeService;
    private readonly NHibernate.ISession _session;

    public HomeController(ILogger<HomeController> logger, IStripeService stripeService, NHibernate.ISession session)
    {
        _logger = logger;
        _stripeService = stripeService;
        _session = session;
    }

    public async Task<IActionResult> Index()
    {
        // Show user count on landing page when in SaaS mode with 75+ users
        if (_stripeService.IsConfigured)
        {
            var userCount = await _session.QueryOver<ApplicationUser>()
                .RowCountAsync();

            if (userCount >= 75)
            {
                ViewBag.UserCount = userCount;
            }
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult Pricing()
    {
        // Only show pricing page when running as SaaS (Stripe configured)
        if (!_stripeService.IsConfigured)
        {
            return NotFound();
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
