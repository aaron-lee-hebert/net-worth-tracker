using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Application.Interfaces;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IAlertService _alertService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IDataExportService _dataExportService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAlertService alertService,
        IEmailService emailService,
        IAuditService auditService,
        IDataExportService dataExportService,
        ISubscriptionService subscriptionService,
        ILogger<SettingsController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _alertService = alertService;
        _emailService = emailService;
        _auditService = auditService;
        _dataExportService = dataExportService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var alertConfig = await _alertService.GetOrCreateConfigurationAsync(user.Id);
        ViewBag.AlertConfig = alertConfig;
        ViewBag.EmailConfigured = _emailService.IsConfigured;
        ViewBag.SupportedLocales = SupportedLocales.Locales.Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = l.Key,
            Text = l.Value,
            Selected = l.Key == user.Locale
        });
        ViewBag.SupportedTimeZones = GetTimeZoneSelectList(user.TimeZone);

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlertSettings(
        bool alertsEnabled,
        decimal netWorthChangeThreshold,
        int cashRunwayMonths,
        bool monthlySnapshotEnabled)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var config = await _alertService.GetOrCreateConfigurationAsync(user.Id);

        config.AlertsEnabled = alertsEnabled;
        config.NetWorthChangeThreshold = Math.Max(0, Math.Min(100, netWorthChangeThreshold));
        config.CashRunwayMonths = Math.Max(0, Math.Min(24, cashRunwayMonths));
        config.MonthlySnapshotEnabled = monthlySnapshotEnabled;

        await _alertService.UpdateConfigurationAsync(config);

        // Audit log - alert settings updated
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.AlertSettingsUpdated,
            EntityType = AuditEntityType.AlertConfiguration,
            EntityId = config.Id,
            NewValue = new { alertsEnabled, netWorthChangeThreshold, cashRunwayMonths, monthlySnapshotEnabled },
            Description = $"Alert settings updated"
        });

        _logger.LogInformation("Updated alert settings for user {UserId}", user.Id);
        TempData["SuccessMessage"] = "Notification settings updated successfully.";
        TempData["ActiveTab"] = "notifications";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllData()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        try
        {
            // Get all accounts for the user
            var accounts = await _accountRepository.GetByUserIdAsync(user.Id);

            // Delete all balance history for each account
            foreach (var account in accounts)
            {
                var balanceHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
                foreach (var history in balanceHistory)
                {
                    await _balanceHistoryRepository.DeleteAsync(history);
                }
                await _accountRepository.DeleteAsync(account);
            }

            // Audit log - all data deleted
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.AllDataDeleted,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = $"All financial data deleted for user {user.Email}"
            });

            _logger.LogInformation("Deleted all data for user {UserId}", user.Id);
            TempData["SuccessMessage"] = "All your financial data has been deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "An error occurred while deleting your data. Please try again.";
        }

        TempData["ActiveTab"] = "data";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        try
        {
            // Delete all accounts and balance history
            var accounts = await _accountRepository.GetByUserIdAsync(user.Id);
            foreach (var account in accounts)
            {
                var balanceHistory = await _balanceHistoryRepository.GetByAccountIdAsync(account.Id);
                foreach (var history in balanceHistory)
                {
                    await _balanceHistoryRepository.DeleteAsync(history);
                }
                await _accountRepository.DeleteAsync(account);
            }

            // Cancel any active subscription
            await _subscriptionService.CancelByUserIdAsync(user.Id);

            // Audit log - account deleted (capture before sign out)
            var userEmail = user.Email;
            var userId = user.Id;
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = userId,
                Action = AuditAction.UserDeleted,
                EntityType = AuditEntityType.User,
                EntityId = userId,
                Description = $"User account deleted: {userEmail}"
            });

            // Sign out the user
            await _signInManager.SignOutAsync();

            // Delete the user account
            await _userManager.DeleteAsync(user);

            _logger.LogInformation("Deleted account for user {UserId}", userId);

            TempData["SuccessMessage"] = "Your account and all data have been permanently deleted.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "An error occurred while deleting your account. Please try again.";
            TempData["ActiveTab"] = "data";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string locale, string timeZone)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Capture old values for audit
        var oldValues = new { user.FirstName, user.LastName, user.Locale, user.TimeZone };

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Locale = SupportedLocales.IsSupported(locale) ? locale : "en-US";
        user.TimeZone = SupportedTimeZones.IsSupported(timeZone) ? timeZone : "America/New_York";
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        // Audit log - profile updated
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.ProfileUpdated,
            EntityType = AuditEntityType.User,
            EntityId = user.Id,
            OldValue = oldValues,
            NewValue = new { firstName, lastName, locale, timeZone },
            Description = "Profile updated"
        });

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            TempData["ErrorMessage"] = "Current password and new password are required.";
            TempData["ActiveTab"] = "security";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "New password and confirmation do not match.";
            TempData["ActiveTab"] = "security";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword.Length < 8)
        {
            TempData["ErrorMessage"] = "New password must be at least 8 characters long.";
            TempData["ActiveTab"] = "security";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);

            // Audit log - password changed successfully
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.PasswordChanged,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = "Password changed successfully"
            });

            TempData["SuccessMessage"] = "Password changed successfully.";
        }
        else
        {
            // Audit log - password change failed
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.PasswordChanged,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = "Password change failed",
                Success = false,
                ErrorMessage = errors
            });

            TempData["ErrorMessage"] = $"Failed to change password: {errors}";
        }

        TempData["ActiveTab"] = "security";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> SetupMfa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Check if MFA is already enabled
        var isMfaEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        ViewBag.IsMfaEnabled = isMfaEnabled;

        if (isMfaEnabled)
        {
            return View();
        }

        // Generate the authenticator key
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await _userManager.GetEmailAsync(user);
        var authenticatorUri = GenerateQrCodeUri(email!, unformattedKey!);

        ViewBag.SharedKey = FormatKey(unformattedKey!);
        ViewBag.AuthenticatorUri = authenticatorUri;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableMfa(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (string.IsNullOrEmpty(code))
        {
            TempData["ErrorMessage"] = "Verification code is required.";
            return RedirectToAction(nameof(SetupMfa));
        }

        // Strip spaces and hyphens
        var verificationCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            TempData["ErrorMessage"] = "Invalid verification code. Please try again.";
            return RedirectToAction(nameof(SetupMfa));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        _logger.LogInformation("User {UserId} enabled MFA", user.Id);

        // Audit log - 2FA enabled
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.TwoFactorEnabled,
            EntityType = AuditEntityType.User,
            EntityId = user.Id,
            Description = $"Two-factor authentication enabled for {user.Email}"
        });

        // Generate recovery codes
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        TempData["RecoveryCodes"] = recoveryCodes?.ToArray();
        TempData["SuccessMessage"] = "Multi-factor authentication has been enabled.";

        return RedirectToAction(nameof(MfaRecoveryCodes));
    }

    public IActionResult MfaRecoveryCodes()
    {
        var recoveryCodes = TempData["RecoveryCodes"] as string[];
        if (recoveryCodes == null || recoveryCodes.Length == 0)
        {
            return RedirectToAction(nameof(SetupMfa));
        }

        return View(recoveryCodes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableMfa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (result.Succeeded)
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("User {UserId} disabled MFA", user.Id);

            // Audit log - 2FA disabled
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.TwoFactorDisabled,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = $"Two-factor authentication disabled for {user.Email}"
            });

            TempData["SuccessMessage"] = "Multi-factor authentication has been disabled.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to disable multi-factor authentication.";
        }

        return RedirectToAction(nameof(SetupMfa));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateNewRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var isMfaEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!isMfaEnabled)
        {
            TempData["ErrorMessage"] = "Cannot generate recovery codes without MFA enabled.";
            return RedirectToAction(nameof(SetupMfa));
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        // Audit log - recovery codes generated
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.TwoFactorRecoveryCodesGenerated,
            EntityType = AuditEntityType.User,
            EntityId = user.Id,
            Description = $"New recovery codes generated for {user.Email}"
        });

        TempData["RecoveryCodes"] = recoveryCodes?.ToArray();
        TempData["SuccessMessage"] = "New recovery codes have been generated. Your old codes are no longer valid.";

        return RedirectToAction(nameof(MfaRecoveryCodes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportAllData()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _dataExportService.ExportAllUserDataAsync(user.Id);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to export data.";
            return RedirectToAction(nameof(Index));
        }

        return File(result.Content!, result.ContentType, result.FileName);
    }

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }
        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        return string.Format(
            AuthenticatorUriFormat,
            Uri.EscapeDataString("Net Worth Tracker"),
            Uri.EscapeDataString(email),
            unformattedKey);
    }

    private static IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetTimeZoneSelectList(string selectedTimeZone)
    {
        var groups = SupportedTimeZones.TimeZoneGroups.Keys
            .ToDictionary(k => k, k => new Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup { Name = k });

        var items = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

        foreach (var group in SupportedTimeZones.TimeZoneGroups)
        {
            foreach (var tzId in group.Value)
            {
                if (SupportedTimeZones.TimeZones.TryGetValue(tzId, out var displayName))
                {
                    items.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = tzId,
                        Text = displayName,
                        Selected = tzId == selectedTimeZone,
                        Group = groups[group.Key]
                    });
                }
            }
        }

        return items;
    }
}
