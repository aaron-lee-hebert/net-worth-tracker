using System.Text.RegularExpressions;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace NetWorthTracker.Infrastructure.Conventions;

/// <summary>
/// Naming convention for PostgreSQL that converts PascalCase names to snake_case.
/// PostgreSQL convention uses lowercase with underscores for table and column names.
/// Handles ASP.NET Identity tables with snake_case naming.
/// </summary>
public partial class PostgresNamingConvention :
    IClassConvention,
    IPropertyConvention,
    IIdConvention,
    IReferenceConvention,
    IHasManyConvention,
    IHasOneConvention
{
    // ASP.NET Identity table name mappings for PostgreSQL (snake_case)
    private static readonly Dictionary<string, string> IdentityTableNames = new()
    {
        ["ApplicationUser"] = "asp_net_users",
        ["ApplicationRole"] = "asp_net_roles"
    };

    public void Apply(IClassInstance instance)
    {
        var entityName = instance.EntityType.Name;

        // Check for ASP.NET Identity entities
        if (IdentityTableNames.TryGetValue(entityName, out var identityTableName))
        {
            instance.Table(identityTableName);
            return;
        }

        // Standard snake_case pluralization for other entities
        var tableName = Pluralize(entityName);
        instance.Table(ToSnakeCase(tableName));
    }

    public void Apply(IPropertyInstance instance)
    {
        instance.Column(ToSnakeCase(instance.Name));
    }

    public void Apply(IIdentityInstance instance)
    {
        instance.Column(ToSnakeCase(instance.Name));
    }

    public void Apply(IManyToOneInstance instance)
    {
        instance.Column(ToSnakeCase(instance.Name) + "_id");
    }

    public void Apply(IOneToManyCollectionInstance instance)
    {
        // Use simplified EntityName for collection key columns
        // Strip common prefixes like "Application" for cleaner FK names
        var entityName = instance.EntityType.Name;
        if (entityName.StartsWith("Application"))
        {
            entityName = entityName["Application".Length..];
        }
        instance.Key.Column(ToSnakeCase(entityName) + "_id");
    }

    public void Apply(IOneToOneInstance instance)
    {
        instance.ForeignKey(ToSnakeCase(instance.Name) + "_id");
    }

    private static string Pluralize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Handle special cases for proper pluralization
        if (name.EndsWith("y") && !name.EndsWith("ay") && !name.EndsWith("ey") && !name.EndsWith("oy") && !name.EndsWith("uy"))
        {
            return name[..^1] + "ies";
        }
        else if (name.EndsWith("s") || name.EndsWith("x") || name.EndsWith("ch") || name.EndsWith("sh"))
        {
            return name + "es";
        }
        else
        {
            return name + "s";
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Insert underscore before uppercase letters and convert to lowercase
        // Handle consecutive uppercase letters (like "ID" -> "id", "URL" -> "url")
        var result = PascalCaseRegex().Replace(name, "$1_$2");
        return result.ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex PascalCaseRegex();
}
