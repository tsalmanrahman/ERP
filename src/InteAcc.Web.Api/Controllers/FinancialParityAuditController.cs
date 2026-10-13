using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Migration;
using InteAcc.Common.Application.Models.Migration;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/audit")]
public class FinancialParityAuditController : ControllerBase
{
    private readonly IFinancialParityAuditService _auditService;

    public FinancialParityAuditController(IFinancialParityAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet("financial-parity")]
    public async Task<ActionResult<FinancialParityAuditReport>> RunParityAudit(
        [FromQuery] int companyId = 1,
        CancellationToken cancellationToken = default)
    {
        var report = await _auditService.RunFinancialParityAuditAsync(companyId, cancellationToken);
        return Ok(report);
    }
}
