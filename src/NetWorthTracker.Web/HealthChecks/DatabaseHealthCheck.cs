using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NetWorthTracker.Web.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly NHibernate.ISession _session;

    public DatabaseHealthCheck(NHibernate.ISession session)
    {
        _session = session;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Execute a simple query to verify database connectivity
            await _session.CreateSQLQuery("SELECT 1").UniqueResultAsync<int>(cancellationToken);
            return HealthCheckResult.Healthy("Database connection is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed.",
                exception: ex);
        }
    }
}
