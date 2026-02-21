namespace FinancialTracker.API.HealthChecks;

/// <summary>
/// Optional attribute for health checks. Use to set a custom name and/or tags.
/// If not applied, the check is registered with a name derived from the class name
/// (e.g. AuthDbHealthCheck -> "auth_db") and no tags.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class HealthCheckRegistrationAttribute : Attribute
{
    /// <summary>Explicit registration name (e.g. "auth_db"). If null, name is derived from class name.</summary>
    public string? Name { get; set; }

    /// <summary>Tags for filtering (e.g. "ready", "live"). Used by /health/ready and similar endpoints.</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}
