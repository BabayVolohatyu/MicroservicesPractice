namespace FinancialTracker.Accounts.Application.Contracts;

public interface ILedgerTransactionApplier
{
    Task<LedgerApplyResult> ApplyAsync(TransactionLedgerEvent ledgerEvent, CancellationToken cancellationToken = default);
}
