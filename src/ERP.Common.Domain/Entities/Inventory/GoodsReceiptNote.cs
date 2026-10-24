using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum GrnType
{
    Vendor = 1,
    Production = 2
}

public enum GrnStatus
{
    Draft = 0,
    Received = 1,
    Cancelled = 2
}

public class GoodsReceiptNote : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public GrnType GrnType { get; set; } = GrnType.Vendor;
    public DateTime GrnDate { get; set; }
    public int WarehouseId { get; set; }
    public int? VendorId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? ProductionOrderId { get; set; }
    public string? ProductionReference { get; set; }
    public string? PoNumber { get; set; }
    public string? ChallanNumber { get; set; }
    public DateTime? ChallanDate { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? GateEntryNumber { get; set; }
    public string? QcStatus { get; set; } = "Passed";
    public decimal TotalAmount { get; set; }
    public GrnStatus Status { get; set; } = GrnStatus.Draft;
    public int? JournalVoucherId { get; set; }
    public string? Remarks { get; set; }

    public ICollection<GoodsReceiptNoteLine> Lines { get; set; } = new List<GoodsReceiptNoteLine>();
    public ICollection<GoodsReceiptReturn> Returns { get; set; } = new List<GoodsReceiptReturn>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public GoodsReceiptNote() { }
}

public class GoodsReceiptNoteLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int GrnId { get; set; }
    public int LineNumber { get; set; }
    public int? PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? StorageBinLocation { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remarks { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public GoodsReceiptNoteLine() { }
}
