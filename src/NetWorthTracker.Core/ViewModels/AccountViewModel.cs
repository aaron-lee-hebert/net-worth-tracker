using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.ViewModels;

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

public class AccountCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType AccountType { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
}

public class AccountEditViewModel
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
