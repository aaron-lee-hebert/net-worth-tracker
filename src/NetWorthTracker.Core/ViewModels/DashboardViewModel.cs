using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.ViewModels;

public class DashboardViewModel
{
    public decimal TotalNetWorth { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public Dictionary<AccountCategory, decimal> TotalsByCategory { get; set; } = new();
    public IList<AccountSummaryViewModel> RecentAccounts { get; set; } = new List<AccountSummaryViewModel>();
    public IList<NetWorthHistoryViewModel> NetWorthHistory { get; set; } = new List<NetWorthHistoryViewModel>();
    public bool IsFirstTimeUser { get; set; }
    public bool ShowFirstAccountSuccess { get; set; }
}

public class AccountSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? Institution { get; set; }
}

public class NetWorthHistoryViewModel
{
    public DateTime Date { get; set; }
    public decimal TotalNetWorth { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
}
