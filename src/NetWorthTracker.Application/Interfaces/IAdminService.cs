using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Interfaces;

public interface IAdminService
{
    // Dashboard metrics
    Task<AdminDashboardViewModel> GetDashboardMetricsAsync();

    // User management
    Task<PagedResult<AdminUserViewModel>> GetUsersAsync(int page, int pageSize, string? search = null);
    Task<AdminUserDetailsViewModel?> GetUserDetailsAsync(Guid userId);
    Task<ServiceResult> SetAdminStatusAsync(Guid adminUserId, Guid targetUserId, bool isAdmin);

    // Audit logs
    Task<PagedResult<AuditLogViewModel>> GetAuditLogsAsync(int page, int pageSize, AuditLogFilter? filter = null);
    Task<string> ExportAuditLogsCsvAsync(AuditLogFilter? filter = null);

    // Subscription analytics
    Task<SubscriptionAnalyticsViewModel> GetSubscriptionAnalyticsAsync();
    Task<PagedResult<AdminSubscriptionViewModel>> GetSubscriptionsAsync(int page, int pageSize, SubscriptionStatus? status = null);
}
