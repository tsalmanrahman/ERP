using InteAcc.Common.Domain.Common;
using InteAcc.Common.Domain.Entities.Inventory;

namespace InteAcc.Common.Domain.Entities.Manufacturing;

public class BillOfMaterials : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string BomNumber { get; set; } = string.Empty;
    public int FinishedGoodProductId { get; set; }
    public Product? FinishedGoodProduct { get; set; }

    public decimal QuantityProduced { get; set; } = 1.0m;
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<BomComponent> Components { get; set; } = new List<BomComponent>();

    public BillOfMaterials() { }

    public decimal CalculateTotalDirectMaterialCost()
    {
        return Components.Sum(c => c.RequiredQuantity * (1m + (c.ScrapPercentage / 100m)) * c.UnitCost);
    }
}

public class BomComponent : Entity<int>
{
    public int BomId { get; set; }
    public BillOfMaterials? Bom { get; set; }

    public int RawMaterialProductId { get; set; }
    public Product? RawMaterialProduct { get; set; }

    public decimal RequiredQuantity { get; set; }
    public decimal ScrapPercentage { get; set; }
    public decimal UnitCost { get; set; }

    public BomComponent() { }
}
