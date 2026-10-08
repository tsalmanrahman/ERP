using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Finance;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/gl/reports")]
[Authorize]
public class FinancialReportsController : ControllerBase
{
    private readonly IFinancialReportingService _financialReportingService;

    public FinancialReportsController(IFinancialReportingService financialReportingService)
    {
        _financialReportingService = financialReportingService;
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        var result = await _financialReportingService.GetTrialBalanceAsync(asOfDate, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("account-statement")]
    public async Task<IActionResult> GetAccountStatement(
        [FromQuery] int accountId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var result = await _financialReportingService.GetAccountStatementAsync(accountId, fromDate, toDate, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }
}
