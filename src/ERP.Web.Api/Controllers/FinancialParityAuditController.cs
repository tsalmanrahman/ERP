using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Migration;
using ERP.Common.Application.Models.Migration;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class FinancialParityAuditController : ControllerBase
{
    private readonly IFinancialParityAuditService _auditService;
    private readonly ICurrentUserContext _currentUserContext;

    public FinancialParityAuditController(IFinancialParityAuditService auditService, ICurrentUserContext currentUserContext)
    {
        _auditService = auditService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet("financial-parity")]
    public async Task<ActionResult<FinancialParityAuditReport>> RunParityAudit(
        [FromQuery] int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var targetCompanyId = companyId ?? _currentUserContext.GetRequiredCompanyId();
        if (!_currentUserContext.HasAccessToCompany(targetCompanyId))
        {
            return Forbid();
        }

        var report = await _auditService.RunFinancialParityAuditAsync(targetCompanyId, cancellationToken);
        return Ok(report);
    }
}
