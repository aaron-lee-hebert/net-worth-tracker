namespace NetWorthTracker.Core.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendEmailVerificationAsync(string to, string verificationLink);
    Task SendPasswordResetAsync(string to, string resetLink);
    bool IsConfigured { get; }
}
