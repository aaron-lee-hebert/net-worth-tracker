namespace NetWorthTracker.Application.Interfaces;

/// <summary>
/// Service for GDPR-compliant data export of all user data.
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Exports all user data as a ZIP file for GDPR compliance.
    /// </summary>
    /// <param name="userId">The user ID to export data for.</param>
    /// <returns>A result containing the ZIP file content.</returns>
    Task<DataExportResult> ExportAllUserDataAsync(Guid userId);
}

/// <summary>
/// Result of a data export operation.
/// </summary>
public class DataExportResult
{
    public bool Success { get; init; }
    public byte[]? Content { get; init; }
    public string? FileName { get; init; }
    public string ContentType { get; init; } = "application/zip";
    public string? ErrorMessage { get; init; }

    public static DataExportResult Ok(byte[] content, string fileName) => new()
    {
        Success = true,
        Content = content,
        FileName = fileName
    };

    public static DataExportResult Error(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
