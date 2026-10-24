using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum StockTransactionType
{
    GoodsReceipt = 1,
    GoodsIssue = 2,
    TransferIn = 3,
    TransferOut = 4,
    Adjustment = 5,
    ProductionReceipt = 6,
    AdjustmentOut = 7
}

public class StockLedger : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public DateTime TransactionDate { get; set; }
    public StockTransactionType TransactionType { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal BalanceQuantity { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public StockLedger() { }
}
