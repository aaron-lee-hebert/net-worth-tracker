using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ISession _session;

    public UserRepository(ISession session)
    {
        _session = session;
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
    {
        return await _session.Query<ApplicationUser>()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        return await _session.GetAsync<ApplicationUser>(id);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _session.Query<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
    }

    public async Task<int> GetCountAsync()
    {
        return await _session.Query<ApplicationUser>().CountAsync();
    }

    public async Task<int> GetCountCreatedAfterAsync(DateTime date)
    {
        return await _session.Query<ApplicationUser>()
            .CountAsync(u => u.CreatedAt >= date);
    }

    public async Task<IEnumerable<ApplicationUser>> GetRecentUsersAsync(int limit = 10)
    {
        return await _session.Query<ApplicationUser>()
            .OrderByDescending(u => u.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> SearchAsync(string searchTerm, int limit = 100)
    {
        var term = searchTerm.ToUpperInvariant();
        return await _session.Query<ApplicationUser>()
            .Where(u => u.NormalizedEmail!.Contains(term) ||
                        (u.FirstName != null && u.FirstName.ToUpper().Contains(term)) ||
                        (u.LastName != null && u.LastName.ToUpper().Contains(term)))
            .OrderByDescending(u => u.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task UpdateAsync(ApplicationUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(user);
        await _session.FlushAsync();
    }
}
