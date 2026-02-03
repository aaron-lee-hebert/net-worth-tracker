using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;
using NetWorthTracker.Application;
using NetWorthTracker.Infrastructure;
using NetWorthTracker.Infrastructure.Data;
using NetWorthTracker.Infrastructure.Health;
using NetWorthTracker.Web.HealthChecks;
using NetWorthTracker.Web.Middleware;
using NetWorthTracker.Web.Services;
using Serilog;

// Configure Serilog early for bootstrap logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Net Worth Tracker");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    });

    // Add Infrastructure services (NHibernate, Repositories)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add Application services
    builder.Services.AddApplication();

    // Configure Data Protection for encryption keys
    var keyPath = builder.Configuration["DataProtection:KeyPath"] ?? "./keys";
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("NetWorthTracker");

    // Add Identity services
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password requirements
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;

        // User settings
        options.User.RequireUniqueEmail = true;

        // Lockout settings for brute force protection
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = false; // Handled manually based on email config
    })
    .AddNHibernateIdentityStores()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Cookie security settings
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    // Add MVC services
    builder.Services.AddControllersWithViews();

    // Add HttpContextAccessor for accessing current user in services
    builder.Services.AddHttpContextAccessor();

    // Add user timezone service for converting timestamps to user's local time
    builder.Services.AddScoped<NetWorthTracker.Core.Services.IUserTimeZoneService, UserTimeZoneService>();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database")
        .AddCheck<BackgroundJobHealthCheck>("background-jobs")
        .AddCheck<MigrationHealthCheck>("migrations");

    // Add background service for alerts
    builder.Services.AddHostedService<AlertBackgroundService>();

    // Configure HSTS (HTTP Strict Transport Security)
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });

    var app = builder.Build();

    // Handle CLI commands before starting the web server
    if (await HandleCliCommands(args, app.Services))
    {
        return; // Exit after handling CLI command
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");

        // HTTPS redirection and HSTS for production
        app.UseHttpsRedirection();
        app.UseHsts();
    }

    // Security headers middleware
    app.Use(async (context, next) =>
    {
        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // XSS protection (legacy, but still useful for older browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions policy (disable features we don't use)
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await next();
    });

    // Serilog request logging (adds structured HTTP request logs)
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    diagnosticContext.Set("UserId", userId);
                }
            }
        };
    });

    app.UseRouting();

    app.UseAuthentication();

    // Set user locale for formatting (dates, numbers, currency)
    app.UseUserLocale();

    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            });
            await context.Response.WriteAsync(result);
        }
    });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    // Seed demo data if enabled
    if (app.Configuration.GetValue<bool>("SeedDemoData"))
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }

    // Migrate existing account numbers to encrypted format if enabled
    if (app.Configuration.GetValue<bool>("MigrateAccountNumbers"))
    {
        using var scope = app.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<AccountNumberMigrator>();
        await migrator.MigrateAsync();
    }

    // Run database migrations on startup if enabled
    if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var result = await migrationRunner.RunMigrationsAsync();

        if (!result.Success)
        {
            Log.Fatal("Database migration failed at version {Version}: {Error}",
                result.FailedVersion, result.ErrorMessage);
            throw new Exception($"Migration {result.FailedVersion} failed: {result.ErrorMessage}");
        }

        if (result.MigrationsApplied > 0)
        {
            Log.Information("Applied {Count} database migrations: {Versions}",
                result.MigrationsApplied,
                string.Join(", ", result.AppliedVersions));
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Application shutting down");
    Log.CloseAndFlush();
}

/// <summary>
/// Handles CLI commands for administrative tasks.
/// Returns true if a command was handled (application should exit).
/// </summary>
static async Task<bool> HandleCliCommands(string[] args, IServiceProvider services)
{
    if (args.Length == 0)
        return false;

    var command = args[0].ToLowerInvariant();

    switch (command)
    {
        case "--reset-password":
            await ResetPasswordCommand(args, services);
            return true;

        case "--make-admin":
            await MakeAdminCommand(args, services);
            return true;

        case "--revoke-admin":
            await RevokeAdminCommand(args, services);
            return true;

        case "--list-users":
            await ListUsersCommand(services);
            return true;

        case "--migrate":
            await RunMigrationsCommand(services);
            return true;

        case "--migrate-status":
            await ShowMigrationStatusCommand(services);
            return true;

        case "--migrate-rollback":
            await RollbackMigrationCommand(services);
            return true;

        case "--help":
        case "-h":
            ShowHelp();
            return true;

        default:
            // Not a CLI command, continue with normal startup
            return false;
    }
}

static async Task ResetPasswordCommand(string[] args, IServiceProvider services)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: --reset-password <email> <new-password>");
        Console.WriteLine("Example: --reset-password user@example.com NewPassword123");
        Environment.Exit(1);
        return;
    }

    var email = args[1];
    var newPassword = args[2];

    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        Console.WriteLine($"Error: User with email '{email}' not found.");
        Environment.Exit(1);
        return;
    }

    // Remove existing password and set new one
    var removeResult = await userManager.RemovePasswordAsync(user);
    if (!removeResult.Succeeded)
    {
        Console.WriteLine("Error removing existing password:");
        foreach (var error in removeResult.Errors)
        {
            Console.WriteLine($"  - {error.Description}");
        }
        Environment.Exit(1);
        return;
    }

    var addResult = await userManager.AddPasswordAsync(user, newPassword);
    if (!addResult.Succeeded)
    {
        Console.WriteLine("Error setting new password:");
        foreach (var error in addResult.Errors)
        {
            Console.WriteLine($"  - {error.Description}");
        }
        Environment.Exit(1);
        return;
    }

    Console.WriteLine($"Password successfully reset for user: {email}");
}

static async Task MakeAdminCommand(string[] args, IServiceProvider services)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: --make-admin <email>");
        Console.WriteLine("Example: --make-admin admin@example.com");
        Environment.Exit(1);
        return;
    }

    var email = args[1];

    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        Console.WriteLine($"Error: User with email '{email}' not found.");
        Environment.Exit(1);
        return;
    }

    if (user.IsAdmin)
    {
        Console.WriteLine($"User '{email}' is already an admin.");
        return;
    }

    user.IsAdmin = true;
    using var transaction = session.BeginTransaction();
    await session.UpdateAsync(user);
    await transaction.CommitAsync();

    Console.WriteLine($"Successfully granted admin access to: {email}");
}

static async Task RevokeAdminCommand(string[] args, IServiceProvider services)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: --revoke-admin <email>");
        Console.WriteLine("Example: --revoke-admin user@example.com");
        Environment.Exit(1);
        return;
    }

    var email = args[1];

    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        Console.WriteLine($"Error: User with email '{email}' not found.");
        Environment.Exit(1);
        return;
    }

    if (!user.IsAdmin)
    {
        Console.WriteLine($"User '{email}' is not an admin.");
        return;
    }

    user.IsAdmin = false;
    using var transaction = session.BeginTransaction();
    await session.UpdateAsync(user);
    await transaction.CommitAsync();

    Console.WriteLine($"Successfully revoked admin access from: {email}");
}

static async Task ListUsersCommand(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();

    var users = await session.QueryOver<ApplicationUser>()
        .OrderBy(u => u.CreatedAt).Asc
        .ListAsync();

    if (users.Count == 0)
    {
        Console.WriteLine("No users found.");
        return;
    }

    Console.WriteLine($"{"Email",-40} {"Name",-25} {"Admin",-6} {"Created",-20}");
    Console.WriteLine(new string('-', 95));

    foreach (var user in users)
    {
        var name = user.DisplayName;
        var admin = user.IsAdmin ? "Yes" : "No";
        var created = user.CreatedAt.ToString("yyyy-MM-dd HH:mm");
        Console.WriteLine($"{user.Email,-40} {name,-25} {admin,-6} {created,-20}");
    }

    Console.WriteLine();
    Console.WriteLine($"Total users: {users.Count}");
    Console.WriteLine($"Admins: {users.Count(u => u.IsAdmin)}");
}

static async Task RunMigrationsCommand(IServiceProvider services)
{
    Console.WriteLine("Running database migrations...");
    Console.WriteLine();

    using var scope = services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    var result = await migrationRunner.RunMigrationsAsync();

    if (result.Success)
    {
        if (result.MigrationsApplied == 0)
        {
            Console.WriteLine("No pending migrations to apply.");
        }
        else
        {
            Console.WriteLine($"Successfully applied {result.MigrationsApplied} migration(s):");
            foreach (var version in result.AppliedVersions)
            {
                Console.WriteLine($"  - {version}");
            }
        }
    }
    else
    {
        Console.WriteLine($"Migration failed at version {result.FailedVersion}:");
        Console.WriteLine($"  Error: {result.ErrorMessage}");
        Environment.Exit(1);
    }
}

static async Task ShowMigrationStatusCommand(IServiceProvider services)
{
    Console.WriteLine("Database Migration Status");
    Console.WriteLine("=========================");
    Console.WriteLine();

    using var scope = services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    var applied = (await migrationRunner.GetAppliedMigrationsAsync()).ToList();
    var pending = (await migrationRunner.GetPendingMigrationsAsync()).ToList();

    Console.WriteLine($"Applied Migrations ({applied.Count}):");
    if (applied.Any())
    {
        foreach (var migration in applied.OrderBy(m => m.Version))
        {
            Console.WriteLine($"  [{migration.Version}] {migration.Description}");
            Console.WriteLine($"           Applied: {migration.AppliedAt:yyyy-MM-dd HH:mm:ss}");
        }
    }
    else
    {
        Console.WriteLine("  (none)");
    }

    Console.WriteLine();
    Console.WriteLine($"Pending Migrations ({pending.Count}):");
    if (pending.Any())
    {
        foreach (var migration in pending.OrderBy(m => m.Version))
        {
            Console.WriteLine($"  [{migration.Version}] {migration.Description}");
        }
    }
    else
    {
        Console.WriteLine("  (none)");
    }
}

static async Task RollbackMigrationCommand(IServiceProvider services)
{
    Console.WriteLine("Rolling back last migration...");
    Console.WriteLine();

    using var scope = services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    var applied = (await migrationRunner.GetAppliedMigrationsAsync()).ToList();
    if (!applied.Any())
    {
        Console.WriteLine("No migrations to rollback.");
        return;
    }

    var lastMigration = applied.OrderByDescending(m => m.Version).First();
    Console.WriteLine($"Rolling back: [{lastMigration.Version}] {lastMigration.Description}");
    Console.Write("Are you sure? (y/N): ");

    var response = Console.ReadLine();
    if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Rollback cancelled.");
        return;
    }

    var result = await migrationRunner.RollbackLastMigrationAsync();

    if (result.Success)
    {
        Console.WriteLine($"Successfully rolled back migration {lastMigration.Version}");
    }
    else
    {
        Console.WriteLine($"Rollback failed: {result.ErrorMessage}");
        Environment.Exit(1);
    }
}

static void ShowHelp()
{
    Console.WriteLine("Net Worth Tracker - Administrative Commands");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet NetWorthTracker.Web.dll [command] [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  --reset-password <email> <new-password>  Reset a user's password");
    Console.WriteLine("  --make-admin <email>                     Grant admin access to a user");
    Console.WriteLine("  --revoke-admin <email>                   Revoke admin access from a user");
    Console.WriteLine("  --list-users                             List all registered users");
    Console.WriteLine();
    Console.WriteLine("Migration Commands:");
    Console.WriteLine("  --migrate                                Run all pending database migrations");
    Console.WriteLine("  --migrate-status                         Show migration status");
    Console.WriteLine("  --migrate-rollback                       Rollback the last migration");
    Console.WriteLine();
    Console.WriteLine("  --help, -h                               Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --reset-password user@example.com NewPass123");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --make-admin admin@example.com");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --revoke-admin user@example.com");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --list-users");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --migrate");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --migrate-status");
    Console.WriteLine();
    Console.WriteLine("Docker usage:");
    Console.WriteLine("  docker exec -it <container> dotnet NetWorthTracker.Web.dll --make-admin admin@example.com");
    Console.WriteLine("  docker exec -it <container> dotnet NetWorthTracker.Web.dll --migrate");
}
