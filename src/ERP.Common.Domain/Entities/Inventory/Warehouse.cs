using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum WarehouseType
{
    RawMaterial = 1,
    FinishedGoods = 2,
    WorkInProgress = 3,
    SparesAndHardware = 4,
    GeneralTransit = 5
}

public class Warehouse : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    // Depot relationship & Warehouse category
    public int? DepotId { get; set; }
    public WarehouseType WarehouseType { get; set; } = WarehouseType.FinishedGoods;

    // Navigation
    public Depot? Depot { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Warehouse() { }
}
