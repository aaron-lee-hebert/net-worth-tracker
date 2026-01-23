namespace NetWorthTracker.Core.Enums;

public enum AccountType
{
    // Banking
    Checking = 0,
    Savings = 1,
    MoneyMarket = 2,
    CD = 3,
    Cash = 4,

    // Investment
    Brokerage = 10,
    Retirement401k = 11,
    Retirement401kRoth = 12,
    IraTraditional = 13,
    IraRoth = 14,
    SepIra = 15,
    Retirement403b = 16,
    Retirement457 = 17,
    Education529 = 18,
    Hsa = 19,
    Pension = 20,

    // Real Estate
    PrimaryResidence = 30,
    RentalProperty = 31,
    VacationHome = 32,
    Land = 33,

    // Vehicles & Property
    Vehicle = 40,
    Boat = 41,
    Rv = 42,
    PersonalProperty = 43,

    // Business
    BusinessAsset = 50,
    BusinessAccount = 51,

    // Secured Debt
    Mortgage = 60,
    AutoLoan = 61,
    HomeEquityLoan = 62,
    Heloc = 63,

    // Unsecured Debt
    CreditCard = 70,
    PersonalLoan = 71,
    StudentLoan = 72,
    MedicalDebt = 73,

    // Other Liabilities
    OtherLoan = 80,
    Liability = 81
}
