namespace FinancialTracker.Transactions.Application.Contracts;

public enum SubtractBalanceFailureReason
{
    None,
    AccountNotFound,
    InsufficientBalance,
    ServiceUnavailable
}
