namespace NetWorthTracker.Core.ViewModels;

public class QuarterlyReportViewModel
{
    public List<string> Quarters { get; set; } = new();
    public List<AccountQuarterlyData> Accounts { get; set; } = new();
    public QuarterlyTotals Totals { get; set; } = new();
}

public class AccountQuarterlyData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsLiability { get; set; }
    public List<decimal?> Balances { get; set; } = new();
}

public class QuarterlyTotals
{
    public List<decimal> NetWorth { get; set; } = new();
    public List<decimal?> PercentChange { get; set; } = new();
}
