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
public class ReportsControllerTests
{
    private Mock<IReportService> _mockReportService = null!;
    private Mock<IExportService> _mockExportService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private ReportsController _controller = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockReportService = new Mock<IReportService>();
        _mockExportService = new Mock<IExportService>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUserId.ToString());

        _controller = new ReportsController(
            _mockReportService.Object,
            _mockExportService.Object,
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
    public async Task Quarterly_ReturnsViewWithReport()
    {
        // Arrange
        var viewModel = new QuarterlyReportViewModel
        {
            Quarters = new List<string> { "Q1 2024" },
            Accounts = new List<AccountQuarterlyData>()
        };

        _mockReportService.Setup(s => s.BuildQuarterlyReportAsync(_testUserId))
            .ReturnsAsync(viewModel);

        // Act
        var result = await _controller.Quarterly() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().Be(viewModel);
    }

    [Test]
    public async Task DownloadCsv_NoData_RedirectsToQuarterly()
    {
        // Arrange
        _mockExportService.Setup(s => s.ExportQuarterlyReportCsvAsync(_testUserId))
            .ReturnsAsync(ExportResult.NoData());

        // Act
        var result = await _controller.DownloadCsv() as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Quarterly");
    }

    [Test]
    public async Task DownloadCsv_WithData_ReturnsFile()
    {
        // Arrange
        _mockExportService.Setup(s => s.ExportQuarterlyReportCsvAsync(_testUserId))
            .ReturnsAsync(ExportResult.Ok("csv,content", "quarterly-report.csv"));

        // Act
        var result = await _controller.DownloadCsv() as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.FileDownloadName.Should().Be("quarterly-report.csv");
        result.ContentType.Should().Be("text/csv");
    }

    [Test]
    public async Task DownloadNetWorthHistoryCsv_NoData_RedirectsToQuarterly()
    {
        // Arrange
        _mockExportService.Setup(s => s.ExportNetWorthHistoryCsvAsync(_testUserId))
            .ReturnsAsync(ExportResult.NoData());

        // Act
        var result = await _controller.DownloadNetWorthHistoryCsv() as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Quarterly");
    }

    [Test]
    public async Task DownloadNetWorthHistoryCsv_WithData_ReturnsFile()
    {
        // Arrange
        _mockExportService.Setup(s => s.ExportNetWorthHistoryCsvAsync(_testUserId))
            .ReturnsAsync(ExportResult.Ok("date,balance", "net-worth-history.csv"));

        // Act
        var result = await _controller.DownloadNetWorthHistoryCsv() as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.FileDownloadName.Should().Be("net-worth-history.csv");
    }
}
