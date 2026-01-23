using Microsoft.AspNetCore.Identity;
using NHibernate;
using NHibernate.Linq;
using NetWorthTracker.Core.Entities;

namespace NetWorthTracker.Infrastructure.Identity;

public class ApplicationRoleStore : IRoleStore<ApplicationRole>
{
    private readonly ISession _session;

    public ApplicationRoleStore(ISession session)
    {
        _session = session;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _session.SaveAsync(role, cancellationToken);
        await _session.FlushAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _session.DeleteAsync(role, cancellationToken);
        await _session.FlushAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Guid.TryParse(roleId, out var id))
        {
            return await _session.GetAsync<ApplicationRole>(id, cancellationToken);
        }
        return null;
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _session.Query<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.NormalizedName);
    }

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id.ToString());
    }

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _session.UpdateAsync(role, cancellationToken);
        await _session.FlushAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
        // Session is managed by DI container
    }
}
