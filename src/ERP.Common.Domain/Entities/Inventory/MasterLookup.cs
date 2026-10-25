using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public class MasterLookup : Entity<int>, IIdentifiableWithGuid, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; } // 0 = Shared Group-Wide Standard, >0 = Subsidiary-Specific
    public string LookupType { get; set; } = string.Empty; // PRODUCT_SUBCATEGORY, MATERIAL_GRADE, PRODUCT_BRAND, PACKAGING_TYPE, TAX_SCHEDULE, QC_POLICY, STORAGE_BIN
    public string LookupCode { get; set; } = string.Empty;
    public string LookupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? NumericValue { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; } = 0;
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

    public MasterLookup() { }
}
