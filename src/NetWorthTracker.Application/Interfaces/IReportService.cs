using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Interfaces;

/// <summary>
/// Service for generating financial reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Builds a quarterly report showing account balances across all historical quarters.
    /// </summary>
    Task<QuarterlyReportViewModel> BuildQuarterlyReportAsync(Guid userId);

    /// <summary>
    /// Gets monthly net worth history data for CSV export.
    /// </summary>
    Task<NetWorthHistoryData> GetNetWorthHistoryAsync(Guid userId);
}

/// <summary>
/// Monthly net worth history data for export.
/// </summary>
public class NetWorthHistoryData
{
    public IReadOnlyList<MonthlyNetWorth> Months { get; init; } = [];
    public bool HasData => Months.Count > 0;
}

/// <summary>
/// Net worth data for a single month.
/// </summary>
public class MonthlyNetWorth
{
    public DateTime Month { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal NetWorth { get; init; }
    public decimal? Change { get; init; }
    public decimal? PercentChange { get; init; }
}
