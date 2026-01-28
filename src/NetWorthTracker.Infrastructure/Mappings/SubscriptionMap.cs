using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;

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
        Map(x => x.CurrentPeriodEnd);
        Map(x => x.TrialStartedAt).Not.Nullable();
        Map(x => x.TrialEndsAt).Not.Nullable();
        Map(x => x.CreatedAt).Not.Nullable();
        Map(x => x.UpdatedAt);

        References(x => x.User)
            .Not.Insert()
            .Not.Update();
    }
}
