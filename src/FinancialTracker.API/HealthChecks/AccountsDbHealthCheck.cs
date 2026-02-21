using FinancialTracker.Accounts.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancialTracker.API.HealthChecks;

[HealthCheckRegistration(Tags = new[] { "ready" })]
public sealed class AccountsDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public AccountsDbHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Accounts database is accessible")
                : HealthCheckResult.Unhealthy("Accounts database is not accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Accounts database check failed", ex);
        }
    }
}
