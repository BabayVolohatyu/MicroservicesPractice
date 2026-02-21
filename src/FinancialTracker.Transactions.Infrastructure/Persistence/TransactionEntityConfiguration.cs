using FinancialTracker.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialTracker.Transactions.Infrastructure.Persistence;

internal sealed class TransactionEntityConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.UserId);
    }
}
