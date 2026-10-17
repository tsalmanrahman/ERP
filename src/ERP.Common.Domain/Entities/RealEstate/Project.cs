using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.RealEstate;

public class Project : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Status { get; set; } = "UnderConstruction"; // Planning, UnderConstruction, Completed

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<PropertyUnit> Units { get; set; } = new List<PropertyUnit>();

    public Project() { }
}
