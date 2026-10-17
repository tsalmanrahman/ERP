using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.SystemAdmin;

public class DocumentSequence : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string FiscalYear { get; set; } = "GLOBAL";
    public long CurrentNumber { get; set; }
    public int NumberLength { get; set; } = 4;
    public string? Suffix { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public DocumentSequence() { }
}
