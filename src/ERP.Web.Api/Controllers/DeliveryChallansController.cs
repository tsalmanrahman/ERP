using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.OrderToCash;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/sales/delivery-challans")]
[Authorize]
public class DeliveryChallansController : ControllerBase
{
    private readonly IDeliveryChallanService _deliveryChallanService;

    public DeliveryChallansController(IDeliveryChallanService deliveryChallanService)
    {
        _deliveryChallanService = deliveryChallanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliveryChallans([FromQuery] int? salesOrderId, CancellationToken cancellationToken)
    {
        var challans = await _deliveryChallanService.GetDeliveryChallansAsync(salesOrderId, cancellationToken);
        return Ok(new { success = true, data = challans });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDeliveryChallanById(int id, CancellationToken cancellationToken)
    {
        var challan = await _deliveryChallanService.GetDeliveryChallanByIdAsync(id, cancellationToken);
        if (challan == null)
        {
            return NotFound(new { success = false, message = $"Delivery challan with ID {id} was not found." });
        }
        return Ok(new { success = true, data = challan });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeliveryChallan([FromBody] CreateDeliveryChallanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var challan = await _deliveryChallanService.CreateDeliveryChallanAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetDeliveryChallanById), new { id = challan.Id }, new { success = true, data = challan });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/dispatch")]
    public async Task<IActionResult> DispatchAndPostCogs(int id, CancellationToken cancellationToken)
    {
        try
        {
            var challan = await _deliveryChallanService.DispatchAndPostCogsAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"Delivery challan {challan.ChallanNumber} successfully dispatched and COGS posted to General Ledger.", data = challan });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
