namespace NetWorthTracker.Core.Interfaces;

public interface IEmailQueueService
{
    Task QueueEmailAsync(string to, string subject, string htmlBody, string? idempotencyKey = null);
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessQueueAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<EmailQueueStats> GetQueueStatsAsync();
}

public class EmailQueueStats
{
    public int Pending { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
    public DateTime? LastProcessedAt { get; set; }
}
