using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class AuditLogMap : ClassMap<AuditLog>
{
    public AuditLogMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId);
        Map(x => x.Action).Not.Nullable().Length(100);
        Map(x => x.EntityType).Length(100);
        Map(x => x.EntityId);
        Map(x => x.OldValue).Length(10000);
        Map(x => x.NewValue).Length(10000);
        Map(x => x.Description).Length(1000);
        Map(x => x.IpAddress).Length(50);
        Map(x => x.UserAgent).Length(500);
        Map(x => x.Timestamp).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.Success).Not.Nullable();
        Map(x => x.ErrorMessage).Length(1000);

        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
    }
}
