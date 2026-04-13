using FinancialTracker.Transactions.Application.Contracts;

namespace FinancialTracker.Transactions.Application.DTOs.Response;

public sealed record AddIncomeResult
{
    public bool Success { get; init; }
    public TransactionResponse? Data { get; init; }
    public SubtractBalanceFailureReason? FailureReason { get; init; }

    public static AddIncomeResult Succeeded(TransactionResponse data) => new()
    {
        Success = true,
        Data = data
    };

    public static AddIncomeResult Failed(SubtractBalanceFailureReason reason) => new()
    {
        Success = false,
        FailureReason = reason
    };
}
