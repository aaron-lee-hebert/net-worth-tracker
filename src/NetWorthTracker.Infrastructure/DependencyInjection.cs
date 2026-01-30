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
using Polly;
using Polly.Extensions.Http;

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

        // Email service (SendGrid)
        services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));
        services.AddScoped<IEmailService, SendGridEmailService>();

        // Stripe service
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        services.AddScoped<IStripeService, StripeService>();

        // Subscription repository
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

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

        // Session management
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserSessionService, UserSessionService>();

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

        // Configure HttpClient with Polly for SendGrid
        var resilienceSettings = configuration.GetSection("Resilience").Get<ResilienceSettings>() ?? new ResilienceSettings();
        services.AddHttpClient("SendGrid")
            .AddPolicyHandler((provider, _) =>
            {
                var logger = provider.GetRequiredService<ILogger<SendGridEmailService>>();
                return ResiliencePolicies.GetRetryPolicy(resilienceSettings, logger);
            })
            .AddPolicyHandler((provider, _) =>
            {
                var logger = provider.GetRequiredService<ILogger<SendGridEmailService>>();
                return ResiliencePolicies.GetCircuitBreakerPolicy(resilienceSettings, logger);
            });

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
