using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;

namespace NetWorthTracker.Infrastructure.Services;

public class EmailQueueService : IEmailQueueService
{
    private readonly IEmailQueueRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailQueueService> _logger;
    private DateTime? _lastProcessedAt;

    public EmailQueueService(
        IEmailQueueRepository repository,
        IEmailService emailService,
        ILogger<EmailQueueService> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task QueueEmailAsync(string to, string subject, string htmlBody, string? idempotencyKey = null)
    {
        // Check for duplicate if idempotency key provided
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey);
            if (existing != null)
            {
                _logger.LogDebug("Email already queued with idempotency key {Key}", idempotencyKey);
                return;
            }
        }

        var email = new EmailQueue
        {
            ToEmail = to,
            Subject = subject,
            HtmlBody = htmlBody,
            IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString(),
            Status = EmailQueueStatus.Pending,
            NextAttemptAt = DateTime.UtcNow
        };

        await _repository.AddAsync(email);
        _logger.LogInformation("Queued email to {To} with subject {Subject}", to, subject);
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        await ProcessQueueAsync(10, cancellationToken);
    }

    public async Task<int> ProcessQueueAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var pendingEmails = await _repository.GetPendingEmailsAsync(batchSize);
        var processedCount = 0;

        foreach (var email in pendingEmails)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessEmailAsync(email);
            processedCount++;
        }

        _lastProcessedAt = DateTime.UtcNow;
        return processedCount;
    }

    private async Task ProcessEmailAsync(EmailQueue email)
    {
        try
        {
            email.Status = EmailQueueStatus.Processing;
            email.AttemptCount++;
            email.LastAttemptAt = DateTime.UtcNow;
            await _repository.UpdateAsync(email);

            await _emailService.SendEmailAsync(email.ToEmail, email.Subject, email.HtmlBody);

            email.Status = EmailQueueStatus.Sent;
            email.SentAt = DateTime.UtcNow;
            await _repository.UpdateAsync(email);

            _logger.LogInformation("Successfully sent email {EmailId} to {To}", email.Id, email.ToEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email {EmailId} to {To}. Attempt {Attempt}/{MaxAttempts}",
                email.Id, email.ToEmail, email.AttemptCount, email.MaxAttempts);

            email.ErrorMessage = ex.Message;

            if (email.AttemptCount >= email.MaxAttempts)
            {
                email.Status = EmailQueueStatus.Failed;
                _logger.LogError("Email {EmailId} permanently failed after {Attempts} attempts",
                    email.Id, email.AttemptCount);
            }
            else
            {
                email.Status = EmailQueueStatus.Pending;
                // Exponential backoff: 1min, 2min, 4min, 8min...
                email.NextAttemptAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, email.AttemptCount - 1));
            }

            await _repository.UpdateAsync(email);
        }
    }

    public async Task<EmailQueueStats> GetQueueStatsAsync()
    {
        var pending = await _repository.GetPendingCountAsync();
        var failed = await _repository.GetFailedCountAsync();
        return new EmailQueueStats
        {
            Pending = pending,
            Failed = failed,
            Total = pending + failed,
            LastProcessedAt = _lastProcessedAt
        };
    }
}
