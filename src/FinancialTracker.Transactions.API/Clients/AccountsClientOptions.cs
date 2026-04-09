namespace FinancialTracker.Transactions.API.Clients;

public sealed class AccountsClientOptions
{
    public const string SectionName = "AccountsService";

    public string BaseUrl { get; init; } = "http://localhost:5002";
    public int TimeoutSeconds { get; init; } = 2;
    public int RetryCount { get; init; } = 3;
    public int RetryBaseDelayMs { get; init; } = 200;
    public int CircuitBreakerFailureThreshold { get; init; } = 3;
    public int CircuitBreakerBreakSeconds { get; init; } = 15;
}
