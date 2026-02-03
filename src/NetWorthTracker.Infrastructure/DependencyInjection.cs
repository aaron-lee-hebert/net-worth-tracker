using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Infrastructure.Data;
using NetWorthTracker.Infrastructure.Health;
using NetWorthTracker.Infrastructure.Identity;
using NetWorthTracker.Infrastructure.Repositories;
using NetWorthTracker.Infrastructure.Resilience;
using NetWorthTracker.Infrastructure.Services;

namespace NetWorthTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<NHibernateHelper>(provider =>
            new NHibernateHelper(configuration, provider.GetService<ILogger<NHibernateHelper>>()));

        services.AddScoped<ISession>(provider =>
        {
            var helper = provider.GetRequiredService<NHibernateHelper>();
            return helper.OpenSession();
        });

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBalanceHistoryRepository, BalanceHistoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Email service (SMTP)
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Alert repositories and service
        services.AddScoped<IAlertConfigurationRepository, AlertConfigurationRepository>();
        services.AddScoped<IMonthlySnapshotRepository, MonthlySnapshotRepository>();
        services.AddScoped<IAlertService, AlertService>();

        // Forecast assumptions repository
        services.AddScoped<IForecastAssumptionsRepository, ForecastAssumptionsRepository>();

        // Audit logging
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditService, AuditService>();

        // Encryption service
        services.AddScoped<IEncryptionService, EncryptionService>();

        // Email queue and resilience
        services.Configure<ResilienceSettings>(configuration.GetSection("Resilience"));
        services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();

        // Job tracking for idempotency
        services.AddScoped<IProcessedJobRepository, ProcessedJobRepository>();

        // Soft delete service
        services.AddScoped<ISoftDeleteService, SoftDeleteService>();

        // Error alert service
        services.AddScoped<IErrorAlertService, ErrorAlertService>();

        // Background services
        services.AddHostedService<EmailProcessingBackgroundService>();
        services.AddHostedService<DataRetentionBackgroundService>();

        // Health checks
        services.AddScoped<BackgroundJobHealthCheck>();

        // Audit logging settings
        services.Configure<AuditSettings>(configuration.GetSection("AuditLogging"));

        // Data migrators
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<AccountNumberMigrator>();

        // Database migrations
        services.Configure<MigrationSettings>(configuration.GetSection("MigrationSettings"));
        services.AddScoped<IMigrationRunner, MigrationRunner>();
        services.AddScoped<MigrationHealthCheck>();

        return services;
    }

    public static IdentityBuilder AddNHibernateIdentityStores(this IdentityBuilder builder)
    {
        builder.Services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();
        builder.Services.AddScoped<IRoleStore<ApplicationRole>, ApplicationRoleStore>();
        builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
        return builder;
    }
}
