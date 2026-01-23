using Microsoft.AspNetCore.Identity;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Infrastructure;
using NetWorthTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (NHibernate, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddNHibernateIdentityStores();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add MVC services
builder.Services.AddControllersWithViews();

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
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

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
