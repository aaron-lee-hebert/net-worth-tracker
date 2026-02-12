using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class SubscriptionMap : ClassMap<Subscription>
{
    public SubscriptionMap()
    {
        // Table name set by convention (Subscriptions for SQLite, subscriptions for PostgreSQL)

        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId).Not.Nullable();
        Map(x => x.StripeCustomerId).Not.Nullable().Length(255);
        Map(x => x.StripeSubscriptionId).Not.Nullable().Length(255);
        Map(x => x.StripePriceId).Not.Nullable().Length(255);
        Map(x => x.Status).CustomType<int>().Not.Nullable();
        Map(x => x.CurrentPeriodStart).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.CurrentPeriodEnd).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.IsDeleted).Not.Nullable().Default("0");
        Map(x => x.DeletedAt).CustomType<PostgresNullableTimestampType>();
    }
}
