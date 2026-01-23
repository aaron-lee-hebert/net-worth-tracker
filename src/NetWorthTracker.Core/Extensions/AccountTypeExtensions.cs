using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.Extensions;

public static class AccountTypeExtensions
{
    public static string GetDisplayName(this AccountType accountType)
    {
        return accountType switch
        {
            // Banking
            AccountType.Checking => "Checking",
            AccountType.Savings => "Savings",
            AccountType.MoneyMarket => "Money Market",
            AccountType.CD => "Certificate of Deposit",
            AccountType.Cash => "Cash",

            // Investment
            AccountType.Brokerage => "Brokerage",
            AccountType.Retirement401k => "401(k)",
            AccountType.Retirement401kRoth => "Roth 401(k)",
            AccountType.IraTraditional => "Traditional IRA",
            AccountType.IraRoth => "Roth IRA",
            AccountType.SepIra => "SEP IRA",
            AccountType.Retirement403b => "403(b)",
            AccountType.Retirement457 => "457",
            AccountType.Education529 => "529 Plan",
            AccountType.Hsa => "HSA",
            AccountType.Pension => "Pension",

            // Real Estate
            AccountType.PrimaryResidence => "Primary Residence",
            AccountType.RentalProperty => "Rental Property",
            AccountType.VacationHome => "Vacation Home",
            AccountType.Land => "Land",

            // Vehicles & Property
            AccountType.Vehicle => "Vehicle",
            AccountType.Boat => "Boat",
            AccountType.Rv => "RV",
            AccountType.PersonalProperty => "Personal Property",

            // Business
            AccountType.BusinessAsset => "Business Asset",
            AccountType.BusinessAccount => "Business Account",

            // Secured Debt
            AccountType.Mortgage => "Mortgage",
            AccountType.AutoLoan => "Auto Loan",
            AccountType.HomeEquityLoan => "Home Equity Loan",
            AccountType.Heloc => "HELOC",

            // Unsecured Debt
            AccountType.CreditCard => "Credit Card",
            AccountType.PersonalLoan => "Personal Loan",
            AccountType.StudentLoan => "Student Loan",
            AccountType.MedicalDebt => "Medical Debt",

            // Other Liabilities
            AccountType.OtherLoan => "Other Loan",
            AccountType.Liability => "Liability",

            _ => accountType.ToString()
        };
    }

    public static AccountCategory GetCategory(this AccountType accountType)
    {
        return accountType switch
        {
            // Banking
            AccountType.Checking or
            AccountType.Savings or
            AccountType.MoneyMarket or
            AccountType.CD or
            AccountType.Cash => AccountCategory.Banking,

            // Investment
            AccountType.Brokerage or
            AccountType.Retirement401k or
            AccountType.Retirement401kRoth or
            AccountType.IraTraditional or
            AccountType.IraRoth or
            AccountType.SepIra or
            AccountType.Retirement403b or
            AccountType.Retirement457 or
            AccountType.Education529 or
            AccountType.Hsa or
            AccountType.Pension => AccountCategory.Investment,

            // Real Estate
            AccountType.PrimaryResidence or
            AccountType.RentalProperty or
            AccountType.VacationHome or
            AccountType.Land => AccountCategory.RealEstate,

            // Vehicles & Property
            AccountType.Vehicle or
            AccountType.Boat or
            AccountType.Rv or
            AccountType.PersonalProperty => AccountCategory.VehiclesAndProperty,

            // Business
            AccountType.BusinessAsset or
            AccountType.BusinessAccount => AccountCategory.Business,

            // Secured Debt
            AccountType.Mortgage or
            AccountType.AutoLoan or
            AccountType.HomeEquityLoan or
            AccountType.Heloc => AccountCategory.SecuredDebt,

            // Unsecured Debt
            AccountType.CreditCard or
            AccountType.PersonalLoan or
            AccountType.StudentLoan or
            AccountType.MedicalDebt => AccountCategory.UnsecuredDebt,

            // Other Liabilities
            AccountType.OtherLoan or
            AccountType.Liability => AccountCategory.OtherLiabilities,

            _ => throw new ArgumentOutOfRangeException(nameof(accountType), accountType, "Unknown account type")
        };
    }

    public static bool IsAsset(this AccountType accountType)
    {
        var category = accountType.GetCategory();
        return category is AccountCategory.Banking
            or AccountCategory.Investment
            or AccountCategory.RealEstate
            or AccountCategory.VehiclesAndProperty
            or AccountCategory.Business;
    }

    public static bool IsLiability(this AccountType accountType)
    {
        return !accountType.IsAsset();
    }

    public static IEnumerable<AccountType> GetTypesByCategory(AccountCategory category)
    {
        return Enum.GetValues<AccountType>().Where(t => t.GetCategory() == category);
    }
}
