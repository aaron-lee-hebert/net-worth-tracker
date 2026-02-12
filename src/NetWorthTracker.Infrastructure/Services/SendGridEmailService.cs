using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        HttpClient httpClient,
        IOptions<SendGridSettings> settings,
        ILogger<SendGridEmailService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.sendgrid.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.ApiKey) &&
                                !string.IsNullOrEmpty(_settings.FromEmail);

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("SendGrid not configured. Would have sent email to {To} with subject: {Subject}", to, subject);
            return;
        }

        try
        {
            var payload = new
            {
                personalizations = new[]
                {
                    new { to = new[] { new { email = to } } }
                },
                from = new { email = _settings.FromEmail, name = _settings.FromName },
                subject,
                content = new[]
                {
                    new { type = "text/html", value = htmlBody }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("v3/mail/send", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SendGrid API error {StatusCode}: {Error}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"SendGrid API returned {response.StatusCode}: {errorBody}");
            }

            _logger.LogInformation("Email sent successfully to {To} via SendGrid", to);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SendGrid", to);
            throw;
        }
    }

    public async Task SendEmailVerificationAsync(string to, string verificationLink)
    {
        var subject = "Verify your email address - Net Worth Tracker";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Georgia, 'Times New Roman', serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #1a3c34; color: #d4a843; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background-color: #faf8f5; }}
        .button {{ display: inline-block; background-color: #1a3c34; color: #d4a843; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Net Worth Tracker</h1>
        </div>
        <div class='content'>
            <h2>Verify Your Email Address</h2>
            <p>Thank you for registering with Net Worth Tracker. Please click the button below to verify your email address and activate your account.</p>
            <p style='text-align: center;'>
                <a href='{verificationLink}' class='button'>Verify Email Address</a>
            </p>
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <p style='word-break: break-all; font-size: 14px;'>{verificationLink}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account with Net Worth Tracker, you can safely ignore this email.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Net Worth Tracker.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetAsync(string to, string resetLink)
    {
        var subject = "Reset your password - Net Worth Tracker";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Georgia, 'Times New Roman', serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #1a3c34; color: #d4a843; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background-color: #faf8f5; }}
        .button {{ display: inline-block; background-color: #1a3c34; color: #d4a843; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Net Worth Tracker</h1>
        </div>
        <div class='content'>
            <h2>Reset Your Password</h2>
            <p>We received a request to reset the password for your Net Worth Tracker account. Click the button below to create a new password.</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <p style='word-break: break-all; font-size: 14px;'>{resetLink}</p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Net Worth Tracker.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body);
    }
}
