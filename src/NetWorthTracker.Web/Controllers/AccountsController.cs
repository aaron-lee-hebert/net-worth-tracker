using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class AccountsController : Controller
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsController(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        UserManager<ApplicationUser> userManager)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(AccountCategory? category = null)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        var accounts = category.HasValue
            ? await _accountRepository.GetByUserIdAndCategoryAsync(userId, category.Value)
            : await _accountRepository.GetByUserIdAsync(userId);

        var viewModels = accounts.Select(a => new AccountViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            AccountType = a.AccountType,
            CurrentBalance = a.CurrentBalance,
            Institution = a.Institution,
            AccountNumber = a.AccountNumber,
            IsActive = a.IsActive
        }).ToList();

        ViewBag.CurrentCategory = category;
        return View(viewModels);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var balanceHistory = await _balanceHistoryRepository.GetByAccountIdAsync(id);

        var viewModel = new AccountViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Description = account.Description,
            AccountType = account.AccountType,
            CurrentBalance = account.CurrentBalance,
            Institution = account.Institution,
            AccountNumber = account.AccountNumber,
            IsActive = account.IsActive
        };

        ViewBag.BalanceHistory = balanceHistory.Select(b => new BalanceHistoryViewModel
        {
            Id = b.Id,
            AccountId = b.AccountId,
            Balance = b.Balance,
            RecordedAt = b.RecordedAt,
            Notes = b.Notes
        }).ToList();

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new AccountCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        // Check if this is the user's first account
        var existingAccounts = await _accountRepository.GetByUserIdAsync(userId);
        var isFirstAccount = !existingAccounts.Any();

        var account = new Account
        {
            Name = model.Name,
            Description = model.Description,
            AccountType = model.AccountType,
            CurrentBalance = model.CurrentBalance,
            Institution = model.Institution,
            AccountNumber = model.AccountNumber,
            UserId = userId,
            IsActive = true
        };

        await _accountRepository.AddAsync(account);

        // Record initial balance in history
        var balanceHistory = new BalanceHistory
        {
            AccountId = account.Id,
            Balance = model.CurrentBalance,
            RecordedAt = DateTime.UtcNow,
            Notes = "Initial balance"
        };

        await _balanceHistoryRepository.AddAsync(balanceHistory);

        // Redirect to Dashboard for first account to show immediate insight
        if (isFirstAccount)
        {
            return RedirectToAction("Index", "Dashboard", new { firstAccount = true });
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var viewModel = new AccountEditViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Description = account.Description,
            AccountType = account.AccountType,
            CurrentBalance = account.CurrentBalance,
            Institution = account.Institution,
            AccountNumber = account.AccountNumber,
            IsActive = account.IsActive
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var previousBalance = account.CurrentBalance;

        account.Name = model.Name;
        account.Description = model.Description;
        account.AccountType = model.AccountType;
        account.CurrentBalance = model.CurrentBalance;
        account.Institution = model.Institution;
        account.AccountNumber = model.AccountNumber;
        account.IsActive = model.IsActive;

        await _accountRepository.UpdateAsync(account);

        // Record balance change in history if balance changed
        if (previousBalance != model.CurrentBalance)
        {
            var balanceHistory = new BalanceHistory
            {
                AccountId = account.Id,
                Balance = model.CurrentBalance,
                RecordedAt = DateTime.UtcNow,
                Notes = "Balance updated"
            };

            await _balanceHistoryRepository.AddAsync(balanceHistory);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var viewModel = new AccountViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Description = account.Description,
            AccountType = account.AccountType,
            CurrentBalance = account.CurrentBalance,
            Institution = account.Institution,
            AccountNumber = account.AccountNumber,
            IsActive = account.IsActive
        };

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        await _accountRepository.DeleteAsync(account);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBalance(Guid accountId, decimal newBalance, string? notes, DateTime? recordedAt)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var effectiveDate = recordedAt ?? DateTime.UtcNow;

        var balanceHistory = new BalanceHistory
        {
            AccountId = accountId,
            Balance = newBalance,
            RecordedAt = effectiveDate,
            Notes = notes
        };

        await _balanceHistoryRepository.AddAsync(balanceHistory);

        await UpdateAccountCurrentBalanceAsync(account);

        return RedirectToAction(nameof(Details), new { id = accountId });
    }

    [HttpGet]
    public async Task<IActionResult> GetBalanceHistory(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var history = await _balanceHistoryRepository.GetByIdAsync(id);

        if (history == null)
        {
            return NotFound();
        }

        var account = await _accountRepository.GetByIdAsync(history.AccountId);
        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        return Json(new BalanceHistoryEditViewModel
        {
            Id = history.Id,
            AccountId = history.AccountId,
            Balance = history.Balance,
            RecordedAt = history.RecordedAt,
            Notes = history.Notes
        });
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
        var history = await _balanceHistoryRepository.GetByIdAsync(model.Id);

        if (history == null)
        {
            return NotFound();
        }

        var account = await _accountRepository.GetByIdAsync(history.AccountId);
        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        history.Balance = model.Balance;
        history.RecordedAt = model.RecordedAt;
        history.Notes = model.Notes;

        await _balanceHistoryRepository.UpdateAsync(history);

        await UpdateAccountCurrentBalanceAsync(account);

        return RedirectToAction(nameof(Details), new { id = history.AccountId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBalanceHistory(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var history = await _balanceHistoryRepository.GetByIdAsync(id);

        if (history == null)
        {
            return NotFound();
        }

        var account = await _accountRepository.GetByIdAsync(history.AccountId);
        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var accountId = history.AccountId;

        await _balanceHistoryRepository.DeleteAsync(history);

        await UpdateAccountCurrentBalanceAsync(account);

        return RedirectToAction(nameof(Details), new { id = accountId });
    }

    private async Task UpdateAccountCurrentBalanceAsync(Account account)
    {
        var allHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
        var latestHistory = allHistory.OrderByDescending(h => h.RecordedAt).FirstOrDefault();

        account.CurrentBalance = latestHistory?.Balance ?? 0;
        await _accountRepository.UpdateAsync(account);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAccountsCsv(AccountCategory? category = null)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        var accounts = category.HasValue
            ? await _accountRepository.GetByUserIdAndCategoryAsync(userId, category.Value)
            : await _accountRepository.GetByUserIdAsync(userId);

        var csv = GenerateAccountsCsv(accounts.ToList());
        var categoryName = category.HasValue ? $"-{category.Value.ToString().ToLower()}" : "";
        var fileName = $"accounts{categoryName}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAccountHistoryCsv(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null || account.UserId != userId)
        {
            return NotFound();
        }

        var history = await _balanceHistoryRepository.GetByAccountIdAsync(id);
        var csv = GenerateAccountHistoryCsv(account, history.ToList());
        var safeName = account.Name.Replace(" ", "-").ToLower();
        var fileName = $"account-history-{safeName}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    private static string GenerateAccountsCsv(List<Account> accounts)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Name,Type,Category,Institution,Account Number,Balance,Status");

        // Group by category
        var orderedAccounts = accounts
            .OrderBy(a => a.AccountType.IsLiability())
            .ThenBy(a => a.AccountType.GetCategory())
            .ThenBy(a => a.Name);

        decimal totalAssets = 0;
        decimal totalLiabilities = 0;

        foreach (var account in orderedAccounts)
        {
            var maskedAccountNum = string.IsNullOrEmpty(account.AccountNumber)
                ? ""
                : "****" + account.AccountNumber[^Math.Min(4, account.AccountNumber.Length)..];

            sb.AppendLine($"\"{EscapeCsv(account.Name)}\",\"{account.AccountType.GetDisplayName()}\",\"{account.AccountType.GetCategory().GetDisplayName()}\",\"{EscapeCsv(account.Institution ?? "")}\",\"{maskedAccountNum}\",{account.CurrentBalance:F2},{(account.IsActive ? "Active" : "Inactive")}");

            if (account.IsActive)
            {
                if (account.AccountType.IsLiability())
                    totalLiabilities += account.CurrentBalance;
                else
                    totalAssets += account.CurrentBalance;
            }
        }

        // Summary rows
        sb.AppendLine();
        sb.AppendLine($"Total Assets,,,,,{totalAssets:F2},");
        sb.AppendLine($"Total Liabilities,,,,,{totalLiabilities:F2},");
        sb.AppendLine($"Net Worth,,,,,{totalAssets - totalLiabilities:F2},");

        return sb.ToString();
    }

    private static string GenerateAccountHistoryCsv(Account account, List<BalanceHistory> history)
    {
        var sb = new StringBuilder();

        // Header with account info
        sb.AppendLine($"Account: {account.Name}");
        sb.AppendLine($"Type: {account.AccountType.GetDisplayName()}");
        sb.AppendLine($"Institution: {account.Institution ?? "N/A"}");
        sb.AppendLine();

        // Column headers
        sb.AppendLine("Date,Balance,Change,% Change,Notes");

        var orderedHistory = history.OrderBy(h => h.RecordedAt).ToList();
        decimal? previousBalance = null;

        foreach (var entry in orderedHistory)
        {
            var change = previousBalance.HasValue ? entry.Balance - previousBalance.Value : (decimal?)null;
            var percentChange = previousBalance.HasValue && previousBalance.Value != 0
                ? (change / Math.Abs(previousBalance.Value)) * 100
                : (decimal?)null;

            sb.AppendLine($"{entry.RecordedAt:yyyy-MM-dd},{entry.Balance:F2},{change?.ToString("F2") ?? ""},{percentChange?.ToString("F2") ?? ""},{EscapeCsv(entry.Notes ?? "")}");

            previousBalance = entry.Balance;
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}
