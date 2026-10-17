using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Domain.Entities.Procurement;

namespace ERP.Common.Infrastructure.Data.Configurations.AccountsPayable;

public class VendorGroupConfiguration : IEntityTypeConfiguration<VendorGroup>
{
    public void Configure(EntityTypeBuilder<VendorGroup> builder)
    {
        builder.ToTable("VendorGroups", "ap");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GroupCode).HasMaxLength(20).IsRequired();
        builder.Property(g => g.GroupName).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => new { g.CompanyId, g.GroupCode }).IsUnique();
        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", "purch");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.PoNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 4);
        builder.HasIndex(o => new { o.CompanyId, o.PoNumber }).IsUnique();

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.RowVersion).IsRowVersion();
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", "purch");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.OrderedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.PurchaseOrderId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class PurchaseBillConfiguration : IEntityTypeConfiguration<PurchaseBill>
{
    public void Configure(EntityTypeBuilder<PurchaseBill> builder)
    {
        builder.ToTable("PurchaseBills", "ap");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BillNumber).HasMaxLength(30).IsRequired();
        builder.Property(b => b.VendorInvoiceNumber).HasMaxLength(50);
        builder.Property(b => b.SubTotal).HasPrecision(18, 4);
        builder.Property(b => b.TaxAmount).HasPrecision(18, 4);
        builder.Property(b => b.TotalAmount).HasPrecision(18, 4);
        builder.Property(b => b.PaidAmount).HasPrecision(18, 4);
        builder.Property(b => b.BalanceAmount).HasPrecision(18, 4);
        builder.HasIndex(b => new { b.CompanyId, b.BillNumber }).IsUnique();

        builder.HasMany(b => b.Lines)
            .WithOne()
            .HasForeignKey(l => l.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class PurchaseBillLineConfiguration : IEntityTypeConfiguration<PurchaseBillLine>
{
    public void Configure(EntityTypeBuilder<PurchaseBillLine> builder)
    {
        builder.ToTable("PurchaseBillLines", "ap");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).HasMaxLength(255).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineAmount).HasPrecision(18, 4);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.BillId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class VendorPaymentConfiguration : IEntityTypeConfiguration<VendorPayment>
{
    public void Configure(EntityTypeBuilder<VendorPayment> builder)
    {
        builder.ToTable("VendorPayments", "ap");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PaymentNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ChequeNumber).HasMaxLength(50);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.Remarks).HasMaxLength(255);
        builder.HasIndex(p => new { p.CompanyId, p.PaymentNumber }).IsUnique();

        builder.HasMany(p => p.Allocations)
            .WithOne()
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class VendorPaymentAllocationConfiguration : IEntityTypeConfiguration<VendorPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<VendorPaymentAllocation> builder)
    {
        builder.ToTable("VendorPaymentAllocations", "ap");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AllocatedAmount).HasPrecision(18, 4);
        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}
