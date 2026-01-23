using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Core.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Account>> GetByUserIdAndTypeAsync(Guid userId, AccountType accountType);
    Task<IEnumerable<Account>> GetByUserIdAndCategoryAsync(Guid userId, AccountCategory category);
    Task<IEnumerable<Account>> GetActiveAccountsByUserIdAsync(Guid userId);
}
