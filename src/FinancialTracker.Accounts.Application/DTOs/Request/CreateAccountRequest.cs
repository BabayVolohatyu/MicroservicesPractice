namespace FinancialTracker.Accounts.Application.DTOs.Request;

public sealed class CreateAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "USD";
}
