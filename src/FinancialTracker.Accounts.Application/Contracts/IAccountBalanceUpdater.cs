namespace FinancialTracker.Accounts.Application.Contracts;

/// <summary>
/// Used by Transactions module to update account balance. Keeps module boundary: no direct repo access from Transactions.
/// </summary>
public interface IAccountBalanceUpdater
{
    Task<bool> AddBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default);
    Task<SubtractBalanceResult> SubtractBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default);
}
