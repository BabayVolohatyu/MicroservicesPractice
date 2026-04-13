using System.Text.Json;
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
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "TransactionCreated",
            Payload = JsonSerializer.Serialize(new
            {
                transactionId = transaction.Id,
                accountId = transaction.AccountId,
                userId = transaction.UserId,
                transactionType = transaction.Type.ToString(),
                amount = transaction.Amount,
                category = transaction.Category,
                note = transaction.Note,
                occurredAtUtc = transaction.OccurredAtUtc
            }),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        _db.OutboxMessages.Add(outboxMessage);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}
