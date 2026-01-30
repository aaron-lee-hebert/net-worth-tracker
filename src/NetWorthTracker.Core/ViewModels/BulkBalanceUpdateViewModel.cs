using System.ComponentModel.DataAnnotations;

namespace NetWorthTracker.Core.ViewModels;

/// <summary>
/// Request model for bulk balance update
/// </summary>
public class BulkBalanceUpdateViewModel
{
    [Required(ErrorMessage = "Recorded date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Recorded Date")]
    public DateTime RecordedAt { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "At least one account must be provided")]
    [MinLength(1, ErrorMessage = "At least one account must be updated")]
    [MaxLength(100, ErrorMessage = "Cannot update more than 100 accounts at once")]
    public List<AccountBalanceUpdateItem> Accounts { get; set; } = new();
}

/// <summary>
/// Individual account balance update item
/// </summary>
public class AccountBalanceUpdateItem
{
    [Required(ErrorMessage = "Account ID is required")]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "New balance is required")]
    [Range(typeof(decimal), "-999999999999.99", "999999999999.99",
        ErrorMessage = "Balance must be between -999,999,999,999.99 and 999,999,999,999.99")]
    [DataType(DataType.Currency)]
    [Display(Name = "New Balance")]
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
/// Account data for the bulk update modal (read-only, no validation needed)
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
