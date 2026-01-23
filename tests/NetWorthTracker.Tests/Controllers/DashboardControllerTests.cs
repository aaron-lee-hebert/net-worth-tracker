using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;
using NetWorthTracker.Web.Controllers;
using System.Security.Claims;

namespace NetWorthTracker.Tests.Controllers;

[TestFixture]
public class DashboardControllerTests
{
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private DashboardController _controller = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockAccountRepository = new Mock<IAccountRepository>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUserId.ToString());

        _controller = new DashboardController(
            _mockAccountRepository.Object,
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
        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Account>());

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
        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), Name = "Checking", AccountType = AccountType.Checking, CurrentBalance = 5000m, UserId = _testUserId },
            new Account { Id = Guid.NewGuid(), Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 10000m, UserId = _testUserId },
            new Account { Id = Guid.NewGuid(), Name = "401k", AccountType = AccountType.Retirement401k, CurrentBalance = 50000m, UserId = _testUserId },
            new Account { Id = Guid.NewGuid(), Name = "Home", AccountType = AccountType.PrimaryResidence, CurrentBalance = 300000m, UserId = _testUserId },
            new Account { Id = Guid.NewGuid(), Name = "Mortgage", AccountType = AccountType.Mortgage, CurrentBalance = 200000m, UserId = _testUserId },
            new Account { Id = Guid.NewGuid(), Name = "Credit Card", AccountType = AccountType.CreditCard, CurrentBalance = 5000m, UserId = _testUserId }
        };

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as DashboardViewModel;
        model.Should().NotBeNull();
        model!.TotalAssets.Should().Be(365000m); // 5000 + 10000 + 50000 + 300000
        model.TotalLiabilities.Should().Be(205000m); // 200000 + 5000
        model.TotalNetWorth.Should().Be(160000m); // 365000 - 205000
    }

    [Test]
    public async Task Index_ReturnsMaxFiveRecentAccounts()
    {
        // Arrange
        var accounts = Enumerable.Range(1, 10)
            .Select(i => new Account
            {
                Id = Guid.NewGuid(),
                Name = $"Account {i}",
                AccountType = AccountType.Checking,
                CurrentBalance = i * 1000m,
                UserId = _testUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        _mockAccountRepository.Setup(r => r.GetActiveAccountsByUserIdAsync(_testUserId))
            .ReturnsAsync(accounts);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as DashboardViewModel;
        model.Should().NotBeNull();
        model!.RecentAccounts.Should().HaveCount(5);
    }
}
