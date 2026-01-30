namespace NetWorthTracker.Infrastructure.Data;

/// <summary>
/// Configuration settings for database migrations
/// </summary>
public class MigrationSettings
{
    /// <summary>
    /// Path to the migrations directory (relative to application root)
    /// </summary>
    public string MigrationsPath { get; set; } = "./migrations";

    /// <summary>
    /// Whether to allow rollback operations
    /// </summary>
    public bool AllowRollback { get; set; } = true;

    /// <summary>
    /// Whether to wrap each migration in its own transaction
    /// </summary>
    public bool TransactionPerMigration { get; set; } = true;

    /// <summary>
    /// Whether to run migrations automatically on application startup
    /// </summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Table name for tracking applied migrations
    /// </summary>
    public string MigrationsTableName { get; set; } = "schema_migrations";
}
