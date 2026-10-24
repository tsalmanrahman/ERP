using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum GoodsReceiptReturnType
{
    VendorReturn = 1,
    ProductionReturn = 2
}

public enum GoodsReceiptReturnStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2
}

public class GoodsReceiptReturn : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote? GoodsReceiptNote { get; set; }
    public GoodsReceiptReturnType ReturnType { get; set; } = GoodsReceiptReturnType.VendorReturn;
    public int WarehouseId { get; set; }
    public int? VendorId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? ProductionOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public GoodsReceiptReturnStatus Status { get; set; } = GoodsReceiptReturnStatus.Draft;
    public int? JournalVoucherId { get; set; }
    public string? Remarks { get; set; }

    public ICollection<GoodsReceiptReturnLine> Lines { get; set; } = new List<GoodsReceiptReturnLine>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public GoodsReceiptReturn() { }
}

public class GoodsReceiptReturnLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int ReturnId { get; set; }
    public int LineNumber { get; set; }
    public int GoodsReceiptNoteLineId { get; set; }
    public GoodsReceiptNoteLine? GoodsReceiptNoteLine { get; set; }
    public int ProductId { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ReturnReason { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public GoodsReceiptReturnLine() { }
}
