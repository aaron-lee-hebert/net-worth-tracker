using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class AlertConfigurationMap : ClassMap<AlertConfiguration>
{
    public AlertConfigurationMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId).Not.Nullable().UniqueKey("UK_AlertConfiguration_UserId");
        Map(x => x.AlertsEnabled).Not.Nullable();
        Map(x => x.NetWorthChangeThreshold).Precision(5).Scale(2);
        Map(x => x.CashRunwayMonths);
        Map(x => x.MonthlySnapshotEnabled).Not.Nullable();
        Map(x => x.LastNetWorthAlertSentAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.LastCashRunwayAlertSentAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.LastMonthlySnapshotSentAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.LastAlertedNetWorth).Precision(18).Scale(2);
        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.IsDeleted).Not.Nullable().Default("0");
        Map(x => x.DeletedAt).CustomType<PostgresNullableTimestampType>();

        References(x => x.User)
            .Not.Insert()
            .Not.Update();
    }
}
