using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public enum ProductCategoryType
{
    FinishedGoods = 1,
    RawMaterials = 2,
    SemiFinishedGoods = 3,
    Consumables = 4,
    Service = 5,
    FixedAsset = 6
}

public class ProductClass : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public ProductCategoryType CategoryType { get; set; }
    public int GlInventoryAccountId { get; set; }
    public int GlCogsAccountId { get; set; }
    public int GlRevenueAccountId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ProductClass() { }
}
