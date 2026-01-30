using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class ProcessedJobMap : ClassMap<ProcessedJob>
{
    public ProcessedJobMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.JobType).Not.Nullable().Length(100);
        Map(x => x.JobKey).Not.Nullable().Length(500);
        Map(x => x.ProcessedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.Success).Not.Nullable();
        Map(x => x.ErrorMessage).Length(2000);
        Map(x => x.Metadata).Length(10000);

        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.UpdatedAt).CustomType<PostgresNullableTimestampType>();
    }
}
