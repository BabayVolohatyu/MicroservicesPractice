using FinancialTracker.Transactions.Application.Contracts;
using FinancialTracker.Transactions.Application.DTOs.Request;
using FinancialTracker.Transactions.Application.DTOs.Response;
using FinancialTracker.Transactions.Domain;

namespace FinancialTracker.Transactions.Application.Services;

public sealed class TransactionsApplicationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountBalanceGateway _balanceGateway;

    public TransactionsApplicationService(
        ITransactionRepository transactionRepository,
        IAccountBalanceGateway balanceGateway)
    {
        _transactionRepository = transactionRepository;
        _balanceGateway = balanceGateway;
    }

    public async Task<TransactionResponse?> AddIncomeAsync(Guid userId, AddIncomeRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = Transaction.CreateIncome(request.AccountId, userId, request.Amount, request.Category, request.Note);
        var updated = await _balanceGateway.AddBalanceAsync(request.AccountId, userId, request.Amount, cancellationToken);
        if (!updated)
            return null;
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        return Map(transaction);
    }

    public async Task<AddExpenseResult> AddExpenseAsync(Guid userId, AddExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = Transaction.CreateExpense(request.AccountId, userId, request.Amount, request.Category, request.Note);
        var subtractResult = await _balanceGateway.SubtractBalanceAsync(request.AccountId, userId, request.Amount, cancellationToken);
        if (!subtractResult.Success)
            return AddExpenseResult.Failed(subtractResult.FailureReason);
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        return AddExpenseResult.Succeeded(Map(transaction));
    }

    private static TransactionResponse Map(Transaction t) => new()
    {
        TransactionId = t.Id,
        AccountId = t.AccountId,
        Type = t.Type.ToString(),
        Amount = t.Amount,
        Category = t.Category,
        OccurredAtUtc = t.OccurredAtUtc
    };
}
