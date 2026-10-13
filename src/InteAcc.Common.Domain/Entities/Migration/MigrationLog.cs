using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.Migration;

public class MigrationLog : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public int RecordsExtracted { get; set; }
    public int RecordsMigrated { get; set; }
    public int RecordsFailed { get; set; }
    public string Status { get; set; } = "Completed";
    public long DurationMs { get; set; }
    public string? Details { get; set; }

    // Audit and Soft Delete
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; } = "SYSTEM";
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
