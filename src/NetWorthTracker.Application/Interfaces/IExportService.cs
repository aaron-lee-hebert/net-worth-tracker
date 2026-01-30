using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Application.Interfaces;

/// <summary>
/// Service for generating CSV exports.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Generates CSV content for the quarterly report.
    /// </summary>
    Task<ExportResult> ExportQuarterlyReportCsvAsync(Guid userId);

    /// <summary>
    /// Generates CSV content for net worth history with monthly granularity.
    /// </summary>
    Task<ExportResult> ExportNetWorthHistoryCsvAsync(Guid userId);

    /// <summary>
    /// Generates CSV content for accounts list, optionally filtered by category.
    /// </summary>
    Task<ExportResult> ExportAccountsCsvAsync(Guid userId, AccountCategory? category = null);

    /// <summary>
    /// Generates CSV content for a single account's balance history.
    /// </summary>
    Task<ExportResult> ExportAccountHistoryCsvAsync(Guid userId, Guid accountId);
}

/// <summary>
/// Result of an export operation containing the file content.
/// </summary>
public class ExportResult
{
    public bool Success { get; init; }
    public string? Content { get; init; }
    public string? FileName { get; init; }
    public string ContentType { get; init; } = "text/csv";
    public string? ErrorMessage { get; init; }

    public static ExportResult Ok(string content, string fileName) => new()
    {
        Success = true,
        Content = content,
        FileName = fileName
    };

    public static ExportResult NoData(string message = "No data available for export") => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
