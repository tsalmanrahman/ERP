using ERP.Common.Application.Models.SystemAdmin;

namespace ERP.Common.Application.Abstractions.SystemAdmin;

public interface IAuditHistoryService
{
    Task<IReadOnlyList<AuditLogDto>> GetAuditHistoryByEntityGuidAsync(string entityName, Guid entitySysGuid, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> GetRecentAuditLogsAsync(int top = 50, CancellationToken cancellationToken = default);
    Task<bool> RestoreEntityAsync(string entityName, Guid entitySysGuid, CancellationToken cancellationToken = default);
}
