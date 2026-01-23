using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.Extensions;

public static class AccountCategoryExtensions
{
    public static string GetDisplayName(this AccountCategory category)
    {
        return category switch
        {
            AccountCategory.Banking => "Banking",
            AccountCategory.Investment => "Investment",
            AccountCategory.RealEstate => "Real Estate",
            AccountCategory.VehiclesAndProperty => "Vehicles & Property",
            AccountCategory.Business => "Business",
            AccountCategory.SecuredDebt => "Secured Debt",
            AccountCategory.UnsecuredDebt => "Unsecured Debt",
            AccountCategory.OtherLiabilities => "Other Liabilities",
            _ => category.ToString()
        };
    }

    public static bool IsAssetCategory(this AccountCategory category)
    {
        return category is AccountCategory.Banking
            or AccountCategory.Investment
            or AccountCategory.RealEstate
            or AccountCategory.VehiclesAndProperty
            or AccountCategory.Business;
    }

    public static bool IsLiabilityCategory(this AccountCategory category)
    {
        return !category.IsAssetCategory();
    }

    public static IEnumerable<AccountCategory> GetAssetCategories()
    {
        return new[]
        {
            AccountCategory.Banking,
            AccountCategory.Investment,
            AccountCategory.RealEstate,
            AccountCategory.VehiclesAndProperty,
            AccountCategory.Business
        };
    }

    public static IEnumerable<AccountCategory> GetLiabilityCategories()
    {
        return new[]
        {
            AccountCategory.SecuredDebt,
            AccountCategory.UnsecuredDebt,
            AccountCategory.OtherLiabilities
        };
    }
}
