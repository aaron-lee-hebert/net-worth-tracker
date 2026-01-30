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
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuditService _auditService;
    private readonly IAuditLogRepository _auditLogRepository;

    public AdminService(
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IAccountRepository accountRepository,
        IAuditService auditService,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
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

        var activeCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Active);
        var trialCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Trialing);
        var expiredCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Expired);
        var canceledCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Canceled);

        // Calculate churn rate (expired + canceled this month / total active at start of month)
        var totalAtStartOfMonth = activeCount + trialCount + expiredCount + canceledCount;
        var churnRate = totalAtStartOfMonth > 0
            ? Math.Round((decimal)(expiredCount + canceledCount) / totalAtStartOfMonth * 100, 2)
            : 0;

        // Get signup trend for last 12 months
        var signupTrend = await GetSignupTrendAsync(12);

        // Get recent signups
        var recentUsers = await _userRepository.GetRecentUsersAsync(10);
        var subscriptions = await _subscriptionRepository.GetAllWithUsersAsync();
        var subscriptionLookup = subscriptions.ToDictionary(s => s.UserId);

        var recentSignups = recentUsers.Select(u =>
        {
            subscriptionLookup.TryGetValue(u.Id, out var sub);
            return new AdminUserViewModel
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt,
                IsAdmin = u.IsAdmin,
                EmailConfirmed = u.EmailConfirmed,
                SubscriptionStatus = sub?.Status,
                SubscriptionEndsAt = sub?.CurrentPeriodEnd ?? sub?.TrialEndsAt,
                IsInTrial = sub?.IsInTrial,
                TrialDaysRemaining = sub?.TrialDaysRemaining
            };
        }).ToList();

        return new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            NewUsersThisMonth = newUsersThisMonth,
            NewUsersLastMonth = newUsersLastMonth,
            ActiveSubscriptions = activeCount,
            TrialUsers = trialCount,
            ExpiredSubscriptions = expiredCount,
            CanceledSubscriptions = canceledCount,
            MonthlyChurnRate = churnRate,
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

        var subscriptions = await _subscriptionRepository.GetAllWithUsersAsync();
        var subscriptionLookup = subscriptions.ToDictionary(s => s.UserId);

        var pagedUsers = userList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<AdminUserViewModel>();
        foreach (var user in pagedUsers)
        {
            var accounts = await _accountRepository.GetByUserIdAsync(user.Id);
            subscriptionLookup.TryGetValue(user.Id, out var sub);

            items.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                IsAdmin = user.IsAdmin,
                EmailConfirmed = user.EmailConfirmed,
                SubscriptionStatus = sub?.Status,
                SubscriptionEndsAt = sub?.CurrentPeriodEnd ?? sub?.TrialEndsAt,
                IsInTrial = sub?.IsInTrial,
                TrialDaysRemaining = sub?.TrialDaysRemaining,
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

        var subscription = await _subscriptionRepository.GetByUserIdAsync(userId);
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
            SubscriptionStatus = subscription?.Status,
            TrialStartedAt = subscription?.TrialStartedAt,
            TrialEndsAt = subscription?.TrialEndsAt,
            CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
            StripeCustomerId = subscription?.StripeCustomerId,
            IsInTrial = subscription?.IsInTrial,
            TrialDaysRemaining = subscription?.TrialDaysRemaining,
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

    public async Task<SubscriptionAnalyticsViewModel> GetSubscriptionAnalyticsAsync()
    {
        var activeCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Active);
        var trialCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Trialing);
        var expiredCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Expired);
        var canceledCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.Canceled);
        var pastDueCount = await _subscriptionRepository.GetCountByStatusAsync(SubscriptionStatus.PastDue);

        var totalCount = await _subscriptionRepository.GetTotalCountAsync();

        // Calculate trial conversion rate
        // (users who went from trial to active) / (total users who completed trial)
        // Simplified: active / (active + expired)
        var completedTrials = activeCount + expiredCount;
        var conversionRate = completedTrials > 0
            ? Math.Round((decimal)activeCount / completedTrials * 100, 2)
            : 0;

        // Calculate monthly churn rate
        var churnRate = totalCount > 0
            ? Math.Round((decimal)(expiredCount + canceledCount) / totalCount * 100, 2)
            : 0;

        var breakdown = new List<SubscriptionStatusBreakdown>();
        if (totalCount > 0)
        {
            breakdown.Add(new SubscriptionStatusBreakdown { Status = "Active", Count = activeCount, Percentage = Math.Round((decimal)activeCount / totalCount * 100, 1) });
            breakdown.Add(new SubscriptionStatusBreakdown { Status = "Trialing", Count = trialCount, Percentage = Math.Round((decimal)trialCount / totalCount * 100, 1) });
            breakdown.Add(new SubscriptionStatusBreakdown { Status = "Past Due", Count = pastDueCount, Percentage = Math.Round((decimal)pastDueCount / totalCount * 100, 1) });
            breakdown.Add(new SubscriptionStatusBreakdown { Status = "Canceled", Count = canceledCount, Percentage = Math.Round((decimal)canceledCount / totalCount * 100, 1) });
            breakdown.Add(new SubscriptionStatusBreakdown { Status = "Expired", Count = expiredCount, Percentage = Math.Round((decimal)expiredCount / totalCount * 100, 1) });
        }

        // Calculate annual churn rate (projected from monthly)
        var annualChurnRate = Math.Min(100, Math.Round((1 - (decimal)Math.Pow((double)(1 - churnRate / 100), 12)) * 100, 2));

        return new SubscriptionAnalyticsViewModel
        {
            TotalSubscriptions = totalCount,
            TotalActive = activeCount,
            TotalTrialing = trialCount,
            TotalExpired = expiredCount,
            TotalCanceled = canceledCount,
            TotalPastDue = pastDueCount,
            TrialConversionRate = conversionRate,
            MonthlyChurnRate = churnRate,
            AnnualChurnRate = annualChurnRate,
            StatusBreakdown = breakdown
        };
    }

    public async Task<PagedResult<AdminSubscriptionViewModel>> GetSubscriptionsAsync(int page, int pageSize, SubscriptionStatus? status = null)
    {
        var subscriptions = status.HasValue
            ? await _subscriptionRepository.GetByStatusAsync(status.Value, 1000)
            : await _subscriptionRepository.GetAllWithUsersAsync();

        var subscriptionList = subscriptions.ToList();
        var totalCount = subscriptionList.Count;

        var pagedSubscriptions = subscriptionList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new AdminSubscriptionViewModel
            {
                Id = s.Id,
                UserId = s.UserId,
                UserEmail = s.User?.Email ?? "Unknown",
                UserDisplayName = s.User?.DisplayName ?? "Unknown",
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                TrialStartedAt = s.TrialStartedAt,
                TrialEndsAt = s.TrialEndsAt,
                CurrentPeriodEnd = s.CurrentPeriodEnd,
                StripeCustomerId = s.StripeCustomerId,
                StripeSubscriptionId = s.StripeSubscriptionId,
                HasActiveAccess = s.HasActiveAccess
            })
            .ToList();

        return new PagedResult<AdminSubscriptionViewModel>
        {
            Items = pagedSubscriptions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
