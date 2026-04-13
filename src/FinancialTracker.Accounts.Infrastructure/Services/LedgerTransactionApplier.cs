using FinancialTracker.Accounts.Application.Contracts;
using FinancialTracker.Accounts.Domain;
using FinancialTracker.Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Accounts.Infrastructure.Services;

public sealed class LedgerTransactionApplier : ILedgerTransactionApplier
{
    private readonly AccountsDbContext _db;

    public LedgerTransactionApplier(AccountsDbContext db)
    {
        _db = db;
    }

    public async Task<LedgerApplyResult> ApplyAsync(TransactionLedgerEvent ledgerEvent, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (await _db.ProcessedLedgerTransactions.AnyAsync(e => e.TransactionId == ledgerEvent.TransactionId, cancellationToken))
        {
            await tx.CommitAsync(cancellationToken);
            return LedgerApplyResult.AlreadyProcessed;
        }

        var account = await _db.FinanceAccounts.FirstOrDefaultAsync(a => a.Id == ledgerEvent.AccountId, cancellationToken);
        if (account is null || account.OwnerId != ledgerEvent.UserId)
        {
            await tx.RollbackAsync(cancellationToken);
            return LedgerApplyResult.AccountNotFound;
        }

        if (string.Equals(ledgerEvent.TransactionType, "Income", StringComparison.OrdinalIgnoreCase))
        {
            account.Credit(ledgerEvent.Amount);
        }
        else
        {
            try
            {
                account.Debit(ledgerEvent.Amount);
            }
            catch (InvalidOperationException)
            {
                await tx.RollbackAsync(cancellationToken);
                return LedgerApplyResult.InsufficientBalance;
            }
        }

        _db.ProcessedLedgerTransactions.Add(new ProcessedLedgerTransaction
        {
            TransactionId = ledgerEvent.TransactionId,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return LedgerApplyResult.Applied;
    }
}
