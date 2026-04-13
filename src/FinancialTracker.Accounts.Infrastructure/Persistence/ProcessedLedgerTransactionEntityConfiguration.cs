using FinancialTracker.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialTracker.Accounts.Infrastructure.Persistence;

internal sealed class ProcessedLedgerTransactionEntityConfiguration : IEntityTypeConfiguration<ProcessedLedgerTransaction>
{
    public void Configure(EntityTypeBuilder<ProcessedLedgerTransaction> builder)
    {
        builder.ToTable("ProcessedLedgerTransactions");
        builder.HasKey(e => e.TransactionId);
    }
}
