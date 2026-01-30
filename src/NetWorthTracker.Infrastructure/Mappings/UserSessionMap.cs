using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class UserSessionMap : ClassMap<UserSession>
{
    public UserSessionMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId).Not.Nullable().Index("IX_UserSession_UserId");
        Map(x => x.SessionToken).Not.Nullable().Length(500).Unique();
        Map(x => x.UserAgent).Length(500);
        Map(x => x.IpAddress).Length(50);
        Map(x => x.DeviceName).Length(200);
        Map(x => x.LastActivityAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.ExpiresAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.IsRevoked).Not.Nullable();
        Map(x => x.RevocationReason).Length(500);

        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
    }
}
