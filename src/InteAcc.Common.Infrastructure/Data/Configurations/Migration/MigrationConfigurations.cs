using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.Migration;

namespace InteAcc.Common.Infrastructure.Data.Configurations.Migration;

public class MigrationLogConfiguration : IEntityTypeConfiguration<MigrationLog>
{
    public void Configure(EntityTypeBuilder<MigrationLog> builder)
    {
        builder.ToTable("MigrationLogs", "sa");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.BatchNumber).HasMaxLength(50).IsRequired();
        builder.Property(m => m.SourceModule).HasMaxLength(50).IsRequired();
        builder.Property(m => m.TargetEntity).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Status).HasMaxLength(20).IsRequired();
        builder.Property(m => m.RowVersion).IsRowVersion();
    }
}

public class CutoverAuditConfiguration : IEntityTypeConfiguration<CutoverAudit>
{
    public void Configure(EntityTypeBuilder<CutoverAudit> builder)
    {
        builder.ToTable("CutoverAudits", "sa");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SignedOffBy).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CfoApprovalCode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.TrialBalanceVariance).HasPrecision(18, 4);
        builder.Property(c => c.ArSubledgerVariance).HasPrecision(18, 4);
        builder.Property(c => c.ApSubledgerVariance).HasPrecision(18, 4);
        builder.Property(c => c.InventorySubledgerVariance).HasPrecision(18, 4);
        builder.Property(c => c.BankSubledgerVariance).HasPrecision(18, 4);
        builder.Property(c => c.CutoverStatus).HasMaxLength(30).IsRequired();
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class HistoricalLedgerArchiveConfiguration : IEntityTypeConfiguration<HistoricalLedgerArchive>
{
    public void Configure(EntityTypeBuilder<HistoricalLedgerArchive> builder)
    {
        builder.ToTable("HistoricalLedgerArchive", "gl");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.FiscalYear).HasMaxLength(10).IsRequired();
        builder.Property(h => h.LegacyVoucherNumber).HasMaxLength(50).IsRequired();
        builder.Property(h => h.AccountCode).HasMaxLength(30).IsRequired();
        builder.Property(h => h.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Debit).HasPrecision(18, 4);
        builder.Property(h => h.Credit).HasPrecision(18, 4);
        builder.Property(h => h.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(h => h.LegacySourceModule).HasMaxLength(20).IsRequired();
        builder.Property(h => h.RowVersion).IsRowVersion();
    }
}
