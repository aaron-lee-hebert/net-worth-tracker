using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.ViewModels;
using NetWorthTracker.Web.Controllers;
using System.Security.Claims;

namespace NetWorthTracker.Web.Tests.Controllers;

[TestFixture]
public class DashboardControllerTests
{
    private Mock<IDashboardService> _mockDashboardService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private DashboardController _controller = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockDashboardService = new Mock<IDashboardService>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUserId.ToString());

        _controller = new DashboardController(
            _mockDashboardService.Object,
            _mockUserManager.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task Index_WithNoAccounts_ReturnsViewWithZeroTotals()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync(_testUserId))
            .ReturnsAsync(new DashboardSummaryResult
            {
                TotalAssets = 0,
                TotalLiabilities = 0,
                TotalNetWorth = 0,
                TotalsByCategory = new Dictionary<AccountCategory, decimal>(),
                RecentAccounts = [],
                HasAccounts = false
            });

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as DashboardViewModel;
        model.Should().NotBeNull();
        model!.TotalNetWorth.Should().Be(0);
        model.TotalAssets.Should().Be(0);
        model.TotalLiabilities.Should().Be(0);
        model.RecentAccounts.Should().BeEmpty();
    }

    [Test]
    public async Task Index_WithAccounts_CalculatesCorrectTotals()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync(_testUserId))
            .ReturnsAsync(new DashboardSummaryResult
            {
                TotalAssets = 365000m,
                TotalLiabilities = 205000m,
                TotalNetWorth = 160000m,
                TotalsByCategory = new Dictionary<AccountCategory, decimal>
                {
                    { AccountCategory.Banking, 15000m },
                    { AccountCategory.Investment, 50000m },
                    { AccountCategory.RealEstate, 300000m },
                    { AccountCategory.SecuredDebt, 200000m },
                    { AccountCategory.UnsecuredDebt, 5000m }
                },
                RecentAccounts =
                [
                    new AccountSummary { Id = Guid.NewGuid(), Name = "Checking", AccountType = AccountType.Checking, CurrentBalance = 5000m },
                    new AccountSummary { Id = Guid.NewGuid(), Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 10000m }
                ],
                HasAccounts = true
            });

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as DashboardViewModel;
        model.Should().NotBeNull();
        model!.TotalAssets.Should().Be(365000m);
        model.TotalLiabilities.Should().Be(205000m);
        model.TotalNetWorth.Should().Be(160000m);
    }

    [Test]
    public async Task Index_ReturnsMaxFiveRecentAccounts()
    {
        // Arrange
        var recentAccounts = Enumerable.Range(1, 5)
            .Select(i => new AccountSummary
            {
                Id = Guid.NewGuid(),
                Name = $"Account {i}",
                AccountType = AccountType.Checking,
                CurrentBalance = i * 1000m
            })
            .ToList();

        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync(_testUserId))
            .ReturnsAsync(new DashboardSummaryResult
            {
                TotalAssets = 15000m,
                TotalLiabilities = 0,
                TotalNetWorth = 15000m,
                TotalsByCategory = new Dictionary<AccountCategory, decimal>(),
                RecentAccounts = recentAccounts,
                HasAccounts = true
            });

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as DashboardViewModel;
        model.Should().NotBeNull();
        model!.RecentAccounts.Should().HaveCount(5);
    }
}
