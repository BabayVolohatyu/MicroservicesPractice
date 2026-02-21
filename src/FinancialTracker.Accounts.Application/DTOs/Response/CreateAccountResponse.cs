namespace FinancialTracker.Accounts.Application.DTOs.Response;

public sealed class CreateAccountResponse
{
    public Guid AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
}
