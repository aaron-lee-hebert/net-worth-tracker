using FluentNHibernate.Mapping;
using NetWorthTracker.Core.Entities;

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
        Map(x => x.CreatedAt).Not.Nullable();
        Map(x => x.ModifiedAt).Not.Nullable();
    }
}
