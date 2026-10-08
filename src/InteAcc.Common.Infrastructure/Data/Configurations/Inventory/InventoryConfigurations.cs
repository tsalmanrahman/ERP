using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.Inventory;

namespace InteAcc.Common.Infrastructure.Data.Configurations.Inventory;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitsOfMeasure", "inv");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UomCode).HasMaxLength(10).IsRequired();
        builder.Property(u => u.UomName).HasMaxLength(50).IsRequired();
        builder.HasIndex(u => new { u.CompanyId, u.UomCode }).IsUnique();
        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}

public class ProductClassConfiguration : IEntityTypeConfiguration<ProductClass>
{
    public void Configure(EntityTypeBuilder<ProductClass> builder)
    {
        builder.ToTable("ProductClasses", "inv");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ClassCode).HasMaxLength(20).IsRequired();
        builder.Property(c => c.ClassName).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => new { c.CompanyId, c.ClassCode }).IsUnique();
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", "inv");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.WarehouseCode).HasMaxLength(20).IsRequired();
        builder.Property(w => w.WarehouseName).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Location).HasMaxLength(200);
        builder.HasIndex(w => new { w.CompanyId, w.WarehouseCode }).IsUnique();
        builder.Property(w => w.RowVersion).IsRowVersion();
    }
}

public class ProductStockConfiguration : IEntityTypeConfiguration<ProductStock>
{
    public void Configure(EntityTypeBuilder<ProductStock> builder)
    {
        builder.ToTable("ProductStock", "inv");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.QuantityOnHand).HasPrecision(18, 4);
        builder.Property(s => s.AllocatedQuantity).HasPrecision(18, 4);
        builder.Property(s => s.AverageCost).HasPrecision(18, 4);
        
        builder.Property(s => s.AvailableQuantity)
            .HasPrecision(18, 4)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(s => s.TotalValuation)
            .HasPrecision(18, 4)
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(s => new { s.CompanyId, s.WarehouseId, s.ProductId }).IsUnique();
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class StockLedgerConfiguration : IEntityTypeConfiguration<StockLedger>
{
    public void Configure(EntityTypeBuilder<StockLedger> builder)
    {
        builder.ToTable("StockLedgers", "inv");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ReferenceNumber).HasMaxLength(50).IsRequired();
        builder.Property(l => l.QuantityIn).HasPrecision(18, 4);
        builder.Property(l => l.QuantityOut).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.TotalCost).HasPrecision(18, 4);
        builder.Property(l => l.BalanceQuantity).HasPrecision(18, 4);
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class GoodsReceiptNoteConfiguration : IEntityTypeConfiguration<GoodsReceiptNote>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNote> builder)
    {
        builder.ToTable("GoodsReceiptNotes", "inv");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GrnNumber).HasMaxLength(30).IsRequired();
        builder.Property(g => g.PoNumber).HasMaxLength(50);
        builder.Property(g => g.ChallanNumber).HasMaxLength(50);
        builder.Property(g => g.TotalAmount).HasPrecision(18, 4);
        builder.Property(g => g.Remarks).HasMaxLength(500);
        builder.HasIndex(g => new { g.CompanyId, g.GrnNumber }).IsUnique();

        builder.HasMany(g => g.Lines)
            .WithOne()
            .HasForeignKey(l => l.GrnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}

public class GoodsReceiptNoteLineConfiguration : IEntityTypeConfiguration<GoodsReceiptNoteLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNoteLine> builder)
    {
        builder.ToTable("GoodsReceiptNoteLines", "inv");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.QuantityReceived).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.BatchNumber).HasMaxLength(50);
        builder.Property(l => l.Remarks).HasMaxLength(250);
        builder.HasIndex(l => new { l.GrnId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", "inv");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AdjustmentNumber).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(255).IsRequired();
        builder.Property(a => a.TotalAmount).HasPrecision(18, 4);
        builder.HasIndex(a => new { a.CompanyId, a.AdjustmentNumber }).IsUnique();

        builder.HasMany(a => a.Lines)
            .WithOne()
            .HasForeignKey(l => l.AdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLine> builder)
    {
        builder.ToTable("StockAdjustmentLines", "inv");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.AdjustmentQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.Notes).HasMaxLength(250);
        builder.HasIndex(l => new { l.AdjustmentId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers", "inv");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TransferNumber).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Remarks).HasMaxLength(250);
        builder.HasIndex(t => new { t.CompanyId, t.TransferNumber }).IsUnique();

        builder.HasMany(t => t.Lines)
            .WithOne()
            .HasForeignKey(l => l.TransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}

public class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("StockTransferLines", "inv");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.TransferQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.TransferId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
