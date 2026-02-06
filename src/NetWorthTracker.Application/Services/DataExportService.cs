using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Application.Services;

public class DataExportService : IDataExportService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IAlertConfigurationRepository _alertConfigurationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DataExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DataExportService(
        UserManager<ApplicationUser> userManager,
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAlertConfigurationRepository alertConfigurationRepository,
        IAuditLogRepository auditLogRepository,
        IEncryptionService encryptionService,
        IAuditService auditService,
        ILogger<DataExportService> logger)
    {
        _userManager = userManager;
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _alertConfigurationRepository = alertConfigurationRepository;
        _auditLogRepository = auditLogRepository;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<DataExportResult> ExportAllUserDataAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return DataExportResult.Error("User not found");
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Export profile data
                await AddJsonToArchive(archive, "profile.json", CreateProfileExport(user));

                // Export accounts (with decrypted account numbers)
                var accounts = await _accountRepository.GetByUserIdAsync(userId);
                await AddJsonToArchive(archive, "accounts.json", CreateAccountsExport(accounts));

                // Export balance history
                var balanceHistory = new List<BalanceHistory>();
                foreach (var account in accounts)
                {
                    var history = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
                    balanceHistory.AddRange(history);
                }
                await AddJsonToArchive(archive, "balance-history.json", CreateBalanceHistoryExport(balanceHistory, accounts));

                // Export alert settings
                var alertConfig = await _alertConfigurationRepository.GetByUserIdAsync(userId);
                await AddJsonToArchive(archive, "settings.json", CreateSettingsExport(alertConfig));

                // Export audit log
                var auditLogs = await _auditLogRepository.GetByUserIdAsync(userId, limit: 10000);
                await AddJsonToArchive(archive, "audit-log.json", CreateAuditLogExport(auditLogs));
            }

            memoryStream.Position = 0;
            var zipContent = memoryStream.ToArray();

            var fileName = $"net-worth-tracker-export-{DateTime.UtcNow:yyyy-MM-dd}.zip";

            await _auditService.LogExportAsync(userId, "GDPR", $"Full data export downloaded: {fileName}");

            _logger.LogInformation("User {UserId} exported all their data", userId);

            return DataExportResult.Ok(zipContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data for user {UserId}", userId);
            return DataExportResult.Error("An error occurred while exporting your data. Please try again.");
        }
    }

    private static async Task AddJsonToArchive(ZipArchive archive, string fileName, object data)
    {
        var entry = archive.CreateEntry(fileName);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await writer.WriteAsync(json);
    }

    private static object CreateProfileExport(ApplicationUser user)
    {
        return new
        {
            exportedAt = DateTime.UtcNow,
            profile = new
            {
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                locale = user.Locale,
                timeZone = user.TimeZone,
                emailConfirmed = user.EmailConfirmed,
                twoFactorEnabled = user.TwoFactorEnabled,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            }
        };
    }

    private object CreateAccountsExport(IEnumerable<Account> accounts)
    {
        return new
        {
            exportedAt = DateTime.UtcNow,
            accounts = accounts.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                description = a.Description,
                accountType = a.AccountType.ToString(),
                currentBalance = a.CurrentBalance,
                institution = a.Institution,
                accountNumber = _encryptionService.Decrypt(a.AccountNumber), // Decrypt for export
                isActive = a.IsActive,
                createdAt = a.CreatedAt,
                updatedAt = a.UpdatedAt
            })
        };
    }

    private static object CreateBalanceHistoryExport(IEnumerable<BalanceHistory> balanceHistory, IEnumerable<Account> accounts)
    {
        var accountNames = accounts.ToDictionary(a => a.Id, a => a.Name);

        return new
        {
            exportedAt = DateTime.UtcNow,
            balanceHistory = balanceHistory.Select(b => new
            {
                id = b.Id,
                accountId = b.AccountId,
                accountName = accountNames.TryGetValue(b.AccountId, out var name) ? name : "Unknown",
                balance = b.Balance,
                notes = b.Notes,
                recordedAt = b.RecordedAt,
                createdAt = b.CreatedAt
            }).OrderByDescending(b => b.recordedAt)
        };
    }

    private static object CreateSettingsExport(AlertConfiguration? alertConfig)
    {
        if (alertConfig == null)
        {
            return new
            {
                exportedAt = DateTime.UtcNow,
                alertSettings = (object?)null
            };
        }

        return new
        {
            exportedAt = DateTime.UtcNow,
            alertSettings = new
            {
                alertsEnabled = alertConfig.AlertsEnabled,
                netWorthChangeThreshold = alertConfig.NetWorthChangeThreshold,
                cashRunwayMonths = alertConfig.CashRunwayMonths,
                monthlySnapshotEnabled = alertConfig.MonthlySnapshotEnabled,
                lastNetWorthAlertSentAt = alertConfig.LastNetWorthAlertSentAt,
                lastCashRunwayAlertSentAt = alertConfig.LastCashRunwayAlertSentAt,
                lastMonthlySnapshotSentAt = alertConfig.LastMonthlySnapshotSentAt
            }
        };
    }

    private static object CreateAuditLogExport(IEnumerable<AuditLog> auditLogs)
    {
        return new
        {
            exportedAt = DateTime.UtcNow,
            auditLog = auditLogs.Select(a => new
            {
                timestamp = a.Timestamp,
                action = a.Action,
                entityType = a.EntityType,
                description = a.Description,
                success = a.Success,
                ipAddress = a.IpAddress
            }).OrderByDescending(a => a.timestamp)
        };
    }
}
