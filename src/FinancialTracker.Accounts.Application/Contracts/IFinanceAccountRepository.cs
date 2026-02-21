using FinancialTracker.Accounts.Domain;

namespace FinancialTracker.Accounts.Application.Contracts;

public interface IFinanceAccountRepository
{
    Task<FinanceAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinanceAccount>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(FinanceAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(FinanceAccount account, CancellationToken cancellationToken = default);
}
