namespace FinancialTracker.Transactions.Application.DTOs.Response;

public sealed class TransactionResponse
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public string Type { get; init; } = string.Empty; // "Income" or "Expense"
    public decimal Amount { get; init; }
    public string? Category { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
