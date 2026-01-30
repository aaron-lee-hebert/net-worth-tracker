using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class AccountMap : ClassMap<Account>
{
    public AccountMap()
    {
        // Table name set by convention (Accounts for SQLite, accounts for PostgreSQL)

        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.Name).Not.Nullable().Length(200);
        Map(x => x.Description).Length(500);
        Map(x => x.AccountType).CustomType<int>();
        Map(x => x.CurrentBalance).Precision(18).Scale(2);
        Map(x => x.Institution).Length(200);
        Map(x => x.AccountNumber).Length(100);
        Map(x => x.IsActive).Not.Nullable();
        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.IsDeleted).Not.Nullable().Default("0");
        Map(x => x.DeletedAt).CustomType<PostgresNullableTimestampType>();

        Map(x => x.UserId).Not.Nullable();
        References(x => x.User)
            // Column name set by convention (UserId for SQLite, user_id for PostgreSQL)
            .Not.Insert()
            .Not.Update();

        HasMany(x => x.BalanceHistories)
            // Key column set by convention (AccountId for SQLite, account_id for PostgreSQL)
            .Inverse()
            .Cascade.AllDeleteOrphan();
    }
}
