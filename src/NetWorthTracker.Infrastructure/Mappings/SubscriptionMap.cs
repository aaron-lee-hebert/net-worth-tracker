using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class SubscriptionMap : ClassMap<Subscription>
{
    public SubscriptionMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId).Not.Nullable().UniqueKey("UK_Subscription_UserId");
        Map(x => x.StripeCustomerId).Length(255);
        Map(x => x.StripeSubscriptionId).Length(255);
        Map(x => x.Status).CustomType<int>().Not.Nullable();
        Map(x => x.CurrentPeriodEnd).CustomType<PostgresNullableTimestampType>();
        Map(x => x.TrialStartedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.TrialEndsAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();

        References(x => x.User)
            .Not.Insert()
            .Not.Update();
    }
}
