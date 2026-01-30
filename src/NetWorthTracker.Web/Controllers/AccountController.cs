using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NetWorthTracker.Core;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if email is confirmed (only when email service is configured)
        if (_emailService.IsConfigured)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Please verify your email address before logging in.");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in", model.Email);

            // Audit log - successful login
            var user = await _userManager.FindByEmailAsync(model.Email);
            await _auditService.LogLoginAttemptAsync(model.Email, success: true, user?.Id, ipAddress, userAgent);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWith2fa), new { returnUrl = model.ReturnUrl, rememberMe = model.RememberMe });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out", model.Email);

            // Audit log - account locked out
            await _auditService.LogLoginAttemptAsync(model.Email, success: false, userId: null, ipAddress, userAgent, "Account locked out");

            ModelState.AddModelError(string.Empty, "Account is locked out. Please try again later.");
            return View(model);
        }

        // Audit log - failed login
        await _auditService.LogLoginAttemptAsync(model.Email, success: false, userId: null, ipAddress, userAgent, "Invalid credentials");

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult LoginWith2fa(string? returnUrl = null, bool rememberMe = false)
    {
        return View(new LoginWith2faViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, model.RememberMe, model.RememberMachine);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        if (result.Succeeded)
        {
            // Audit log - successful 2FA login
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.TwoFactorLoginSuccess,
                EntityType = AuditEntityType.User,
                Description = $"2FA login successful for {user.Email}",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            // Audit log - 2FA lockout
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.TwoFactorLoginFailed,
                EntityType = AuditEntityType.User,
                Description = $"2FA login failed (lockout) for {user.Email}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = false,
                ErrorMessage = "Account locked out"
            });

            ModelState.AddModelError(string.Empty, "Account is locked out. Please try again later.");
            return View(model);
        }

        // Audit log - failed 2FA
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.TwoFactorLoginFailed,
            EntityType = AuditEntityType.User,
            Description = $"2FA login failed for {user.Email}",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = false,
            ErrorMessage = "Invalid authenticator code"
        });

        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        return View(model);
    }

    [HttpGet]
    public IActionResult LoginWithRecoveryCode(string? returnUrl = null)
    {
        return View(new LoginWithRecoveryCodeViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        if (result.Succeeded)
        {
            // Audit log - recovery code used
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.RecoveryCodeUsed,
                EntityType = AuditEntityType.User,
                Description = $"Recovery code used for login by {user.Email}",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            // Audit log - lockout after recovery code attempt
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.TwoFactorLoginFailed,
                EntityType = AuditEntityType.User,
                Description = $"Recovery code login failed (lockout) for {user.Email}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = false,
                ErrorMessage = "Account locked out"
            });

            ModelState.AddModelError(string.Empty, "Account is locked out. Please try again later.");
            return View(model);
        }

        // Audit log - invalid recovery code
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.TwoFactorLoginFailed,
            EntityType = AuditEntityType.User,
            Description = $"Invalid recovery code for {user.Email}",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = false,
            ErrorMessage = "Invalid recovery code"
        });

        ModelState.AddModelError(string.Empty, "Invalid recovery code.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        ViewBag.SupportedLocales = SupportedLocales.Locales.Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = l.Key,
            Text = l.Value,
            Selected = l.Key == "en-US"
        });
        ViewBag.SupportedTimeZones = GetTimeZoneSelectList("America/New_York");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.SupportedLocales = SupportedLocales.Locales.Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = l.Key,
                Text = l.Value,
                Selected = l.Key == model.Locale
            });
            ViewBag.SupportedTimeZones = GetTimeZoneSelectList(model.TimeZone);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Locale = SupportedLocales.IsSupported(model.Locale) ? model.Locale : "en-US",
            TimeZone = SupportedTimeZones.IsSupported(model.TimeZone) ? model.TimeZone : "America/New_York"
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created a new account", model.Email);

            // Audit log - user registered
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.UserRegistered,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = $"New user registered: {model.Email}",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            // Generate email confirmation token and send verification email
            if (_emailService.IsConfigured)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token },
                    Request.Scheme);

                await _emailService.SendEmailVerificationAsync(user.Email!, confirmationLink!);
                return RedirectToAction(nameof(RegisterConfirmation));
            }

            // If email is not configured, auto-confirm and sign in (for self-hosted without email)
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ViewBag.SupportedLocales = SupportedLocales.Locales.Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = l.Key,
            Text = l.Value,
            Selected = l.Key == model.Locale
        });
        ViewBag.SupportedTimeZones = GetTimeZoneSelectList(model.TimeZone);
        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return View("EmailVerification", new EmailVerificationViewModel
            {
                Success = false,
                Message = "Unable to verify email. User not found."
            });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} confirmed their email", user.Email);

            // Audit log - email verified
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.EmailVerified,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = $"Email verified for {user.Email}"
            });

            return View("EmailVerification", new EmailVerificationViewModel
            {
                Success = true,
                Message = "Your email has been verified. You can now log in."
            });
        }

        return View("EmailVerification", new EmailVerificationViewModel
        {
            Success = false,
            Message = "Email verification failed. The link may have expired."
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);

        // Audit log - logout
        if (user != null)
        {
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.Logout,
                EntityType = AuditEntityType.User,
                Description = $"User logged out: {user.Email}"
            });
        }

        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (!_emailService.IsConfigured)
        {
            return View("ForgotPasswordNotAvailable");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!_emailService.IsConfigured)
        {
            return View("ForgotPasswordNotAvailable");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            // Don't reveal that the user does not exist or is not confirmed
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { email = model.Email, token },
            Request.Scheme);

        await _emailService.SendPasswordResetAsync(user.Email!, resetLink!);
        _logger.LogInformation("Password reset requested for user {Email}", model.Email);

        // Audit log - password reset requested
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        await _auditService.LogAsync(new AuditLogEntry
        {
            UserId = user.Id,
            Action = AuditAction.PasswordResetRequested,
            EntityType = AuditEntityType.User,
            EntityId = user.Id,
            Description = $"Password reset requested for {model.Email}",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("A token is required for password reset.");
        }
        return View(new ResetPasswordViewModel { Email = email ?? string.Empty, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successfully for user {Email}", model.Email);

            // Audit log - password reset completed
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = user.Id,
                Action = AuditAction.PasswordResetCompleted,
                EntityType = AuditEntityType.User,
                EntityId = user.Id,
                Description = $"Password reset completed for {model.Email}",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup> GetTimeZoneGroups()
    {
        return SupportedTimeZones.TimeZoneGroups.Keys
            .Select(g => new Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup { Name = g })
            .ToList();
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
