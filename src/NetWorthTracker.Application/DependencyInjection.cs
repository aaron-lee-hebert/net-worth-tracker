using Microsoft.Extensions.DependencyInjection;
using NetWorthTracker.Application.Interfaces;
using NetWorthTracker.Application.Services;

namespace NetWorthTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IAccountManagementService, AccountManagementService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
