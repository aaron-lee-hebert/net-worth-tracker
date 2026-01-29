using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class MonthlySnapshotMap : ClassMap<MonthlySnapshot>
{
    public MonthlySnapshotMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId).Not.Nullable();
        Map(x => x.Month).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.NetWorth).Precision(18).Scale(2);
        Map(x => x.TotalAssets).Precision(18).Scale(2);
        Map(x => x.TotalLiabilities).Precision(18).Scale(2);
        Map(x => x.NetWorthDelta).Precision(18).Scale(2);
        Map(x => x.NetWorthDeltaPercent).Precision(10).Scale(2);
        Map(x => x.BiggestContributorName).Length(200);
        Map(x => x.BiggestContributorDelta).Precision(18).Scale(2);
        Map(x => x.BiggestContributorPositive);
        Map(x => x.Interpretation).Length(500);
        Map(x => x.EmailSent).Not.Nullable();
        Map(x => x.EmailSentAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();

        References(x => x.User)
            .Not.Insert()
            .Not.Update();
    }
}
