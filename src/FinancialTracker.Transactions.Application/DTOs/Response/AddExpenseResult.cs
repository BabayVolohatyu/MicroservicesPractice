using FinancialTracker.Accounts.Application.Contracts;

namespace FinancialTracker.Transactions.Application.DTOs.Response;

/// <summary>
/// Result of adding an expense: either success with transaction data or a specific failure reason.
/// </summary>
public sealed record AddExpenseResult
{
    public bool Success { get; init; }
    public TransactionResponse? Data { get; init; }
    public SubtractBalanceFailureReason? FailureReason { get; init; }

    public static AddExpenseResult Succeeded(TransactionResponse data) => new()
    {
        Success = true,
        Data = data
    };

    public static AddExpenseResult Failed(SubtractBalanceFailureReason reason) => new()
    {
        Success = false,
        FailureReason = reason
    };
}
