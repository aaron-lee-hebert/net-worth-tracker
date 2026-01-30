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
public class AccountManagementServiceTests
{
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IBalanceHistoryRepository> _mockBalanceHistoryRepository = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private AccountManagementService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockBalanceHistoryRepository = new Mock<IBalanceHistoryRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _service = new AccountManagementService(
            _mockAccountRepository.Object,
            _mockBalanceHistoryRepository.Object,
            _mockAuditService.Object);
    }

    #region GetAccounts Tests

    [Test]
    public async Task GetAccountsAsync_NoAccounts_ReturnsEmptyList()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetAccountsAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAccountsAsync_WithAccounts_ReturnsMappedViewModels()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Name = "Savings",
                Description = "My savings account",
                AccountType = AccountType.Savings,
                CurrentBalance = 10000m,
                Institution = "Big Bank",
                AccountNumber = "12345",
                IsActive = true
            }
        };

        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetAccountsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Savings");
        result[0].Description.Should().Be("My savings account");
        result[0].AccountType.Should().Be(AccountType.Savings);
        result[0].CurrentBalance.Should().Be(10000m);
    }

    [Test]
    public async Task GetAccountsAsync_WithCategory_FiltersAccounts()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m }
        };

        _mockAccountRepository.Setup(r => r.GetByUserIdAndCategoryAsync(_testUserId, AccountCategory.Banking))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetAccountsAsync(_testUserId, AccountCategory.Banking);

        // Assert
        result.Should().HaveCount(1);
        _mockAccountRepository.Verify(r => r.GetByUserIdAndCategoryAsync(_testUserId, AccountCategory.Banking), Times.Once);
    }

    #endregion

    #region GetAccountDetails Tests

    [Test]
    public async Task GetAccountDetailsAsync_AccountNotFound_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _service.GetAccountDetailsAsync(_testUserId, accountId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAccountDetailsAsync_AccountNotOwnedByUser_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = otherUserId,
            Name = "Other User's Account"
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.GetAccountDetailsAsync(_testUserId, accountId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAccountDetailsAsync_ValidAccount_ReturnsDetailsWithHistory()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = _testUserId,
            Name = "My Account",
            AccountType = AccountType.Checking,
            CurrentBalance = 5000m
        };

        var history = new List<BalanceHistory>
        {
            new BalanceHistory { Id = Guid.NewGuid(), AccountId = accountId, Balance = 4000m, RecordedAt = DateTime.UtcNow.AddDays(-30) },
            new BalanceHistory { Id = Guid.NewGuid(), AccountId = accountId, Balance = 5000m, RecordedAt = DateTime.UtcNow }
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetAccountDetailsAsync(_testUserId, accountId);

        // Assert
        result.Should().NotBeNull();
        result!.Account.Name.Should().Be("My Account");
        result.BalanceHistory.Should().HaveCount(2);
    }

    #endregion

    #region CreateAccount Tests

    [Test]
    public async Task CreateAccountAsync_FirstAccount_ReturnsIsFirstAccountTrue()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        var model = new AccountCreateViewModel
        {
            Name = "New Account",
            AccountType = AccountType.Checking,
            CurrentBalance = 1000m
        };

        // Act
        var result = await _service.CreateAccountAsync(_testUserId, model);

        // Assert
        result.IsFirstAccount.Should().BeTrue();
    }

    [Test]
    public async Task CreateAccountAsync_NotFirstAccount_ReturnsIsFirstAccountFalse()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = _testUserId, Name = "Existing" }
            });

        var model = new AccountCreateViewModel
        {
            Name = "New Account",
            AccountType = AccountType.Checking,
            CurrentBalance = 1000m
        };

        // Act
        var result = await _service.CreateAccountAsync(_testUserId, model);

        // Assert
        result.IsFirstAccount.Should().BeFalse();
    }

    [Test]
    public async Task CreateAccountAsync_CreatesAccountAndInitialHistory()
    {
        // Arrange
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

        var model = new AccountCreateViewModel
        {
            Name = "My Savings",
            Description = "Primary savings",
            AccountType = AccountType.Savings,
            CurrentBalance = 5000m,
            Institution = "Big Bank",
            AccountNumber = "12345"
        };

        Account? createdAccount = null;
        _mockAccountRepository.Setup(r => r.AddAsync(It.IsAny<Account>()))
            .Callback<Account>(a => createdAccount = a)
            .ReturnsAsync((Account a) => a);

        // Act
        var result = await _service.CreateAccountAsync(_testUserId, model);

        // Assert
        _mockAccountRepository.Verify(r => r.AddAsync(It.Is<Account>(a =>
            a.Name == "My Savings" &&
            a.AccountType == AccountType.Savings &&
            a.CurrentBalance == 5000m &&
            a.UserId == _testUserId &&
            a.IsActive == true)), Times.Once);

        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.Is<BalanceHistory>(h =>
            h.Balance == 5000m &&
            h.Notes == "Initial balance")), Times.Once);
    }

    #endregion

    #region UpdateAccount Tests

    [Test]
    public async Task UpdateAccountAsync_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        var model = new AccountEditViewModel { Id = accountId, Name = "Updated" };

        // Act
        var result = await _service.UpdateAccountAsync(_testUserId, accountId, model);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAccountAsync_AccountNotOwnedByUser_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, UserId = Guid.NewGuid(), Name = "Other" };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        var model = new AccountEditViewModel { Id = accountId, Name = "Updated" };

        // Act
        var result = await _service.UpdateAccountAsync(_testUserId, accountId, model);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAccountAsync_BalanceChanged_CreatesHistoryRecord()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            UserId = _testUserId,
            Name = "My Account",
            CurrentBalance = 5000m
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        var model = new AccountEditViewModel
        {
            Id = accountId,
            Name = "My Account",
            CurrentBalance = 6000m,
            AccountType = AccountType.Checking,
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAccountAsync(_testUserId, accountId, model);

        // Assert
        result.Success.Should().BeTrue();
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.Is<BalanceHistory>(h =>
            h.AccountId == accountId &&
            h.Balance == 6000m &&
            h.Notes == "Balance updated")), Times.Once);
    }

    [Test]
    public async Task UpdateAccountAsync_BalanceUnchanged_DoesNotCreateHistory()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            UserId = _testUserId,
            Name = "My Account",
            CurrentBalance = 5000m
        };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        var model = new AccountEditViewModel
        {
            Id = accountId,
            Name = "My Account Updated",
            CurrentBalance = 5000m, // Same balance
            AccountType = AccountType.Checking,
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAccountAsync(_testUserId, accountId, model);

        // Assert
        result.Success.Should().BeTrue();
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.IsAny<BalanceHistory>()), Times.Never);
    }

    #endregion

    #region DeleteAccount Tests

    [Test]
    public async Task DeleteAccountAsync_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _service.DeleteAccountAsync(_testUserId, accountId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAccountAsync_ValidAccount_DeletesAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, UserId = _testUserId, Name = "To Delete" };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.DeleteAccountAsync(_testUserId, accountId);

        // Assert
        result.Success.Should().BeTrue();
        _mockAccountRepository.Verify(r => r.DeleteAsync(account), Times.Once);
    }

    #endregion

    #region AddBalanceRecord Tests

    [Test]
    public async Task AddBalanceRecordAsync_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _service.AddBalanceRecordAsync(_testUserId, accountId, 1000m, null, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task AddBalanceRecordAsync_ValidRequest_CreatesHistoryAndUpdatesBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, UserId = _testUserId, Name = "Account", CurrentBalance = 5000m };

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory>
            {
                new BalanceHistory { AccountId = accountId, Balance = 6000m, RecordedAt = DateTime.UtcNow }
            });

        // Act
        var result = await _service.AddBalanceRecordAsync(_testUserId, accountId, 6000m, "Manual update", null);

        // Assert
        result.Success.Should().BeTrue();
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.Is<BalanceHistory>(h =>
            h.AccountId == accountId &&
            h.Balance == 6000m &&
            h.Notes == "Manual update")), Times.Once);
    }

    [Test]
    public async Task AddBalanceRecordAsync_WithRecordedAt_UsesProvidedDate()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, UserId = _testUserId, Name = "Account", CurrentBalance = 5000m };
        var customDate = new DateTime(2024, 1, 15);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory>());

        // Act
        await _service.AddBalanceRecordAsync(_testUserId, accountId, 6000m, null, customDate);

        // Assert
        _mockBalanceHistoryRepository.Verify(r => r.AddAsync(It.Is<BalanceHistory>(h =>
            h.RecordedAt == customDate)), Times.Once);
    }

    #endregion

    #region GetBalanceRecord Tests

    [Test]
    public async Task GetBalanceRecordAsync_HistoryNotFound_ReturnsNull()
    {
        // Arrange
        var historyId = Guid.NewGuid();

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(historyId))
            .ReturnsAsync((BalanceHistory?)null);

        // Act
        var result = await _service.GetBalanceRecordAsync(_testUserId, historyId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetBalanceRecordAsync_AccountNotOwnedByUser_ReturnsNull()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var history = new BalanceHistory { Id = historyId, AccountId = accountId, Balance = 5000m };
        var account = new Account { Id = accountId, UserId = Guid.NewGuid() }; // Different user

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(historyId))
            .ReturnsAsync(history);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.GetBalanceRecordAsync(_testUserId, historyId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetBalanceRecordAsync_ValidRequest_ReturnsViewModel()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var history = new BalanceHistory
        {
            Id = historyId,
            AccountId = accountId,
            Balance = 5000m,
            RecordedAt = new DateTime(2024, 1, 15),
            Notes = "Test note"
        };

        var account = new Account { Id = accountId, UserId = _testUserId };

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(historyId))
            .ReturnsAsync(history);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.GetBalanceRecordAsync(_testUserId, historyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(historyId);
        result.Balance.Should().Be(5000m);
        result.Notes.Should().Be("Test note");
    }

    #endregion

    #region UpdateBalanceRecord Tests

    [Test]
    public async Task UpdateBalanceRecordAsync_HistoryNotFound_ReturnsNotFound()
    {
        // Arrange
        var model = new BalanceHistoryEditViewModel { Id = Guid.NewGuid() };

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(model.Id))
            .ReturnsAsync((BalanceHistory?)null);

        // Act
        var result = await _service.UpdateBalanceRecordAsync(_testUserId, model);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task UpdateBalanceRecordAsync_ValidRequest_UpdatesHistory()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var history = new BalanceHistory { Id = historyId, AccountId = accountId, Balance = 5000m };
        var account = new Account { Id = accountId, UserId = _testUserId, CurrentBalance = 5000m };

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(historyId))
            .ReturnsAsync(history);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory> { history });

        var model = new BalanceHistoryEditViewModel
        {
            Id = historyId,
            AccountId = accountId,
            Balance = 6000m,
            RecordedAt = new DateTime(2024, 1, 15),
            Notes = "Updated"
        };

        // Act
        var result = await _service.UpdateBalanceRecordAsync(_testUserId, model);

        // Assert
        result.Success.Should().BeTrue();
        _mockBalanceHistoryRepository.Verify(r => r.UpdateAsync(It.Is<BalanceHistory>(h =>
            h.Balance == 6000m && h.Notes == "Updated")), Times.Once);
    }

    #endregion

    #region DeleteBalanceRecord Tests

    [Test]
    public async Task DeleteBalanceRecordAsync_HistoryNotFound_ReturnsNotFound()
    {
        // Arrange
        var historyId = Guid.NewGuid();

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(historyId))
            .ReturnsAsync((BalanceHistory?)null);

        // Act
        var result = await _service.DeleteBalanceRecordAsync(_testUserId, historyId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task DeleteBalanceRecordAsync_ValidRequest_DeletesHistory()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var history = new BalanceHistory { Id = historyId, AccountId = accountId, Balance = 5000m };
        var account = new Account { Id = accountId, UserId = _testUserId, CurrentBalance = 5000m };

        _mockBalanceHistoryRepository.Setup(r => r.GetByIdAsync(historyId))
            .ReturnsAsync(history);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        _mockBalanceHistoryRepository.Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<BalanceHistory>());

        // Act
        var result = await _service.DeleteBalanceRecordAsync(_testUserId, historyId);

        // Assert
        result.Success.Should().BeTrue();
        result.RelatedId.Should().Be(accountId);
        _mockBalanceHistoryRepository.Verify(r => r.DeleteAsync(history), Times.Once);
    }

    #endregion
}
