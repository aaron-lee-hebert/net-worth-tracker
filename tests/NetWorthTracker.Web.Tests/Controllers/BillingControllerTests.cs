using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Core.ViewModels;
using NetWorthTracker.Web.Controllers;
using System.Security.Claims;

namespace NetWorthTracker.Web.Tests.Controllers;

[TestFixture]
public class BillingControllerTests
{
    private Mock<ISubscriptionService> _mockSubscriptionService = null!;
    private BillingController _controller = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockSubscriptionService = new Mock<ISubscriptionService>();

        _controller = new BillingController(_mockSubscriptionService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region Index Tests

    [Test]
    public async Task Index_NoSubscription_ReturnsViewWithoutSubscription()
    {
        // Arrange
        _mockSubscriptionService.Setup(s => s.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as BillingViewModel;
        model.Should().NotBeNull();
        model!.HasSubscription.Should().BeFalse();
        model.Status.Should().BeNull();
        model.CurrentPeriodEnd.Should().BeNull();
        model.StripePriceId.Should().BeNull();
    }

    [Test]
    public async Task Index_ActiveSubscription_ReturnsViewWithSubscriptionDetails()
    {
        // Arrange
        var periodEnd = DateTime.UtcNow.AddMonths(1);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = periodEnd,
            StripePriceId = "price_123"
        };

        _mockSubscriptionService.Setup(s => s.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as BillingViewModel;
        model.Should().NotBeNull();
        model!.HasSubscription.Should().BeTrue();
        model.Status.Should().Be(SubscriptionStatus.Active);
        model.CurrentPeriodEnd.Should().Be(periodEnd);
        model.StripePriceId.Should().Be("price_123");
    }

    [Test]
    public async Task Index_CanceledSubscription_ReturnsViewWithCanceledStatus()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Status = SubscriptionStatus.Canceled,
            StripePriceId = "price_123"
        };

        _mockSubscriptionService.Setup(s => s.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as BillingViewModel;
        model.Should().NotBeNull();
        model!.HasSubscription.Should().BeTrue();
        model.Status.Should().Be(SubscriptionStatus.Canceled);
    }

    #endregion

    #region CreateCheckoutSession Tests

    [Test]
    public void CreateCheckoutSession_RedirectsToIndex()
    {
        // Act
        var result = _controller.CreateCheckoutSession() as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
    }

    #endregion

    #region CreatePortalSession Tests

    [Test]
    public void CreatePortalSession_RedirectsToIndex()
    {
        // Act
        var result = _controller.CreatePortalSession() as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
    }

    #endregion

    #region Success Tests

    [Test]
    public void Success_ReturnsView()
    {
        // Act
        var result = _controller.Success() as ViewResult;

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Cancel Tests

    [Test]
    public void Cancel_ReturnsView()
    {
        // Act
        var result = _controller.Cancel() as ViewResult;

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
