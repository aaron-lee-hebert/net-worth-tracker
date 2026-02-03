using System.Text;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuditService _auditService;
    private readonly IAuditLogRepository _auditLogRepository;

    public AdminService(
        IUserRepository userRepository,
        IAccountRepository accountRepository,
        IAuditService auditService,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _accountRepository = accountRepository;
        _auditService = auditService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<AdminDashboardViewModel> GetDashboardMetricsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var totalUsers = await _userRepository.GetCountAsync();
        var newUsersThisMonth = await _userRepository.GetCountCreatedAfterAsync(startOfMonth);
        var newUsersLastMonth = await _userRepository.GetCountCreatedAfterAsync(startOfLastMonth) - newUsersThisMonth;

        // Get signup trend for last 12 months
        var signupTrend = await GetSignupTrendAsync(12);

        // Get recent signups
        var recentUsers = await _userRepository.GetRecentUsersAsync(10);

        var recentSignups = recentUsers.Select(u => new AdminUserViewModel
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            DisplayName = u.DisplayName,
            CreatedAt = u.CreatedAt,
            IsAdmin = u.IsAdmin,
            EmailConfirmed = u.EmailConfirmed
        }).ToList();

        return new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            NewUsersThisMonth = newUsersThisMonth,
            NewUsersLastMonth = newUsersLastMonth,
            SignupTrend = signupTrend,
            RecentSignups = recentSignups
        };
    }

    private async Task<List<SignupTrendPoint>> GetSignupTrendAsync(int months)
    {
        var result = new List<SignupTrendPoint>();
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var countAfterStart = await _userRepository.GetCountCreatedAfterAsync(monthStart);
            var countAfterEnd = await _userRepository.GetCountCreatedAfterAsync(monthEnd);
            var countInMonth = countAfterStart - countAfterEnd;

            result.Add(new SignupTrendPoint
            {
                Month = monthStart.ToString("MMM yyyy"),
                Count = countInMonth
            });
        }

        return result;
    }

    public async Task<PagedResult<AdminUserViewModel>> GetUsersAsync(int page, int pageSize, string? search = null)
    {
        var users = string.IsNullOrWhiteSpace(search)
            ? await _userRepository.GetAllAsync()
            : await _userRepository.SearchAsync(search);

        var userList = users.ToList();
        var totalCount = userList.Count;

        var pagedUsers = userList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<AdminUserViewModel>();
        foreach (var user in pagedUsers)
        {
            var accounts = await _accountRepository.GetByUserIdAsync(user.Id);

            items.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                IsAdmin = user.IsAdmin,
                EmailConfirmed = user.EmailConfirmed,
                AccountCount = accounts.Count()
            });
        }

        return new PagedResult<AdminUserViewModel>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailsViewModel?> GetUserDetailsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        var accountList = accounts.ToList();

        var totalAssets = accountList.Where(a => a.IsActive && a.AccountType.IsAsset()).Sum(a => a.CurrentBalance);
        var totalLiabilities = accountList.Where(a => a.IsActive && a.AccountType.IsLiability()).Sum(a => a.CurrentBalance);

        var recentActivity = await _auditService.GetByUserIdAsync(userId, 20);

        return new AdminUserDetailsViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsAdmin = user.IsAdmin,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Locale = user.Locale,
            TimeZone = user.TimeZone,
            AccountCount = accountList.Count,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = totalAssets - totalLiabilities,
            RecentActivity = recentActivity.Select(a => new AuditLogViewModel
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                UserId = a.UserId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                Success = a.Success,
                ErrorMessage = a.ErrorMessage
            }).ToList()
        };
    }

    public async Task<ServiceResult> SetAdminStatusAsync(Guid adminUserId, Guid targetUserId, bool isAdmin)
    {
        // Prevent self-demotion
        if (adminUserId == targetUserId && !isAdmin)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = "You cannot remove your own admin status."
            };
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId);
        if (targetUser == null)
        {
            return ServiceResult.NotFound();
        }

        var oldValue = targetUser.IsAdmin;
        targetUser.IsAdmin = isAdmin;
        await _userRepository.UpdateAsync(targetUser);

        // Audit log the admin status change
        await _auditService.LogEntityActionAsync(
            adminUserId,
            isAdmin ? "Admin.Granted" : "Admin.Revoked",
            AuditEntityType.User,
            targetUserId,
            oldValue: new { IsAdmin = oldValue },
            newValue: new { IsAdmin = isAdmin },
            description: $"Admin status {(isAdmin ? "granted to" : "revoked from")} {targetUser.Email}");

        return ServiceResult.Ok();
    }

    public async Task<PagedResult<AuditLogViewModel>> GetAuditLogsAsync(int page, int pageSize, AuditLogFilter? filter = null)
    {
        var logs = await _auditService.GetRecentAsync(1000, 0); // Get more for filtering
        var logList = logs.ToList();

        // Apply filters
        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Action))
                logList = logList.Where(l => l.Action.Contains(filter.Action, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                logList = logList.Where(l => l.EntityType.Contains(filter.EntityType, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filter.UserId.HasValue)
                logList = logList.Where(l => l.UserId == filter.UserId.Value).ToList();

            if (filter.From.HasValue)
                logList = logList.Where(l => l.Timestamp >= filter.From.Value).ToList();

            if (filter.To.HasValue)
                logList = logList.Where(l => l.Timestamp <= filter.To.Value).ToList();

            if (filter.SuccessOnly == true)
                logList = logList.Where(l => l.Success).ToList();
        }

        var totalCount = logList.Count;

        // Get user emails for display
        var userIds = logList.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = new Dictionary<Guid, string>();
        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
                users[userId] = user.Email ?? "Unknown";
        }

        var pagedLogs = logList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogViewModel
            {
                Id = l.Id,
                Timestamp = l.Timestamp,
                UserId = l.UserId,
                UserEmail = l.UserId.HasValue && users.TryGetValue(l.UserId.Value, out var email) ? email : null,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Description = l.Description,
                IpAddress = l.IpAddress,
                Success = l.Success,
                ErrorMessage = l.ErrorMessage,
                OldValue = l.OldValue,
                NewValue = l.NewValue
            })
            .ToList();

        return new PagedResult<AuditLogViewModel>
        {
            Items = pagedLogs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<string> ExportAuditLogsCsvAsync(AuditLogFilter? filter = null)
    {
        var result = await GetAuditLogsAsync(1, 10000, filter);
        var sb = new StringBuilder();

        sb.AppendLine("Timestamp,User,Action,Entity Type,Entity ID,Description,IP Address,Success,Error");

        foreach (var log in result.Items)
        {
            sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{EscapeCsv(log.UserEmail)}\",\"{EscapeCsv(log.Action)}\",\"{EscapeCsv(log.EntityType)}\",\"{log.EntityId}\",\"{EscapeCsv(log.Description)}\",\"{EscapeCsv(log.IpAddress)}\",{log.Success},\"{EscapeCsv(log.ErrorMessage)}\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}
