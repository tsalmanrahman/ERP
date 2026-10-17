using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.SystemAdmin;
using ERP.Common.Application.Models.SystemAdmin;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/sa/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditHistoryService _auditHistoryService;

    public AuditController(IAuditHistoryService auditHistoryService)
    {
        _auditHistoryService = auditHistoryService;
    }

    [HttpGet("entity/{entityName}/{entitySysGuid:guid}")]
    public async Task<IActionResult> GetAuditHistory(string entityName, Guid entitySysGuid, CancellationToken cancellationToken)
    {
        var logs = await _auditHistoryService.GetAuditHistoryByEntityGuidAsync(entityName, entitySysGuid, cancellationToken);
        return Ok(new { success = true, data = logs });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int top = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _auditHistoryService.GetRecentAuditLogsAsync(top, cancellationToken);
        return Ok(new { success = true, data = logs });
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreEntity([FromBody] RestoreEntityRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName) || request.SysGuid == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "EntityName and SysGuid are required." });
        }

        var success = await _auditHistoryService.RestoreEntityAsync(request.EntityName, request.SysGuid, cancellationToken);
        if (!success)
        {
            return NotFound(new { success = false, message = $"Could not restore {request.EntityName} with GUID {request.SysGuid}. Entity not found or not deleted." });
        }

        return Ok(new { success = true, message = $"{request.EntityName} record successfully restored." });
    }
}
