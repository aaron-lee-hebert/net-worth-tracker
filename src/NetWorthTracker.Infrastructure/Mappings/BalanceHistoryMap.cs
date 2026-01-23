using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Infrastructure.Mappings;

public class BalanceHistoryMap : ClassMap<BalanceHistory>
{
    public BalanceHistoryMap()
    {
        // Table name set by convention (BalanceHistories for SQLite, balance_histories for PostgreSQL)

        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.Balance).Precision(18).Scale(2).Not.Nullable();
        Map(x => x.RecordedAt).Not.Nullable();
        Map(x => x.Notes).Length(1000);
        Map(x => x.CreatedAt).Not.Nullable();
        Map(x => x.UpdatedAt);

        Map(x => x.AccountId).Not.Nullable();
        References(x => x.Account)
            // Column name set by convention (AccountId for SQLite, account_id for PostgreSQL)
            .Not.Insert()
            .Not.Update();
    }
}
