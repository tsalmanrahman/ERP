using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.AccountsReceivable;

namespace ERP.Common.Infrastructure.Data.Configurations.AccountsReceivable;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "ar");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode).HasMaxLength(30).IsRequired();
        builder.Property(c => c.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.ContactPerson).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.AddressLine1).HasMaxLength(200);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.CountryCode).HasMaxLength(10);
        builder.Property(c => c.TaxId).HasMaxLength(50);

        builder.Property(c => c.CurrentBalance).HasPrecision(18, 4);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 4);

        builder.HasIndex(c => new { c.CompanyId, c.CustomerCode }).IsUnique();

        builder.Property(c => c.RowVersion).IsRowVersion();
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
