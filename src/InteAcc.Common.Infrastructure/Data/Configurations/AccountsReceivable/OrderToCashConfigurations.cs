using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.AccountsReceivable;
using InteAcc.Common.Domain.Entities.Sales;

namespace InteAcc.Common.Infrastructure.Data.Configurations.AccountsReceivable;

public class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("CustomerGroups", "ar");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GroupCode).HasMaxLength(20).IsRequired();
        builder.Property(g => g.GroupName).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => new { g.CompanyId, g.GroupCode }).IsUnique();
        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", "ar");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.SubTotal).HasPrecision(18, 4);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 4);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 4);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 4);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 4);
        builder.Property(i => i.BalanceAmount).HasPrecision(18, 4);
        builder.HasIndex(i => new { i.CompanyId, i.InvoiceNumber }).IsUnique();

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.RowVersion).IsRowVersion();
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines", "ar");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).HasMaxLength(255).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineAmount).HasPrecision(18, 4);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.InvoiceId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class CustomerReceiptConfiguration : IEntityTypeConfiguration<CustomerReceipt>
{
    public void Configure(EntityTypeBuilder<CustomerReceipt> builder)
    {
        builder.ToTable("CustomerReceipts", "ar");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReceiptNumber).HasMaxLength(30).IsRequired();
        builder.Property(r => r.ChequeNumber).HasMaxLength(50);
        builder.Property(r => r.Reference).HasMaxLength(100);
        builder.Property(r => r.Remarks).HasMaxLength(255);
        builder.Property(r => r.Amount).HasPrecision(18, 4);
        builder.HasIndex(r => new { r.CompanyId, r.ReceiptNumber }).IsUnique();

        builder.HasMany(r => r.Allocations)
            .WithOne()
            .HasForeignKey(a => a.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

public class CustomerReceiptAllocationConfiguration : IEntityTypeConfiguration<CustomerReceiptAllocation>
{
    public void Configure(EntityTypeBuilder<CustomerReceiptAllocation> builder)
    {
        builder.ToTable("CustomerReceiptAllocations", "ar");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AllocatedAmount).HasPrecision(18, 4);
        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class SalesCenterConfiguration : IEntityTypeConfiguration<SalesCenter>
{
    public void Configure(EntityTypeBuilder<SalesCenter> builder)
    {
        builder.ToTable("SalesCenters", "sales");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CenterCode).HasMaxLength(20).IsRequired();
        builder.Property(c => c.CenterName).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => new { c.CompanyId, c.CenterCode }).IsUnique();
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", "sales");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 4);
        builder.HasIndex(o => new { o.CompanyId, o.OrderNumber }).IsUnique();

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.RowVersion).IsRowVersion();
    }
}

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines", "sales");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.OrderedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.ShippedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.SalesOrderId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class DeliveryChallanConfiguration : IEntityTypeConfiguration<DeliveryChallan>
{
    public void Configure(EntityTypeBuilder<DeliveryChallan> builder)
    {
        builder.ToTable("DeliveryChallans", "sales");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.ChallanNumber).HasMaxLength(30).IsRequired();
        builder.Property(d => d.TotalCogsAmount).HasPrecision(18, 4);
        builder.Property(d => d.DriverName).HasMaxLength(100);
        builder.Property(d => d.VehicleNumber).HasMaxLength(50);
        builder.Property(d => d.Remarks).HasMaxLength(255);
        builder.HasIndex(d => new { d.CompanyId, d.ChallanNumber }).IsUnique();

        builder.HasMany(d => d.Lines)
            .WithOne()
            .HasForeignKey(l => l.DeliveryChallanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class DeliveryChallanLineConfiguration : IEntityTypeConfiguration<DeliveryChallanLine>
{
    public void Configure(EntityTypeBuilder<DeliveryChallanLine> builder)
    {
        builder.ToTable("DeliveryChallanLines", "sales");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.DispatchedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineCogsAmount).HasPrecision(18, 4);
        builder.Property(l => l.BatchNumber).HasMaxLength(50);
        builder.HasIndex(l => new { l.DeliveryChallanId, l.LineNumber }).IsUnique();
        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
