using FinancialTracker.Transactions.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancialTracker.API.HealthChecks;

[HealthCheckRegistration(Tags = new[] { "ready" })]
public sealed class TransactionsDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public TransactionsDbHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Transactions database is accessible")
                : HealthCheckResult.Unhealthy("Transactions database is not accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Transactions database check failed", ex);
        }
    }
}
