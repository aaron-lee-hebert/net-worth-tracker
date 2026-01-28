using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Core.Services;
using NetWorthTracker.Infrastructure.Data;
using NetWorthTracker.Infrastructure.Identity;
using NetWorthTracker.Infrastructure.Repositories;
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

        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    public static IdentityBuilder AddNHibernateIdentityStores(this IdentityBuilder builder)
    {
        builder.Services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();
        builder.Services.AddScoped<IRoleStore<ApplicationRole>, ApplicationRoleStore>();
        return builder;
    }
}
