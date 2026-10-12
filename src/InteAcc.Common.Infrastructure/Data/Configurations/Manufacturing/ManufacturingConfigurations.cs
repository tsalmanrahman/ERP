using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.Manufacturing;

namespace InteAcc.Common.Infrastructure.Data.Configurations.Manufacturing;

public class BillOfMaterialsConfiguration : IEntityTypeConfiguration<BillOfMaterials>
{
    public void Configure(EntityTypeBuilder<BillOfMaterials> builder)
    {
        builder.ToTable("BillOfMaterials", "mfg");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BomNumber).HasMaxLength(30).IsRequired();
        builder.Property(b => b.QuantityProduced).HasPrecision(18, 4);
        builder.Property(b => b.Version).HasMaxLength(10).IsRequired();

        builder.HasOne(b => b.FinishedGoodProduct)
            .WithMany()
            .HasForeignKey(b => b.FinishedGoodProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Components)
            .WithOne(c => c.Bom)
            .HasForeignKey(c => c.BomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.CompanyId, b.BomNumber }).IsUnique();
        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class BomComponentConfiguration : IEntityTypeConfiguration<BomComponent>
{
    public void Configure(EntityTypeBuilder<BomComponent> builder)
    {
        builder.ToTable("BomComponents", "mfg");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.RequiredQuantity).HasPrecision(18, 4);
        builder.Property(c => c.ScrapPercentage).HasPrecision(5, 2);
        builder.Property(c => c.UnitCost).HasPrecision(18, 4);

        builder.HasOne(c => c.RawMaterialProduct)
            .WithMany()
            .HasForeignKey(c => c.RawMaterialProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable("ProductionOrders", "mfg");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.TargetQuantity).HasPrecision(18, 4);
        builder.Property(o => o.CompletedQuantity).HasPrecision(18, 4);
        builder.Property(o => o.TotalMaterialCost).HasPrecision(18, 4);
        builder.Property(o => o.Status).HasMaxLength(30).IsRequired();

        builder.HasOne(o => o.Bom)
            .WithMany()
            .HasForeignKey(o => o.BomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.FinishedGoodProduct)
            .WithMany()
            .HasForeignKey(o => o.FinishedGoodProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.RawMaterialWarehouse)
            .WithMany()
            .HasForeignKey(o => o.RawMaterialWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.FinishedGoodWarehouse)
            .WithMany()
            .HasForeignKey(o => o.FinishedGoodWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.CompanyId, o.OrderNumber }).IsUnique();
        builder.Property(o => o.RowVersion).IsRowVersion();
    }
}
