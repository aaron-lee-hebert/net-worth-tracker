using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        IReportService reportService,
        IExportService exportService,
        UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _exportService = exportService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Quarterly()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var viewModel = await _reportService.BuildQuarterlyReportAsync(userId);
        return View(viewModel);
    }

    [HttpGet]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> DownloadCsv()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _exportService.ExportQuarterlyReportCsvAsync(userId);

        if (!result.Success)
        {
            return RedirectToAction(nameof(Quarterly));
        }

        return File(Encoding.UTF8.GetBytes(result.Content!), result.ContentType, result.FileName);
    }

    [HttpGet]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> DownloadNetWorthHistoryCsv()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _exportService.ExportNetWorthHistoryCsvAsync(userId);

        if (!result.Success)
        {
            return RedirectToAction(nameof(Quarterly));
        }

        return File(Encoding.UTF8.GetBytes(result.Content!), result.ContentType, result.FileName);
    }
}
