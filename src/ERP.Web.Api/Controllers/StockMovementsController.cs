using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Inventory;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/inv/[controller]")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _stockMovementService;

    public StockMovementsController(IStockMovementService stockMovementService)
    {
        _stockMovementService = stockMovementService;
    }

    // --- Adjustments ---

    [HttpGet("adjustments")]
    public async Task<IActionResult> GetAdjustments([FromQuery] int? warehouseId, CancellationToken cancellationToken)
    {
        var adjustments = await _stockMovementService.GetStockAdjustmentsAsync(warehouseId, cancellationToken);
        return Ok(new { success = true, data = adjustments });
    }

    [HttpGet("adjustments/{id:int}")]
    public async Task<IActionResult> GetAdjustmentById(int id, CancellationToken cancellationToken)
    {
        var adjustment = await _stockMovementService.GetStockAdjustmentByIdAsync(id, cancellationToken);
        if (adjustment == null)
        {
            return NotFound(new { success = false, message = $"Stock adjustment with ID {id} was not found." });
        }
        return Ok(new { success = true, data = adjustment });
    }

    [HttpPost("adjustments")]
    public async Task<IActionResult> ProcessStockAdjustment([FromBody] CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockMovementService.ProcessStockAdjustmentAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Stock adjustment {result.AdjustmentNumber} successfully processed and posted to General Ledger.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // --- Transfers ---

    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers(CancellationToken cancellationToken)
    {
        var transfers = await _stockMovementService.GetStockTransfersAsync(cancellationToken);
        return Ok(new { success = true, data = transfers });
    }

    [HttpGet("transfers/{id:int}")]
    public async Task<IActionResult> GetTransferById(int id, CancellationToken cancellationToken)
    {
        var transfer = await _stockMovementService.GetStockTransferByIdAsync(id, cancellationToken);
        if (transfer == null)
        {
            return NotFound(new { success = false, message = $"Stock transfer with ID {id} was not found." });
        }
        return Ok(new { success = true, data = transfer });
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> ProcessStockTransfer([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockMovementService.ProcessStockTransferAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Stock transfer {result.TransferNumber} successfully completed.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
