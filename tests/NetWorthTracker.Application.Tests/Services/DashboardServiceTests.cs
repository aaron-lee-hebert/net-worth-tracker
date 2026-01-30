using FluentAssertions;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Application.Services;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Application.Tests.Services;

[TestFixture]
public class DashboardServiceTests
{
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IBalanceHistoryRepository> _mockBalanceHistoryRepository = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private DashboardService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockBalanceHistoryRepository = new Mock<IBalanceHistoryRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _service = new DashboardService(
            _mockAccountRepository.Object,
            _mockBalanceHistoryRepository.Object,
            _mockAuditService.Object);
    }

    #region GetDashboardSummary Tests

    [Test]
    public async Task GetDashboardSummaryAsync_NoAccounts_ReturnsZeroTotals()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.TotalAssets.Should().Be(0);
        result.TotalLiabilities.Should().Be(0);
        result.TotalNetWorth.Should().Be(0);
        result.HasAccounts.Should().BeFalse();
        result.RecentAccounts.Should().BeEmpty();
    }

    [Test]
    public async Task GetDashboardSummaryAsync_WithAssets_CalculatesCorrectTotals()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Checking", AccountType = AccountType.Checking, CurrentBalance = 5000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 10000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Brokerage", AccountType = AccountType.Brokerage, CurrentBalance = 25000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.TotalAssets.Should().Be(40000m);
        result.TotalLiabilities.Should().Be(0);
        result.TotalNetWorth.Should().Be(40000m);
        result.HasAccounts.Should().BeTrue();
    }

    [Test]
    public async Task GetDashboardSummaryAsync_WithLiabilities_CalculatesCorrectTotals()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 5000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Auto Loan", AccountType = AccountType.AutoLoan, CurrentBalance = 15000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.TotalAssets.Should().Be(0);
        result.TotalLiabilities.Should().Be(20000m);
        result.TotalNetWorth.Should().Be(-20000m);
    }

    [Test]
    public async Task GetDashboardSummaryAsync_MixedAssetsAndLiabilities_CalculatesNetWorth()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 50000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Home", AccountType = AccountType.PrimaryResidence, CurrentBalance = 300000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Mortgage", AccountType = AccountType.Mortgage, CurrentBalance = 200000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 5000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.TotalAssets.Should().Be(350000m);
        result.TotalLiabilities.Should().Be(205000m);
        result.TotalNetWorth.Should().Be(145000m);
    }

    [Test]
    public async Task GetDashboardSummaryAsync_GroupsByCategory()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Checking", AccountType = AccountType.Checking, CurrentBalance = 5000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 10000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "401k", AccountType = AccountType.Retirement401k, CurrentBalance = 50000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.TotalsByCategory.Should().ContainKey(AccountCategory.Banking);
        result.TotalsByCategory[AccountCategory.Banking].Should().Be(15000m);
        result.TotalsByCategory.Should().ContainKey(AccountCategory.Investment);
        result.TotalsByCategory[AccountCategory.Investment].Should().Be(50000m);
    }

    [Test]
    public async Task GetDashboardSummaryAsync_ReturnsMaxFiveRecentAccounts()
    {
        // Arrange
        var accounts = Enumerable.Range(1, 7)
            .Select(i => new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = $"Account {i}",
                AccountType = AccountType.Checking,
                CurrentBalance = i * 1000m,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.RecentAccounts.Should().HaveCount(5);
    }

    [Test]
    public async Task GetDashboardSummaryAsync_RecentAccountsOrderedByMostRecent()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Old", AccountType = AccountType.Checking, CurrentBalance = 1000m, CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = null },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Updated", AccountType = AccountType.Checking, CurrentBalance = 2000m, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-1) },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "New", AccountType = AccountType.Checking, CurrentBalance = 3000m, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = null }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetDashboardSummaryAsync(_testUserId);

        // Assert
        result.RecentAccounts.First().Name.Should().Be("Updated"); // Most recently updated
    }

    #endregion

    #region GetAccountsForBulkUpdate Tests

    [Test]
    public async Task GetAccountsForBulkUpdateAsync_NoAccounts_ReturnsEmptyList()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetAccountsForBulkUpdateAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAccountsForBulkUpdateAsync_OrdersByCategoryThenName()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Zeta Brokerage", AccountType = AccountType.Brokerage, CurrentBalance = 1000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Alpha Checking", AccountType = AccountType.Checking, CurrentBalance = 2000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Beta Savings", AccountType = AccountType.Savings, CurrentBalance = 3000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetAccountsForBulkUpdateAsync(_testUserId);

        // Assert
        result.Should().HaveCount(3);
        // Banking should come before Investment
        result[0].Name.Should().Be("Alpha Checking");
        result[1].Name.Should().Be("Beta Savings");
        result[2].Name.Should().Be("Zeta Brokerage");
    }

    [Test]
    public async Task GetAccountsForBulkUpdateAsync_IncludesLiabilityFlag()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m },
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 2000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetAccountsForBulkUpdateAsync(_testUserId);

        // Assert
        result.Single(a => a.Name == "Savings").IsLiability.Should().BeFalse();
        result.Single(a => a.Name == "Credit Card").IsLiability.Should().BeTrue();
    }

    #endregion

    #region BulkUpdateBalances Tests

    [Test]
    public async Task BulkUpdateBalancesAsync_NoAccounts_ReturnsFailure()
    {
        // Arrange
        var request = new BulkUpdateRequest
        {
            Accounts = new List<AccountBalanceUpdate>(),
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _service.BulkUpdateBalancesAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("No accounts to update");
    }

    [Test]
    public async Task BulkUpdateBalancesAsync_AccountNotOwned_SkipsUpdate()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var userAccounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "My Account", AccountType = AccountType.Checking, CurrentBalance = 1000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(userAccounts);

        var request = new BulkUpdateRequest
        {
            Accounts = new List<AccountBalanceUpdate>
            {
                new AccountBalanceUpdate { AccountId = accountId, NewBalance = 5000m }
            },
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _service.BulkUpdateBalancesAsync(_testUserId, request);

        // Assert
        result.UpdatedCount.Should().Be(0);
    }

    [Test]
    public async Task BulkUpdateBalancesAsync_BalanceUnchanged_SkipsUpdate()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userAccounts = new List<Account>
        {
            new Account { Id = accountId, UserId = _testUserId, Name = "My Account", AccountType = AccountType.Checking, CurrentBalance = 1000m }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(userAccounts);

        var request = new BulkUpdateRequest
        {
            Accounts = new List<AccountBalanceUpdate>
            {
                new AccountBalanceUpdate { AccountId = accountId, NewBalance = 1000m } // Same balance
            },
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _service.BulkUpdateBalancesAsync(_testUserId, request);

        // Assert
        result.UpdatedCount.Should().Be(0);
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.IsAny<BalanceHistory>()), Times.Never);
    }

    [Test]
    public async Task BulkUpdateBalancesAsync_NewBalance_CreatesHistoryRecord()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recordedAt = DateTime.UtcNow;

        var account = new Account { Id = accountId, UserId = _testUserId, Name = "My Account", AccountType = AccountType.Checking, CurrentBalance = 1000m };
        var userAccounts = new List<Account> { account };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(userAccounts);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAndDateAsync(accountId, recordedAt))
            .ReturnsAsync((BalanceHistory?)null);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory>
            {
                new BalanceHistory { AccountId = accountId, Balance = 2000m, RecordedAt = recordedAt }
            });

        var request = new BulkUpdateRequest
        {
            Accounts = new List<AccountBalanceUpdate>
            {
                new AccountBalanceUpdate { AccountId = accountId, NewBalance = 2000m }
            },
            RecordedAt = recordedAt,
            Notes = "Monthly update"
        };

        // Act
        var result = await _service.BulkUpdateBalancesAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCount.Should().Be(1);
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.Is<BalanceHistory>(h =>
            h.AccountId == accountId &&
            h.Balance == 2000m &&
            h.Notes == "Monthly update")), Times.Once);
    }

    [Test]
    public async Task BulkUpdateBalancesAsync_ExistingRecordForDate_UpdatesExisting()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recordedAt = DateTime.UtcNow;

        var account = new Account { Id = accountId, UserId = _testUserId, Name = "My Account", AccountType = AccountType.Checking, CurrentBalance = 1000m };
        var existingRecord = new BalanceHistory { Id = Guid.NewGuid(), AccountId = accountId, Balance = 1500m, RecordedAt = recordedAt };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account> { account });

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAndDateAsync(accountId, recordedAt))
            .ReturnsAsync(existingRecord);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory> { existingRecord });

        var request = new BulkUpdateRequest
        {
            Accounts = new List<AccountBalanceUpdate>
            {
                new AccountBalanceUpdate { AccountId = accountId, NewBalance = 2000m }
            },
            RecordedAt = recordedAt
        };

        // Act
        var result = await _service.BulkUpdateBalancesAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        _mockBalanceHistoryRepository.Verify(r => r.UpdateAsync(It.Is<BalanceHistory>(h =>
            h.Id == existingRecord.Id && h.Balance == 2000m)), Times.Once);
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.IsAny<BalanceHistory>()), Times.Never);
    }

    #endregion
}
