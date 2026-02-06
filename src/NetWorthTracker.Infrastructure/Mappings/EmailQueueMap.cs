using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class EmailQueueMap : ClassMap<EmailQueue>
{
    public EmailQueueMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.ToEmail).Not.Nullable().Length(500);
        Map(x => x.Subject).Not.Nullable().Length(500);
        Map(x => x.HtmlBody).Not.Nullable().Length(50000);
        Map(x => x.Status).CustomType<int>().Not.Nullable();
        Map(x => x.AttemptCount).Not.Nullable();
        Map(x => x.MaxAttempts).Not.Nullable();
        Map(x => x.LastAttemptAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.NextAttemptAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.SentAt).CustomType<PostgresNullableTimestampType>();
        Map(x => x.ErrorMessage).Length(2000);
        Map(x => x.IdempotencyKey).Length(100).Index("IX_EmailQueue_IdempotencyKey");

        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
    }
}
