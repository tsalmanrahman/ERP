using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.SystemAdmin;

namespace InteAcc.Common.Infrastructure.Data.Configurations.SystemAdmin;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", "sa");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.CompanyCode)
            .IsUnique();

        builder.Property(c => c.CompanyName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.AddressLine1).HasMaxLength(200);
        builder.Property(c => c.AddressLine2).HasMaxLength(200);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.CountryCode).HasMaxLength(10);
        builder.Property(c => c.Phone1).HasMaxLength(50);
        builder.Property(c => c.Phone2).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.WebAddress).HasMaxLength(150);
        builder.Property(c => c.BaseCurrency).HasMaxLength(10);

        builder.Property(c => c.CreatedBy).HasMaxLength(50);
        builder.Property(c => c.ModifiedBy).HasMaxLength(50);
        builder.Property(c => c.DeletedBy).HasMaxLength(50);

        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
