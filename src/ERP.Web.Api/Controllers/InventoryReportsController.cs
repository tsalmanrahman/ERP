using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Inventory;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/inv/reports")]
[Route("api/inv/[controller]")]
[Authorize]
public class InventoryReportsController : ControllerBase
{
    private readonly IInventoryReportingService _reportingService;

    public InventoryReportsController(IInventoryReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("stock-ledger")]
    public async Task<IActionResult> GetStockLedger(
        [FromQuery] int productId,
        [FromQuery] int? warehouseId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return BadRequest(new { success = false, message = "Valid productId is required." });
        }

        try
        {
            var report = await _reportingService.GetStockLedgerReportAsync(productId, warehouseId, fromDate, toDate, cancellationToken);
            return Ok(new { success = true, data = report });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("valuation")]
    public async Task<IActionResult> GetStockValuation(
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        var report = await _reportingService.GetStockValuationSummaryAsync(warehouseId, cancellationToken);
        return Ok(new { success = true, data = report });
    }
}
