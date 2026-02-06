using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace NetWorthTracker.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilienceSettings settings, ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                settings.RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(
                    settings.RetryBaseDelayMs * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms. Error: {Error}",
                        retryCount, delay.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilienceSettings settings, ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                settings.CircuitBreakerThreshold,
                TimeSpan.FromSeconds(settings.CircuitBreakerDurationSeconds),
                onBreak: (outcome, duration) =>
                {
                    logger.LogError(
                        "Circuit breaker opened for {Duration}s. Error: {Error}",
                        duration.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ResilienceSettings settings, ILogger logger)
    {
        var retryPolicy = GetRetryPolicy(settings, logger);
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(settings, logger);

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }
}
