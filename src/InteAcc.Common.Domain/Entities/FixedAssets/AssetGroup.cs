using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.FixedAssets;

public class AssetGroup : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal DepreciationRate { get; set; }

    public int GlAssetAccountId { get; set; }
    public int GlAccumDeprAccountId { get; set; }
    public int GlDeprExpenseAccountId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public AssetGroup() { }
}
