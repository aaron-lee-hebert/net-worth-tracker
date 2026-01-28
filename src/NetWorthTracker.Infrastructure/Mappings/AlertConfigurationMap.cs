using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;

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
        Map(x => x.LastNetWorthAlertSentAt);
        Map(x => x.LastCashRunwayAlertSentAt);
        Map(x => x.LastMonthlySnapshotSentAt);
        Map(x => x.LastAlertedNetWorth).Precision(18).Scale(2);
        Map(x => x.CreatedAt).Not.Nullable();
        Map(x => x.UpdatedAt);

        References(x => x.User)
            .Not.Insert()
            .Not.Update();
    }
}
