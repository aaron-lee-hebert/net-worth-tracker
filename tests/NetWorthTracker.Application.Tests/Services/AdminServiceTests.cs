using FluentAssertions;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Application.Services;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Tests.Services;

[TestFixture]
public class AdminServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<ISubscriptionRepository> _mockSubscriptionRepository = null!;
    private Mock<IAccountRepository> _mockAccountRepository = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private Mock<IAuditLogRepository> _mockAuditLogRepository = null!;
    private AdminService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _mockAuditLogRepository = new Mock<IAuditLogRepository>();

        _service = new AdminService(
            _mockUserRepository.Object,
            _mockSubscriptionRepository.Object,
            _mockAccountRepository.Object,
            _mockAuditService.Object,
            _mockAuditLogRepository.Object);
    }

    #region GetDashboardMetrics Tests

    [Test]
    public async Task GetDashboardMetricsAsync_ReturnsCorrectUserCounts()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetCountAsync()).ReturnsAsync(100);
        _mockUserRepository.Setup(r => r.GetCountCreatedAfterAsync(It.IsAny<DateTime>())).ReturnsAsync(10);
        _mockUserRepository.Setup(r => r.GetRecentUsersAsync(It.IsAny<int>())).ReturnsAsync(new List<ApplicationUser>());
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(It.IsAny<SubscriptionStatus>())).ReturnsAsync(0);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());

        // Act
        var result = await _service.GetDashboardMetricsAsync();

        // Assert
        result.TotalUsers.Should().Be(100);
    }

    [Test]
    public async Task GetDashboardMetricsAsync_ReturnsCorrectSubscriptionCounts()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetCountAsync()).ReturnsAsync(50);
        _mockUserRepository.Setup(r => r.GetCountCreatedAfterAsync(It.IsAny<DateTime>())).ReturnsAsync(5);
        _mockUserRepository.Setup(r => r.GetRecentUsersAsync(It.IsAny<int>())).ReturnsAsync(new List<ApplicationUser>());

        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Active)).ReturnsAsync(30);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Trialing)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Expired)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Canceled)).ReturnsAsync(3);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());

        // Act
        var result = await _service.GetDashboardMetricsAsync();

        // Assert
        result.ActiveSubscriptions.Should().Be(30);
        result.TrialUsers.Should().Be(10);
        result.ExpiredSubscriptions.Should().Be(5);
        result.CanceledSubscriptions.Should().Be(3);
    }

    [Test]
    public async Task GetDashboardMetricsAsync_CalculatesChurnRate()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetCountAsync()).ReturnsAsync(100);
        _mockUserRepository.Setup(r => r.GetCountCreatedAfterAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
        _mockUserRepository.Setup(r => r.GetRecentUsersAsync(It.IsAny<int>())).ReturnsAsync(new List<ApplicationUser>());

        // 80 active, 10 trialing, 5 expired, 5 canceled = 100 total
        // Churn = (5 + 5) / 100 = 10%
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Active)).ReturnsAsync(80);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Trialing)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Expired)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Canceled)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());

        // Act
        var result = await _service.GetDashboardMetricsAsync();

        // Assert
        result.MonthlyChurnRate.Should().Be(10m);
    }

    [Test]
    public async Task GetDashboardMetricsAsync_ReturnsRecentSignups()
    {
        // Arrange
        var recentUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), Email = "user1@test.com", FirstName = "User", LastName = "One" },
            new ApplicationUser { Id = Guid.NewGuid(), Email = "user2@test.com", FirstName = "User", LastName = "Two" }
        };

        _mockUserRepository.Setup(r => r.GetCountAsync()).ReturnsAsync(10);
        _mockUserRepository.Setup(r => r.GetCountCreatedAfterAsync(It.IsAny<DateTime>())).ReturnsAsync(2);
        _mockUserRepository.Setup(r => r.GetRecentUsersAsync(10)).ReturnsAsync(recentUsers);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(It.IsAny<SubscriptionStatus>())).ReturnsAsync(0);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());

        // Act
        var result = await _service.GetDashboardMetricsAsync();

        // Assert
        result.RecentSignups.Should().HaveCount(2);
        result.RecentSignups[0].Email.Should().Be("user1@test.com");
    }

    #endregion

    #region GetUsers Tests

    [Test]
    public async Task GetUsersAsync_ReturnsPagedResults()
    {
        // Arrange
        var users = Enumerable.Range(1, 25)
            .Select(i => new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = $"user{i}@test.com",
                FirstName = $"User{i}"
            })
            .ToList();

        _mockUserRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetUsersAsync(1, 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public async Task GetUsersAsync_WithSearch_FiltersResults()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), Email = "john@test.com", FirstName = "John" },
            new ApplicationUser { Id = Guid.NewGuid(), Email = "jane@test.com", FirstName = "Jane" }
        };

        _mockUserRepository.Setup(r => r.SearchAsync("john", It.IsAny<int>())).ReturnsAsync(users.Take(1));
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetUsersAsync(1, 10, "john");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("john@test.com");
    }

    [Test]
    public async Task GetUsersAsync_IncludesSubscriptionStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = userId, Email = "user@test.com", FirstName = "Test" }
        };

        var subscriptions = new List<Subscription>
        {
            new Subscription { UserId = userId, Status = SubscriptionStatus.Active }
        };

        _mockUserRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(subscriptions);
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetUsersAsync(1, 10);

        // Assert
        result.Items[0].SubscriptionStatus.Should().Be(SubscriptionStatus.Active);
    }

    [Test]
    public async Task GetUsersAsync_IncludesAccountCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = userId, Email = "user@test.com", FirstName = "Test" }
        };

        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = userId, Name = "Account 1" },
            new Account { Id = Guid.NewGuid(), UserId = userId, Name = "Account 2" }
        };

        _mockUserRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(new List<Subscription>());
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);

        // Act
        var result = await _service.GetUsersAsync(1, 10);

        // Assert
        result.Items[0].AccountCount.Should().Be(2);
    }

    #endregion

    #region GetUserDetails Tests

    [Test]
    public async Task GetUserDetailsAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.GetUserDetailsAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetUserDetailsAsync_ReturnsUserDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsAdmin = true,
            EmailConfirmed = true,
            TwoFactorEnabled = false,
            Locale = "en-US",
            TimeZone = "America/New_York"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Subscription?)null);
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        _mockAuditService.Setup(s => s.GetByUserIdAsync(userId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<AuditLog>());

        // Act
        var result = await _service.GetUserDetailsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
        result.IsAdmin.Should().BeTrue();
        result.EmailConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task GetUserDetailsAsync_CalculatesNetWorth()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, Email = "test@example.com", FirstName = "Test" };

        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), UserId = userId, AccountType = AccountType.Savings, CurrentBalance = 10000m, IsActive = true },
            new Account { Id = Guid.NewGuid(), UserId = userId, AccountType = AccountType.CreditCard, CurrentBalance = 2000m, IsActive = true }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Subscription?)null);
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);
        _mockAuditService.Setup(s => s.GetByUserIdAsync(userId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<AuditLog>());

        // Act
        var result = await _service.GetUserDetailsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalAssets.Should().Be(10000m);
        result.TotalLiabilities.Should().Be(2000m);
        result.NetWorth.Should().Be(8000m);
    }

    [Test]
    public async Task GetUserDetailsAsync_IncludesSubscriptionDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, Email = "test@example.com", FirstName = "Test" };
        var subscription = new Subscription
        {
            UserId = userId,
            Status = SubscriptionStatus.Active,
            TrialStartedAt = DateTime.UtcNow.AddDays(-30),
            TrialEndsAt = DateTime.UtcNow.AddDays(-16),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(14),
            StripeCustomerId = "cus_123"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(subscription);
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        _mockAuditService.Setup(s => s.GetByUserIdAsync(userId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<AuditLog>());

        // Act
        var result = await _service.GetUserDetailsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.SubscriptionStatus.Should().Be(SubscriptionStatus.Active);
        result.StripeCustomerId.Should().Be("cus_123");
    }

    [Test]
    public async Task GetUserDetailsAsync_IncludesRecentActivity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, Email = "test@example.com", FirstName = "Test" };

        var auditLogs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), UserId = userId, Action = "Login", Timestamp = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), UserId = userId, Action = "AccountCreated", Timestamp = DateTime.UtcNow.AddHours(-1) }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockSubscriptionRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Subscription?)null);
        _mockAccountRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Account>());
        _mockAuditService.Setup(s => s.GetByUserIdAsync(userId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(auditLogs);

        // Act
        var result = await _service.GetUserDetailsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.RecentActivity.Should().HaveCount(2);
        result.RecentActivity[0].Action.Should().Be("Login");
    }

    #endregion

    #region SetAdminStatus Tests

    [Test]
    public async Task SetAdminStatusAsync_SelfDemotion_ReturnsFail()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();

        // Act
        var result = await _service.SetAdminStatusAsync(adminUserId, adminUserId, false);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot remove your own admin status");
    }

    [Test]
    public async Task SetAdminStatusAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.SetAdminStatusAsync(adminUserId, targetUserId, true);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task SetAdminStatusAsync_GrantAdmin_UpdatesUser()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = new ApplicationUser { Id = targetUserId, Email = "target@test.com", IsAdmin = false };

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);

        // Act
        var result = await _service.SetAdminStatusAsync(adminUserId, targetUserId, true);

        // Assert
        result.Success.Should().BeTrue();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.Is<ApplicationUser>(u => u.IsAdmin == true)), Times.Once);
    }

    [Test]
    public async Task SetAdminStatusAsync_RevokeAdmin_UpdatesUser()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = new ApplicationUser { Id = targetUserId, Email = "target@test.com", IsAdmin = true };

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);

        // Act
        var result = await _service.SetAdminStatusAsync(adminUserId, targetUserId, false);

        // Assert
        result.Success.Should().BeTrue();
        _mockUserRepository.Verify(r => r.UpdateAsync(It.Is<ApplicationUser>(u => u.IsAdmin == false)), Times.Once);
    }

    [Test]
    public async Task SetAdminStatusAsync_LogsAuditEvent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = new ApplicationUser { Id = targetUserId, Email = "target@test.com", IsAdmin = false };

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);

        // Act
        await _service.SetAdminStatusAsync(adminUserId, targetUserId, true);

        // Assert
        _mockAuditService.Verify(s => s.LogEntityActionAsync(
            adminUserId,
            "Admin.Granted",
            AuditEntityType.User,
            targetUserId,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region GetAuditLogs Tests

    [Test]
    public async Task GetAuditLogsAsync_ReturnsPagedResults()
    {
        // Arrange
        var logs = Enumerable.Range(1, 100)
            .Select(i => new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = $"Action{i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        _mockAuditService.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(logs);

        // Act
        var result = await _service.GetAuditLogsAsync(1, 20);

        // Assert
        result.Items.Should().HaveCount(20);
        result.TotalCount.Should().Be(100);
        result.TotalPages.Should().Be(5);
    }

    [Test]
    public async Task GetAuditLogsAsync_WithActionFilter_FiltersResults()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), Action = "LoginSuccess", Timestamp = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), Action = "LoginFailed", Timestamp = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), Action = "AccountCreated", Timestamp = DateTime.UtcNow }
        };

        _mockAuditService.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(logs);

        var filter = new AuditLogFilter { Action = "Login" };

        // Act
        var result = await _service.GetAuditLogsAsync(1, 50, filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.All(l => l.Action.Contains("Login")).Should().BeTrue();
    }

    [Test]
    public async Task GetAuditLogsAsync_WithDateFilter_FiltersResults()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), Action = "Action1", Timestamp = now.AddDays(-1) },
            new AuditLog { Id = Guid.NewGuid(), Action = "Action2", Timestamp = now.AddDays(-5) },
            new AuditLog { Id = Guid.NewGuid(), Action = "Action3", Timestamp = now.AddDays(-10) }
        };

        _mockAuditService.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(logs);

        var filter = new AuditLogFilter { From = now.AddDays(-7) };

        // Act
        var result = await _service.GetAuditLogsAsync(1, 50, filter);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAuditLogsAsync_WithUserIdFilter_FiltersResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), UserId = userId, Action = "Action1", Timestamp = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Action = "Action2", Timestamp = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), UserId = userId, Action = "Action3", Timestamp = DateTime.UtcNow }
        };

        _mockAuditService.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(logs);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Email = "test@test.com" });

        var filter = new AuditLogFilter { UserId = userId };

        // Act
        var result = await _service.GetAuditLogsAsync(1, 50, filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.All(l => l.UserId == userId).Should().BeTrue();
    }

    #endregion

    #region ExportAuditLogsCsv Tests

    [Test]
    public async Task ExportAuditLogsCsvAsync_ReturnsCsvWithHeaders()
    {
        // Arrange
        _mockAuditService.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<AuditLog>());

        // Act
        var result = await _service.ExportAuditLogsCsvAsync();

        // Assert
        result.Should().StartWith("Timestamp,User,Action,Entity Type,Entity ID,Description,IP Address,Success,Error");
    }

    [Test]
    public async Task ExportAuditLogsCsvAsync_IncludesLogEntries()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "LoginSuccess",
                EntityType = "User",
                IpAddress = "192.168.1.1",
                Success = true,
                Timestamp = DateTime.UtcNow
            }
        };

        _mockAuditService.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(logs);

        // Act
        var result = await _service.ExportAuditLogsCsvAsync();

        // Assert
        result.Should().Contain("LoginSuccess");
        result.Should().Contain("192.168.1.1");
    }

    #endregion

    #region GetSubscriptionAnalytics Tests

    [Test]
    public async Task GetSubscriptionAnalyticsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Active)).ReturnsAsync(50);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Trialing)).ReturnsAsync(20);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Expired)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Canceled)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.PastDue)).ReturnsAsync(2);
        _mockSubscriptionRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(87);

        // Act
        var result = await _service.GetSubscriptionAnalyticsAsync();

        // Assert
        result.TotalActive.Should().Be(50);
        result.TotalTrialing.Should().Be(20);
        result.TotalExpired.Should().Be(10);
        result.TotalCanceled.Should().Be(5);
        result.TotalPastDue.Should().Be(2);
    }

    [Test]
    public async Task GetSubscriptionAnalyticsAsync_CalculatesTrialConversionRate()
    {
        // Arrange
        // 40 active out of 40 + 10 expired = 80% conversion
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Active)).ReturnsAsync(40);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Trialing)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Expired)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Canceled)).ReturnsAsync(0);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.PastDue)).ReturnsAsync(0);
        _mockSubscriptionRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(60);

        // Act
        var result = await _service.GetSubscriptionAnalyticsAsync();

        // Assert
        result.TrialConversionRate.Should().Be(80m);
    }

    [Test]
    public async Task GetSubscriptionAnalyticsAsync_CalculatesMonthlyChurnRate()
    {
        // Arrange
        // (5 expired + 5 canceled) / 100 total = 10%
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Active)).ReturnsAsync(80);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Trialing)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Expired)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Canceled)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.PastDue)).ReturnsAsync(0);
        _mockSubscriptionRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(100);

        // Act
        var result = await _service.GetSubscriptionAnalyticsAsync();

        // Assert
        result.MonthlyChurnRate.Should().Be(10m);
    }

    [Test]
    public async Task GetSubscriptionAnalyticsAsync_IncludesStatusBreakdown()
    {
        // Arrange
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Active)).ReturnsAsync(50);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Trialing)).ReturnsAsync(25);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Expired)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.Canceled)).ReturnsAsync(10);
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(SubscriptionStatus.PastDue)).ReturnsAsync(5);
        _mockSubscriptionRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(100);

        // Act
        var result = await _service.GetSubscriptionAnalyticsAsync();

        // Assert
        result.StatusBreakdown.Should().HaveCount(5);
        result.StatusBreakdown.Single(s => s.Status == "Active").Percentage.Should().Be(50m);
        result.StatusBreakdown.Single(s => s.Status == "Trialing").Percentage.Should().Be(25m);
    }

    [Test]
    public async Task GetSubscriptionAnalyticsAsync_NoSubscriptions_ReturnsZeros()
    {
        // Arrange
        _mockSubscriptionRepository.Setup(r => r.GetCountByStatusAsync(It.IsAny<SubscriptionStatus>())).ReturnsAsync(0);
        _mockSubscriptionRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(0);

        // Act
        var result = await _service.GetSubscriptionAnalyticsAsync();

        // Assert
        result.TotalSubscriptions.Should().Be(0);
        result.TrialConversionRate.Should().Be(0);
        result.MonthlyChurnRate.Should().Be(0);
        result.StatusBreakdown.Should().BeEmpty();
    }

    #endregion

    #region GetSubscriptions Tests

    [Test]
    public async Task GetSubscriptionsAsync_ReturnsPagedResults()
    {
        // Arrange
        var subscriptions = Enumerable.Range(1, 30)
            .Select(i => new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                User = new ApplicationUser { Email = $"user{i}@test.com", FirstName = $"User{i}" },
                Status = SubscriptionStatus.Active
            })
            .ToList();

        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetSubscriptionsAsync(1, 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(30);
        result.TotalPages.Should().Be(3);
    }

    [Test]
    public async Task GetSubscriptionsAsync_WithStatusFilter_FiltersResults()
    {
        // Arrange
        var subscriptions = new List<Subscription>
        {
            new Subscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), User = new ApplicationUser { Email = "a@test.com" }, Status = SubscriptionStatus.Active },
            new Subscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), User = new ApplicationUser { Email = "b@test.com" }, Status = SubscriptionStatus.Trialing }
        };

        _mockSubscriptionRepository.Setup(r => r.GetByStatusAsync(SubscriptionStatus.Trialing, 1000))
            .ReturnsAsync(subscriptions.Where(s => s.Status == SubscriptionStatus.Trialing));

        // Act
        var result = await _service.GetSubscriptionsAsync(1, 10, SubscriptionStatus.Trialing);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Test]
    public async Task GetSubscriptionsAsync_IncludesUserDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscriptions = new List<Subscription>
        {
            new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = new ApplicationUser { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User" },
                Status = SubscriptionStatus.Active,
                StripeCustomerId = "cus_123",
                StripeSubscriptionId = "sub_456"
            }
        };

        _mockSubscriptionRepository.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetSubscriptionsAsync(1, 10);

        // Assert
        result.Items[0].UserEmail.Should().Be("test@example.com");
        result.Items[0].UserDisplayName.Should().Be("Test User");
        result.Items[0].StripeCustomerId.Should().Be("cus_123");
        result.Items[0].StripeSubscriptionId.Should().Be("sub_456");
    }

    #endregion
}
