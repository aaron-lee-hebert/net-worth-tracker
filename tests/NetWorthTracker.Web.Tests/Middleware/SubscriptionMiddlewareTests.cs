using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Infrastructure.Services;
using NetWorthTracker.Web.Middleware;
using System.Security.Claims;
using System.Text.Json;

namespace NetWorthTracker.Web.Tests.Middleware;

[TestFixture]
public class SubscriptionMiddlewareTests
{
    private Mock<ISubscriptionService> _mockSubscriptionService = null!;
    private Guid _testUserId;
    private bool _nextWasCalled;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockSubscriptionService = new Mock<ISubscriptionService>();
        _nextWasCalled = false;
    }

    private SubscriptionMiddleware CreateMiddleware(AppMode appMode)
    {
        var appSettings = Options.Create(new AppSettings { AppMode = appMode });
        return new SubscriptionMiddleware(
            (context) =>
            {
                _nextWasCalled = true;
                return Task.CompletedTask;
            },
            appSettings);
    }

    private HttpContext CreateHttpContext(string path, bool authenticated = true, Guid? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (authenticated)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, (userId ?? _testUserId).ToString())
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        }

        return context;
    }

    #region SelfHosted Mode Tests

    [Test]
    public async Task SelfHostedMode_AlwaysCallsNext()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.SelfHosted);
        var context = CreateHttpContext("/Dashboard");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SelfHostedMode_NeverChecksSubscription()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.SelfHosted);
        var context = CreateHttpContext("/api/accounts");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region SaaS Mode - Bypass Tests

    [Test]
    public async Task SaasMode_BypassesAccountRoutes()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Account/Login");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesSettingsRoutes()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Settings");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesSettingsSubRoutes()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Settings/DeleteAccount");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesBillingRoutes()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Billing");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesWebhookRoutes()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/api/webhooks/stripe");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesHealthCheck()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/health");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesStaticFiles()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/css/site.css");

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task SaasMode_BypassesUnauthenticatedUsers()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Dashboard", authenticated: false);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region SaaS Mode - Active Subscription Tests

    [Test]
    public async Task SaasMode_ActiveSubscription_CallsNext()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Dashboard");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ReturnsAsync(true);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeTrue();
    }

    [Test]
    public async Task SaasMode_ActiveSubscription_ChecksCorrectUserId()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Dashboard");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ReturnsAsync(true);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.HasActiveSubscriptionAsync(_testUserId), Times.Once);
    }

    #endregion

    #region SaaS Mode - No Subscription / UI Redirect Tests

    [Test]
    public async Task SaasMode_NoSubscription_UiRoute_RedirectsToBilling()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Dashboard");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ReturnsAsync(false);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(302);
        context.Response.Headers.Location.ToString().Should().Be("/Billing");
    }

    [Test]
    public async Task SaasMode_NoSubscription_AccountsRoute_RedirectsToBilling()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Accounts");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ReturnsAsync(false);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.Headers.Location.ToString().Should().Be("/Billing");
    }

    #endregion

    #region SaaS Mode - No Subscription / API 403 Tests

    [Test]
    public async Task SaasMode_NoSubscription_ApiRoute_Returns403()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/api/accounts");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ReturnsAsync(false);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(403);
        context.Response.ContentType.Should().Be("application/json");
    }

    [Test]
    public async Task SaasMode_NoSubscription_ApiRoute_ReturnsJsonError()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/api/accounts");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ReturnsAsync(false);

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("error").GetString()
            .Should().Be("Active subscription required.");
    }

    #endregion

    #region SaaS Mode - Fail-Closed Tests

    [Test]
    public async Task SaasMode_SubscriptionCheckThrows_UiRoute_RedirectsToBilling()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/Dashboard");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.Headers.Location.ToString().Should().Be("/Billing");
    }

    [Test]
    public async Task SaasMode_SubscriptionCheckThrows_ApiRoute_Returns403()
    {
        // Arrange
        var middleware = CreateMiddleware(AppMode.Saas);
        var context = CreateHttpContext("/api/data");

        _mockSubscriptionService.Setup(s => s.HasActiveSubscriptionAsync(_testUserId))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        await middleware.InvokeAsync(context, _mockSubscriptionService.Object);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(403);
    }

    #endregion
}
