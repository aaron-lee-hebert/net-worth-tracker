using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.ViewModels;
using NetWorthTracker.Web.Authorization;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
[AdminOnly]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(IAdminService adminService, UserManager<ApplicationUser> userManager)
    {
        _adminService = adminService;
        _userManager = userManager;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var dashboard = await _adminService.GetDashboardMetricsAsync();
        return View(dashboard);
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users(int page = 1, string? search = null)
    {
        var result = await _adminService.GetUsersAsync(page, 20, search);
        ViewBag.Search = search;
        return View(result);
    }

    // GET: /Admin/UserDetails/{id}
    public async Task<IActionResult> UserDetails(Guid id)
    {
        var user = await _adminService.GetUserDetailsAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // POST: /Admin/SetAdminStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAdminStatus(Guid id, bool isAdmin)
    {
        var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _adminService.SetAdminStatusAsync(currentUserId, id, isAdmin);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = isAdmin
                ? "Admin access granted successfully."
                : "Admin access revoked successfully.";
        }

        return RedirectToAction(nameof(UserDetails), new { id });
    }

    // GET: /Admin/AuditLogs
    public async Task<IActionResult> AuditLogs(
        int page = 1,
        string? action = null,
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        Guid? userId = null)
    {
        var filter = new AuditLogFilter
        {
            Action = action,
            EntityType = entityType,
            UserId = userId,
            From = from,
            To = to
        };

        var result = await _adminService.GetAuditLogsAsync(page, 50, filter);

        ViewBag.ActionFilter = action;
        ViewBag.EntityTypeFilter = entityType;
        ViewBag.FromFilter = from;
        ViewBag.ToFilter = to;
        ViewBag.UserIdFilter = userId;

        return View(result);
    }

    // GET: /Admin/ExportAuditLogs
    public async Task<IActionResult> ExportAuditLogs(
        string? action = null,
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var filter = new AuditLogFilter
        {
            Action = action,
            EntityType = entityType,
            From = from,
            To = to
        };

        var csv = await _adminService.ExportAuditLogsCsvAsync(filter);
        var fileName = $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    // GET: /Admin/Subscriptions
    public async Task<IActionResult> Subscriptions(int page = 1, SubscriptionStatus? status = null)
    {
        var result = await _adminService.GetSubscriptionsAsync(page, 20, status);
        ViewBag.StatusFilter = status;
        return View(result);
    }

    // GET: /Admin/Analytics
    public async Task<IActionResult> Analytics()
    {
        var analytics = await _adminService.GetSubscriptionAnalyticsAsync();
        return View(analytics);
    }
}
