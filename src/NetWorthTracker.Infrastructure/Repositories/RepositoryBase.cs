using NHibernate;
using NetWorthTracker.Core.Entities;
using NetWorthTracker.Core.Interfaces;

namespace NetWorthTracker.Infrastructure.Repositories;

public class RepositoryBase<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ISession Session;

    public RepositoryBase(ISession session)
    {
        Session = session;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await Session.GetAsync<T>(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Task.FromResult(Session.Query<T>().ToList());
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await Session.SaveAsync(entity);
        await Session.FlushAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await Session.UpdateAsync(entity);
        await Session.FlushAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        await Session.DeleteAsync(entity);
        await Session.FlushAsync();
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }
}
