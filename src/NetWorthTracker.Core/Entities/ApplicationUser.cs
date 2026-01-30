using Microsoft.AspNetCore.Identity;

namespace NetWorthTracker.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public virtual string? FirstName { get; set; }
    public virtual string? LastName { get; set; }
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual DateTime? UpdatedAt { get; set; }
    public virtual IList<Account> Accounts { get; set; } = new List<Account>();

    // MFA properties
    public virtual string? AuthenticatorKey { get; set; }
    public virtual string? RecoveryCodes { get; set; }

    // Locale preference (e.g., "en-US", "en-GB")
    public virtual string Locale { get; set; } = "en-US";

    // IANA timezone identifier (e.g., "America/New_York", "Europe/London")
    public virtual string TimeZone { get; set; } = "America/New_York";

    /// <summary>
    /// Whether the user has admin privileges
    /// </summary>
    public virtual bool IsAdmin { get; set; } = false;

    /// <summary>
    /// Gets the user's display name. Returns FirstName + LastName if available, otherwise falls back to UserName.
    /// </summary>
    public virtual string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName))
            {
                return $"{FirstName} {LastName}".Trim();
            }
            return UserName ?? Email ?? "User";
        }
    }
}
