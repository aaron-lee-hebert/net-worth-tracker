using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAccountRepository accountRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        ILogger<SettingsController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountRepository = accountRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        return View(user);
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

            _logger.LogInformation("Deleted all data for user {UserId}", user.Id);
            TempData["SuccessMessage"] = "All your financial data has been deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "An error occurred while deleting your data. Please try again.";
        }

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

            // Sign out the user
            await _signInManager.SignOutAsync();

            // Delete the user account
            await _userManager.DeleteAsync(user);

            _logger.LogInformation("Deleted account for user {UserId}", user.Id);

            TempData["SuccessMessage"] = "Your account and all data have been permanently deleted.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "An error occurred while deleting your account. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FirstName = firstName;
        user.LastName = lastName;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

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
            return RedirectToAction(nameof(Index));
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "New password and confirmation do not match.";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword.Length < 8)
        {
            TempData["ErrorMessage"] = "New password must be at least 8 characters long.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully.";
        }
        else
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = $"Failed to change password: {errors}";
        }

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
        TempData["RecoveryCodes"] = recoveryCodes?.ToArray();
        TempData["SuccessMessage"] = "New recovery codes have been generated. Your old codes are no longer valid.";

        return RedirectToAction(nameof(MfaRecoveryCodes));
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
}
