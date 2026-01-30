using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NetWorthTracker.Web.Authorization;

/// <summary>
/// Authorization filter that restricts access to admin users only.
/// Checks for the "IsAdmin" claim with value "true".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Must be authenticated
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Must have IsAdmin claim with value "true"
        var isAdminClaim = user.FindFirst("IsAdmin");
        if (isAdminClaim == null || !bool.TryParse(isAdminClaim.Value, out var isAdmin) || !isAdmin)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
