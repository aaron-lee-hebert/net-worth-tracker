namespace NetWorthTracker.Core.ViewModels;

public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<AccountChartData> Accounts { get; set; } = new();
    public Dictionary<string, List<decimal>> ByType { get; set; } = new();
    public List<decimal> NetWorth { get; set; } = new();
}

public class AccountChartData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsAsset { get; set; }
    public List<decimal> Data { get; set; } = new();
}
