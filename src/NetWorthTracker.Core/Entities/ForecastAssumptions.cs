namespace NetWorthTracker.Core.Entities;

public class ForecastAssumptions
{
    public virtual Guid Id { get; set; }
    public virtual Guid UserId { get; set; }

    // Asset growth rates (annual, as decimals e.g., 0.07 = 7%)
    public virtual decimal? InvestmentGrowthRate { get; set; }
    public virtual decimal? RealEstateGrowthRate { get; set; }
    public virtual decimal? BankingGrowthRate { get; set; }
    public virtual decimal? BusinessGrowthRate { get; set; }
    public virtual decimal? VehicleDepreciationRate { get; set; }

    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime ModifiedAt { get; set; }

    // Default values (static, for reference)
    public static class Defaults
    {
        public const decimal InvestmentGrowthRate = 0.07m;      // 7%
        public const decimal RealEstateGrowthRate = 0.02m;      // 2%
        public const decimal BankingGrowthRate = 0.005m;        // 0.5%
        public const decimal BusinessGrowthRate = 0.03m;        // 3%
        public const decimal VehicleDepreciationRate = 0.15m;   // 15%
    }

    public virtual decimal GetInvestmentRate() => InvestmentGrowthRate ?? Defaults.InvestmentGrowthRate;
    public virtual decimal GetRealEstateRate() => RealEstateGrowthRate ?? Defaults.RealEstateGrowthRate;
    public virtual decimal GetBankingRate() => BankingGrowthRate ?? Defaults.BankingGrowthRate;
    public virtual decimal GetBusinessRate() => BusinessGrowthRate ?? Defaults.BusinessGrowthRate;
    public virtual decimal GetVehicleRate() => VehicleDepreciationRate ?? Defaults.VehicleDepreciationRate;

    public virtual bool HasCustomOverrides()
    {
        return InvestmentGrowthRate.HasValue ||
               RealEstateGrowthRate.HasValue ||
               BankingGrowthRate.HasValue ||
               BusinessGrowthRate.HasValue ||
               VehicleDepreciationRate.HasValue;
    }

    public virtual void ResetToDefaults()
    {
        InvestmentGrowthRate = null;
        RealEstateGrowthRate = null;
        BankingGrowthRate = null;
        BusinessGrowthRate = null;
        VehicleDepreciationRate = null;
        ModifiedAt = DateTime.UtcNow;
    }
}
