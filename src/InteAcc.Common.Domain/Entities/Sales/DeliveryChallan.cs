using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.Sales;

public enum DeliveryChallanStatus
{
    Draft = 0,
    Dispatched = 1,
    Invoiced = 2,
    Cancelled = 3
}

public class DeliveryChallan : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string ChallanNumber { get; set; } = string.Empty;
    public DateTime ChallanDate { get; set; }
    public int SalesOrderId { get; set; }
    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public decimal TotalCogsAmount { get; set; }
    public DeliveryChallanStatus Status { get; set; } = DeliveryChallanStatus.Draft;
    public int? JournalVoucherId { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Remarks { get; set; }

    public ICollection<DeliveryChallanLine> Lines { get; set; } = new List<DeliveryChallanLine>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DeliveryChallan() { }
}

public class DeliveryChallanLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int DeliveryChallanId { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCogsAmount { get; set; }
    public string? BatchNumber { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DeliveryChallanLine() { }
}
