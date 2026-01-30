using FluentAssertions;
using Moq;
using NHibernate;
using NUnit.Framework;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Infrastructure.Repositories;

namespace NetWorthTracker.Infrastructure.Tests.Repositories;

[TestFixture]
public class AccountRepositoryTests
{
    private Mock<ISession> _mockSession = null!;
    private AccountRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSession = new Mock<ISession>();
        _repository = new AccountRepository(_mockSession.Object);
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ReturnsAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedAccount = new Account
        {
            Id = accountId,
            Name = "Test Account",
            AccountType = AccountType.Checking,
            CurrentBalance = 1000m
        };

        _mockSession.Setup(s => s.GetAsync<Account>(accountId, default))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _repository.GetByIdAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(accountId);
        result.Name.Should().Be("Test Account");
    }

    [Test]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _mockSession.Setup(s => s.GetAsync<Account>(accountId, default))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _repository.GetByIdAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task AddAsync_SavesAndReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Name = "New Account",
            AccountType = AccountType.Brokerage,
            CurrentBalance = 5000m,
            UserId = Guid.NewGuid()
        };

        _mockSession.Setup(s => s.SaveAsync(account, default))
            .Returns(Task.FromResult<object?>(account.Id));
        _mockSession.Setup(s => s.FlushAsync(default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _repository.AddAsync(account);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Account");
        _mockSession.Verify(s => s.SaveAsync(account, default), Times.Once);
        _mockSession.Verify(s => s.FlushAsync(default), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_UpdatesAccountAndSetsUpdatedAt()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Test Account",
            AccountType = AccountType.CreditCard,
            CurrentBalance = 2000m
        };

        _mockSession.Setup(s => s.UpdateAsync(account, default))
            .Returns(Task.CompletedTask);
        _mockSession.Setup(s => s.FlushAsync(default))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.UpdateAsync(account);

        // Assert
        account.UpdatedAt.Should().NotBeNull();
        account.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _mockSession.Verify(s => s.UpdateAsync(account, default), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_DeletesAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Account to Delete"
        };

        _mockSession.Setup(s => s.DeleteAsync(account, default))
            .Returns(Task.CompletedTask);
        _mockSession.Setup(s => s.FlushAsync(default))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.DeleteAsync(account);

        // Assert
        _mockSession.Verify(s => s.DeleteAsync(account, default), Times.Once);
        _mockSession.Verify(s => s.FlushAsync(default), Times.Once);
    }
}
