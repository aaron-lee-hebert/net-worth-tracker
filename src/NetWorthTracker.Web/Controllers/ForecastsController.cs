using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class ForecastsController : Controller
{
    private readonly IForecastService _forecastService;
    private readonly UserManager<ApplicationUser> _userManager;

    private const int DefaultForecastMonths = 60;

    public ForecastsController(
        IForecastService forecastService,
        UserManager<ApplicationUser> userManager)
    {
        _forecastService = forecastService;
        _userManager = userManager;
    }

    public IActionResult Index(int forecastMonths = DefaultForecastMonths)
    {
        ViewBag.ForecastMonths = forecastMonths;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetForecastData(int forecastMonths = DefaultForecastMonths)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var viewModel = await _forecastService.GetForecastDataAsync(userId, forecastMonths);
        return Json(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetAssumptions()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var viewModel = await _forecastService.GetAssumptionsAsync(userId);
        return Json(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssumptions([FromBody] ForecastAssumptionsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = string.Join("; ", errors) });
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        await _forecastService.SaveAssumptionsAsync(userId, model);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAssumptions()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        await _forecastService.ResetAssumptionsAsync(userId);
        return Json(new { success = true });
    }
}
