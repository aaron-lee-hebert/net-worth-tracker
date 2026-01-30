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
public class AccountsControllerTests
{
    private Mock<IAccountManagementService> _mockAccountService = null!;
    private Mock<IExportService> _mockExportService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private AccountsController _controller = null!;
    private Guid _testUserId;

    [SetUp]
    public void SetUp()
    {
        _testUserId = Guid.NewGuid();
        _mockAccountService = new Mock<IAccountManagementService>();
        _mockExportService = new Mock<IExportService>();

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUserId.ToString());

        _controller = new AccountsController(
            _mockAccountService.Object,
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

    #region Index Tests

    [Test]
    public async Task Index_ReturnsViewWithAccounts()
    {
        // Arrange
        var accounts = new List<AccountViewModel>
        {
            new AccountViewModel { Id = Guid.NewGuid(), Name = "Savings", AccountType = AccountType.Savings, CurrentBalance = 5000m }
        };

        _mockAccountService.Setup(s => s.GetAccountsAsync(_testUserId, null))
            .ReturnsAsync(accounts);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().BeEquivalentTo(accounts);
    }

    [Test]
    public async Task Index_WithCategory_FiltersAccounts()
    {
        // Arrange
        _mockAccountService.Setup(s => s.GetAccountsAsync(_testUserId, AccountCategory.Banking))
            .ReturnsAsync(new List<AccountViewModel>());

        // Act
        var result = await _controller.Index(AccountCategory.Banking) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        _mockAccountService.Verify(s => s.GetAccountsAsync(_testUserId, AccountCategory.Banking), Times.Once);
    }

    #endregion

    #region Details Tests

    [Test]
    public async Task Details_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_testUserId, accountId))
            .ReturnsAsync((AccountDetailsResult?)null);

        // Act
        var result = await _controller.Details(accountId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Details_ValidAccount_ReturnsViewWithDetails()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var details = new AccountDetailsResult
        {
            Account = new AccountViewModel { Id = accountId, Name = "My Account" },
            BalanceHistory = new List<BalanceHistoryViewModel>()
        };

        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_testUserId, accountId))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.Details(accountId) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().Be(details.Account);
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_Get_ReturnsViewWithEmptyModel()
    {
        // Act
        var result = _controller.Create() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().BeOfType<AccountCreateViewModel>();
    }

    [Test]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        // Arrange
        var model = new AccountCreateViewModel();
        _controller.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _controller.Create(model) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().Be(model);
    }

    [Test]
    public async Task Create_Post_FirstAccount_RedirectsToDashboardWithFlag()
    {
        // Arrange
        var model = new AccountCreateViewModel
        {
            Name = "First Account",
            AccountType = AccountType.Checking,
            CurrentBalance = 1000m
        };

        _mockAccountService.Setup(s => s.CreateAccountAsync(_testUserId, model))
            .ReturnsAsync(AccountCreateResult.Ok(Guid.NewGuid(), true));

        // Act
        var result = await _controller.Create(model) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
        result.ControllerName.Should().Be("Dashboard");
        result.RouteValues.Should().ContainKey("firstAccount");
    }

    [Test]
    public async Task Create_Post_NotFirstAccount_RedirectsToIndex()
    {
        // Arrange
        var model = new AccountCreateViewModel
        {
            Name = "Another Account",
            AccountType = AccountType.Savings,
            CurrentBalance = 5000m
        };

        _mockAccountService.Setup(s => s.CreateAccountAsync(_testUserId, model))
            .ReturnsAsync(AccountCreateResult.Ok(Guid.NewGuid(), false));

        // Act
        var result = await _controller.Create(model) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
        result.ControllerName.Should().BeNull();
    }

    #endregion

    #region Edit Tests

    [Test]
    public async Task Edit_Get_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_testUserId, accountId))
            .ReturnsAsync((AccountDetailsResult?)null);

        // Act
        var result = await _controller.Edit(accountId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Edit_Get_ValidAccount_ReturnsViewWithModel()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var details = new AccountDetailsResult
        {
            Account = new AccountViewModel
            {
                Id = accountId,
                Name = "My Account",
                AccountType = AccountType.Checking,
                CurrentBalance = 5000m,
                IsActive = true
            },
            BalanceHistory = new List<BalanceHistoryViewModel>()
        };

        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_testUserId, accountId))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.Edit(accountId) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var model = result!.Model as AccountEditViewModel;
        model.Should().NotBeNull();
        model!.Name.Should().Be("My Account");
    }

    [Test]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var model = new AccountEditViewModel { Id = Guid.NewGuid() }; // Different ID

        // Act
        var result = await _controller.Edit(accountId, model);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Edit_Post_UpdateFails_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var model = new AccountEditViewModel { Id = accountId, Name = "Updated" };

        _mockAccountService.Setup(s => s.UpdateAccountAsync(_testUserId, accountId, model))
            .ReturnsAsync(ServiceResult.NotFound());

        // Act
        var result = await _controller.Edit(accountId, model);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Edit_Post_Success_RedirectsToIndex()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var model = new AccountEditViewModel
        {
            Id = accountId,
            Name = "Updated Account",
            AccountType = AccountType.Checking,
            CurrentBalance = 5000m,
            IsActive = true
        };

        _mockAccountService.Setup(s => s.UpdateAccountAsync(_testUserId, accountId, model))
            .ReturnsAsync(ServiceResult.Ok());

        // Act
        var result = await _controller.Edit(accountId, model) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_Get_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService.Setup(s => s.GetAccountDetailsAsync(_testUserId, accountId))
            .ReturnsAsync((AccountDetailsResult?)null);

        // Act
        var result = await _controller.Delete(accountId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task DeleteConfirmed_DeleteFails_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService.Setup(s => s.DeleteAccountAsync(_testUserId, accountId))
            .ReturnsAsync(ServiceResult.NotFound());

        // Act
        var result = await _controller.DeleteConfirmed(accountId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task DeleteConfirmed_Success_RedirectsToIndex()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService.Setup(s => s.DeleteAccountAsync(_testUserId, accountId))
            .ReturnsAsync(ServiceResult.Ok());

        // Act
        var result = await _controller.DeleteConfirmed(accountId) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
    }

    #endregion

    #region UpdateBalance Tests

    [Test]
    public async Task UpdateBalance_Success_RedirectsToDetails()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService.Setup(s => s.AddBalanceRecordAsync(_testUserId, accountId, 5000m, "Test", null))
            .ReturnsAsync(ServiceResult.Ok(accountId));

        // Act
        var result = await _controller.UpdateBalance(accountId, 5000m, "Test", null) as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Details");
        result.RouteValues!["id"].Should().Be(accountId);
    }

    #endregion

    #region Export Tests

    [Test]
    public async Task ExportAccountsCsv_NoData_RedirectsToIndex()
    {
        // Arrange
        _mockExportService.Setup(s => s.ExportAccountsCsvAsync(_testUserId, null))
            .ReturnsAsync(ExportResult.NoData());

        // Act
        var result = await _controller.ExportAccountsCsv() as RedirectToActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
    }

    [Test]
    public async Task ExportAccountsCsv_Success_ReturnsFile()
    {
        // Arrange
        _mockExportService.Setup(s => s.ExportAccountsCsvAsync(_testUserId, null))
            .ReturnsAsync(ExportResult.Ok("csv content", "accounts.csv"));

        // Act
        var result = await _controller.ExportAccountsCsv() as FileContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.FileDownloadName.Should().Be("accounts.csv");
    }

    #endregion
}
