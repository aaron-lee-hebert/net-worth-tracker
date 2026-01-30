using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Core.Interfaces;

/// <summary>
/// Repository for querying ApplicationUser data for admin purposes.
/// Note: This is separate from Identity's IUserStore which handles authentication.
/// </summary>
public interface IUserRepository
{
    Task<IEnumerable<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> GetByIdAsync(Guid id);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<int> GetCountAsync();
    Task<int> GetCountCreatedAfterAsync(DateTime date);
    Task<IEnumerable<ApplicationUser>> GetRecentUsersAsync(int limit = 10);
    Task<IEnumerable<ApplicationUser>> SearchAsync(string searchTerm, int limit = 100);
    Task UpdateAsync(ApplicationUser user);
}
