using FluentAssertions;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Services;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Tests.Services;

[TestFixture]
public class ForecastServiceTests
{
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IBalanceHistoryRepository> _mockBalanceHistoryRepository = null!;
    private Mock<IForecastAssumptionsRepository> _mockAssumptionsRepository = null!;
    private ForecastService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockBalanceHistoryRepository = new Mock<IBalanceHistoryRepository>();
        _mockAssumptionsRepository = new Mock<IForecastAssumptionsRepository>();
        _service = new ForecastService(
            _mockAccountRepository.Object,
            _mockBalanceHistoryRepository.Object,
            _mockAssumptionsRepository.Object);
    }

    #region GetForecastData Tests

    [Test]
    public async Task GetForecastDataAsync_NoAccounts_ReturnsEmptyViewModel()
    {
        // Arrange
        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId);

        // Assert
        result.Accounts.Should().BeEmpty();
        result.Labels.Should().BeEmpty();
    }

    [Test]
    public async Task GetForecastDataAsync_NoHistory_ReturnsEmptyViewModel()
    {
        // Arrange
        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<BalanceHistory>());

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId);

        // Assert
        result.Accounts.Should().BeEmpty();
    }

    [Test]
    public async Task GetForecastDataAsync_WithAccounts_ReturnsForecasts()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Brokerage", AccountType = AccountType.Brokerage, CurrentBalance = 50000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 45000m, RecordedAt = DateTime.UtcNow.AddMonths(-6) },
            new BalanceHistory { AccountId = accountId, Balance = 47500m, RecordedAt = DateTime.UtcNow.AddMonths(-3) },
            new BalanceHistory { AccountId = accountId, Balance = 50000m, RecordedAt = DateTime.UtcNow }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId, 12);

        // Assert
        result.Accounts.Should().HaveCount(1);
        result.Accounts[0].Name.Should().Be("Brokerage");
        result.Accounts[0].CurrentBalance.Should().Be(50000m);
        result.ForecastMonths.Should().Be(12);
    }

    [Test]
    public async Task GetForecastDataAsync_InvestmentAccount_ProjectsGrowth()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Brokerage", AccountType = AccountType.Brokerage, CurrentBalance = 100000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 100000m, RecordedAt = DateTime.UtcNow }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId, 48); // 4 years

        // Assert
        result.Accounts.Should().HaveCount(1);
        result.Accounts[0].ProjectedBalance.Should().BeGreaterThan(100000m); // Should show growth
    }

    [Test]
    public async Task GetForecastDataAsync_VehicleAccount_ProjectsDepreciation()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Car", AccountType = AccountType.Vehicle, CurrentBalance = 25000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 25000m, RecordedAt = DateTime.UtcNow }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId, 48); // 4 years

        // Assert
        result.Accounts.Should().HaveCount(1);
        result.Accounts[0].ProjectedBalance.Should().BeLessThan(25000m); // Should show depreciation
    }

    [Test]
    public async Task GetForecastDataAsync_DebtAccount_ProjectsPayoff()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        var accounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 5000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = accountId, Balance = 6000m, RecordedAt = DateTime.UtcNow.AddMonths(-3) },
            new BalanceHistory { AccountId = accountId, Balance = 5500m, RecordedAt = DateTime.UtcNow.AddMonths(-2) },
            new BalanceHistory { AccountId = accountId, Balance = 5000m, RecordedAt = DateTime.UtcNow }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId, 60);

        // Assert
        result.Accounts.Should().HaveCount(1);
        result.Accounts[0].IsLiability.Should().BeTrue();
        result.Accounts[0].ProjectedBalance.Should().BeLessThanOrEqualTo(5000m);
    }

    [Test]
    public async Task GetForecastDataAsync_CalculatesSummary()
    {
        // Arrange
        var assetAccountId = Guid.NewGuid();
        var liabilityAccountId = Guid.NewGuid();

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        var accounts = new List<Account>
        {
            new Account { Id = assetAccountId, UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 20000m },
            new Account { Id = liabilityAccountId, UserId = _testUserId, Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 5000m }
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { AccountId = assetAccountId, Balance = 20000m, RecordedAt = DateTime.UtcNow },
            new BalanceHistory { AccountId = liabilityAccountId, Balance = 5000m, RecordedAt = DateTime.UtcNow }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetForecastDataAsync(_testUserId, 12);

        // Assert
        result.Summary.Should().NotBeNull();
        result.Summary.CurrentAssets.Should().Be(20000m);
        result.Summary.CurrentLiabilities.Should().Be(5000m);
        result.Summary.CurrentNetWorth.Should().Be(15000m);
    }

    #endregion

    #region GetAssumptions Tests

    [Test]
    public async Task GetAssumptionsAsync_NoCustomAssumptions_ReturnsDefaults()
    {
        // Arrange
        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        // Act
        var result = await _service.GetAssumptionsAsync(_testUserId);

        // Assert
        result.InvestmentGrowthRate.Should().Be(7m); // Default 7%
        result.RealEstateGrowthRate.Should().Be(2m); // Default 2%
        result.VehicleDepreciationRate.Should().Be(15m); // Default 15%
        result.HasCustomOverrides.Should().BeFalse();
    }

    [Test]
    public async Task GetAssumptionsAsync_WithCustomAssumptions_ReturnsCustomValues()
    {
        // Arrange
        var assumptions = new ForecastAssumptions
        {
            UserId = _testUserId,
            InvestmentGrowthRate = 0.10m,
            RealEstateGrowthRate = 0.03m,
            VehicleDepreciationRate = 0.20m
        };

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(assumptions);

        // Act
        var result = await _service.GetAssumptionsAsync(_testUserId);

        // Assert
        result.InvestmentGrowthRate.Should().Be(10m);
        result.RealEstateGrowthRate.Should().Be(3m);
        result.VehicleDepreciationRate.Should().Be(20m);
    }

    #endregion

    #region SaveAssumptions Tests

    [Test]
    public async Task SaveAssumptionsAsync_UpdatesRepository()
    {
        // Arrange
        var assumptions = new ForecastAssumptions { UserId = _testUserId };

        _mockAssumptionsRepository.Setup(r => r.GetOrCreateAsync(_testUserId))
            .ReturnsAsync(assumptions);

        var model = new ForecastAssumptionsViewModel
        {
            InvestmentGrowthRate = 8m,
            RealEstateGrowthRate = 3m,
            BankingGrowthRate = 1m,
            BusinessGrowthRate = 5m,
            VehicleDepreciationRate = 12m
        };

        // Act
        await _service.SaveAssumptionsAsync(_testUserId, model);

        // Assert
        _mockAssumptionsRepository.Verify(r => r.UpdateAsync(It.Is<ForecastAssumptions>(a =>
            a.InvestmentGrowthRate == 0.08m &&
            a.RealEstateGrowthRate == 0.03m &&
            a.VehicleDepreciationRate == 0.12m)), Times.Once);
    }

    [Test]
    public async Task SaveAssumptionsAsync_ConvertsPercentToDecimal()
    {
        // Arrange
        var assumptions = new ForecastAssumptions { UserId = _testUserId };

        _mockAssumptionsRepository.Setup(r => r.GetOrCreateAsync(_testUserId))
            .ReturnsAsync(assumptions);

        var model = new ForecastAssumptionsViewModel
        {
            InvestmentGrowthRate = 7m, // 7%
            RealEstateGrowthRate = 2m,
            BankingGrowthRate = 0.5m,
            BusinessGrowthRate = 3m,
            VehicleDepreciationRate = 15m
        };

        // Act
        await _service.SaveAssumptionsAsync(_testUserId, model);

        // Assert
        assumptions.InvestmentGrowthRate.Should().Be(0.07m);
        assumptions.RealEstateGrowthRate.Should().Be(0.02m);
        assumptions.BankingGrowthRate.Should().Be(0.005m);
    }

    #endregion

    #region ResetAssumptions Tests

    [Test]
    public async Task ResetAssumptionsAsync_NoExistingAssumptions_DoesNotThrow()
    {
        // Arrange
        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((ForecastAssumptions?)null);

        // Act
        Func<Task> act = async () => await _service.ResetAssumptionsAsync(_testUserId);

        // Assert
        await act.Should().NotThrowAsync();
        _mockAssumptionsRepository.Verify(r => r.UpdateAsync(It.IsAny<ForecastAssumptions>()), Times.Never);
    }

    [Test]
    public async Task ResetAssumptionsAsync_WithExistingAssumptions_ResetsToDefaults()
    {
        // Arrange
        var assumptions = new ForecastAssumptions
        {
            UserId = _testUserId,
            InvestmentGrowthRate = 0.10m,
            RealEstateGrowthRate = 0.05m
        };

        _mockAssumptionsRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(assumptions);

        // Act
        await _service.ResetAssumptionsAsync(_testUserId);

        // Assert
        _mockAssumptionsRepository.Verify(r => r.UpdateAsync(assumptions), Times.Once);
    }

    #endregion
}
