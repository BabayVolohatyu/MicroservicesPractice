using FinancialTracker.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Accounts.Infrastructure.Persistence;

public sealed class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options) : base(options) { }

    public DbSet<FinanceAccount> FinanceAccounts => Set<FinanceAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);
    }
}
