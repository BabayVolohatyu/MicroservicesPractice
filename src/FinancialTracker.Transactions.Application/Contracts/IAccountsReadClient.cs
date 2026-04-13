namespace FinancialTracker.Transactions.Application.Contracts;

/// <summary>
/// Read-only access to Accounts (HTTP GET) for validation before persisting a transaction.
/// Balance changes are applied only via Kafka consumers in Accounts.
/// </summary>
public interface IAccountsReadClient
{
    Task<AccountsBalanceReadResult> GetBalanceAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default);
}

public enum AccountsReadStatus
{
    Ok,
    NotFound,
    Unavailable
}

public readonly record struct AccountsBalanceReadResult(AccountsReadStatus Status, decimal Balance);
