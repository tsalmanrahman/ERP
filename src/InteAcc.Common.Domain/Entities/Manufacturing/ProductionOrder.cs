using InteAcc.Common.Domain.Common;
using InteAcc.Common.Domain.Entities.Inventory;

namespace InteAcc.Common.Domain.Entities.Manufacturing;

public class ProductionOrder : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int BomId { get; set; }
    public BillOfMaterials? Bom { get; set; }

    public int FinishedGoodProductId { get; set; }
    public Product? FinishedGoodProduct { get; set; }

    public decimal TargetQuantity { get; set; }
    public decimal CompletedQuantity { get; set; }
    public int RawMaterialWarehouseId { get; set; }
    public Warehouse? RawMaterialWarehouse { get; set; }
    public int FinishedGoodWarehouseId { get; set; }
    public Warehouse? FinishedGoodWarehouse { get; set; }

    public DateTime OrderDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed, Cancelled
    public int? JournalVoucherId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ProductionOrder() { }
}
