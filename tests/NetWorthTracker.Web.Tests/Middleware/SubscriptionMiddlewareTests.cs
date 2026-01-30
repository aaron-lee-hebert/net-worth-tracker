using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Web.Middleware;
using System.Security.Claims;

namespace NetWorthTracker.Web.Tests.Middleware;

[TestFixture]
public class SubscriptionMiddlewareTests
{
    private Mock<IStripeService> _mockStripeService = null!;
    private Mock<ISubscriptionRepository> _mockSubscriptionRepository = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<ILogger<SubscriptionMiddleware>> _mockLogger = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockStripeService = new Mock<IStripeService>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockLogger = new Mock<ILogger<SubscriptionMiddleware>>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private SubscriptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new SubscriptionMiddleware(next, _mockLogger.Object);
    }

    private HttpContext CreateHttpContext(string path, bool isAuthenticated, string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        if (isAuthenticated)
        {
            var claims = new List<Claim>();
            if (userId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        }

        return context;
    }

    [Test]
    public async Task InvokeAsync_ExcludedPath_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/", false);

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_StaticFile_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/css/site.css", true, _testUserId.ToString());

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_UnauthenticatedUser_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Dashboard", false);

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_StripeNotConfigured_CallsNext()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(false);

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Dashboard", true, _testUserId.ToString());

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_ActiveSubscription_CallsNext()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(true);

        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(30)
        };

        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Accounts/Create", true, _testUserId.ToString());

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_NoSubscription_CreatesTrialSubscription()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(true);
        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync((Subscription?)null);

        Subscription? createdSubscription = null;
        _mockSubscriptionRepository.Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .Callback<Subscription>(s => createdSubscription = s)
            .ReturnsAsync((Subscription s) => s);

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Dashboard", true, _testUserId.ToString());

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
        _mockSubscriptionRepository.Verify(r => r.AddAsync(It.Is<Subscription>(s =>
            s.UserId == _testUserId &&
            s.Status == SubscriptionStatus.Trialing)), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_ExpiredSubscription_ReadOnlyPath_AllowsAccess()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(true);

        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Expired,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-5)
        };

        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Dashboard", true, _testUserId.ToString());
        context.Request.Method = "GET";

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
        context.Items["ReadOnlyMode"].Should().Be(true);
    }

    [Test]
    public async Task InvokeAsync_ExpiredSubscription_WritePath_RedirectsToSubscription()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(true);

        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Expired,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-5)
        };

        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Accounts/Create", true, _testUserId.ToString());
        context.Request.Method = "POST"; // POST requests are write operations and should be blocked

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.Headers["Location"].Should().Contain("/Subscription?expired=true");
    }

    [Test]
    public async Task InvokeAsync_TrialActive_CallsNext()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(true);

        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Trialing,
            TrialStartedAt = DateTime.UtcNow.AddDays(-5),
            TrialEndsAt = DateTime.UtcNow.AddDays(9)
        };

        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext("/Accounts/Create", true, _testUserId.ToString());

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_SetsSubscriptionInfoInHttpContext()
    {
        // Arrange
        _mockStripeService.Setup(s => s.IsConfigured).Returns(true);

        var subscription = new Subscription
        {
            UserId = _testUserId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(30)
        };

        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(subscription);

        var middleware = CreateMiddleware(ctx => Task.CompletedTask);
        var context = CreateHttpContext("/Dashboard", true, _testUserId.ToString());

        // Act
        await middleware.InvokeAsync(context, _mockStripeService.Object, _mockSubscriptionRepository.Object, _mockUserManager.Object);

        // Assert
        context.Items["Subscription"].Should().Be(subscription);
        context.Items["HasActiveAccess"].Should().Be(true);
    }
}
