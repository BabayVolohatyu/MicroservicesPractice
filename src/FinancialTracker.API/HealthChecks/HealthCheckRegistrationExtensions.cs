using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancialTracker.API.HealthChecks;

/// <summary>
/// Registers all <see cref="IHealthCheck"/> implementations from an assembly by convention.
/// New features only need to add a class implementing IHealthCheck (and optionally
/// <see cref="HealthCheckRegistrationAttribute"/>) — no need to touch Program.cs.
/// </summary>
public static class HealthCheckRegistrationExtensions
{
    private static readonly Regex PascalToSnake = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);

    /// <summary>
    /// Discovers and registers every concrete <see cref="IHealthCheck"/> in the given assembly.
    /// Name: from <see cref="HealthCheckRegistrationAttribute.Name"/> or derived from class name
    /// (e.g. AuthDbHealthCheck -> "auth_db"). Tags: from attribute or empty.
    /// </summary>
    public static IHealthChecksBuilder AddAllHealthChecksFromAssembly(this IHealthChecksBuilder builder, Assembly assembly)
    {
        var checkTypes = assembly.GetTypes()
            .Where(t => typeof(IHealthCheck).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        var addCheckMethod = typeof(HealthCheckRegistrationExtensions).GetMethod(nameof(AddCheckInternal), BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var type in checkTypes)
        {
            var attr = type.GetCustomAttribute<HealthCheckRegistrationAttribute>();
            var name = attr?.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                name = DeriveName(type.Name);
            var tags = attr?.Tags ?? Array.Empty<string>();

            var generic = addCheckMethod.MakeGenericMethod(type);
            generic.Invoke(null, new object?[] { builder, name, tags });
        }

        return builder;
    }

    private static IHealthChecksBuilder AddCheckInternal<T>(IHealthChecksBuilder builder, string name, string[] tags)
        where T : class, IHealthCheck
    {
        return builder.AddCheck<T>(name, failureStatus: null, tags);
    }

    /// <summary>
    /// Registers a single health check by type with convention-based name and optional tags.
    /// Use this from module extensions when a module adds its own health checks.
    /// </summary>
    public static IHealthChecksBuilder AddHealthCheck<T>(this IHealthChecksBuilder builder, string? name = null, string[]? tags = null)
        where T : class, IHealthCheck
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<HealthCheckRegistrationAttribute>();
        var resolvedName = name ?? attr?.Name?.Trim();
        if (string.IsNullOrEmpty(resolvedName))
            resolvedName = DeriveName(type.Name);
        var resolvedTags = tags ?? attr?.Tags ?? Array.Empty<string>();

        return builder.AddCheck<T>(resolvedName, failureStatus: null, resolvedTags);
    }

    /// <summary>
    /// Converts PascalCase to snake_case and strips a trailing "HealthCheck" suffix.
    /// E.g. AuthDbHealthCheck -> auth_db.
    /// </summary>
    public static string DeriveName(string className)
    {
        var baseName = className.EndsWith("HealthCheck", StringComparison.OrdinalIgnoreCase)
            ? className[..^"HealthCheck".Length]
            : className;
        if (baseName.Length == 0) baseName = className;
        return PascalToSnake.Replace(baseName, "$1_$2").ToLowerInvariant();
    }
}
