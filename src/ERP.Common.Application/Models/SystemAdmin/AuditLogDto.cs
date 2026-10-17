namespace ERP.Common.Application.Models.SystemAdmin;

public class AuditLogDto
{
    public long Id { get; set; }
    public Guid SysGuid { get; set; }
    public int CompanyId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntitySysGuid { get; set; }
    public string? RecordIdentifier { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AffectedColumns { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class RestoreEntityRequest
{
    public string EntityName { get; set; } = string.Empty;
    public Guid SysGuid { get; set; }
}

public class SoftDeleteEntityRequest
{
    public string EntityName { get; set; } = string.Empty;
    public Guid SysGuid { get; set; }
    public string? Reason { get; set; }
}
