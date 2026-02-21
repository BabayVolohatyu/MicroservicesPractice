using FinancialTracker.Auth.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancialTracker.API.HealthChecks;

[HealthCheckRegistration(Tags = new[] { "ready" })]
public sealed class AuthDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public AuthDbHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Auth database is accessible")
                : HealthCheckResult.Unhealthy("Auth database is not accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Auth database check failed", ex);
        }
    }
}
