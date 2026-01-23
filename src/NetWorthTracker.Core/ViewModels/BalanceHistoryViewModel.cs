using System.ComponentModel.DataAnnotations;

namespace NetWorthTracker.Core.ViewModels;

public class BalanceHistoryViewModel
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
}

public class BalanceHistoryCreateViewModel
{
    public Guid AccountId { get; set; }
    public decimal Balance { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

public class BalanceHistoryEditViewModel
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    [Required]
    public decimal Balance { get; set; }
    [Required]
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
}
