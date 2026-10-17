using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.AccountsPayable;

namespace ERP.Common.Infrastructure.Data.Configurations.AccountsPayable;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors", "ap");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VendorCode).HasMaxLength(30).IsRequired();
        builder.Property(v => v.VendorName).HasMaxLength(150).IsRequired();
        builder.Property(v => v.ContactPerson).HasMaxLength(100);
        builder.Property(v => v.Phone).HasMaxLength(50);
        builder.Property(v => v.Email).HasMaxLength(100);
        builder.Property(v => v.AddressLine1).HasMaxLength(200);
        builder.Property(v => v.City).HasMaxLength(100);
        builder.Property(v => v.CountryCode).HasMaxLength(10);
        builder.Property(v => v.TaxId).HasMaxLength(50);

        builder.Property(v => v.CurrentBalance).HasPrecision(18, 4);
        builder.Property(v => v.CreditLimit).HasPrecision(18, 4);

        builder.HasIndex(v => new { v.CompanyId, v.VendorCode }).IsUnique();

        builder.Property(v => v.RowVersion).IsRowVersion();
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
