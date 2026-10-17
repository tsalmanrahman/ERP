using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.GeneralLedger;

namespace ERP.Common.Infrastructure.Data.Configurations.GeneralLedger;

public class JournalVoucherConfiguration : IEntityTypeConfiguration<JournalVoucher>
{
    public void Configure(EntityTypeBuilder<JournalVoucher> builder)
    {
        builder.ToTable("JournalVouchers", "gl");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VoucherNumber).HasMaxLength(30).IsRequired();
        builder.Property(v => v.Reference).HasMaxLength(50);
        builder.Property(v => v.Description).HasMaxLength(255);
        builder.Property(v => v.PostedBy).HasMaxLength(50);

        builder.HasIndex(v => new { v.CompanyId, v.VoucherNumber }).IsUnique();

        builder.HasMany(v => v.Lines)
            .WithOne(l => l.Voucher)
            .HasForeignKey(l => l.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(v => v.RowVersion).IsRowVersion();
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}

public class JournalVoucherLineConfiguration : IEntityTypeConfiguration<JournalVoucherLine>
{
    public void Configure(EntityTypeBuilder<JournalVoucherLine> builder)
    {
        builder.ToTable("JournalVoucherLines", "gl");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Debit).HasPrecision(18, 4);
        builder.Property(l => l.Credit).HasPrecision(18, 4);
        builder.Property(l => l.BaseDebit).HasPrecision(18, 4);
        builder.Property(l => l.BaseCredit).HasPrecision(18, 4);
        builder.Property(l => l.ExchangeRate).HasPrecision(18, 6);
        builder.Property(l => l.CurrencyCode).HasMaxLength(10);
        builder.Property(l => l.Particulars).HasMaxLength(255);

        builder.HasIndex(l => new { l.VoucherId, l.LineNumber }).IsUnique();

        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
