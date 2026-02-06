using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Services;

public class AccountManagementService : IAccountManagementService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IAuditService _auditService;
    private readonly IEncryptionService _encryptionService;

    public AccountManagementService(
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAuditService auditService,
        IEncryptionService encryptionService)
    {
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _auditService = auditService;
        _encryptionService = encryptionService;
    }

    public async Task<IReadOnlyList<AccountViewModel>> GetAccountsAsync(Guid userId, AccountCategory? category = null)
    {
        var accounts = category.HasValue
            ? await _accountRepository.GetByUserIdAndCategoryAsync(userId, category.Value)
            : await _accountRepository.GetByUserIdAsync(userId);

        return accounts.Select(a => new AccountViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            AccountType = a.AccountType,
            CurrentBalance = a.CurrentBalance,
            Institution = a.Institution,
            AccountNumber = _encryptionService.Decrypt(a.AccountNumber),
            IsActive = a.IsActive
        }).ToList();
    }

    public async Task<AccountDetailsResult?> GetAccountDetailsAsync(Guid userId, Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account == null || account.UserId != userId)
        {
            return null;
        }

        var balanceHistory = await _balanceHistoryRepository.GetByAccountIdAsync(accountId);

        return new AccountDetailsResult
        {
            Account = new AccountViewModel
            {
                Id = account.Id,
                Name = account.Name,
                Description = account.Description,
                AccountType = account.AccountType,
                CurrentBalance = account.CurrentBalance,
                Institution = account.Institution,
                AccountNumber = _encryptionService.Decrypt(account.AccountNumber),
                IsActive = account.IsActive
            },
            BalanceHistory = balanceHistory.Select(b => new BalanceHistoryViewModel
            {
                Id = b.Id,
                AccountId = b.AccountId,
                Balance = b.Balance,
                RecordedAt = b.RecordedAt,
                Notes = b.Notes
            }).ToList()
        };
    }

    public async Task<AccountCreateResult> CreateAccountAsync(Guid userId, AccountCreateViewModel model)
    {
        var existingAccounts = await _accountRepository.GetByUserIdAsync(userId);
        var isFirstAccount = !existingAccounts.Any();

        var account = new Account
        {
            Name = model.Name,
            Description = model.Description,
            AccountType = model.AccountType,
            CurrentBalance = model.CurrentBalance,
            Institution = model.Institution,
            AccountNumber = _encryptionService.Encrypt(model.AccountNumber),
            UserId = userId,
            IsActive = true
        };

        await _accountRepository.AddAsync(account);

        var balanceHistory = new BalanceHistory
        {
            AccountId = account.Id,
            Balance = model.CurrentBalance,
            RecordedAt = DateTime.UtcNow,
            Notes = "Initial balance"
        };

        await _balanceHistoryRepository.AddAsync(balanceHistory);

        // Audit log - account created
        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.AccountCreated,
            AuditEntityType.Account,
            account.Id,
            oldValue: null,
            newValue: new { account.Name, account.AccountType, account.CurrentBalance, account.Institution },
            description: $"Created account '{account.Name}' with initial balance {account.CurrentBalance:C}");

        return AccountCreateResult.Ok(account.Id, isFirstAccount);
    }

    public async Task<ServiceResult> UpdateAccountAsync(Guid userId, Guid accountId, AccountEditViewModel model)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account == null || account.UserId != userId)
        {
            return ServiceResult.NotFound();
        }

        // Capture old values for audit
        var oldValues = new
        {
            account.Name,
            account.AccountType,
            account.CurrentBalance,
            account.Institution,
            account.IsActive
        };

        var previousBalance = account.CurrentBalance;

        account.Name = model.Name;
        account.Description = model.Description;
        account.AccountType = model.AccountType;
        account.CurrentBalance = model.CurrentBalance;
        account.Institution = model.Institution;
        account.AccountNumber = _encryptionService.Encrypt(model.AccountNumber);
        account.IsActive = model.IsActive;

        await _accountRepository.UpdateAsync(account);

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

        // Audit log - account updated
        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.AccountUpdated,
            AuditEntityType.Account,
            account.Id,
            oldValue: oldValues,
            newValue: new { account.Name, account.AccountType, account.CurrentBalance, account.Institution, account.IsActive },
            description: $"Updated account '{account.Name}'");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAccountAsync(Guid userId, Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account == null || account.UserId != userId)
        {
            return ServiceResult.NotFound();
        }

        // Skip if already deleted
        if (account.IsDeleted)
        {
            return ServiceResult.NotFound();
        }

        // Capture for audit before soft delete
        var accountName = account.Name;
        var accountType = account.AccountType;
        var accountBalance = account.CurrentBalance;

        // Soft delete the account
        account.IsDeleted = true;
        account.DeletedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account);

        // Audit log - account deleted
        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.AccountDeleted,
            AuditEntityType.Account,
            accountId,
            oldValue: new { Name = accountName, AccountType = accountType, CurrentBalance = accountBalance },
            newValue: null,
            description: $"Deleted account '{accountName}'");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddBalanceRecordAsync(Guid userId, Guid accountId, decimal newBalance, string? notes, DateTime? recordedAt)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account == null || account.UserId != userId)
        {
            return ServiceResult.NotFound();
        }

        var previousBalance = account.CurrentBalance;
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

        // Audit log - balance updated
        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.BalanceUpdated,
            AuditEntityType.BalanceHistory,
            balanceHistory.Id,
            oldValue: new { Balance = previousBalance },
            newValue: new { Balance = newBalance, RecordedAt = effectiveDate },
            description: $"Added balance record for '{account.Name}': {previousBalance:C} → {newBalance:C}");

        return ServiceResult.Ok(accountId);
    }

    public async Task<BalanceHistoryEditViewModel?> GetBalanceRecordAsync(Guid userId, Guid historyId)
    {
        var history = await _balanceHistoryRepository.GetByIdAsync(historyId);

        if (history == null)
        {
            return null;
        }

        var account = await _accountRepository.GetByIdAsync(history.AccountId);
        if (account == null || account.UserId != userId)
        {
            return null;
        }

        return new BalanceHistoryEditViewModel
        {
            Id = history.Id,
            AccountId = history.AccountId,
            Balance = history.Balance,
            RecordedAt = history.RecordedAt,
            Notes = history.Notes
        };
    }

    public async Task<ServiceResult> UpdateBalanceRecordAsync(Guid userId, BalanceHistoryEditViewModel model)
    {
        var history = await _balanceHistoryRepository.GetByIdAsync(model.Id);

        if (history == null)
        {
            return ServiceResult.NotFound();
        }

        var account = await _accountRepository.GetByIdAsync(history.AccountId);
        if (account == null || account.UserId != userId)
        {
            return ServiceResult.NotFound();
        }

        // Capture old values for audit
        var oldValues = new { history.Balance, history.RecordedAt };

        history.Balance = model.Balance;
        history.RecordedAt = model.RecordedAt;
        history.Notes = model.Notes;

        await _balanceHistoryRepository.UpdateAsync(history);

        await UpdateAccountCurrentBalanceAsync(account);

        // Audit log - balance record updated
        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.BalanceUpdated,
            AuditEntityType.BalanceHistory,
            history.Id,
            oldValue: oldValues,
            newValue: new { history.Balance, history.RecordedAt },
            description: $"Updated balance record for '{account.Name}'");

        return ServiceResult.Ok(history.AccountId);
    }

    public async Task<ServiceResult> DeleteBalanceRecordAsync(Guid userId, Guid historyId)
    {
        var history = await _balanceHistoryRepository.GetByIdAsync(historyId);

        if (history == null)
        {
            return ServiceResult.NotFound();
        }

        var account = await _accountRepository.GetByIdAsync(history.AccountId);
        if (account == null || account.UserId != userId)
        {
            return ServiceResult.NotFound();
        }

        var accountId = history.AccountId;
        var deletedBalance = history.Balance;
        var deletedDate = history.RecordedAt;

        await _balanceHistoryRepository.DeleteAsync(history);

        await UpdateAccountCurrentBalanceAsync(account);

        // Audit log - balance record deleted
        await _auditService.LogEntityActionAsync(
            userId,
            AuditAction.BalanceRecordDeleted,
            AuditEntityType.BalanceHistory,
            historyId,
            oldValue: new { Balance = deletedBalance, RecordedAt = deletedDate },
            newValue: null,
            description: $"Deleted balance record for '{account.Name}': {deletedBalance:C} on {deletedDate:d}");

        return ServiceResult.Ok(accountId);
    }

    private async Task UpdateAccountCurrentBalanceAsync(Account account)
    {
        var allHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
        var latestHistory = allHistory.OrderByDescending(h => h.RecordedAt).FirstOrDefault();

        account.CurrentBalance = latestHistory?.Balance ?? 0;
        await _accountRepository.UpdateAsync(account);
    }
}
