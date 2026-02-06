using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NetWorthTracker.Integration.Tests;

internal class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _testDbPath;

    public CustomWebApplicationFactory()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"networth_test_{Guid.NewGuid()}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Remove existing configuration sources
            config.Sources.Clear();

            // Add test-specific configuration
            var testConfig = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "SQLite",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}",
                ["SeedDemoData"] = "false",
                ["RunMigrationsOnStartup"] = "false",
                ["SendGrid:ApiKey"] = "",
                ["SendGrid:FromEmail"] = "test@example.com",
                ["Stripe:SecretKey"] = "",
                ["Stripe:WebhookSecret"] = "",
                ["Seq:ServerUrl"] = "",
                ["ErrorAlerts:Enabled"] = "false",
                ["DataRetention:Enabled"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Additional test-specific service configuration can go here
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    public HttpClient CreateAuthenticatedClient(string? userId = null)
    {
        // For tests that need authenticated requests, we'll need to set up
        // cookies or tokens. This is a placeholder for that functionality.
        return CreateClient();
    }
}
