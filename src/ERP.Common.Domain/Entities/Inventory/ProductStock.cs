using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public class ProductStock : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal AvailableQuantity { get; private set; } // Computed in SQL: QuantityOnHand - AllocatedQuantity
    public decimal AverageCost { get; set; }
    public decimal TotalValuation { get; private set; } // Computed in SQL: QuantityOnHand * AverageCost
    public DateTime? LastTransactionDate { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ProductStock() { }
}
