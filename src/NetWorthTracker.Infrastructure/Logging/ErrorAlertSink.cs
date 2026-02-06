using Microsoft.Extensions.DependencyInjection;
using NetWorthTracker.Infrastructure.Services;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace NetWorthTracker.Infrastructure.Logging;

public class ErrorAlertSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFormatProvider? _formatProvider;

    public ErrorAlertSink(IServiceProvider serviceProvider, IFormatProvider? formatProvider = null)
    {
        _serviceProvider = serviceProvider;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error)
        {
            return;
        }

        // Fire and forget - we don't want to block logging
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var errorAlertService = scope.ServiceProvider.GetService<IErrorAlertService>();

                if (errorAlertService == null)
                {
                    return;
                }

                var errorType = GetErrorType(logEvent);
                var message = logEvent.RenderMessage(_formatProvider);
                var stackTrace = logEvent.Exception?.ToString();

                await errorAlertService.SendErrorAlertAsync(errorType, message, stackTrace);
            }
            catch
            {
                // Silently fail - we don't want to cause issues in the logging pipeline
            }
        });
    }

    private static string GetErrorType(LogEvent logEvent)
    {
        if (logEvent.Exception != null)
        {
            return logEvent.Exception.GetType().Name;
        }

        // Try to get source context
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var context = sourceContext.ToString().Trim('"');
            var lastDot = context.LastIndexOf('.');
            return lastDot >= 0 ? context[(lastDot + 1)..] : context;
        }

        return logEvent.Level == LogEventLevel.Fatal ? "FatalError" : "ApplicationError";
    }
}

public static class ErrorAlertSinkExtensions
{
    public static LoggerConfiguration ErrorAlertSink(
        this LoggerSinkConfiguration loggerConfiguration,
        IServiceProvider serviceProvider,
        IFormatProvider? formatProvider = null)
    {
        return loggerConfiguration.Sink(new ErrorAlertSink(serviceProvider, formatProvider));
    }
}
