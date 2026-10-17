using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.ProcureToPay;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/ap/reports")]
[Authorize]
public class AccountsPayableReportsController : ControllerBase
{
    private readonly IAccountsPayableReportingService _reportingService;

    public AccountsPayableReportsController(IAccountsPayableReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("aging")]
    public async Task<IActionResult> GetVendorAging([FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        var report = await _reportingService.GetVendorAgingReportAsync(asOfDate, cancellationToken);
        return Ok(new { success = true, data = report });
    }

    [HttpGet("statements/{vendorId:int}")]
    public async Task<IActionResult> GetVendorStatement(
        int vendorId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportingService.GetVendorStatementReportAsync(vendorId, fromDate, toDate, cancellationToken);
            return Ok(new { success = true, data = report });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
