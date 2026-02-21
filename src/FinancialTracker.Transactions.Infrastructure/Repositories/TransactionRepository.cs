using FinancialTracker.Transactions.Application.Contracts;
using FinancialTracker.Transactions.Domain;
using FinancialTracker.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Transactions.Infrastructure.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly TransactionsDbContext _db;

    public TransactionRepository(TransactionsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}
