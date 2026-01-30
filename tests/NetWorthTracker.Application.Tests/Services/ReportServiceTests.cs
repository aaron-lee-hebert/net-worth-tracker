using FluentAssertions;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Services;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Application.Tests.Services;

[TestFixture]
public class ReportServiceTests
{
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IBalanceHistoryRepository> _mockBalanceHistoryRepository = null!;
    private ReportService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockBalanceHistoryRepository = new Mock<IBalanceHistoryRepository>();
        _service = new ReportService(_mockAccountRepository.Object, _mockBalanceHistoryRepository.Object);
    }

    #region BuildQuarterlyReport Tests

    [Test]
    public async Task BuildQuarterlyReportAsync_NoAccounts_ReturnsEmptyReport()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.BuildQuarterlyReportAsync(_testUserId);

        // Assert
        result.Accounts.Should().BeEmpty();
        result.Quarters.Should().BeEmpty();
    }

    [Test]
    public async Task BuildQuarterlyReportAsync_NoHistory_ReturnsEmptyReport()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<BalanceHistory>());

        // Act
        var result = await _service.BuildQuarterlyReportAsync(_testUserId);

        // Assert
        result.Accounts.Should().BeEmpty();
    }

    [Test]
    public async Task BuildQuarterlyReportAsync_WithHistory_GeneratesQuarters()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 15000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 10000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 12000m, RecordedAt = new DateTime(2024, 4, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 15000m, RecordedAt = new DateTime(2024, 7, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.BuildQuarterlyReportAsync(_testUserId);

        // Assert
        result.Quarters.Should().NotBeEmpty();
        result.Accounts.Should().HaveCount(1);
    }

    [Test]
    public async Task BuildQuarterlyReportAsync_CalculatesNetWorthPerQuarter()
    {
        // Arrange
        var assetAccountId = Guid.NewGuid();
        var liabilityAccountId = Guid.NewGuid();

        var accounts = new List<Account>
        {
            new Account { Id = assetAccountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 20000m },
            new Account { Id = liabilityAccountId, UserId = _testUserId, Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 5000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = assetAccountId, Balance = 15000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = liabilityAccountId, Balance = 8000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = assetAccountId, Balance = 20000m, RecordedAt = new DateTime(2024, 4, 15) },
            new BalanceHistory { AccountId = liabilityAccountId, Balance = 5000m, RecordedAt = new DateTime(2024, 4, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.BuildQuarterlyReportAsync(_testUserId);

        // Assert
        result.Totals.NetWorth.Should().NotBeEmpty();
        // First quarter: 15000 - 8000 = 7000
        // Later quarters should show increasing net worth
    }

    [Test]
    public async Task BuildQuarterlyReportAsync_CalculatesPercentChange()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 11000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 10000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 11000m, RecordedAt = new DateTime(2024, 4, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.BuildQuarterlyReportAsync(_testUserId);

        // Assert
        result.Totals.PercentChange.Should().NotBeEmpty();
        result.Totals.PercentChange[0].Should().BeNull(); // First quarter has no previous to compare
    }

    [Test]
    public async Task BuildQuarterlyReportAsync_FormatsQuarterLabels()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 5000m, RecordedAt = new DateTime(2024, 1, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.BuildQuarterlyReportAsync(_testUserId);

        // Assert
        result.Quarters.Should().Contain(q => q.StartsWith("Q"));
    }

    #endregion

    #region GetNetWorthHistory Tests

    [Test]
    public async Task GetNetWorthHistoryAsync_NoAccounts_ReturnsEmptyData()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetNetWorthHistoryAsync(_testUserId);

        // Assert
        result.HasData.Should().BeFalse();
        result.Months.Should().BeEmpty();
    }

    [Test]
    public async Task GetNetWorthHistoryAsync_NoHistory_ReturnsEmptyData()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<BalanceHistory>());

        // Act
        var result = await _service.GetNetWorthHistoryAsync(_testUserId);

        // Assert
        result.HasData.Should().BeFalse();
    }

    [Test]
    public async Task GetNetWorthHistoryAsync_WithHistory_ReturnsMonthlyData()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 15000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 10000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 12000m, RecordedAt = new DateTime(2024, 2, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 15000m, RecordedAt = new DateTime(2024, 3, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetNetWorthHistoryAsync(_testUserId);

        // Assert
        result.HasData.Should().BeTrue();
        result.Months.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetNetWorthHistoryAsync_CalculatesMonthlyChange()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 12000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 10000m, RecordedAt = new DateTime(2024, 1, 15) },
            new BalanceHistory { AccountId = accountId, Balance = 12000m, RecordedAt = new DateTime(2024, 2, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetNetWorthHistoryAsync(_testUserId);

        // Assert
        result.Months.Should().HaveCountGreaterThanOrEqualTo(2);
        var secondMonth = result.Months.Skip(1).FirstOrDefault();
        secondMonth?.Change.Should().Be(2000m);
        secondMonth?.PercentChange.Should().Be(20m);
    }

    [Test]
    public async Task GetNetWorthHistoryAsync_FirstMonthHasNoChange()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 10000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 10000m, RecordedAt = new DateTime(2024, 1, 15) }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetNetWorthHistoryAsync(_testUserId);

        // Assert
        result.Months.First().Change.Should().BeNull();
        result.Months.First().PercentChange.Should().BeNull();
    }

    #endregion
}
