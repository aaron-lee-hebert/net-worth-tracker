using System.ComponentModel.DataAnnotations;

namespace NetWorthTracker.Core.ViewModels;

/// <summary>
/// View model for displaying balance history (read-only)
/// </summary>
public class BalanceHistoryViewModel
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// View model for creating a new balance history record
/// </summary>
public class BalanceHistoryCreateViewModel
{
    [Required(ErrorMessage = "Account ID is required")]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "Balance is required")]
    [Range(typeof(decimal), "-999999999999.99", "999999999999.99",
        ErrorMessage = "Balance must be between -999,999,999,999.99 and 999,999,999,999.99")]
    [DataType(DataType.Currency)]
    public decimal Balance { get; set; }

    [Required(ErrorMessage = "Recorded date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Recorded Date")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// View model for editing an existing balance history record
/// </summary>
public class BalanceHistoryEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "Balance is required")]
    [Range(typeof(decimal), "-999999999999.99", "999999999999.99",
        ErrorMessage = "Balance must be between -999,999,999,999.99 and 999,999,999,999.99")]
    [DataType(DataType.Currency)]
    public decimal Balance { get; set; }

    [Required(ErrorMessage = "Recorded date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Recorded Date")]
    public DateTime RecordedAt { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}
