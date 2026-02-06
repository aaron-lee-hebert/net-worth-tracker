namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Service for running database migrations
/// </summary>
public interface IMigrationRunner
{
    /// <summary>
    /// Run all pending migrations
    /// </summary>
    Task<MigrationResult> RunMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of pending migrations that haven't been applied
    /// </summary>
    Task<IEnumerable<MigrationInfo>> GetPendingMigrationsAsync();

    /// <summary>
    /// Get list of migrations that have been applied
    /// </summary>
    Task<IEnumerable<MigrationInfo>> GetAppliedMigrationsAsync();

    /// <summary>
    /// Rollback the last applied migration
    /// </summary>
    Task<MigrationResult> RollbackLastMigrationAsync();

    /// <summary>
    /// Ensure the migrations tracking table exists
    /// </summary>
    Task EnsureMigrationTableExistsAsync();
}

/// <summary>
/// Information about a migration
/// </summary>
public class MigrationInfo
{
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Checksum { get; set; }
    public DateTime? AppliedAt { get; set; }
    public bool IsApplied => AppliedAt.HasValue;
    public string? UpSql { get; set; }
    public string? DownSql { get; set; }
}

/// <summary>
/// Result of a migration operation
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public int MigrationsApplied { get; set; }
    public List<string> AppliedVersions { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? FailedVersion { get; set; }

    public static MigrationResult Succeeded(int count, List<string> versions) => new()
    {
        Success = true,
        MigrationsApplied = count,
        AppliedVersions = versions
    };

    public static MigrationResult Failed(string version, string error) => new()
    {
        Success = false,
        FailedVersion = version,
        ErrorMessage = error
    };

    public static MigrationResult NoMigrations() => new()
    {
        Success = true,
        MigrationsApplied = 0
    };
}
