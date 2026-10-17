using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.OrderToCash;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/ar/reports")]
[Authorize]
public class AccountsReceivableReportsController : ControllerBase
{
    private readonly IAccountsReceivableReportingService _reportingService;

    public AccountsReceivableReportsController(IAccountsReceivableReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("aging")]
    public async Task<IActionResult> GetCustomerAging([FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        var report = await _reportingService.GetCustomerAgingReportAsync(asOfDate, cancellationToken);
        return Ok(new { success = true, data = report });
    }

    [HttpGet("statements/{customerId:int}")]
    public async Task<IActionResult> GetCustomerStatement(
        int customerId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportingService.GetCustomerStatementReportAsync(customerId, fromDate, toDate, cancellationToken);
            return Ok(new { success = true, data = report });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
