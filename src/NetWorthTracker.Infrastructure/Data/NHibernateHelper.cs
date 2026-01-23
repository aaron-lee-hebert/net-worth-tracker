using System.Text.RegularExpressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;
using NetWorthTracker.Infrastructure.Conventions;
using NetWorthTracker.Infrastructure.Mappings;

namespace NetWorthTracker.Infrastructure.Data;

public class NHibernateHelper
{
    private readonly ISessionFactory _sessionFactory;
    private readonly ILogger<NHibernateHelper>? _logger;

    public NHibernateHelper(IConfiguration configuration, ILogger<NHibernateHelper>? logger = null)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var databaseProvider = configuration["DatabaseProvider"] ?? "SQLite";

        _logger?.LogInformation("Initializing NHibernate with provider {Provider} and connection string {ConnectionString}",
            databaseProvider, connectionString);

        var isNewDatabase = false;
        if (databaseProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            isNewDatabase = EnsureSqliteDirectoryExists(connectionString);
        }

        Configuration? nhConfig = null;
        var fluentConfig = Fluently.Configure()
            .Database(GetDatabaseConfiguration(databaseProvider, connectionString))
            .Mappings(m =>
            {
                m.FluentMappings.AddFromAssemblyOf<AccountMap>();

                // Apply naming conventions based on database provider
                if (databaseProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
                {
                    // PostgreSQL: snake_case table and column names
                    m.FluentMappings.Conventions.Add<PostgresNamingConvention>();
                }
                else
                {
                    // SQLite: PascalCase with pluralized table names
                    m.FluentMappings.Conventions.Add<DefaultNamingConvention>();
                }
            })
            .ExposeConfiguration(cfg => nhConfig = cfg);

        _sessionFactory = fluentConfig.BuildSessionFactory();

        // Create or update schema after session factory is built
        if (nhConfig != null)
        {
            CreateOrUpdateSchema(nhConfig, isNewDatabase);
        }
    }

    private void CreateOrUpdateSchema(Configuration cfg, bool isNewDatabase)
    {
        try
        {
            if (isNewDatabase)
            {
                _logger?.LogInformation("New database detected. Creating schema...");
                var schemaExport = new SchemaExport(cfg);
                schemaExport.Create(script => _logger?.LogDebug("Schema script: {Script}", script), true);
                _logger?.LogInformation("Schema created successfully");
            }
            else
            {
                _logger?.LogInformation("Existing database detected. Running SchemaUpdate...");
                var schemaUpdate = new SchemaUpdate(cfg);
                schemaUpdate.Execute(script => _logger?.LogDebug("Schema script: {Script}", script), true);

                if (schemaUpdate.Exceptions.Count > 0)
                {
                    foreach (var ex in schemaUpdate.Exceptions)
                    {
                        _logger?.LogError(ex, "SchemaUpdate exception occurred");
                    }
                }
                else
                {
                    _logger?.LogInformation("SchemaUpdate completed successfully");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create/update schema");
            throw;
        }
    }

    private static bool EnsureSqliteDirectoryExists(string connectionString)
    {
        var match = Regex.Match(connectionString, @"Data Source=([^;]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var dbPath = match.Groups[1].Value;
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Return true if this is a new database (file doesn't exist or is empty)
            return !File.Exists(dbPath) || new FileInfo(dbPath).Length == 0;
        }
        return true;
    }

    private static IPersistenceConfigurer GetDatabaseConfiguration(string provider, string connectionString)
    {
        return provider.ToLowerInvariant() switch
        {
            "sqlite" => SQLiteConfiguration.Standard
                .ConnectionString(connectionString),
            "postgresql" => PostgreSQLConfiguration.PostgreSQL83
                .ConnectionString(connectionString)
                .Driver<NpgsqlDriver>(),
            _ => throw new InvalidOperationException($"Unsupported database provider: {provider}. Use 'SQLite' or 'PostgreSQL'.")
        };
    }

    public ISession OpenSession()
    {
        return _sessionFactory.OpenSession();
    }

    public ISessionFactory SessionFactory => _sessionFactory;
}
