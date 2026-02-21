namespace FinancialTracker.Accounts.Application.DTOs.Response;

public sealed class BalanceResponse
{
    public Guid AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Currency { get; init; } = string.Empty;
}
