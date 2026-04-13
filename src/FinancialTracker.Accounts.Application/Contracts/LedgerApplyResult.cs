namespace FinancialTracker.Accounts.Application.Contracts;

public enum LedgerApplyResult
{
    Applied,
    AlreadyProcessed,
    AccountNotFound,
    InsufficientBalance
}
