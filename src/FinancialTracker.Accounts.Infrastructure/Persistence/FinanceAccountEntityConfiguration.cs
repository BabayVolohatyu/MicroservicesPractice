using FinancialTracker.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialTracker.Accounts.Infrastructure.Persistence;

internal sealed class FinanceAccountEntityConfiguration : IEntityTypeConfiguration<FinanceAccount>
{
    public void Configure(EntityTypeBuilder<FinanceAccount> builder)
    {
        builder.ToTable("FinanceAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Balance).HasPrecision(18, 2).IsRequired();
        builder.HasIndex(x => x.OwnerId);
    }
}
