namespace FinancialTracker.Transactions.Application.Contracts;

public interface IAccountBalanceGateway
{
    Task<bool> AddBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default);
    Task<SubtractBalanceResult> SubtractBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default);
}

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
    public static SubtractBalanceResult ServiceUnavailable() => new(false, SubtractBalanceFailureReason.ServiceUnavailable);
}

public enum SubtractBalanceFailureReason
{
    None,
    AccountNotFound,
    InsufficientBalance,
    ServiceUnavailable
}
