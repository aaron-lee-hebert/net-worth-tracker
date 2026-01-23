using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Infrastructure.Data;

public class DemoDataSeeder
{
    private readonly NHibernateHelper _nhibernateHelper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DemoDataSeeder> _logger;

    private const string DemoEmail = "demo@example.com";
    private const string DemoPassword = "DemoPassword123";

    public DemoDataSeeder(
        NHibernateHelper nhibernateHelper,
        UserManager<ApplicationUser> userManager,
        ILogger<DemoDataSeeder> logger)
    {
        _nhibernateHelper = nhibernateHelper;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var existingUser = await _userManager.FindByEmailAsync(DemoEmail);
        if (existingUser != null)
        {
            _logger.LogInformation("Demo user already exists, skipping seed");
            return;
        }

        _logger.LogInformation("Creating demo user and seed data...");

        // Create demo user
        var demoUser = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "User",
            CreatedAt = DateTime.UtcNow.AddYears(-5)
        };

        var result = await _userManager.CreateAsync(demoUser, DemoPassword);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create demo user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        // Reload user to get the ID
        demoUser = await _userManager.FindByEmailAsync(DemoEmail);
        if (demoUser == null)
        {
            _logger.LogError("Failed to find demo user after creation");
            return;
        }

        using var session = _nhibernateHelper.OpenSession();
        using var transaction = session.BeginTransaction();

        try
        {
            var accounts = CreateAccounts(demoUser.Id);

            foreach (var account in accounts)
            {
                await session.SaveAsync(account);

                var balanceHistories = GenerateBalanceHistory(account);
                foreach (var history in balanceHistories)
                {
                    await session.SaveAsync(history);
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Demo data seeded successfully. Login with: {Email} / {Password}",
                DemoEmail, DemoPassword);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to seed demo data");
            throw;
        }
    }

    private List<Account> CreateAccounts(Guid userId)
    {
        return new List<Account>
        {
            // Banking
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Primary Checking",
                Description = "Main checking account for daily expenses",
                AccountType = AccountType.Checking,
                CurrentBalance = 4250.00m,
                Institution = "Chase",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-5)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Emergency Fund",
                Description = "6-month emergency fund",
                AccountType = AccountType.Savings,
                CurrentBalance = 18500.00m,
                Institution = "Marcus by Goldman Sachs",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-5)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "High-Yield Savings",
                Description = "Travel and large purchase fund",
                AccountType = AccountType.Savings,
                CurrentBalance = 8200.00m,
                Institution = "Ally Bank",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-3)
            },

            // Retirement
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "401(k)",
                Description = "Employer retirement plan",
                AccountType = AccountType.Retirement401k,
                CurrentBalance = 87500.00m,
                Institution = "Fidelity",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-5)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Roth IRA",
                Description = "Individual retirement account",
                AccountType = AccountType.IraRoth,
                CurrentBalance = 32000.00m,
                Institution = "Vanguard",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-5)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "HSA",
                Description = "Health Savings Account",
                AccountType = AccountType.Hsa,
                CurrentBalance = 12800.00m,
                Institution = "Fidelity",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-4)
            },

            // Investment
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Brokerage Account",
                Description = "Taxable investment account",
                AccountType = AccountType.Brokerage,
                CurrentBalance = 24500.00m,
                Institution = "Schwab",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-3)
            },

            // Real Estate
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Home",
                Description = "Primary residence - 3BR/2BA",
                AccountType = AccountType.PrimaryResidence,
                CurrentBalance = 385000.00m,
                Institution = "",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-4)
            },

            // Vehicle
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "2022 Honda Accord",
                Description = "Primary vehicle",
                AccountType = AccountType.Vehicle,
                CurrentBalance = 22000.00m,
                Institution = "",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-2)
            },

            // Liabilities
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Mortgage",
                Description = "30-year fixed @ 6.5%",
                AccountType = AccountType.Mortgage,
                CurrentBalance = 298000.00m,
                Institution = "Rocket Mortgage",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-4)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Auto Loan",
                Description = "5-year @ 4.9%",
                AccountType = AccountType.AutoLoan,
                CurrentBalance = 14500.00m,
                Institution = "Capital One Auto",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-2)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Chase Sapphire",
                Description = "Primary credit card - paid in full monthly",
                AccountType = AccountType.CreditCard,
                CurrentBalance = 1850.00m,
                Institution = "Chase",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-5)
            },
            new Account
            {
                Id = Guid.NewGuid(),
                Name = "Student Loans",
                Description = "Federal student loans",
                AccountType = AccountType.StudentLoan,
                CurrentBalance = 18200.00m,
                Institution = "Nelnet",
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-5)
            }
        };
    }

    private List<BalanceHistory> GenerateBalanceHistory(Account account)
    {
        var histories = new List<BalanceHistory>();
        var now = DateTime.UtcNow;
        var accountAge = (now - account.CreatedAt).TotalDays / 365.25;
        var yearsToGenerate = Math.Min(5, accountAge);
        var quarterCount = (int)(yearsToGenerate * 4);

        if (quarterCount <= 0) return histories;

        // Calculate starting balance and growth pattern based on account type
        var (startBalance, growthPattern) = GetGrowthParameters(account);

        for (int i = quarterCount; i >= 0; i--)
        {
            var recordDate = GetQuarterEndDate(now, -i);

            // Skip if before account creation
            if (recordDate < account.CreatedAt)
                continue;

            var progress = 1.0 - ((double)i / quarterCount);
            var balance = CalculateBalance(startBalance, account.CurrentBalance, progress, growthPattern, i);

            histories.Add(new BalanceHistory
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Balance = balance,
                RecordedAt = recordDate,
                CreatedAt = recordDate
            });
        }

        return histories;
    }

    private static DateTime GetQuarterEndDate(DateTime fromDate, int quartersOffset)
    {
        var targetDate = fromDate.AddMonths(quartersOffset * 3);
        var quarter = (targetDate.Month - 1) / 3;
        var quarterEndMonth = (quarter + 1) * 3;
        var quarterEnd = new DateTime(targetDate.Year, quarterEndMonth, 1).AddMonths(1).AddDays(-1);

        // If we're in the current quarter, use current date
        if (quartersOffset == 0)
            return fromDate;

        return new DateTime(quarterEnd.Year, quarterEnd.Month, quarterEnd.Day, 12, 0, 0, DateTimeKind.Utc);
    }

    private static (decimal startBalance, string growthPattern) GetGrowthParameters(Account account)
    {
        return account.AccountType switch
        {
            // Assets that grow with market returns + contributions
            AccountType.Retirement401k => (account.CurrentBalance * 0.35m, "investment"),
            AccountType.IraRoth => (account.CurrentBalance * 0.40m, "investment"),
            AccountType.Brokerage => (account.CurrentBalance * 0.45m, "investment"),
            AccountType.Hsa => (account.CurrentBalance * 0.30m, "steady"),

            // Savings - steady growth
            AccountType.Savings => (account.CurrentBalance * 0.50m, "steady"),
            AccountType.Checking => (account.CurrentBalance * 0.85m, "volatile"),

            // Real estate - appreciation
            AccountType.PrimaryResidence => (account.CurrentBalance * 0.82m, "appreciation"),

            // Vehicles - depreciation
            AccountType.Vehicle => (account.CurrentBalance * 1.35m, "depreciation"),

            // Liabilities - paying down
            AccountType.Mortgage => (account.CurrentBalance * 1.08m, "paydown"),
            AccountType.AutoLoan => (account.CurrentBalance * 1.65m, "paydown"),
            AccountType.StudentLoan => (account.CurrentBalance * 1.45m, "paydown"),
            AccountType.CreditCard => (account.CurrentBalance * 1.0m, "volatile"),

            _ => (account.CurrentBalance * 0.7m, "steady")
        };
    }

    private static decimal CalculateBalance(decimal start, decimal end, double progress, string pattern, int periodsRemaining)
    {
        var random = new Random((int)(start * 100) + periodsRemaining);
        var noise = (decimal)((random.NextDouble() - 0.5) * 0.06);

        var rawBalance = pattern switch
        {
            "investment" => CalculateInvestmentGrowth(start, end, progress, noise),
            "steady" => start + ((end - start) * (decimal)progress) + (end * noise * 0.3m),
            "volatile" => end + (end * (decimal)((random.NextDouble() - 0.5) * 0.4)),
            "appreciation" => start + ((end - start) * (decimal)Math.Pow(progress, 0.8)) + (end * noise * 0.02m),
            "depreciation" => start - ((start - end) * (decimal)Math.Pow(progress, 0.7)),
            "paydown" => start - ((start - end) * (decimal)progress) + (start * noise * 0.01m),
            _ => start + ((end - start) * (decimal)progress)
        };

        return Math.Round(Math.Max(0, rawBalance), 2);
    }

    private static decimal CalculateInvestmentGrowth(decimal start, decimal end, double progress, decimal noise)
    {
        // Simulate market with some volatility
        var baseGrowth = start + ((end - start) * (decimal)Math.Pow(progress, 0.9));

        // Add market-like fluctuations
        var marketNoise = noise * baseGrowth * 0.15m;

        return baseGrowth + marketNoise;
    }
}
