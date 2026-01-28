using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure;
using NetWorthTracker.Infrastructure.Data;
using NetWorthTracker.Web.HealthChecks;
using NetWorthTracker.Web.Middleware;
using NetWorthTracker.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (NHibernate, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

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
});

// Add MVC services
builder.Services.AddControllersWithViews();

// Add rate limiting for auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Rate limit for authentication endpoints (login, register, password reset)
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Stricter rate limit for password reset to prevent enumeration
    options.AddPolicy("password-reset", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

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

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

// Set user locale for formatting (dates, numbers, currency)
app.UseUserLocale();

app.UseAuthorization();

// Check subscription status for authenticated users
app.UseSubscriptionMiddleware();

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

app.Run();

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

        case "--list-users":
            await ListUsersCommand(services);
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

    Console.WriteLine($"{"Email",-40} {"Name",-30} {"Created",-20}");
    Console.WriteLine(new string('-', 90));

    foreach (var user in users)
    {
        var name = user.DisplayName;
        var created = user.CreatedAt.ToString("yyyy-MM-dd HH:mm");
        Console.WriteLine($"{user.Email,-40} {name,-30} {created,-20}");
    }

    Console.WriteLine();
    Console.WriteLine($"Total users: {users.Count}");
}

static void ShowHelp()
{
    Console.WriteLine("Net Worth Tracker - Administrative Commands");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet NetWorthTracker.Web.dll [command] [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  --reset-password <email> <new-password>  Reset a user's password");
    Console.WriteLine("  --list-users                             List all registered users");
    Console.WriteLine("  --help, -h                               Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --reset-password user@example.com NewPass123");
    Console.WriteLine("  dotnet NetWorthTracker.Web.dll --list-users");
    Console.WriteLine();
    Console.WriteLine("Docker usage:");
    Console.WriteLine("  docker exec -it <container> dotnet NetWorthTracker.Web.dll --reset-password user@example.com NewPass123");
}
