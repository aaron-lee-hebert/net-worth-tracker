using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Infrastructure.Identity;

/// <summary>
/// Custom claims principal factory that adds the IsAdmin claim to authenticated users.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add IsAdmin claim
        identity.AddClaim(new Claim("IsAdmin", user.IsAdmin.ToString().ToLowerInvariant()));

        // Add TimeZone claim for timezone conversion in views
        if (!string.IsNullOrEmpty(user.TimeZone))
        {
            identity.AddClaim(new Claim("TimeZone", user.TimeZone));
        }

        return identity;
    }
}
