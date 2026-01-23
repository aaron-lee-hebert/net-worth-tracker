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
}
