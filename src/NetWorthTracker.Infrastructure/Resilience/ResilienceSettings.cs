namespace NetWorthTracker.Infrastructure.Resilience;

public class ResilienceSettings
{
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 1000;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 30;
}
