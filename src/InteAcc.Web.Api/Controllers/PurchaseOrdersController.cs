using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Models.ProcureToPay;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/po/orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPurchaseOrders([FromQuery] int? vendorId, CancellationToken cancellationToken)
    {
        var orders = await _purchaseOrderService.GetPurchaseOrdersAsync(vendorId, cancellationToken);
        return Ok(new { success = true, data = orders });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPurchaseOrderById(int id, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound(new { success = false, message = $"Purchase order with ID {id} was not found." });
        }
        return Ok(new { success = true, data = order });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderService.CreatePurchaseOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPurchaseOrderById), new { id = order.Id }, new { success = true, data = order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> IssuePurchaseOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _purchaseOrderService.IssuePurchaseOrderAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"Purchase order {order.PoNumber} successfully issued.", data = order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
