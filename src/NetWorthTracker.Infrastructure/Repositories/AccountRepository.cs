using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;
using NetWorthTracker.Core.Extensions;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class AccountRepository : RepositoryBase<Account>, IAccountRepository
{
    public AccountRepository(ISession session) : base(session)
    {
    }

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId)
    {
        return await Session.Query<Account>()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetByUserIdAndTypeAsync(Guid userId, AccountType accountType)
    {
        return await Session.Query<Account>()
            .Where(a => a.UserId == userId && a.AccountType == accountType)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetByUserIdAndCategoryAsync(Guid userId, AccountCategory category)
    {
        var typesInCategory = AccountTypeExtensions.GetTypesByCategory(category).ToList();
        return await Session.Query<Account>()
            .Where(a => a.UserId == userId && typesInCategory.Contains(a.AccountType))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetActiveAccountsByUserIdAsync(Guid userId)
    {
        return await Session.Query<Account>()
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }
}
