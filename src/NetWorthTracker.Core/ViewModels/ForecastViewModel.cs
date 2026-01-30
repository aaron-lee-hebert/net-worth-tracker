using System.ComponentModel.DataAnnotations;

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

/// <summary>
/// View model for forecast growth rate assumptions
/// </summary>
public class ForecastAssumptionsViewModel
{
    [Range(-50, 100, ErrorMessage = "Investment growth rate must be between -50% and 100%")]
    [Display(Name = "Investment Growth Rate (%)")]
    public decimal InvestmentGrowthRate { get; set; } = 7.0m;

    [Range(-50, 100, ErrorMessage = "Real estate growth rate must be between -50% and 100%")]
    [Display(Name = "Real Estate Growth Rate (%)")]
    public decimal RealEstateGrowthRate { get; set; } = 2.0m;

    [Range(-50, 100, ErrorMessage = "Banking growth rate must be between -50% and 100%")]
    [Display(Name = "Banking Growth Rate (%)")]
    public decimal BankingGrowthRate { get; set; } = 0.5m;

    [Range(-50, 100, ErrorMessage = "Business growth rate must be between -50% and 100%")]
    [Display(Name = "Business Growth Rate (%)")]
    public decimal BusinessGrowthRate { get; set; } = 3.0m;

    [Range(0, 100, ErrorMessage = "Vehicle depreciation rate must be between 0% and 100%")]
    [Display(Name = "Vehicle Depreciation Rate (%)")]
    public decimal VehicleDepreciationRate { get; set; } = 15.0m;

    public bool HasCustomOverrides { get; set; }

    // Defaults for display
    public static decimal DefaultInvestmentRate => 7.0m;
    public static decimal DefaultRealEstateRate => 2.0m;
    public static decimal DefaultBankingRate => 0.5m;
    public static decimal DefaultBusinessRate => 3.0m;
    public static decimal DefaultVehicleRate => 15.0m;
}
