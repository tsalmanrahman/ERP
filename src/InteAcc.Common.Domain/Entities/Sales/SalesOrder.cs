using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.Sales;

public enum SalesOrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Dispatched = 2,
    Invoiced = 3,
    Cancelled = 4
}

public class SalesOrder : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public int SalesCenterId { get; set; }
    public decimal TotalAmount { get; set; }
    public SalesOrderStatus OrderStatus { get; set; } = SalesOrderStatus.Draft;

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public SalesOrder() { }
}

public class SalesOrderLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int SalesOrderId { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public SalesOrderLine() { }
}
