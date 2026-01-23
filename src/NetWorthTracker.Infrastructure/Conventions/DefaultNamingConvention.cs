using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace NetWorthTracker.Infrastructure.Conventions;

/// <summary>
/// Default naming convention for SQLite.
/// Uses PascalCase with pluralized table names.
/// Handles ASP.NET Identity tables with standard naming.
/// </summary>
public class DefaultNamingConvention :
    IClassConvention,
    IReferenceConvention,
    IHasManyConvention
{
    // ASP.NET Identity table name mappings
    private static readonly Dictionary<string, string> IdentityTableNames = new()
    {
        ["ApplicationUser"] = "AspNetUsers",
        ["ApplicationRole"] = "AspNetRoles"
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

        // Standard pluralization for other entities
        instance.Table(Pluralize(entityName));
    }

    public void Apply(IManyToOneInstance instance)
    {
        // Use PropertyName + "Id" for foreign key columns (e.g., User -> UserId)
        instance.Column(instance.Name + "Id");
    }

    public void Apply(IOneToManyCollectionInstance instance)
    {
        // Use simplified EntityName + "Id" for collection key columns
        // Strip common prefixes like "Application" for cleaner FK names
        var entityName = instance.EntityType.Name;
        if (entityName.StartsWith("Application"))
        {
            entityName = entityName["Application".Length..];
        }
        instance.Key.Column(entityName + "Id");
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
}
