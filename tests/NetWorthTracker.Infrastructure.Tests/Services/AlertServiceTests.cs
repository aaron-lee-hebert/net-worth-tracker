using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Infrastructure.Services;

namespace NetWorthTracker.Infrastructure.Tests.Services;

[TestFixture]
public class AlertServiceTests
{
    private Mock<IAlertConfigurationRepository> _mockConfigRepository = null!;
    private Mock<IMonthlySnapshotRepository> _mockSnapshotRepository = null!;
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IBalanceHistoryRepository> _mockBalanceHistoryRepository = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<ILogger<AlertService>> _mockLogger = null!;
    private AlertService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockConfigRepository = new Mock<IAlertConfigurationRepository>();
        _mockSnapshotRepository = new Mock<IMonthlySnapshotRepository>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockBalanceHistoryRepository = new Mock<IBalanceHistoryRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AlertService>>();

        _service = new AlertService(
            _mockConfigRepository.Object,
            _mockSnapshotRepository.Object,
            _mockAccountRepository.Object,
            _mockBalanceHistoryRepository.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    #region GetOrCreateConfiguration Tests

    [Test]
    public async Task GetOrCreateConfigurationAsync_ConfigurationExists_ReturnsExisting()
    {
        // Arrange
        var existingConfig = new AlertConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            AlertsEnabled = true,
            NetWorthChangeThreshold = 10m
        };

        _mockConfigRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(existingConfig);

        // Act
        var result = await _service.GetOrCreateConfigurationAsync(_testUserId);

        // Assert
        result.Should().BeSameAs(existingConfig);
        _mockConfigRepository.Verify(r => r.AddAsync(It.IsAny<AlertConfiguration>()), Times.Never);
    }

    [Test]
    public async Task GetOrCreateConfigurationAsync_ConfigurationDoesNotExist_CreatesNewWithDefaults()
    {
        // Arrange
        _mockConfigRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((AlertConfiguration?)null);

        AlertConfiguration? capturedConfig = null;
        _mockConfigRepository.Setup(r => r.AddAsync(It.IsAny<AlertConfiguration>()))
            .Callback<AlertConfiguration>(c => capturedConfig = c)
            .ReturnsAsync((AlertConfiguration c) => c);

        // Act
        var result = await _service.GetOrCreateConfigurationAsync(_testUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.AlertsEnabled.Should().BeTrue();
        result.NetWorthChangeThreshold.Should().Be(5m);
        result.CashRunwayMonths.Should().Be(3);
        result.MonthlySnapshotEnabled.Should().BeTrue();
        _mockConfigRepository.Verify(r => r.AddAsync(It.IsAny<AlertConfiguration>()), Times.Once);
    }

    #endregion

    #region GenerateMonthlySnapshot Tests

    [Test]
    public async Task GenerateMonthlySnapshotAsync_SnapshotExists_ReturnsExisting()
    {
        // Arrange
        var month = new DateTime(2025, 1, 1);
        var existingSnapshot = new MonthlySnapshot
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Month = month,
            NetWorth = 100000m
        };

        _mockSnapshotRepository.Setup(r => r.GetByUserIdAndMonthAsync(_testUserId, month))
            .ReturnsAsync(existingSnapshot);

        // Act
        var result = await _service.GenerateMonthlySnapshotAsync(_testUserId, month);

        // Assert
        result.Should().BeSameAs(existingSnapshot);
        _mockAccountRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GenerateMonthlySnapshotAsync_NoAccounts_ReturnsNull()
    {
        // Arrange
        var month = new DateTime(2025, 1, 15);

        _mockSnapshotRepository.Setup(r => r.GetByUserIdAndMonthAsync(_testUserId, It.IsAny<DateTime>()))
            .ReturnsAsync((MonthlySnapshot?)null);

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GenerateMonthlySnapshotAsync(_testUserId, month);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GenerateMonthlySnapshotAsync_WithAccounts_CalculatesCorrectNetWorth()
    {
        // Arrange
        var month = new DateTime(2025, 1, 15);
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, AccountType = AccountType.Checking, CurrentBalance = 10000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, AccountType = AccountType.Brokerage, CurrentBalance = 50000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, AccountType = AccountType.Mortgage, CurrentBalance = 200000m }
        };

        _mockSnapshotRepository.Setup(r => r.GetByUserIdAndMonthAsync(_testUserId, It.IsAny<DateTime>()))
            .ReturnsAsync((MonthlySnapshot?)null);

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<BalanceHistory>());

        MonthlySnapshot? capturedSnapshot = null;
        _mockSnapshotRepository.Setup(r => r.AddAsync(It.IsAny<MonthlySnapshot>()))
            .Callback<MonthlySnapshot>(s => capturedSnapshot = s)
            .ReturnsAsync((MonthlySnapshot s) => s);

        // Act
        var result = await _service.GenerateMonthlySnapshotAsync(_testUserId, month);

        // Assert
        result.Should().NotBeNull();
        result!.TotalAssets.Should().Be(60000m); // 10000 + 50000
        result.TotalLiabilities.Should().Be(200000m);
        result.NetWorth.Should().Be(-140000m); // 60000 - 200000
        result.Month.Should().Be(new DateTime(2025, 1, 1));
    }

    [Test]
    public async Task GenerateMonthlySnapshotAsync_WithPreviousSnapshot_CalculatesDelta()
    {
        // Arrange
        var month = new DateTime(2025, 2, 15);
        var previousMonth = new DateTime(2025, 1, 1);
        var startOfMonth = new DateTime(2025, 2, 1);

        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, AccountType = AccountType.Savings, CurrentBalance = 15000m }
        };

        var previousSnapshot = new MonthlySnapshot
        {
            UserId = _testUserId,
            Month = previousMonth,
            NetWorth = 10000m
        };

        _mockSnapshotRepository.Setup(r => r.GetByUserIdAndMonthAsync(_testUserId, startOfMonth))
            .ReturnsAsync((MonthlySnapshot?)null);

        _mockSnapshotRepository.Setup(r => r.GetByUserIdAndMonthAsync(_testUserId, previousMonth))
            .ReturnsAsync(previousSnapshot);

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<BalanceHistory>());

        _mockSnapshotRepository.Setup(r => r.AddAsync(It.IsAny<MonthlySnapshot>()))
            .ReturnsAsync((MonthlySnapshot s) => s);

        // Act
        var result = await _service.GenerateMonthlySnapshotAsync(_testUserId, month);

        // Assert
        result.Should().NotBeNull();
        result!.NetWorthDelta.Should().Be(5000m); // 15000 - 10000
        result.NetWorthDeltaPercent.Should().Be(50m); // (5000 / 10000) * 100
    }

    #endregion

    #region ProcessAlerts Tests

    [Test]
    public async Task ProcessAlertsAsync_EmailNotConfigured_SkipsProcessing()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(false);

        // Act
        await _service.ProcessAlertsAsync();

        // Assert
        _mockConfigRepository.Verify(r => r.GetAllEnabledAsync(), Times.Never);
    }

    [Test]
    public async Task ProcessAlertsAsync_MaxAlertsPerDayReached_StopsProcessing()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(true);

        var configs = Enumerable.Range(1, 10).Select(i => new AlertConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AlertsEnabled = true,
            NetWorthChangeThreshold = 1m,
            LastAlertedNetWorth = 10000m,
            LastNetWorthAlertSentAt = null // Not sent today
        }).ToList();

        _mockConfigRepository.Setup(r => r.GetAllEnabledAsync())
            .ReturnsAsync(configs);

        // Set up accounts with significant change for each user
        foreach (var config in configs)
        {
            _mockAccountRepository.Setup(r => r.GetByUserIdAsync(config.UserId))
                .ReturnsAsync(new List<Account>
                {
                    new Account { UserId = config.UserId, AccountType = AccountType.Savings, CurrentBalance = 20000m }
                });
        }

        // Act
        await _service.ProcessAlertsAsync();

        // Assert - should have stopped at 5 (MaxAlertsPerDay)
        _mockConfigRepository.Verify(r => r.UpdateAsync(It.IsAny<AlertConfiguration>()), Times.AtMost(5));
    }

    [Test]
    public async Task ProcessAlertsAsync_NetWorthChangeExceedsThreshold_TriggersAlert()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(true);

        var config = new AlertConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            AlertsEnabled = true,
            NetWorthChangeThreshold = 5m,
            LastAlertedNetWorth = 10000m,
            LastNetWorthAlertSentAt = null
        };

        _mockConfigRepository.Setup(r => r.GetAllEnabledAsync())
            .ReturnsAsync(new List<AlertConfiguration> { config });

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>
            {
                new Account { UserId = _testUserId, AccountType = AccountType.Savings, CurrentBalance = 11000m }
            });

        // Act
        await _service.ProcessAlertsAsync();

        // Assert
        _mockConfigRepository.Verify(r => r.UpdateAsync(It.Is<AlertConfiguration>(c =>
            c.LastNetWorthAlertSentAt != null && c.LastAlertedNetWorth == 11000m)), Times.Once);
    }

    [Test]
    public async Task ProcessAlertsAsync_NoAccounts_DoesNotTriggerAlert()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(true);

        var config = new AlertConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            AlertsEnabled = true
        };

        _mockConfigRepository.Setup(r => r.GetAllEnabledAsync())
            .ReturnsAsync(new List<AlertConfiguration> { config });

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        await _service.ProcessAlertsAsync();

        // Assert
        _mockConfigRepository.Verify(r => r.UpdateAsync(It.IsAny<AlertConfiguration>()), Times.Never);
    }

    #endregion

    #region SendPendingSnapshotEmails Tests

    [Test]
    public async Task SendPendingSnapshotEmailsAsync_EmailNotConfigured_SkipsProcessing()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(false);

        // Act
        await _service.SendPendingSnapshotEmailsAsync();

        // Assert
        _mockSnapshotRepository.Verify(r => r.GetUnsentSnapshotsAsync(), Times.Never);
    }

    [Test]
    public async Task SendPendingSnapshotEmailsAsync_UserDisabledSnapshots_MarksAsSentWithoutSending()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(true);

        var snapshot = new MonthlySnapshot
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Month = new DateTime(2025, 1, 1),
            EmailSent = false
        };

        var config = new AlertConfiguration
        {
            UserId = _testUserId,
            MonthlySnapshotEnabled = false
        };

        _mockSnapshotRepository.Setup(r => r.GetUnsentSnapshotsAsync())
            .ReturnsAsync(new List<MonthlySnapshot> { snapshot });

        _mockConfigRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(config);

        // Act
        await _service.SendPendingSnapshotEmailsAsync();

        // Assert
        _mockSnapshotRepository.Verify(r => r.UpdateAsync(It.Is<MonthlySnapshot>(s =>
            s.EmailSent == true && s.EmailSentAt == null)), Times.Once);
    }

    [Test]
    public async Task SendPendingSnapshotEmailsAsync_UserEnabledSnapshots_SendsEmail()
    {
        // Arrange
        _mockEmailService.Setup(e => e.IsConfigured).Returns(true);

        var snapshot = new MonthlySnapshot
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Month = new DateTime(2025, 1, 1),
            NetWorth = 100000m,
            EmailSent = false
        };

        var config = new AlertConfiguration
        {
            UserId = _testUserId,
            MonthlySnapshotEnabled = true
        };

        _mockSnapshotRepository.Setup(r => r.GetUnsentSnapshotsAsync())
            .ReturnsAsync(new List<MonthlySnapshot> { snapshot });

        _mockConfigRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(config);

        // Act
        await _service.SendPendingSnapshotEmailsAsync();

        // Assert
        _mockSnapshotRepository.Verify(r => r.UpdateAsync(It.Is<MonthlySnapshot>(s =>
            s.EmailSent == true && s.EmailSentAt != null)), Times.Once);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Test]
    public async Task UpdateConfigurationAsync_UpdatesRepository()
    {
        // Arrange
        var config = new AlertConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            AlertsEnabled = false
        };

        // Act
        await _service.UpdateConfigurationAsync(config);

        // Assert
        _mockConfigRepository.Verify(r => r.UpdateAsync(config), Times.Once);
    }

    #endregion
}
