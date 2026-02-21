namespace FinancialTracker.Transactions.Application.DTOs.Request;

public sealed class AddExpenseRequest
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string? Category { get; init; }
    public string? Note { get; init; }
}
