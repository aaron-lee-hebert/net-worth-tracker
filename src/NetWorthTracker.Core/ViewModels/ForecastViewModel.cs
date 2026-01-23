namespace NetWorthTracker.Core.ViewModels;

public class ForecastViewModel
{
    public List<string> Labels { get; set; } = new(); // Dates for x-axis
    public List<decimal> HistoricalNetWorth { get; set; } = new();
    public List<decimal?> ForecastedNetWorth { get; set; } = new();
    public List<AccountForecast> Accounts { get; set; } = new();
    public ForecastSummary Summary { get; set; } = new();
    public int HistoricalMonths { get; set; }
    public int ForecastMonths { get; set; }
}

public class AccountForecast
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsLiability { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal ProjectedBalance { get; set; }
    public decimal MonthlyChange { get; set; }
    public decimal AnnualGrowthRate { get; set; }
    public DateTime? PayoffDate { get; set; } // For debts
    public string TrendDirection { get; set; } = "stable"; // up, down, stable
    public List<decimal> HistoricalData { get; set; } = new();
    public List<decimal?> ForecastData { get; set; } = new();
}

public class ForecastSummary
{
    public decimal CurrentNetWorth { get; set; }
    public decimal ProjectedNetWorth { get; set; }
    public decimal ProjectedChange { get; set; }
    public decimal ProjectedChangePercent { get; set; }
    public decimal CurrentAssets { get; set; }
    public decimal ProjectedAssets { get; set; }
    public decimal CurrentLiabilities { get; set; }
    public decimal ProjectedLiabilities { get; set; }
    public DateTime ProjectionDate { get; set; }
}
