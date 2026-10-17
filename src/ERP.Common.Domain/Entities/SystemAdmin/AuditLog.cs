using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.SystemAdmin;

public class AuditLog : Entity<long>
{
    public int CompanyId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntitySysGuid { get; set; }
    public string? RecordIdentifier { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Restore
    public string? UserId { get; set; }
    public string UserName { get; set; } = "System";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AffectedColumns { get; set; }
    public string Source { get; set; } = "Web API";

    public AuditLog() { }
}
