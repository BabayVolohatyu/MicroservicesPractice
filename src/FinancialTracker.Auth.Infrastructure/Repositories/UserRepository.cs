using FinancialTracker.Auth.Application.Contracts;
using FinancialTracker.Auth.Domain;
using FinancialTracker.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Auth.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;

    public UserRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
