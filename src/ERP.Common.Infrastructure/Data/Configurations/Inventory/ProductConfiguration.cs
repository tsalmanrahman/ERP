using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.Inventory;

namespace ERP.Common.Infrastructure.Data.Configurations.Inventory;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "inv");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.BarcodeGtin).HasMaxLength(50);
        builder.Property(p => p.MaterialGrade).HasMaxLength(100);
        builder.Property(p => p.PackSize).HasMaxLength(50);
        builder.Property(p => p.TaxScheduleCode).HasMaxLength(30);
        builder.Property(p => p.DefaultStorageBin).HasMaxLength(50);
        builder.Property(p => p.QcPolicy).HasMaxLength(50);
        builder.Property(p => p.LifecycleStatus).HasMaxLength(20);
        builder.Property(p => p.HsCode).HasMaxLength(20);
        builder.Property(p => p.WeightUom).HasMaxLength(20);
        builder.Property(p => p.DimensionUom).HasMaxLength(20);
        builder.Property(p => p.CostingMethod).HasMaxLength(30);

        builder.Property(p => p.StandardCost).HasPrecision(18, 4);
        builder.Property(p => p.CurrentCost).HasPrecision(18, 4);
        builder.Property(p => p.SellingPrice).HasPrecision(18, 4);
        builder.Property(p => p.MinimumRetailPrice).HasPrecision(18, 4);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 4);
        builder.Property(p => p.SafetyStock).HasPrecision(18, 4);
        builder.Property(p => p.UomConversionFactor).HasPrecision(18, 6);
        builder.Property(p => p.NetWeight).HasPrecision(18, 4);
        builder.Property(p => p.GrossWeight).HasPrecision(18, 4);
        builder.Property(p => p.Length).HasPrecision(18, 4);
        builder.Property(p => p.Width).HasPrecision(18, 4);
        builder.Property(p => p.Height).HasPrecision(18, 4);
        builder.Property(p => p.VolumeCbm).HasPrecision(18, 4);
        builder.Property(p => p.TaxRatePercent).HasPrecision(5, 2);

        builder.HasIndex(p => new { p.CompanyId, p.ProductCode }).IsUnique();

        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
