using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancialTracker.API.HealthChecks;

[HealthCheckRegistration(Tags = new[] { "ready" })]
public sealed class JwtConfigHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public JwtConfigHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var secret = _configuration["JWT_SECRET"];
        var issuer = _configuration["JWT_ISSUER"];
        var audience = _configuration["JWT_AUDIENCE"];

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(secret)) missing.Add("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(issuer)) missing.Add("JWT_ISSUER");
        if (string.IsNullOrWhiteSpace(audience)) missing.Add("JWT_AUDIENCE");

        if (missing.Count != 0)
            return Task.FromResult(HealthCheckResult.Unhealthy($"JWT configuration missing: {string.Join(", ", missing)}"));

        return Task.FromResult(HealthCheckResult.Healthy("JWT configuration is present"));
    }
}
