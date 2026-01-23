using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Infrastructure.Mappings;

public class ApplicationRoleMap : ClassMap<ApplicationRole>
{
    public ApplicationRoleMap()
    {
        // Table name set by convention (AspNetRoles for SQLite, asp_net_roles for PostgreSQL)

        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.Name).Length(256);
        Map(x => x.NormalizedName).Length(256);
        Map(x => x.ConcurrencyStamp).Length(int.MaxValue);
    }
}
