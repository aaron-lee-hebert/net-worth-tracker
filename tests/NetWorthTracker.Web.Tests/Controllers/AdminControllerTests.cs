using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.ViewModels;
using NetWorthTracker.Web.Controllers;
using System.Security.Claims;

namespace NetWorthTracker.Web.Tests.Controllers;

[TestFixture]
public class AdminControllerTests
{
    private Mock<IAdminService> _mockAdminService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private AdminController _controller = null!;
    private Guid _testAdminUserId;

    [SetUp]
    public void SetUp()
    {
        _testAdminUserId = Guid.NewGuid();
        _mockAdminService = new Mock<IAdminService>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testAdminUserId.ToString());

        _controller = new AdminController(
            _mockAdminService.Object,
            _mockUserManager.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testAdminUserId.ToString()),
            new Claim("IsAdmin", "true")
        }, "mock"));

        var httpContext = new DefaultHttpContext { User = user };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        _controller.TempData = tempData;
    }

    #region Index Tests

    [Test]
    public async Task Index_ReturnsViewWithDashboardMetrics()
    {
        // Arrange
        var dashboard = new AdminDashboardViewModel
        {
            TotalUsers = 100,
            ActiveSubscriptions = 50,
            TrialUsers = 20,
            MonthlyChurnRate = 5m
        };

        _mockAdminService.Setup(s => s.GetDashboardMetricsAsync())
            .ReturnsAsync(dashboard);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as AdminDashboardViewModel;
        model.Should().NotBeNull();
        model!.TotalUsers.Should().Be(100);
        model.ActiveSubscriptions.Should().Be(50);
    }

    #endregion

    #region Users Tests

    [Test]
    public async Task Users_ReturnsViewWithPagedUsers()
    {
        // Arrange
        var pagedResult = new PagedResult<AdminUserViewModel>
        {
            Items = new List<AdminUserViewModel>
            {
                new AdminUserViewModel { Id = Guid.NewGuid(), Email = "user1@test.com" },
                new AdminUserViewModel { Id = Guid.NewGuid(), Email = "user2@test.com" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mockAdminService.Setup(s => s.GetUsersAsync(1, 20, null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.Users() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as PagedResult<AdminUserViewModel>;
        model.Should().NotBeNull();
        model!.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task Users_WithSearch_PassesSearchParameter()
    {
        // Arrange
        var pagedResult = new PagedResult<AdminUserViewModel>
        {
            Items = new List<AdminUserViewModel>
            {
                new AdminUserViewModel { Id = Guid.NewGuid(), Email = "john@test.com" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockAdminService.Setup(s => s.GetUsersAsync(1, 20, "john"))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.Users(1, "john") as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData["Search"].Should().Be("john");
    }

    [Test]
    public async Task Users_WithPage_PassesPageParameter()
    {
        // Arrange
        var pagedResult = new PagedResult<AdminUserViewModel>
        {
            Items = new List<AdminUserViewModel>(),
            TotalCount = 50,
            Page = 3,
            PageSize = 20
        };

        _mockAdminService.Setup(s => s.GetUsersAsync(3, 20, null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.Users(3) as ViewResult;

        // Assert
        _mockAdminService.Verify(s => s.GetUsersAsync(3, 20, null), Times.Once);
    }

    #endregion

    #region UserDetails Tests

    [Test]
    public async Task UserDetails_UserExists_ReturnsView()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDetails = new AdminUserDetailsViewModel
        {
            Id = userId,
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        _mockAdminService.Setup(s => s.GetUserDetailsAsync(userId))
            .ReturnsAsync(userDetails);

        // Act
        var result = await _controller.UserDetails(userId) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as AdminUserDetailsViewModel;
        model.Should().NotBeNull();
        model!.Email.Should().Be("test@example.com");
    }

    [Test]
    public async Task UserDetails_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockAdminService.Setup(s => s.GetUserDetailsAsync(userId))
            .ReturnsAsync((AdminUserDetailsViewModel?)null);

        // Act
        var result = await _controller.UserDetails(userId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region SetAdminStatus Tests

    [Test]
    public async Task SetAdminStatus_Success_RedirectsWithSuccessMessage()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        _mockAdminService.Setup(s => s.SetAdminStatusAsync(_testAdminUserId, targetUserId, true))
            .ReturnsAsync(ServiceResult.Ok());

        // Act
        var result = await _controller.SetAdminStatus(targetUserId, true) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("UserDetails");
        _controller.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Test]
    public async Task SetAdminStatus_Failure_RedirectsWithErrorMessage()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        _mockAdminService.Setup(s => s.SetAdminStatusAsync(_testAdminUserId, targetUserId, false))
            .ReturnsAsync(new ServiceResult { Success = false, ErrorMessage = "Cannot remove own admin" });

        // Act
        var result = await _controller.SetAdminStatus(targetUserId, false) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("UserDetails");
        _controller.TempData["ErrorMessage"].Should().Be("Cannot remove own admin");
    }

    [Test]
    public async Task SetAdminStatus_GrantAdmin_ShowsCorrectMessage()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        _mockAdminService.Setup(s => s.SetAdminStatusAsync(_testAdminUserId, targetUserId, true))
            .ReturnsAsync(ServiceResult.Ok());

        // Act
        await _controller.SetAdminStatus(targetUserId, true);

        // Assert
        _controller.TempData["SuccessMessage"].Should().Be("Admin access granted successfully.");
    }

    [Test]
    public async Task SetAdminStatus_RevokeAdmin_ShowsCorrectMessage()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        _mockAdminService.Setup(s => s.SetAdminStatusAsync(_testAdminUserId, targetUserId, false))
            .ReturnsAsync(ServiceResult.Ok());

        // Act
        await _controller.SetAdminStatus(targetUserId, false);

        // Assert
        _controller.TempData["SuccessMessage"].Should().Be("Admin access revoked successfully.");
    }

    #endregion

    #region AuditLogs Tests

    [Test]
    public async Task AuditLogs_ReturnsViewWithPagedLogs()
    {
        // Arrange
        var pagedResult = new PagedResult<AuditLogViewModel>
        {
            Items = new List<AuditLogViewModel>
            {
                new AuditLogViewModel { Id = Guid.NewGuid(), Action = "LoginSuccess" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50
        };

        _mockAdminService.Setup(s => s.GetAuditLogsAsync(1, 50, It.IsAny<AuditLogFilter>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.AuditLogs() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as PagedResult<AuditLogViewModel>;
        model.Should().NotBeNull();
        model!.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task AuditLogs_WithFilters_PassesFilterToService()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        var pagedResult = new PagedResult<AuditLogViewModel>
        {
            Items = new List<AuditLogViewModel>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };

        _mockAdminService.Setup(s => s.GetAuditLogsAsync(1, 50, It.Is<AuditLogFilter>(f =>
            f.Action == "Login" &&
            f.EntityType == "User" &&
            f.From == from &&
            f.To == to &&
            f.UserId == userId)))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.AuditLogs(1, "Login", "User", from, to, userId);

        // Assert
        _mockAdminService.Verify(s => s.GetAuditLogsAsync(1, 50, It.Is<AuditLogFilter>(f =>
            f.Action == "Login" &&
            f.EntityType == "User")), Times.Once);
    }

    [Test]
    public async Task AuditLogs_SetsViewBagFilters()
    {
        // Arrange
        var pagedResult = new PagedResult<AuditLogViewModel>
        {
            Items = new List<AuditLogViewModel>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };

        _mockAdminService.Setup(s => s.GetAuditLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AuditLogFilter>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.AuditLogs(1, "Login", "User") as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData["ActionFilter"].Should().Be("Login");
        result.ViewData["EntityTypeFilter"].Should().Be("User");
    }

    #endregion

    #region ExportAuditLogs Tests

    [Test]
    public async Task ExportAuditLogs_ReturnsCsvFile()
    {
        // Arrange
        var csvContent = "Timestamp,User,Action\n2024-01-01,test@test.com,Login";

        _mockAdminService.Setup(s => s.ExportAuditLogsCsvAsync(It.IsAny<AuditLogFilter>()))
            .ReturnsAsync(csvContent);

        // Act
        var result = await _controller.ExportAuditLogs() as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ContentType.Should().Be("text/csv");
        result.FileDownloadName.Should().StartWith("audit-logs-");
        result.FileDownloadName.Should().EndWith(".csv");
    }

    [Test]
    public async Task ExportAuditLogs_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        _mockAdminService.Setup(s => s.ExportAuditLogsCsvAsync(It.Is<AuditLogFilter>(f =>
            f.Action == "Login" &&
            f.From == from &&
            f.To == to)))
            .ReturnsAsync("csv content");

        // Act
        await _controller.ExportAuditLogs("Login", "User", from, to);

        // Assert
        _mockAdminService.Verify(s => s.ExportAuditLogsCsvAsync(It.Is<AuditLogFilter>(f =>
            f.Action == "Login")), Times.Once);
    }

    #endregion

    #region Subscriptions Tests

    [Test]
    public async Task Subscriptions_ReturnsViewWithPagedSubscriptions()
    {
        // Arrange
        var pagedResult = new PagedResult<AdminSubscriptionViewModel>
        {
            Items = new List<AdminSubscriptionViewModel>
            {
                new AdminSubscriptionViewModel { Id = Guid.NewGuid(), UserEmail = "user@test.com", Status = SubscriptionStatus.Active }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockAdminService.Setup(s => s.GetSubscriptionsAsync(1, 20, null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.Subscriptions() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as PagedResult<AdminSubscriptionViewModel>;
        model.Should().NotBeNull();
        model!.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task Subscriptions_WithStatusFilter_PassesStatusToService()
    {
        // Arrange
        var pagedResult = new PagedResult<AdminSubscriptionViewModel>
        {
            Items = new List<AdminSubscriptionViewModel>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mockAdminService.Setup(s => s.GetSubscriptionsAsync(1, 20, SubscriptionStatus.Trialing))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.Subscriptions(1, SubscriptionStatus.Trialing) as ViewResult;

        // Assert
        _mockAdminService.Verify(s => s.GetSubscriptionsAsync(1, 20, SubscriptionStatus.Trialing), Times.Once);
        result!.ViewData["StatusFilter"].Should().Be(SubscriptionStatus.Trialing);
    }

    [Test]
    public async Task Subscriptions_WithPage_PassesPageToService()
    {
        // Arrange
        var pagedResult = new PagedResult<AdminSubscriptionViewModel>
        {
            Items = new List<AdminSubscriptionViewModel>(),
            TotalCount = 50,
            Page = 3,
            PageSize = 20
        };

        _mockAdminService.Setup(s => s.GetSubscriptionsAsync(3, 20, null))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.Subscriptions(3);

        // Assert
        _mockAdminService.Verify(s => s.GetSubscriptionsAsync(3, 20, null), Times.Once);
    }

    #endregion

    #region Analytics Tests

    [Test]
    public async Task Analytics_ReturnsViewWithAnalytics()
    {
        // Arrange
        var analytics = new SubscriptionAnalyticsViewModel
        {
            TotalSubscriptions = 100,
            TotalActive = 60,
            TotalTrialing = 20,
            TotalExpired = 10,
            TotalCanceled = 10,
            TrialConversionRate = 85.7m,
            MonthlyChurnRate = 5.5m,
            StatusBreakdown = new List<SubscriptionStatusBreakdown>()
        };

        _mockAdminService.Setup(s => s.GetSubscriptionAnalyticsAsync())
            .ReturnsAsync(analytics);

        // Act
        var result = await _controller.Analytics() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as SubscriptionAnalyticsViewModel;
        model.Should().NotBeNull();
        model!.TotalSubscriptions.Should().Be(100);
        model.TrialConversionRate.Should().Be(85.7m);
    }

    #endregion
}
