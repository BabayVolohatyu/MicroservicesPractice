using FinancialTracker.Accounts.Application.Contracts;
using FinancialTracker.Accounts.Domain;
using FinancialTracker.Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Accounts.Infrastructure.Repositories;

public sealed class FinanceAccountRepository : IFinanceAccountRepository
{
    private readonly AccountsDbContext _db;

    public FinanceAccountRepository(AccountsDbContext db)
    {
        _db = db;
    }

    public async Task<FinanceAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.FinanceAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FinanceAccount>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        await _db.FinanceAccounts.Where(a => a.OwnerId == ownerId).ToListAsync(cancellationToken);

    public async Task AddAsync(FinanceAccount account, CancellationToken cancellationToken = default)
    {
        _db.FinanceAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FinanceAccount account, CancellationToken cancellationToken = default)
    {
        _db.FinanceAccounts.Update(account);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
