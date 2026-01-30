using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class AccountsController : Controller
{
    private readonly IAccountManagementService _accountService;
    private readonly IExportService _exportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsController(
        IAccountManagementService accountService,
        IExportService exportService,
        UserManager<ApplicationUser> userManager)
    {
        _accountService = accountService;
        _exportService = exportService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(AccountCategory? category = null)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var viewModels = await _accountService.GetAccountsAsync(userId, category);

        ViewBag.CurrentCategory = category;
        return View(viewModels);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.GetAccountDetailsAsync(userId, id);

        if (result == null)
        {
            return NotFound();
        }

        ViewBag.BalanceHistory = result.BalanceHistory;
        return View(result.Account);
    }

    public IActionResult Create()
    {
        return View(new AccountCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-create")]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.CreateAccountAsync(userId, model);

        if (result.IsFirstAccount)
        {
            return RedirectToAction("Index", "Dashboard", new { firstAccount = true });
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var details = await _accountService.GetAccountDetailsAsync(userId, id);

        if (details == null)
        {
            return NotFound();
        }

        var viewModel = new AccountEditViewModel
        {
            Id = details.Account.Id,
            Name = details.Account.Name,
            Description = details.Account.Description,
            AccountType = details.Account.AccountType,
            CurrentBalance = details.Account.CurrentBalance,
            Institution = details.Account.Institution,
            AccountNumber = details.Account.AccountNumber,
            IsActive = details.Account.IsActive
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-update")]
    public async Task<IActionResult> Edit(Guid id, AccountEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.UpdateAccountAsync(userId, id, model);

        if (!result.Success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var details = await _accountService.GetAccountDetailsAsync(userId, id);

        if (details == null)
        {
            return NotFound();
        }

        return View(details.Account);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-update")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.DeleteAccountAsync(userId, id);

        if (!result.Success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-update")]
    public async Task<IActionResult> UpdateBalance(Guid accountId, decimal newBalance, string? notes, DateTime? recordedAt)
    {
        // Validate balance range
        const decimal minBalance = -999999999999.99m;
        const decimal maxBalance = 999999999999.99m;
        if (newBalance < minBalance || newBalance > maxBalance)
        {
            ModelState.AddModelError("newBalance", $"Balance must be between {minBalance:N2} and {maxBalance:N2}");
            return BadRequest(ModelState);
        }

        // Validate notes length
        if (notes?.Length > 1000)
        {
            ModelState.AddModelError("notes", "Notes cannot exceed 1000 characters");
            return BadRequest(ModelState);
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.AddBalanceRecordAsync(userId, accountId, newBalance, notes, recordedAt);

        if (!result.Success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { id = accountId });
    }

    [HttpGet]
    public async Task<IActionResult> GetBalanceHistory(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var history = await _accountService.GetBalanceRecordAsync(userId, id);

        if (history == null)
        {
            return NotFound();
        }

        return Json(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBalanceHistory(BalanceHistoryEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.UpdateBalanceRecordAsync(userId, model);

        if (!result.Success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { id = result.RelatedId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBalanceHistory(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _accountService.DeleteBalanceRecordAsync(userId, id);

        if (!result.Success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { id = result.RelatedId });
    }

    [HttpGet]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportAccountsCsv(AccountCategory? category = null)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _exportService.ExportAccountsCsvAsync(userId, category);

        if (!result.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        return File(Encoding.UTF8.GetBytes(result.Content!), result.ContentType, result.FileName);
    }

    [HttpGet]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportAccountHistoryCsv(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var result = await _exportService.ExportAccountHistoryCsvAsync(userId, id);

        if (!result.Success)
        {
            return NotFound();
        }

        return File(Encoding.UTF8.GetBytes(result.Content!), result.ContentType, result.FileName);
    }
}
