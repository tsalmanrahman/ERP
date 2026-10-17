using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum StockAdjustmentType
{
    PhysicalCountVariance = 1,
    DamagedOrScrap = 2,
    OpeningBalance = 3,
    Expired = 4
}

public enum StockAdjustmentStatus
{
    Draft = 0,
    Approved = 1,
    Cancelled = 2
}

public class StockAdjustment : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public int WarehouseId { get; set; }
    public StockAdjustmentType AdjustmentType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public StockAdjustmentStatus Status { get; set; } = StockAdjustmentStatus.Draft;
    public int? JournalVoucherId { get; set; }

    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public StockAdjustment() { }
}

public class StockAdjustmentLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int AdjustmentId { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public decimal AdjustmentQuantity { get; set; } // Positive for gain, Negative for loss
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public StockAdjustmentLine() { }
}
