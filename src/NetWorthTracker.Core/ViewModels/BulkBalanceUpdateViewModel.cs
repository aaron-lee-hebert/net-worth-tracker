namespace NetWorthTracker.Core.ViewModels;

/// <summary>
/// Request model for bulk balance update
/// </summary>
public class BulkBalanceUpdateViewModel
{
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
    public List<AccountBalanceUpdateItem> Accounts { get; set; } = new();
}

/// <summary>
/// Individual account balance update item
/// </summary>
public class AccountBalanceUpdateItem
{
    public Guid AccountId { get; set; }
    public decimal NewBalance { get; set; }
}

/// <summary>
/// Response model for bulk balance update
/// </summary>
public class BulkBalanceUpdateResponse
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Account data for the bulk update modal
/// </summary>
public class AccountForBulkUpdateViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Category { get; set; } = string.Empty;
    public int CategoryOrder { get; set; }
    public bool IsLiability { get; set; }
}
