using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure.Types;

namespace NetWorthTracker.Infrastructure.Mappings;

public class ForecastAssumptionsMap : ClassMap<ForecastAssumptions>
{
    public ForecastAssumptionsMap()
    {
        Id(x => x.Id).GeneratedBy.GuidComb();

        Map(x => x.UserId).Not.Nullable().UniqueKey("UK_ForecastAssumptions_UserId");
        Map(x => x.InvestmentGrowthRate).Precision(5).Scale(4);
        Map(x => x.RealEstateGrowthRate).Precision(5).Scale(4);
        Map(x => x.BankingGrowthRate).Precision(5).Scale(4);
        Map(x => x.BusinessGrowthRate).Precision(5).Scale(4);
        Map(x => x.VehicleDepreciationRate).Precision(5).Scale(4);
        Map(x => x.CreatedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.ModifiedAt).CustomType<PostgresTimestampType>().Not.Nullable();
        Map(x => x.IsDeleted).Not.Nullable().Default("0");
        Map(x => x.DeletedAt).CustomType<PostgresNullableTimestampType>();
    }
}
