using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.ViewModels;
using NetWorthTracker.Web.Controllers;
using System.Security.Claims;

namespace NetWorthTracker.Web.Tests.Controllers;

[TestFixture]
public class ForecastsControllerTests
{
    private Mock<IForecastService> _mockForecastService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private ForecastsController _controller = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockForecastService = new Mock<IForecastService>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUserId.ToString());

        _controller = new ForecastsController(
            _mockForecastService.Object,
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
    public void Index_ReturnsViewWithDefaultForecastMonths()
    {
        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        ((int)_controller.ViewBag.ForecastMonths).Should().Be(60);
    }

    [Test]
    public void Index_WithCustomForecastMonths_SetsViewBag()
    {
        // Act
        var result = _controller.Index(36) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        ((int)_controller.ViewBag.ForecastMonths).Should().Be(36);
    }

    [Test]
    public async Task GetForecastData_ReturnsJsonWithViewModel()
    {
        // Arrange
        var viewModel = new ForecastViewModel
        {
            ForecastMonths = 60,
            Accounts = new List<AccountForecast>()
        };

        _mockForecastService.Setup(s => s.GetForecastDataAsync(_testUserId, 60))
            .ReturnsAsync(viewModel);

        // Act
        var result = await _controller.GetForecastData() as JsonResult;

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(viewModel);
    }

    [Test]
    public async Task GetForecastData_WithCustomMonths_PassesToService()
    {
        // Arrange
        var viewModel = new ForecastViewModel { ForecastMonths = 24 };

        _mockForecastService.Setup(s => s.GetForecastDataAsync(_testUserId, 24))
            .ReturnsAsync(viewModel);

        // Act
        var result = await _controller.GetForecastData(24) as JsonResult;

        // Assert
        _mockForecastService.Verify(s => s.GetForecastDataAsync(_testUserId, 24), Times.Once);
    }

    [Test]
    public async Task GetAssumptions_ReturnsJsonWithViewModel()
    {
        // Arrange
        var viewModel = new ForecastAssumptionsViewModel
        {
            InvestmentGrowthRate = 7m,
            RealEstateGrowthRate = 2m
        };

        _mockForecastService.Setup(s => s.GetAssumptionsAsync(_testUserId))
            .ReturnsAsync(viewModel);

        // Act
        var result = await _controller.GetAssumptions() as JsonResult;

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(viewModel);
    }

    [Test]
    public async Task SaveAssumptions_ReturnsJsonSuccess()
    {
        // Arrange
        var model = new ForecastAssumptionsViewModel
        {
            InvestmentGrowthRate = 8m,
            RealEstateGrowthRate = 3m
        };

        // Act
        var result = await _controller.SaveAssumptions(model) as JsonResult;

        // Assert
        result.Should().NotBeNull();
        _mockForecastService.Verify(s => s.SaveAssumptionsAsync(_testUserId, model), Times.Once);
    }

    [Test]
    public async Task ResetAssumptions_ReturnsJsonSuccess()
    {
        // Act
        var result = await _controller.ResetAssumptions() as JsonResult;

        // Assert
        result.Should().NotBeNull();
        _mockForecastService.Verify(s => s.ResetAssumptionsAsync(_testUserId), Times.Once);
    }
}
