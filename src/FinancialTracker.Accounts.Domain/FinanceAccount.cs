namespace FinancialTracker.Accounts.Domain;

public sealed class FinanceAccount
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "USD";
    public decimal Balance { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private FinanceAccount() { }

    public static FinanceAccount Create(Guid ownerId, string name, string currency = "USD")
    {
        return new FinanceAccount
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Currency = currency,
            Balance = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive");
        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive");
        if (Balance < amount) throw new InvalidOperationException("Insufficient balance");
        Balance -= amount;
    }
}
