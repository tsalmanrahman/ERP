using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.Inventory;

public enum StockTransferStatus
{
    Draft = 0,
    InTransit = 1,
    Completed = 2,
    Cancelled = 3
}

public class StockTransfer : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;
    public string? Remarks { get; set; }

    public ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public StockTransfer() { }
}

public class StockTransferLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int TransferId { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public decimal TransferQuantity { get; set; }
    public decimal UnitCost { get; set; }
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

    public StockTransferLine() { }
}
