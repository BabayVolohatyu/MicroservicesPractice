namespace FinancialTracker.Accounts.Application.Contracts;

public sealed record TransactionLedgerEvent(
    Guid TransactionId,
    Guid AccountId,
    Guid UserId,
    string TransactionType,
    decimal Amount);
