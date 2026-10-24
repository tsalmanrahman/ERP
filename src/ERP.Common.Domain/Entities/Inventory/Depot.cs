using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum DepotType
{
    ManufacturingPlant = 1,
    RegionalDepot = 2,
    CentralDistributionHub = 3
}

public class Depot : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string DepotCode { get; set; } = string.Empty;
    public string DepotName { get; set; } = string.Empty;
    public DepotType DepotType { get; set; } = DepotType.ManufacturingPlant;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Depot() { }
}
