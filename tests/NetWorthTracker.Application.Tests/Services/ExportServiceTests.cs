using FluentAssertions;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Application.Services;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Tests.Services;

[TestFixture]
public class ExportServiceTests
{
    private Mock<IReportService> _mockReportService = null!;
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IBalanceHistoryRepository> _mockBalanceHistoryRepository = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private ExportService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockReportService = new Mock<IReportService>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockBalanceHistoryRepository = new Mock<IBalanceHistoryRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _service = new ExportService(
            _mockReportService.Object,
            _mockAccountRepository.Object,
            _mockBalanceHistoryRepository.Object,
            _mockAuditService.Object);
    }

    #region ExportQuarterlyReportCsv Tests

    [Test]
    public async Task ExportQuarterlyReportCsvAsync_NoAccounts_ReturnsNoData()
    {
        // Arrange
        _mockReportService.Setup(r => r.BuildQuarterlyReportAsync(_testUserId))
            .ReturnsAsync(new QuarterlyReportViewModel());

        // Act
        var result = await _service.ExportQuarterlyReportCsvAsync(_testUserId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExportQuarterlyReportCsvAsync_WithAccounts_ReturnsCsv()
    {
        // Arrange
        var report = new QuarterlyReportViewModel
        {
            Quarters = new List<string> { "Q1 2024", "Q2 2024" },
            Accounts = new List<AccountQuarterlyData>
            {
                new AccountQuarterlyData
                {
                    Name = "Savings",
                    Type = "Savings Account",
                    IsLiability = false,
                    Balances = new List<decimal?> { 10000m, 12000m }
                }
            },
            Totals = new QuarterlyTotals
            {
                NetWorth = new List<decimal> { 10000m, 12000m },
                PercentChange = new List<decimal?> { null, 20m }
            }
        };

        _mockReportService.Setup(r => r.BuildQuarterlyReportAsync(_testUserId))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ExportQuarterlyReportCsvAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Savings");
        result.Content.Should().Contain("Q1 2024");
        result.Content.Should().Contain("Q2 2024");
        result.FileName.Should().StartWith("net-worth-quarterly-report-");
        result.FileName.Should().EndWith(".csv");
    }

    [Test]
    public async Task ExportQuarterlyReportCsvAsync_IncludesNetWorthTotals()
    {
        // Arrange
        var report = new QuarterlyReportViewModel
        {
            Quarters = new List<string> { "Q1 2024" },
            Accounts = new List<AccountQuarterlyData>
            {
                new AccountQuarterlyData
                {
                    Name = "Test",
                    Type = "Checking",
                    IsLiability = false,
                    Balances = new List<decimal?> { 5000m }
                }
            },
            Totals = new QuarterlyTotals
            {
                NetWorth = new List<decimal> { 5000m },
                PercentChange = new List<decimal?> { null }
            }
        };

        _mockReportService.Setup(r => r.BuildQuarterlyReportAsync(_testUserId))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ExportQuarterlyReportCsvAsync(_testUserId);

        // Assert
        result.Content.Should().Contain("Net Worth");
        result.Content.Should().Contain("5000");
    }

    #endregion

    #region ExportNetWorthHistoryCsv Tests

    [Test]
    public async Task ExportNetWorthHistoryCsvAsync_NoData_ReturnsNoData()
    {
        // Arrange
        _mockReportService.Setup(r => r.GetNetWorthHistoryAsync(_testUserId))
            .ReturnsAsync(new NetWorthHistoryData());

        // Act
        var result = await _service.ExportNetWorthHistoryCsvAsync(_testUserId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExportNetWorthHistoryCsvAsync_WithData_ReturnsCsv()
    {
        // Arrange
        var history = new NetWorthHistoryData
        {
            Months = new List<MonthlyNetWorth>
            {
                new MonthlyNetWorth
                {
                    Month = new DateTime(2024, 1, 1),
                    TotalAssets = 50000m,
                    TotalLiabilities = 10000m,
                    NetWorth = 40000m,
                    Change = null,
                    PercentChange = null
                },
                new MonthlyNetWorth
                {
                    Month = new DateTime(2024, 2, 1),
                    TotalAssets = 55000m,
                    TotalLiabilities = 9000m,
                    NetWorth = 46000m,
                    Change = 6000m,
                    PercentChange = 15m
                }
            }
        };

        _mockReportService.Setup(r => r.GetNetWorthHistoryAsync(_testUserId))
            .ReturnsAsync(history);

        // Act
        var result = await _service.ExportNetWorthHistoryCsvAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Date,Total Assets,Total Liabilities,Net Worth,Change,% Change");
        result.Content.Should().Contain("2024-01");
        result.Content.Should().Contain("2024-02");
        result.FileName.Should().StartWith("net-worth-history-");
    }

    #endregion

    #region ExportAccountsCsv Tests

    [Test]
    public async Task ExportAccountsCsvAsync_NoAccounts_ReturnsNoData()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.ExportAccountsCsvAsync(_testUserId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExportAccountsCsvAsync_WithAccounts_ReturnsCsv()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = "My Savings",
                AccountType = AccountType.Savings,
                Institution = "Big Bank",
                AccountNumber = "1234567890",
                CurrentBalance = 25000m,
                IsActive = true
            }
        };

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.ExportAccountsCsvAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("My Savings");
        result.Content.Should().Contain("Big Bank");
        result.Content.Should().Contain("25000");
        result.FileName.Should().StartWith("accounts-");
    }

    [Test]
    public async Task ExportAccountsCsvAsync_MasksAccountNumber()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = "My Account",
                AccountType = AccountType.Checking,
                AccountNumber = "1234567890",
                CurrentBalance = 5000m,
                IsActive = true
            }
        };

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.ExportAccountsCsvAsync(_testUserId);

        // Assert
        result.Content.Should().Contain("****7890");
        result.Content.Should().NotContain("1234567890");
    }

    [Test]
    public async Task ExportAccountsCsvAsync_WithCategory_FiltersAccounts()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = "Savings",
                AccountType = AccountType.Savings,
                CurrentBalance = 5000m,
                IsActive = true
            }
        };

        _mockAccountRepository.Setup(r => r.GetByUserIdAndCategoryAsync(_testUserId, AccountCategory.Banking))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.ExportAccountsCsvAsync(_testUserId, AccountCategory.Banking);

        // Assert
        result.Success.Should().BeTrue();
        result.FileName.Should().Contain("-banking-");
    }

    [Test]
    public async Task ExportAccountsCsvAsync_IncludesTotals()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = "Savings",
                AccountType = AccountType.Savings,
                CurrentBalance = 10000m,
                IsActive = true
            },
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = "Credit Card",
                AccountType = AccountType.CreditCard,
                CurrentBalance = 2000m,
                IsActive = true
            }
        };

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.ExportAccountsCsvAsync(_testUserId);

        // Assert
        result.Content.Should().Contain("Total Assets");
        result.Content.Should().Contain("Total Liabilities");
        result.Content.Should().Contain("Net Worth");
    }

    #endregion

    #region ExportAccountHistoryCsv Tests

    [Test]
    public async Task ExportAccountHistoryCsvAsync_AccountNotFound_ReturnsNoData()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _service.ExportAccountHistoryCsvAsync(_testUserId, accountId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExportAccountHistoryCsvAsync_AccountNotOwnedByUser_ReturnsNoData()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = otherUserId, // Different user
            Name = "Other Account"
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.ExportAccountHistoryCsvAsync(_testUserId, accountId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExportAccountHistoryCsvAsync_NoHistory_ReturnsNoData()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = _testUserId,
            Name = "My Account",
            AccountType = AccountType.Checking
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory>());

        // Act
        var result = await _service.ExportAccountHistoryCsvAsync(_testUserId, accountId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No balance history found");
    }

    [Test]
    public async Task ExportAccountHistoryCsvAsync_WithHistory_ReturnsCsv()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = _testUserId,
            Name = "My Savings",
            AccountType = AccountType.Savings,
            Institution = "Big Bank"
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 5000m, RecordedAt = new DateTime(2024, 1, 15), Notes = "Initial" },
            new BalanceHistory { AccountId = accountId, Balance = 6000m, RecordedAt = new DateTime(2024, 2, 15), Notes = "Deposit" }
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(history);

        // Act
        var result = await _service.ExportAccountHistoryCsvAsync(_testUserId, accountId);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Account: My Savings");
        result.Content.Should().Contain("Institution: Big Bank");
        result.Content.Should().Contain("Date,Balance,Change,% Change,Notes");
        result.Content.Should().Contain("Initial");
        result.Content.Should().Contain("Deposit");
        result.FileName.Should().Contain("my-savings");
    }

    [Test]
    public async Task ExportAccountHistoryCsvAsync_CalculatesChangeAndPercent()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = _testUserId,
            Name = "Savings",
            AccountType = AccountType.Savings
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 10000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 12000m, RecordedAt = new DateTime(2024, 2, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(history);

        // Act
        var result = await _service.ExportAccountHistoryCsvAsync(_testUserId, accountId);

        // Assert
        result.Content.Should().Contain("2000"); // Change amount
        result.Content.Should().Contain("20"); // Percent change
    }

    #endregion
}
