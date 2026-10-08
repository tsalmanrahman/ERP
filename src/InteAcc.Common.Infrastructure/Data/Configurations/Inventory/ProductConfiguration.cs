using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.Inventory;

namespace InteAcc.Common.Infrastructure.Data.Configurations.Inventory;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "inv");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductCode).HasMaxLength(40).IsRequired();
        builder.Property(p => p.ProductName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.PackSize).HasMaxLength(50);

        builder.Property(p => p.StandardCost).HasPrecision(18, 4);
        builder.Property(p => p.CurrentCost).HasPrecision(18, 4);
        builder.Property(p => p.SellingPrice).HasPrecision(18, 4);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 4);

        builder.HasIndex(p => new { p.CompanyId, p.ProductCode }).IsUnique();

        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
