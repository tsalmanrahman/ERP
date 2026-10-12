using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Specialized;
using InteAcc.Common.Application.Models.Specialized;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/mfg")]
[Authorize]
public class ManufacturingController : ControllerBase
{
    private readonly IManufacturingService _manufacturingService;

    public ManufacturingController(IManufacturingService manufacturingService)
    {
        _manufacturingService = manufacturingService;
    }

    [HttpGet("boms")]
    public async Task<IActionResult> GetBoms([FromQuery] int? finishedGoodProductId, CancellationToken cancellationToken)
    {
        var list = await _manufacturingService.GetBomsAsync(finishedGoodProductId, cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpGet("boms/{id:int}")]
    public async Task<IActionResult> GetBomById(int id, CancellationToken cancellationToken)
    {
        var bom = await _manufacturingService.GetBomByIdAsync(id, cancellationToken);
        if (bom == null)
        {
            return NotFound(new { success = false, message = $"BOM with ID {id} was not found." });
        }
        return Ok(new { success = true, data = bom });
    }

    [HttpPost("boms")]
    public async Task<IActionResult> CreateBom([FromBody] CreateBomRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var bom = await _manufacturingService.CreateBomAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"BOM {bom.BomNumber} created successfully.", data = bom });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("production-orders")]
    public async Task<IActionResult> GetProductionOrders([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var list = await _manufacturingService.GetProductionOrdersAsync(status, cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpGet("production-orders/{id:int}")]
    public async Task<IActionResult> GetProductionOrderById(int id, CancellationToken cancellationToken)
    {
        var order = await _manufacturingService.GetProductionOrderByIdAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound(new { success = false, message = $"Production Order with ID {id} was not found." });
        }
        return Ok(new { success = true, data = order });
    }

    [HttpPost("production-orders")]
    public async Task<IActionResult> CreateProductionOrder([FromBody] CreateProductionOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _manufacturingService.CreateProductionOrderAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Production Order {order.OrderNumber} created successfully.", data = order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("production-orders/complete")]
    public async Task<IActionResult> CompleteProductionOrder([FromBody] CompleteProductionOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _manufacturingService.CompleteProductionOrderAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Production Order {order.OrderNumber} completed, stock updated, and posted to GL voucher {order.JournalVoucherNumber}.", data = order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
