using FinancialTracker.Transactions.Domain;

namespace FinancialTracker.Transactions.Application.Contracts;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
