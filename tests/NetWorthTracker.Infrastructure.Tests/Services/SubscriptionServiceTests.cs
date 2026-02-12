using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NHibernate;
using NUnit.Framework;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Infrastructure.Services;

namespace NetWorthTracker.Infrastructure.Tests.Services;

[TestFixture]
public class SubscriptionServiceTests
{
    private Mock<ISession> _mockSession = null!;
    private Mock<ILogger<SubscriptionService>> _mockLogger = null!;
    private Mock<IQueryOver<Subscription, Subscription>> _mockQueryOver = null!;
    private Mock<ITransaction> _mockTransaction = null!;
    private SubscriptionService _service = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockSession = new Mock<ISession>();
        _mockLogger = new Mock<ILogger<SubscriptionService>>();
        _mockQueryOver = new Mock<IQueryOver<Subscription, Subscription>>();
        _mockTransaction = new Mock<ITransaction>();

        _mockTransaction.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);
        _mockSession.Setup(s => s.BeginTransaction()).Returns(_mockTransaction.Object);

        _service = new SubscriptionService(_mockSession.Object, _mockLogger.Object);
    }

    private void SetupQueryOverReturning(Subscription? result)
    {
        _mockQueryOver
            .Setup(q => q.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Subscription, bool>>>()))
            .Returns(_mockQueryOver.Object);
        _mockQueryOver
            .Setup(q => q.SingleOrDefaultAsync(default))
            .ReturnsAsync(result);
        _mockSession
            .Setup(s => s.QueryOver<Subscription>())
            .Returns(_mockQueryOver.Object);
    }

    #region GetByUserIdAsync Tests

    [Test]
    public async Task GetByUserIdAsync_SubscriptionExists_ReturnsSubscription()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Status = SubscriptionStatus.Active,
            StripeCustomerId = "cus_123",
            StripeSubscriptionId = "sub_123"
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUserId);
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Test]
    public async Task GetByUserIdAsync_NoSubscription_ReturnsNull()
    {
        // Arrange
        SetupQueryOverReturning(null);

        // Act
        var result = await _service.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region HasActiveSubscriptionAsync Tests

    [Test]
    public async Task HasActiveSubscriptionAsync_ActiveSubscription_ReturnsTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Active
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HasActiveSubscriptionAsync_TrialingSubscription_ReturnsTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Trialing
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HasActiveSubscriptionAsync_CanceledSubscription_ReturnsFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Canceled
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasActiveSubscriptionAsync_PastDueSubscription_ReturnsFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.PastDue
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasActiveSubscriptionAsync_ExpiredSubscription_ReturnsFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Expired
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasActiveSubscriptionAsync_UnpaidSubscription_ReturnsFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Unpaid
        };

        SetupQueryOverReturning(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasActiveSubscriptionAsync_NoSubscription_ReturnsFalse()
    {
        // Arrange
        SetupQueryOverReturning(null);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(_testUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CreateOrUpdateFromStripeAsync Tests

    [Test]
    public async Task CreateOrUpdateFromStripeAsync_NewSubscription_CreatesAndReturns()
    {
        // Arrange
        SetupQueryOverReturning(null);

        _mockSession.Setup(s => s.SaveAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.FromResult<object?>(Guid.NewGuid()));

        var periodStart = DateTime.UtcNow;
        var periodEnd = DateTime.UtcNow.AddMonths(1);

        // Act
        var result = await _service.CreateOrUpdateFromStripeAsync(
            _testUserId, "cus_new", "sub_new", "price_new",
            SubscriptionStatus.Active, periodStart, periodEnd);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.StripeCustomerId.Should().Be("cus_new");
        result.StripeSubscriptionId.Should().Be("sub_new");
        result.StripePriceId.Should().Be("price_new");
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.CurrentPeriodStart.Should().Be(periodStart);
        result.CurrentPeriodEnd.Should().Be(periodEnd);

        _mockSession.Verify(s => s.SaveAsync(It.IsAny<Subscription>(), default), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Test]
    public async Task CreateOrUpdateFromStripeAsync_ExistingSubscription_UpdatesAndReturns()
    {
        // Arrange
        var existing = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            StripeCustomerId = "cus_old",
            StripeSubscriptionId = "sub_old",
            StripePriceId = "price_old",
            Status = SubscriptionStatus.Incomplete,
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow
        };

        SetupQueryOverReturning(existing);

        _mockSession.Setup(s => s.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        var newPeriodStart = DateTime.UtcNow;
        var newPeriodEnd = DateTime.UtcNow.AddMonths(1);

        // Act
        var result = await _service.CreateOrUpdateFromStripeAsync(
            _testUserId, "cus_updated", "sub_updated", "price_updated",
            SubscriptionStatus.Active, newPeriodStart, newPeriodEnd);

        // Assert
        result.Should().BeSameAs(existing);
        result.StripeCustomerId.Should().Be("cus_updated");
        result.StripeSubscriptionId.Should().Be("sub_updated");
        result.StripePriceId.Should().Be("price_updated");
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.CurrentPeriodStart.Should().Be(newPeriodStart);
        result.CurrentPeriodEnd.Should().Be(newPeriodEnd);
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _mockSession.Verify(s => s.UpdateAsync(existing, default), Times.Once);
        _mockSession.Verify(s => s.SaveAsync(It.IsAny<Subscription>(), default), Times.Never);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Test]
    public async Task UpdateStatusAsync_SubscriptionFound_UpdatesStatus()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            StripeSubscriptionId = "sub_123",
            Status = SubscriptionStatus.Active
        };

        SetupQueryOverReturning(subscription);

        _mockSession.Setup(s => s.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateStatusAsync("sub_123", SubscriptionStatus.Canceled);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        subscription.UpdatedAt.Should().NotBeNull();
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _mockSession.Verify(s => s.UpdateAsync(subscription, default), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Test]
    public async Task UpdateStatusAsync_SubscriptionNotFound_DoesNotThrow()
    {
        // Arrange
        SetupQueryOverReturning(null);

        // Act
        await _service.UpdateStatusAsync("sub_nonexistent", SubscriptionStatus.Canceled);

        // Assert
        _mockSession.Verify(s => s.UpdateAsync(It.IsAny<Subscription>(), default), Times.Never);
        _mockSession.Verify(s => s.BeginTransaction(), Times.Never);
    }

    #endregion

    #region CancelByUserIdAsync Tests

    [Test]
    public async Task CancelByUserIdAsync_SubscriptionExists_CancelsSubscription()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            StripeSubscriptionId = "sub_123",
            Status = SubscriptionStatus.Active
        };

        SetupQueryOverReturning(subscription);

        _mockSession.Setup(s => s.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CancelByUserIdAsync(_testUserId);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        subscription.UpdatedAt.Should().NotBeNull();
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _mockSession.Verify(s => s.UpdateAsync(subscription, default), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Test]
    public async Task CancelByUserIdAsync_NoSubscription_DoesNotThrow()
    {
        // Arrange
        SetupQueryOverReturning(null);

        // Act
        await _service.CancelByUserIdAsync(_testUserId);

        // Assert
        _mockSession.Verify(s => s.UpdateAsync(It.IsAny<Subscription>(), default), Times.Never);
        _mockSession.Verify(s => s.BeginTransaction(), Times.Never);
    }

    #endregion
}
