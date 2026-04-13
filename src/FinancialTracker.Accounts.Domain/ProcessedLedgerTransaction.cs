namespace FinancialTracker.Accounts.Domain;

/// <summary>
/// Idempotency record: each transaction id from the Transactions service is applied at most once.
/// </summary>
public sealed class ProcessedLedgerTransaction
{
    public Guid TransactionId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
