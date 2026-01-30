using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Data;

/// <summary>
/// Migrates existing plaintext account numbers to encrypted format.
/// </summary>
public class AccountNumberMigrator
{
    private readonly ISession _session;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<AccountNumberMigrator> _logger;

    public AccountNumberMigrator(
        ISession session,
        IEncryptionService encryptionService,
        ILogger<AccountNumberMigrator> logger)
    {
        _session = session;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Migrates all existing plaintext account numbers to encrypted format.
    /// </summary>
    /// <returns>The number of accounts migrated.</returns>
    public async Task<int> MigrateAsync()
    {
        _logger.LogInformation("Starting account number encryption migration...");

        // Get all accounts with non-null, non-empty account numbers
        var accounts = await _session.Query<Account>()
            .Where(a => a.AccountNumber != null && a.AccountNumber != "")
            .ToListAsync();

        var migratedCount = 0;

        foreach (var account in accounts)
        {
            // Skip if already encrypted
            if (_encryptionService.IsEncrypted(account.AccountNumber))
            {
                _logger.LogDebug("Account {AccountId} already encrypted, skipping", account.Id);
                continue;
            }

            try
            {
                var encrypted = _encryptionService.Encrypt(account.AccountNumber);
                account.AccountNumber = encrypted;
                account.UpdatedAt = DateTime.UtcNow;
                await _session.SaveOrUpdateAsync(account);
                migratedCount++;

                _logger.LogDebug("Encrypted account number for account {AccountId}", account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt account number for account {AccountId}", account.Id);
            }
        }

        await _session.FlushAsync();

        _logger.LogInformation("Account number encryption migration complete. Migrated {Count} accounts.", migratedCount);

        return migratedCount;
    }
}
