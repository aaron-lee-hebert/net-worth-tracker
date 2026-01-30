using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHibernate;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Data;

/// <summary>
/// Runs database migrations from SQL files
/// </summary>
public class MigrationRunner : IMigrationRunner
{
    private readonly ISession _session;
    private readonly MigrationSettings _settings;
    private readonly ILogger<MigrationRunner> _logger;

    // Regex to parse migration filename: NNN_description.sql
    private static readonly Regex FileNamePattern = new(@"^(\d{3})_(.+)\.sql$", RegexOptions.Compiled);

    // Marker for rollback section in migration files
    private const string RollbackMarker = "-- @ROLLBACK";

    public MigrationRunner(
        ISession session,
        IOptions<MigrationSettings> settings,
        ILogger<MigrationRunner> logger)
    {
        _session = session;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnsureMigrationTableExistsAsync()
    {
        var sql = $@"
            CREATE TABLE IF NOT EXISTS {_settings.MigrationsTableName} (
                version VARCHAR(50) PRIMARY KEY,
                description VARCHAR(500),
                checksum VARCHAR(64),
                applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            )";

        try
        {
            await _session.CreateSQLQuery(sql).ExecuteUpdateAsync();
            await _session.FlushAsync();
            _logger.LogDebug("Ensured migrations table exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create migrations table");
            throw;
        }
    }

    public async Task<MigrationResult> RunMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMigrationTableExistsAsync();

        var pending = (await GetPendingMigrationsAsync()).ToList();

        if (!pending.Any())
        {
            _logger.LogInformation("No pending migrations to apply");
            return MigrationResult.NoMigrations();
        }

        _logger.LogInformation("Found {Count} pending migrations", pending.Count);

        var appliedVersions = new List<string>();

        foreach (var migration in pending.OrderBy(m => m.Version))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Migration cancelled after {Count} migrations", appliedVersions.Count);
                break;
            }

            try
            {
                await ApplyMigrationAsync(migration);
                appliedVersions.Add(migration.Version);
                _logger.LogInformation("Applied migration {Version}: {Description}",
                    migration.Version, migration.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply migration {Version}", migration.Version);
                return MigrationResult.Failed(migration.Version, ex.Message);
            }
        }

        return MigrationResult.Succeeded(appliedVersions.Count, appliedVersions);
    }

    public async Task<IEnumerable<MigrationInfo>> GetPendingMigrationsAsync()
    {
        await EnsureMigrationTableExistsAsync();

        var applied = (await GetAppliedMigrationsAsync()).ToList();
        var appliedVersions = applied.Select(m => m.Version).ToHashSet();

        var allMigrations = GetMigrationFiles();

        return allMigrations
            .Where(m => !appliedVersions.Contains(m.Version))
            .OrderBy(m => m.Version);
    }

    public async Task<IEnumerable<MigrationInfo>> GetAppliedMigrationsAsync()
    {
        await EnsureMigrationTableExistsAsync();

        var sql = $@"
            SELECT version, description, checksum, applied_at
            FROM {_settings.MigrationsTableName}
            ORDER BY version";

        var results = new List<MigrationInfo>();

        try
        {
            var query = _session.CreateSQLQuery(sql);
            var rows = await query.ListAsync();

            foreach (object[] row in rows)
            {
                results.Add(new MigrationInfo
                {
                    Version = row[0]?.ToString() ?? string.Empty,
                    Description = row[1]?.ToString() ?? string.Empty,
                    Checksum = row[2]?.ToString(),
                    AppliedAt = row[3] != null ? DateTime.Parse(row[3].ToString()!) : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get applied migrations");
            throw;
        }

        return results;
    }

    public async Task<MigrationResult> RollbackLastMigrationAsync()
    {
        if (!_settings.AllowRollback)
        {
            return MigrationResult.Failed("", "Rollback is not allowed by configuration");
        }

        var applied = (await GetAppliedMigrationsAsync())
            .OrderByDescending(m => m.Version)
            .FirstOrDefault();

        if (applied == null)
        {
            return MigrationResult.Failed("", "No migrations to rollback");
        }

        // Find the migration file to get the rollback SQL
        var migrationFiles = GetMigrationFiles();
        var migration = migrationFiles.FirstOrDefault(m => m.Version == applied.Version);

        if (migration == null)
        {
            return MigrationResult.Failed(applied.Version,
                $"Migration file not found for version {applied.Version}");
        }

        if (string.IsNullOrEmpty(migration.DownSql))
        {
            return MigrationResult.Failed(applied.Version,
                $"Migration {applied.Version} has no rollback SQL");
        }

        try
        {
            await RollbackMigrationAsync(migration);
            _logger.LogInformation("Rolled back migration {Version}", migration.Version);
            return MigrationResult.Succeeded(1, new List<string> { migration.Version });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback migration {Version}", migration.Version);
            return MigrationResult.Failed(migration.Version, ex.Message);
        }
    }

    private List<MigrationInfo> GetMigrationFiles()
    {
        var migrations = new List<MigrationInfo>();
        var migrationsPath = Path.GetFullPath(_settings.MigrationsPath);

        if (!Directory.Exists(migrationsPath))
        {
            _logger.LogWarning("Migrations directory not found: {Path}", migrationsPath);
            return migrations;
        }

        var files = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => Path.GetFileName(f));

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var match = FileNamePattern.Match(fileName);

            if (!match.Success)
            {
                _logger.LogWarning("Skipping file with invalid name format: {FileName}", fileName);
                continue;
            }

            var content = File.ReadAllText(file);
            var (upSql, downSql) = ParseMigrationContent(content);

            migrations.Add(new MigrationInfo
            {
                Version = match.Groups[1].Value,
                Description = match.Groups[2].Value.Replace('_', ' '),
                FileName = fileName,
                Checksum = ComputeChecksum(content),
                UpSql = upSql,
                DownSql = downSql
            });
        }

        return migrations;
    }

    private (string upSql, string? downSql) ParseMigrationContent(string content)
    {
        var rollbackIndex = content.IndexOf(RollbackMarker, StringComparison.OrdinalIgnoreCase);

        if (rollbackIndex < 0)
        {
            return (content.Trim(), null);
        }

        var upSql = content.Substring(0, rollbackIndex).Trim();
        var downSql = content.Substring(rollbackIndex + RollbackMarker.Length).Trim();

        return (upSql, downSql);
    }

    private async Task ApplyMigrationAsync(MigrationInfo migration)
    {
        if (string.IsNullOrEmpty(migration.UpSql))
        {
            throw new InvalidOperationException($"Migration {migration.Version} has no SQL content");
        }

        using var transaction = _settings.TransactionPerMigration
            ? _session.BeginTransaction()
            : null;

        try
        {
            // Execute the migration SQL
            // Split by GO or semicolon for multi-statement migrations
            var statements = SplitSqlStatements(migration.UpSql);

            foreach (var statement in statements)
            {
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    await _session.CreateSQLQuery(statement).ExecuteUpdateAsync();
                }
            }

            // Record the migration
            var insertSql = $@"
                INSERT INTO {_settings.MigrationsTableName} (version, description, checksum, applied_at)
                VALUES (:version, :description, :checksum, :appliedAt)";

            await _session.CreateSQLQuery(insertSql)
                .SetParameter("version", migration.Version)
                .SetParameter("description", migration.Description)
                .SetParameter("checksum", migration.Checksum)
                .SetParameter("appliedAt", DateTime.UtcNow)
                .ExecuteUpdateAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            await _session.FlushAsync();
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    private async Task RollbackMigrationAsync(MigrationInfo migration)
    {
        using var transaction = _settings.TransactionPerMigration
            ? _session.BeginTransaction()
            : null;

        try
        {
            // Execute the rollback SQL
            var statements = SplitSqlStatements(migration.DownSql!);

            foreach (var statement in statements)
            {
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    await _session.CreateSQLQuery(statement).ExecuteUpdateAsync();
                }
            }

            // Remove the migration record
            var deleteSql = $@"
                DELETE FROM {_settings.MigrationsTableName}
                WHERE version = :version";

            await _session.CreateSQLQuery(deleteSql)
                .SetParameter("version", migration.Version)
                .ExecuteUpdateAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            await _session.FlushAsync();
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    private static List<string> SplitSqlStatements(string sql)
    {
        // Split on semicolons, but be careful about strings
        // This is a simple implementation - may need enhancement for complex SQL
        var statements = new List<string>();
        var currentStatement = new StringBuilder();
        var inString = false;
        var stringChar = ' ';

        for (int i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            // Handle string literals
            if ((c == '\'' || c == '"') && (i == 0 || sql[i - 1] != '\\'))
            {
                if (!inString)
                {
                    inString = true;
                    stringChar = c;
                }
                else if (c == stringChar)
                {
                    inString = false;
                }
            }

            if (c == ';' && !inString)
            {
                var statement = currentStatement.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(statement) && !IsCommentOnly(statement))
                {
                    statements.Add(statement);
                }
                currentStatement.Clear();
            }
            else
            {
                currentStatement.Append(c);
            }
        }

        // Add any remaining statement
        var remaining = currentStatement.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(remaining) && !IsCommentOnly(remaining))
        {
            statements.Add(remaining);
        }

        return statements;
    }

    private static bool IsCommentOnly(string sql)
    {
        var lines = sql.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l));

        return lines.All(l => l.StartsWith("--") || l.StartsWith("/*"));
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
