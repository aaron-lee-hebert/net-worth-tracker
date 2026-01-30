using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Services;

public class ErrorAlertSettings
{
    public bool Enabled { get; set; } = false;
    public string RecipientEmail { get; set; } = string.Empty;
    public int MaxAlertsPerHour { get; set; } = 10;
}

public interface IErrorAlertService
{
    Task SendErrorAlertAsync(string errorType, string message, string? stackTrace = null);
    bool ShouldSendAlert(string errorType);
}

public class ErrorAlertService : IErrorAlertService
{
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<ErrorAlertService> _logger;
    private readonly ErrorAlertSettings _settings;
    private readonly ConcurrentDictionary<string, List<DateTime>> _alertHistory = new();
    private readonly object _cleanupLock = new();

    public ErrorAlertService(
        IEmailQueueService emailQueueService,
        ILogger<ErrorAlertService> logger,
        IConfiguration configuration)
    {
        _emailQueueService = emailQueueService;
        _logger = logger;
        _settings = configuration.GetSection("ErrorAlerts").Get<ErrorAlertSettings>() ?? new ErrorAlertSettings();
    }

    public bool ShouldSendAlert(string errorType)
    {
        if (!_settings.Enabled || string.IsNullOrEmpty(_settings.RecipientEmail))
        {
            return false;
        }

        CleanupOldAlerts();

        var history = _alertHistory.GetOrAdd(errorType, _ => new List<DateTime>());

        lock (history)
        {
            var recentAlerts = history.Count(t => t > DateTime.UtcNow.AddHours(-1));
            return recentAlerts < _settings.MaxAlertsPerHour;
        }
    }

    public async Task SendErrorAlertAsync(string errorType, string message, string? stackTrace = null)
    {
        if (!ShouldSendAlert(errorType))
        {
            _logger.LogDebug("Error alert suppressed for {ErrorType} - max alerts per hour reached or alerts disabled", errorType);
            return;
        }

        // Record this alert
        var history = _alertHistory.GetOrAdd(errorType, _ => new List<DateTime>());
        lock (history)
        {
            history.Add(DateTime.UtcNow);
        }

        var subject = $"[Net Worth Tracker] Error Alert: {errorType}";
        var htmlBody = BuildAlertEmailBody(errorType, message, stackTrace);

        try
        {
            var idempotencyKey = $"error-alert-{errorType}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm}";
            await _emailQueueService.QueueEmailAsync(_settings.RecipientEmail, subject, htmlBody, idempotencyKey);
            _logger.LogInformation("Error alert queued for {ErrorType}", errorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue error alert for {ErrorType}", errorType);
        }
    }

    private void CleanupOldAlerts()
    {
        lock (_cleanupLock)
        {
            var cutoff = DateTime.UtcNow.AddHours(-2);

            foreach (var history in _alertHistory.Values)
            {
                lock (history)
                {
                    history.RemoveAll(t => t < cutoff);
                }
            }
        }
    }

    private static string BuildAlertEmailBody(string errorType, string message, string? stackTrace)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var machineName = Environment.MachineName;

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; border-radius: 4px 4px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }}
        .field {{ margin-bottom: 15px; }}
        .label {{ font-weight: bold; color: #495057; }}
        .value {{ margin-top: 5px; padding: 10px; background-color: white; border: 1px solid #dee2e6; border-radius: 4px; }}
        .stack-trace {{ font-family: monospace; font-size: 12px; white-space: pre-wrap; word-wrap: break-word; max-height: 300px; overflow-y: auto; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>Error Alert</h2>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>Error Type</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(errorType)}</div>
            </div>
            <div class='field'>
                <div class='label'>Timestamp</div>
                <div class='value'>{timestamp}</div>
            </div>
            <div class='field'>
                <div class='label'>Server</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(machineName)}</div>
            </div>
            <div class='field'>
                <div class='label'>Message</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(message)}</div>
            </div>";

        if (!string.IsNullOrEmpty(stackTrace))
        {
            body += $@"
            <div class='field'>
                <div class='label'>Stack Trace</div>
                <div class='value stack-trace'>{System.Net.WebUtility.HtmlEncode(stackTrace)}</div>
            </div>";
        }

        body += @"
        </div>
    </div>
</body>
</html>";

        return body;
    }
}
