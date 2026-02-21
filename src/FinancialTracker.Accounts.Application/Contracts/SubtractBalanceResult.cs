namespace FinancialTracker.Accounts.Application.Contracts;

/// <summary>
/// Result of attempting to subtract balance (e.g. for an expense).
/// </summary>
public readonly record struct SubtractBalanceResult
{
    public bool Success { get; }
    public SubtractBalanceFailureReason FailureReason { get; }

    private SubtractBalanceResult(bool success, SubtractBalanceFailureReason failureReason)
    {
        Success = success;
        FailureReason = failureReason;
    }

    public static SubtractBalanceResult Succeeded() => new(true, SubtractBalanceFailureReason.None);
    public static SubtractBalanceResult AccountNotFound() => new(false, SubtractBalanceFailureReason.AccountNotFound);
    public static SubtractBalanceResult InsufficientBalance() => new(false, SubtractBalanceFailureReason.InsufficientBalance);
}

public enum SubtractBalanceFailureReason
{
    None,
    AccountNotFound,
    InsufficientBalance
}
