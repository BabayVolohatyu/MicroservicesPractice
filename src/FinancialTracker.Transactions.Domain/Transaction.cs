namespace FinancialTracker.Transactions.Domain;

public enum TransactionType
{
    Income,
    Expense
}

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Category { get; private set; }
    public string? Note { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Transaction() { }

    public static Transaction CreateIncome(Guid accountId, Guid userId, decimal amount, string? category = null, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive");
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            Type = TransactionType.Income,
            Amount = amount,
            Category = category,
            Note = note,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static Transaction CreateExpense(Guid accountId, Guid userId, decimal amount, string? category = null, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive");
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            Type = TransactionType.Expense,
            Amount = amount,
            Category = category,
            Note = note,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
