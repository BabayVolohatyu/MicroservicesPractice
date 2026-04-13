using FinancialTracker.Transactions.Application.Contracts;
using FinancialTracker.Transactions.Application.DTOs.Request;
using FinancialTracker.Transactions.Application.DTOs.Response;
using FinancialTracker.Transactions.Domain;

namespace FinancialTracker.Transactions.Application.Services;

public sealed class TransactionsApplicationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountsReadClient _accountsRead;

    public TransactionsApplicationService(
        ITransactionRepository transactionRepository,
        IAccountsReadClient accountsRead)
    {
        _transactionRepository = transactionRepository;
        _accountsRead = accountsRead;
    }

    public async Task<AddIncomeResult> AddIncomeAsync(Guid userId, AddIncomeRequest request, CancellationToken cancellationToken = default)
    {
        var read = await _accountsRead.GetBalanceAsync(userId, request.AccountId, cancellationToken);
        if (read.Status == AccountsReadStatus.Unavailable)
            return AddIncomeResult.Failed(SubtractBalanceFailureReason.ServiceUnavailable);
        if (read.Status == AccountsReadStatus.NotFound)
            return AddIncomeResult.Failed(SubtractBalanceFailureReason.AccountNotFound);

        var transaction = Transaction.CreateIncome(request.AccountId, userId, request.Amount, request.Category, request.Note);
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        return AddIncomeResult.Succeeded(Map(transaction));
    }

    public async Task<AddExpenseResult> AddExpenseAsync(Guid userId, AddExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var read = await _accountsRead.GetBalanceAsync(userId, request.AccountId, cancellationToken);
        if (read.Status == AccountsReadStatus.Unavailable)
            return AddExpenseResult.Failed(SubtractBalanceFailureReason.ServiceUnavailable);
        if (read.Status == AccountsReadStatus.NotFound)
            return AddExpenseResult.Failed(SubtractBalanceFailureReason.AccountNotFound);
        if (read.Balance < request.Amount)
            return AddExpenseResult.Failed(SubtractBalanceFailureReason.InsufficientBalance);

        var transaction = Transaction.CreateExpense(request.AccountId, userId, request.Amount, request.Category, request.Note);
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
