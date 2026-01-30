using System.ComponentModel.DataAnnotations;
using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.ViewModels;

/// <summary>
/// View model for displaying account information (read-only)
/// </summary>
public class AccountViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType AccountType { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// View model for creating a new account
/// </summary>
public class AccountCreateViewModel
{
    [Required(ErrorMessage = "Account name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [Display(Name = "Account Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Account type is required")]
    [EnumDataType(typeof(AccountType), ErrorMessage = "Invalid account type")]
    [Display(Name = "Account Type")]
    public AccountType AccountType { get; set; }

    [Required(ErrorMessage = "Current balance is required")]
    [Range(typeof(decimal), "-999999999999.99", "999999999999.99",
        ErrorMessage = "Balance must be between -999,999,999,999.99 and 999,999,999,999.99")]
    [DataType(DataType.Currency)]
    [Display(Name = "Current Balance")]
    public decimal CurrentBalance { get; set; }

    [StringLength(100, ErrorMessage = "Institution name cannot exceed 100 characters")]
    public string? Institution { get; set; }

    [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-\*]+$", ErrorMessage = "Account number can only contain letters, numbers, hyphens, and asterisks")]
    [Display(Name = "Account Number")]
    public string? AccountNumber { get; set; }
}

/// <summary>
/// View model for editing an existing account
/// </summary>
public class AccountEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Account name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [Display(Name = "Account Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Account type is required")]
    [EnumDataType(typeof(AccountType), ErrorMessage = "Invalid account type")]
    [Display(Name = "Account Type")]
    public AccountType AccountType { get; set; }

    [Required(ErrorMessage = "Current balance is required")]
    [Range(typeof(decimal), "-999999999999.99", "999999999999.99",
        ErrorMessage = "Balance must be between -999,999,999,999.99 and 999,999,999,999.99")]
    [DataType(DataType.Currency)]
    [Display(Name = "Current Balance")]
    public decimal CurrentBalance { get; set; }

    [StringLength(100, ErrorMessage = "Institution name cannot exceed 100 characters")]
    public string? Institution { get; set; }

    [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-\*]+$", ErrorMessage = "Account number can only contain letters, numbers, hyphens, and asterisks")]
    [Display(Name = "Account Number")]
    public string? AccountNumber { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}
