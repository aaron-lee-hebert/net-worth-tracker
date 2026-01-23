using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class ApplicationUserMap : ClassMap<ApplicationUser>
{
    public ApplicationUserMap()
    {
        // Table name set by convention (AspNetUsers for SQLite, asp_net_users for PostgreSQL)

        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserName).Length(256);
        Map(x => x.NormalizedUserName).Length(256);
        Map(x => x.Email).Length(256);
        Map(x => x.NormalizedEmail).Length(256);
        Map(x => x.EmailConfirmed);
        Map(x => x.PasswordHash).Length(int.MaxValue);
        Map(x => x.SecurityStamp).Length(int.MaxValue);
        Map(x => x.ConcurrencyStamp).Length(int.MaxValue);
        Map(x => x.PhoneNumber).Length(int.MaxValue);
        Map(x => x.PhoneNumberConfirmed);
        Map(x => x.TwoFactorEnabled);
        Map(x => x.LockoutEnd).CustomType<DateTimeOffsetUserType>();
        Map(x => x.LockoutEnabled);
        Map(x => x.AccessFailedCount);

        Map(x => x.FirstName).Length(100);
        Map(x => x.LastName).Length(100);
        Map(x => x.CreatedAt);
        Map(x => x.UpdatedAt);

        // MFA properties
        Map(x => x.AuthenticatorKey).Length(500);
        Map(x => x.RecoveryCodes).Length(int.MaxValue);

        HasMany(x => x.Accounts)
            // Key column set by convention (UserId for SQLite, user_id for PostgreSQL)
            .Inverse()
            .Cascade.AllDeleteOrphan();
    }
}
